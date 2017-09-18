﻿using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Picturepark.SDK.V1.Tests
{
	public class TransferTests : IClassFixture<SDKClientFixture>
	{
		private readonly SDKClientFixture _fixture;
		private readonly PictureparkClient _client;

		public TransferTests(SDKClientFixture fixture)
		{
			_fixture = fixture;
			_client = _fixture.Client;
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldCreateBatchAndUploadFiles()
		{
			var filePaths = new List<string>
			{
				Path.Combine(_fixture.ExampleFilesBasePath, "0322_ERSauUNQ3ag.jpg"),
				Path.Combine(_fixture.ExampleFilesBasePath, "0314_JYFmYif4n70.jpg"),
				Path.Combine(_fixture.ExampleFilesBasePath, "0050_6qORI5j_6n8.jpg")
			};
			string transferName = nameof(ShouldCreateBatchAndUploadFiles) + "-" + new Random().Next(1000, 9999).ToString();
			List<string> fileNames = filePaths.Select(Path.GetFileName).ToList();

			// Create batch
			Transfer transfer = await _client.Transfers.CreateBatchAsync(fileNames, transferName);

			// Upload files
			string directoryPath = Path.GetDirectoryName(filePaths.First());

			await _client.Transfers.UploadFilesAsync(
				filePaths,
				directoryPath,
				transfer,
				successDelegate: (file) =>
				{
					Console.WriteLine(file);
				},
				errorDelegate: (error) =>
				{
					Console.WriteLine(error);
				}
			);

			// Import batch
			await ImportBatchAsync(transfer, transferName);

			// Import metadata
			var request = new FileTransferSearchRequest() { Limit = 1000 };
			FileTransferSearchResult fileTransferSearchResult = await _client.Transfers.SearchFilesAsync(request);
			string fileTransferId = fileTransferSearchResult.Results.Where(i => i.TransferId == transfer.Id).Select(i => i.Id).FirstOrDefault();

			// Todo SAF
			// Program.metadataApiExamples.ImportMetadata(null, fileTransferId);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldCreateBatchFromFiles()
		{
			string batchName = new Random().Next(1000, 9999).ToString();
			var files = new List<string>() { Path.Combine(_fixture.ExampleFilesBasePath, "0030_JabLtzJl8bc.jpg") };

			Transfer result = await _client.Transfers.CreateBatchAsync(files, batchName);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldCreateBatchFromWebUrls()
		{
			string batchName = "UrlImport " + new Random().Next(1000, 9999).ToString();

			var urls = new List<string>
			{
				"https://picturepark.com/wp-content/uploads/2013/06/home-marquee.jpg",
				"http://cdn1.spiegel.de/images/image-733178-900_breitwand_180x67-zgpe-733178.jpg",
				"http://cdn3.spiegel.de/images/image-1046236-700_poster_16x9-ymle-1046236.jpg",
				"https://dgpcopy.next-picturepark.com/Go/A6ZYegLz/D/15/1"
			};
			var request = new CreateTransferRequest()
			{
				Name = batchName,
				TransferType = TransferType.WebDownload,
				WebLinks = urls.Select(url => new TransferWebLink() { Url = url, Identifier = Guid.NewGuid().ToString() }).ToList()
			};

			Transfer result = await _client.Transfers.CreateBatchAsync(request);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldDeleteBatch()
		{
			var transferId = _fixture.GetRandomTransferId(TransferState.TransferReady, 10);

			Assert.True(transferId != string.Empty);

			await _client.Transfers.DeleteAsync(transferId);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldGetBatch()
		{
			var transferId = _fixture.GetRandomTransferId(null, 20);
			TransferDetail result = await _client.Transfers.GetAsync(transferId);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldGetFile()
		{
			var fileTransferId = _fixture.GetRandomFileTransferId(20);
			FileTransferDetail result = await _client.Transfers.GetFileAsync(fileTransferId);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldSearchTransfers()
		{
			var request = new TransferSearchRequest() { Limit = 5, SearchString = "*" };
			TransferSearchResult result = await _client.Transfers.SearchAsync(request);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldSearchFiles()
		{
			var request = new FileTransferSearchRequest() { Limit = 20, SearchString = "*" };
			FileTransferSearchResult result = await _client.Transfers.SearchFilesAsync(request);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldUploadAndImportFiles()
		{
			const int desiredUploadFiles = 10;
			string batchName = nameof(ShouldUploadAndImportFiles) + "-" + new Random().Next(1000, 9999).ToString();

			var filesInDirectory = Directory.GetFiles(_fixture.ExampleFilesBasePath, "*").ToList();

			int numberOfFilesInDir = filesInDirectory.Count;
			int numberOfUploadFiles = Math.Min(desiredUploadFiles, numberOfFilesInDir);

			int randomNumber = new Random().Next(0, numberOfFilesInDir - numberOfUploadFiles);
			List<string> importFilePaths = filesInDirectory.Skip(randomNumber).Take(numberOfUploadFiles).ToList();

			Transfer transfer = await _client.Transfers.CreateBatchAsync(importFilePaths.Select(Path.GetFileName).ToList(), batchName);

			await _client.Transfers.UploadFilesAsync(
				importFilePaths,
				_fixture.ExampleFilesBasePath,
				transfer,
				successDelegate: (file) =>
				{
					Console.WriteLine(file);
				},
				errorDelegate: (error) =>
				{
					Console.WriteLine(error);
				});

			await ImportBatchAsync(transfer, batchName);
		}

		internal async Task ImportBatchAsync(Transfer transfer, string collectionName)
		{
			var request = new FileTransfer2ContentCreateRequest
			{
				TransferId = transfer.Id,
				ContentPermissionSetIds = new List<string>(),
				Metadata = null,
				LayerSchemaIds = new List<string>()
			};

			await _client.Transfers.ImportBatchAsync(transfer, request);
		}
	}
}
