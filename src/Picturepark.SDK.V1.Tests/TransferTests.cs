﻿using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Tests.Fixtures;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		public async Task ShouldGetBlacklist()
		{
			/// Act
			var blacklist = await _client.Transfers.GetBlacklistAsync();

			/// Assert
			Assert.NotNull(blacklist?.Items);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldCreateTransferFromFiles()
		{
			/// Arrange
			var transferName = new Random().Next(1000, 9999).ToString();
			var files = new List<string>
			{
				Path.Combine(_fixture.ExampleFilesBasePath, "0030_JabLtzJl8bc.jpg")
			};

			/// Act
			var result = await _client.Transfers.CreateTransferAsync(files, transferName);

			/// Assert
			Assert.NotNull(result);
		}

		// [Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldDeleteFiles()
		{
			// TODO: Fix ShouldDeleteFiles unit test

			/// Arrange
			var transferName = new Random().Next(1000, 9999).ToString();
			var files = new List<string>
			{
				Path.Combine(_fixture.ExampleFilesBasePath, "0030_JabLtzJl8bc.jpg")
			};

			var transfer = await _client.Transfers.CreateTransferAsync(files, transferName);
			var searchRequest = new FileTransferSearchRequest
			{
				Limit = 20,
				SearchString = "*",
				Filter = new TermFilter { Field = "transferId", Term = transfer.Id }
			};

			var fileParameter = new FileParameter(new MemoryStream(new byte[] { 1, 2, 3 }));
			await _client.Transfers.UploadFileAsync(transfer.Id, "foobar", fileParameter);

			FileTransferSearchResult result = await _client.Transfers.SearchFilesAsync(searchRequest);

			/// Act
			var request = new FileTransferDeleteRequest
			{
				TransferId = transfer.Id,
				FileTransferIds = new List<string> { result.Results[0].Id }
			};

			await _client.Transfers.DeleteFilesAsync(request);

			/// Assert
			await Assert.ThrowsAsync<ApiException>(async () => await _client.Transfers.GetAsync(transfer.Id)); // TODO: TransferClient.GetAsync: Should correctly throw NotFoundException
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldCancelTransfer()
		{
			/// Arrange
			var transferName = new Random().Next(1000, 9999).ToString();
			var files = new List<string>
			{
				Path.Combine(_fixture.ExampleFilesBasePath, "0030_JabLtzJl8bc.jpg")
			};

			var transfer = await _client.Transfers.CreateTransferAsync(files, transferName);
			var oldTransfer = await _client.Transfers.GetAsync(transfer.Id);

			/// Act
			await _client.Transfers.CancelTransferAsync(transfer.Id);

			/// Assert
			var currentTransfer = await _client.Transfers.GetAsync(transfer.Id);

			Assert.Equal(TransferState.Created, oldTransfer.State); // TODO: ShouldCancelTransfer: Check asserts; they are probably incorrect!
			Assert.Equal(TransferState.TransferReady, currentTransfer.State);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldCreateTransferFromWebUrls()
		{
			/// Arrange
			var transferName = "UrlImport " + new Random().Next(1000, 9999);
			var urls = new List<string>
			{
				"https://picturepark.com/wp-content/uploads/2013/06/home-marquee.jpg",
				"http://cdn1.spiegel.de/images/image-733178-900_breitwand_180x67-zgpe-733178.jpg",
				"http://cdn3.spiegel.de/images/image-1046236-700_poster_16x9-ymle-1046236.jpg",
				"https://dgpcopy.next-picturepark.com/Go/A6ZYegLz/D/15/1"
			};

			/// Act
			var request = new CreateTransferRequest
			{
				Name = transferName,
				TransferType = TransferType.WebDownload,
				WebLinks = urls.Select(url => new TransferWebLink
				{
					Url = url,
					Identifier = Guid.NewGuid().ToString()
				}).ToList()
			};

			Transfer result = await _client.Transfers.CreateTransferAsync(request);

			/// Assert
			Assert.NotNull(result);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldDelete()
		{
			/// Arrange
			var transferId = _fixture.GetRandomTransferId(TransferState.TransferReady, 10);
			var transfer = await _client.Transfers.GetAsync(transferId);

			/// Act
			await _client.Transfers.DeleteAsync(transferId);

			/// Assert
			await Assert.ThrowsAsync<ApiException>(async () => await _client.Transfers.GetAsync(transferId)); // TODO: TransferClient.GetAsync: Should correctly throw NotFoundException
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldGet()
		{
			/// Arrange
			var transferId = _fixture.GetRandomTransferId(null, 20);

			/// Act
			TransferDetail result = await _client.Transfers.GetAsync(transferId);

			/// Assert
			Assert.NotNull(result);
			Assert.Equal(transferId, result.Id);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldGetFile()
		{
			/// Arrange
			var fileTransferId = _fixture.GetRandomFileTransferId(20);

			/// Act
			FileTransferDetail result = await _client.Transfers.GetFileAsync(fileTransferId);

			/// Assert
			Assert.NotNull(result);
			Assert.Equal(fileTransferId, result.Id);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldSearch()
		{
			/// Arrange
			var request = new TransferSearchRequest { Limit = 5, SearchString = "*" };

			/// Act
			TransferSearchResult result = await _client.Transfers.SearchAsync(request);

			/// Assert
			Assert.NotNull(result);
			Assert.Equal(5, result.Results.Count);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldSearchFiles()
		{
			/// Arrange
			var request = new FileTransferSearchRequest { Limit = 20, SearchString = "*" };

			/// Act
			FileTransferSearchResult result = await _client.Transfers.SearchFilesAsync(request);

			/// Assert
			Assert.NotNull(result);
			Assert.Equal(20, result.Results.Count);
		}

		[Fact]
		[Trait("Stack", "Transfers")]
		public async Task ShouldUploadAndImportFiles()
		{
			const int desiredUploadFiles = 10;
			var transferName = nameof(ShouldUploadAndImportFiles) + "-" + new Random().Next(1000, 9999);

			var filesInDirectory = Directory.GetFiles(_fixture.ExampleFilesBasePath, "*").ToList();

			var numberOfFilesInDir = filesInDirectory.Count;
			var numberOfUploadFiles = Math.Min(desiredUploadFiles, numberOfFilesInDir);

			var randomNumber = new Random().Next(0, numberOfFilesInDir - numberOfUploadFiles);
			var importFilePaths = filesInDirectory.Skip(randomNumber).Take(numberOfUploadFiles).ToList();

			var uploadOptions = new UploadOptions
			{
				ConcurrentUploads = 4,
				ChunkSize = 20 * 1024,
				SuccessDelegate = Console.WriteLine,
				ErrorDelegate = Console.WriteLine
			};

			var transfer = await _client.Transfers.UploadFilesAsync(transferName, importFilePaths, uploadOptions);

			await ImportTransferAsync(transfer, transferName);
		}

		internal async Task ImportTransferAsync(Transfer transfer, string collectionName)
		{
			var request = new FileTransfer2ContentCreateRequest
			{
				TransferId = transfer.Id,
				ContentPermissionSetIds = new List<string>(),
				Metadata = null,
				LayerSchemaIds = new List<string>()
			};

			await _client.Transfers.ImportTransferAsync(transfer, request);
		}
	}
}
