﻿using Newtonsoft.Json;
using System;
using System.IO;
using System.Reflection;
using Picturepark.SDK.V1.Authentication;
using Picturepark.SDK.V1.Contract;

namespace Picturepark.SDK.V1.Tests.Fixtures
{
	public class SDKClientFixture : IDisposable
	{
		private readonly PictureparkClient _client;
		private readonly TestConfiguration _configuration;

		public SDKClientFixture()
		{
			ProjectDirectory = Path.GetFullPath(Path.GetDirectoryName(typeof(SDKClientFixture).GetTypeInfo().Assembly.Location) + "/../../../");

			// Fix
			if (!File.Exists(ProjectDirectory + "Configuration.json"))
				ProjectDirectory += "../";

			if (!Directory.Exists(TempDirectory))
				Directory.CreateDirectory(TempDirectory);

			var configurationJson = File.ReadAllText(ProjectDirectory + "Configuration.json");
			_configuration = JsonConvert.DeserializeObject<TestConfiguration>(configurationJson);

			var authClient = new AccessTokenAuthClient(_configuration.Server, _configuration.AccessToken);
			_client = new PictureparkClient(new PictureparkClientSettings(authClient));
		}

		public string ProjectDirectory { get; }

		public string TempDirectory => ProjectDirectory + "/Temp";

		public string ExampleFilesBasePath => ProjectDirectory + "/ExampleData/Pool";

		public TestConfiguration Configuration => _configuration;

		public PictureparkClient Client => _client;

		public Contract.ContentSearchResult GetRandomContents(string searchString, int limit)
		{
			return RandomHelper.GetRandomContents(_client, searchString, limit);
		}

		public string GetRandomContentId(string searchString, int limit)
		{
			return RandomHelper.GetRandomContentId(_client, searchString, limit);
		}

		public string GetRandomContentPermissionSetId(int limit)
		{
			return RandomHelper.GetRandomContentPermissionSetId(_client, limit);
		}

		public string GetRandomBatchTransferId(TransferState? transferState, int limit)
		{
			return RandomHelper.GetRandomBatchTransferId(_client, transferState, limit);
		}

		public string GetRandomFileTransferId(int limit)
		{
			return RandomHelper.GetRandomFileTransferId(_client, limit);
		}

		public string GetRandomMetadataPermissionSetId(int limit)
		{
			return RandomHelper.GetRandomMetadataPermissionSetId(_client, limit);
		}

		public string GetRandomSchemaId(int limit)
		{
			return RandomHelper.GetRandomSchemaId(_client, limit);
		}

		public string GetRandomObjectId(string metadataSchemaId, int limit)
		{
			return RandomHelper.GetRandomObjectId(_client, metadataSchemaId, limit);
		}

		public string GetRandomShareId(EntityType entityType, int limit)
		{
			return RandomHelper.GetRandomShareId(_client, entityType, limit);
		}

		public void Dispose()
		{
			_client.Dispose();
		}
	}
}
