﻿using Picturepark.SDK.V1.ServiceProvider.Buffer;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Net.Security;
using System.Security.Authentication;

namespace Picturepark.SDK.V1.ServiceProvider
{
    public class ServiceProviderClient : IDisposable
    {
        private readonly Configuration _configuration;

        private readonly IConnection _connection;
        private readonly IModel _liveStreamModel;
        private readonly IModel _requestMessageModel;
        private readonly LiveStreamBuffer _liveStreamBuffer;

        private LiveStreamConsumer _liveStreamConsumer;

        public ServiceProviderClient(Configuration configuration)
        {
            _configuration = configuration;

            ConnectionFactory factory = new ConnectionFactory();

            factory.HostName = configuration.Host;
            factory.Port = int.Parse(configuration.Port);
            factory.UserName = configuration.User;
            factory.Password = configuration.Password;
            factory.AutomaticRecoveryEnabled = true;
            factory.VirtualHost = configuration.ServiceProviderId;
            factory.NetworkRecoveryInterval = TimeSpan.FromSeconds(10);

            if (_configuration.UseSsl)
            {
                factory.Ssl = new SslOption()
                {
                    Version = SslProtocols.Tls12,
                    Enabled = true,
                    ServerName = factory.HostName,
                    AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors
                };
            }

            _connection = factory.CreateConnection();
            _liveStreamModel = _connection.CreateModel();
            _requestMessageModel = _connection.CreateModel();

            _liveStreamBuffer = new LiveStreamBuffer(new LiveStreamBufferPriorityQueue());
            _liveStreamBuffer.Start();
        }

        public void Dispose()
        {
            ////_liveStreamConsumer.Stop();
            _liveStreamModel.Close();
            _requestMessageModel.Close();
            _liveStreamBuffer.Stop();
            _connection.Close();
        }

        public IObservable<EventPattern<EventArgsLiveStreamMessage>> GetLiveStreamObserver(int bufferSize = 0, int delayMilliseconds = 0)
        {
            // buffer
            _liveStreamBuffer.BufferHoldBackTimeMilliseconds = delayMilliseconds;

            // exchange
            var exchangeName = "pp.livestream";
            _liveStreamModel.ExchangeDeclare(exchangeName, ExchangeType.Fanout);

            var args = new Dictionary<string, object>();
            args.Add("x-max-priority", _configuration.DefaultQueuePriorityMax);

            // queue
            var queueName = _liveStreamModel.QueueDeclare($"{exchangeName}.{_configuration.NodeId}", true, false, false, args);
            _liveStreamModel.QueueBind(queueName, exchangeName, string.Empty, null);
            _liveStreamModel.BasicQos(0, (ushort)bufferSize, false);

            // create observable
            var result = Observable.FromEventPattern<EventArgsLiveStreamMessage>(
                ev => _liveStreamBuffer.BufferedReceive += ev,
                ev => _liveStreamBuffer.BufferedReceive -= ev
            );

            // create consumer for RabbitMQ events
            _liveStreamConsumer = new LiveStreamConsumer(_configuration, _liveStreamModel);
            _liveStreamConsumer.Received += (o, e) => { _liveStreamBuffer.Enqueue(e); };

            // consumer
            var consumer = new EventingBasicConsumer(_requestMessageModel);
            consumer.Received += (o, e) => { _liveStreamConsumer.OnReceived(o, e); };
            _liveStreamModel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

            return result;
        }
    }
}
