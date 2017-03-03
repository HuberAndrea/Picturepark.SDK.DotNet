﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Contract.Extensions;
using Picturepark.SDK.V1.Tests.Fixtures;

namespace Picturepark.SDK.V1.Tests
{
	public class ContentTests : IClassFixture<SDKClientFixture>
	{
		private SDKClientFixture _fixture;
		private PictureparkClient _client;

		public ContentTests(SDKClientFixture fixture)
		{
			_fixture = fixture;
			_client = _fixture.Client;
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldAggregateByChannel()
		{
			string channelId = "RootChannel";

			var request = new ContentAggregationRequest() { SearchString = string.Empty };
			ObjectAggregationResult result = await _client.Contents.AggregateByChannelAsync(channelId, request);

			request.Aggregators = new List<AggregatorBase>()
			{
				new TermsAggregator { Name = "Test", Field = "ContentType", Size = 10 }
			};

			result = await _client.Contents.AggregateByChannelAsync(channelId, request);
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldAggregateWithAggregators()
		{
			//// TODO: finds one content, but logic is not clear. Must be extended and asserted with useful data

			var request = new ContentAggregationRequest() { SearchString = string.Empty };
			request.Aggregators = new List<AggregatorBase>();

			// First Aggregator
			request.Aggregators.Add(new TermsAggregator { Name = "Aggregator1", Field = "ContentType", Size = 10 });

			// Second Aggregator
			var ranges = new List<NumericRange>()
			{
				new NumericRange() { From = null, To = 499,  Names = new TranslatedStringDictionary { { "en", "Aggregator2a" } } },
				new NumericRange() { From = 500, To = 5000, Names = new TranslatedStringDictionary { { "en", "Aggregator2b" } } }
			};

			var numRangeAggregator = new NumericRangeAggregator()
			{
				Name = "NumberAggregator",
				Field = "Original.Width",
				Ranges = ranges
			};

			request.Aggregators.Add(numRangeAggregator);
			ObjectAggregationResult result = await _client.Contents.AggregateAsync(request);
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldAggregateWithoutAggregators()
		{
			// Todo: does not find anything
			// Check: does Aggregate() without Aggregators make any sense?
			//// TODO: Must be extended and asserted with useful data.

			var request = new ContentAggregationRequest() { SearchString = "*" };
			ObjectAggregationResult result = await _client.Contents.AggregateAsync(request);

			var numRangeFilter = new NumericRangeFilter() { Field = "ContentType", Range = new NumericRange { From = 2, To = 5 } };
			request.Filter = numRangeFilter;
			result = await _client.Contents.AggregateAsync(request);

			request.Filter = null;
			request.LifeCycleFilter = LifeCycleFilter.All;
			result = await _client.Contents.AggregateAsync(request);

			request.Aggregators = new List<AggregatorBase>();
			result = await _client.Contents.AggregateAsync(request);
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldCreateBatchContentDownload()
		{
			var request = new ContentBatchDownloadRequest();
			request.Contents = new List<Content>();

			string contentId1 = _fixture.GetRandomContentId("*.jpg", 50);
			string contentId2 = _fixture.GetRandomContentId("*.jpg", 50);
			Assert.False(string.IsNullOrEmpty(contentId1));

			request.Contents.Add(new Content() { ContentId = contentId1, OutputFormatId = "Original" });
			if (contentId1 != contentId2)
				request.Contents.Add(new Content() { ContentId = contentId2, OutputFormatId = "Original" });

			ContentBatchDownloadItem result = await _client.Contents.CreateBatchContentDownloadAsync(request);
			Assert.True(result.DownloadToken != null);
		}

		// [Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldCreateVirtualContents()
		{
			//// TODO BRO: Implement

			string contentId = _fixture.GetRandomContentId("*.jpg", 20);
			Assert.False(string.IsNullOrEmpty(contentId));

			await Task.FromResult<object>(null);
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldDownloadMultiple()
		{
			int maxNumberOfDownloadFiles = 3;
			string searchString = string.Empty;

			ContentSearchResult result = _fixture.GetRandomContents(searchString, maxNumberOfDownloadFiles);
			Assert.True(result.Results.Count > 0);

			await _client.Contents.DownloadFilesAsync(
				result,
				_fixture.TempDirectory,
				true,
				successDelegate: (content) =>
				{
					Console.WriteLine(content.GetFileMetadata().FileName);
				},
				errorDelegate: (error) =>
				{
					Console.WriteLine(error);
				});
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldDownloadSingle()
		{
			string contentId = _fixture.GetRandomContentId("*.jpg", 20);
			Assert.False(string.IsNullOrEmpty(contentId));
			ContentDetailViewItem contentDetail = await _client.Contents.GetAsync(contentId);

			var fileMetadata = contentDetail.GetFileMetadata();
			var fileName = new Random().Next(0, 999999).ToString() + "-" + fileMetadata.FileName + ".jpg";
			var filePath = Path.Combine(_fixture.TempDirectory, fileName);

			if (File.Exists(filePath))
				File.Delete(filePath);

			using (var response = await _client.Contents.DownloadAsync(contentId, "Original", "bytes=0-20000000"))
			{
				var stream = response.Stream;
				Assert.Equal(true, stream.CanRead);

				response.Stream.SaveFile(filePath);
				Assert.True(File.Exists(filePath));
			}
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldDownloadSingleResized()
		{
			// Download a resized version of an image file
			string contentId = _fixture.GetRandomContentId("*.jpg", 20);
			Assert.False(string.IsNullOrEmpty(contentId));
			ContentDetailViewItem contentDetail = await _client.Contents.GetAsync(contentId);

			var fileMetadata = contentDetail.GetFileMetadata();
			var fileName = new Random().Next(0, 999999).ToString() + "-" + fileMetadata.FileName + ".jpg";
			var filePath = Path.Combine(_fixture.TempDirectory, fileName);

			if (File.Exists(filePath))
				File.Delete(filePath);

			using (var response = await _client.Contents.DownloadResizedAsync(contentId, "Original", 200, 200))
			{
				response.Stream.SaveFile(filePath);
			}

			Assert.True(File.Exists(filePath));
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldDownloadSingleThumbnail()
		{
			// Download a resized version of an image file
			string contentId = _fixture.GetRandomContentId("*.jpg", 20);
			Assert.False(string.IsNullOrEmpty(contentId));

			var fileName = new Random().Next(0, 999999).ToString() + "-" + contentId + ".jpg";
			var filePath = Path.Combine(_fixture.TempDirectory, fileName);

			if (File.Exists(filePath))
				File.Delete(filePath);

			using (var response = await _client.Contents.DownloadThumbnailAsync(contentId, ThumbnailSize.Small))
			{
				response.Stream.SaveFile(filePath);
			}

			Assert.True(File.Exists(filePath));
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldGet()
		{
			// Todo: Typed Aufruf?  CustomContentDetailViewItem<ContentMetadata> result = Get<ContentMetadata>(contentId);
			string contentId = _fixture.GetRandomContentId("*.jpg", 20);
			Assert.False(string.IsNullOrEmpty(contentId));

			ContentDetailViewItem result = await _client.Contents.GetAsync(contentId);
			Assert.True(result.EntityType == EntityType.Content);
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldGetDocumentMetadata()
		{
			string contentId = _fixture.GetRandomContentId("*.doc", 20);
			if (string.IsNullOrEmpty(contentId))
				contentId = _fixture.GetRandomContentId("*.docx", 20);
			Assert.False(string.IsNullOrEmpty(contentId));

			ContentDetailViewItem result = await _client.Contents.GetAsync(contentId);

			FileMetadata fileMetadata = result.GetFileMetadata();
			Assert.False(string.IsNullOrEmpty(fileMetadata.FileName));
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldGetResolved()
		{
			string contentId = _fixture.GetRandomContentId("*.jpg", 20);
			ContentDetailViewItem result = await _client.Contents.GetAsync(contentId, true);
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldSearch()
		{
			var channelIds = new List<string> { "RootChannel" };
			var languages = new List<string>();
			string searchString = "*";

			var sortInfos = new List<SortInfo>
			{
				new SortInfo { Direction = SortDirection.Asc, Field = PropertyHelper.GetName<ContentDetailViewItem>(i => i.Audit.CreationDate) }
			};

			var filter = new TermFilter { Field = "MetadataSchemaIds", Term = "Base" };
			var filter2 = new TermFilter { Field = "Audit.CreatedByUser.Id", Term = "Base" };

			// TODO BRO: Implement generic filter creator
			//// var filter = new Filter().TermFilter<ContentDetailViewItem>(i => i.MetadataSchemaIds, "Base");
			//// var filter2 = new Filter().TermFilter<ContentDetailViewItem>(i => i.Audit.CreatedByUser.Id, "Base");

			var and = new AndFilter { Filters = new List<FilterBase> { filter, filter2 } };

			var request = new ContentSearchRequest()
			{
				ChannelIds = channelIds,
				SearchString = searchString,
				Sort = sortInfos,
				Start = 0,
				Limit = 8
			};

			ContentSearchResult result = await _client.Contents.SearchAsync(request);
			Assert.True(result.Results.Count > 0);
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldSearchByChannel()
		{
			string channelId = "RootChannel";
			string searchString = "*";

			var sortInfos = new List<SortInfo>()
			{
				new SortInfo { Direction = SortDirection.Asc, Field = "Audit.CreationDate" }
			};

			var request = new ContentSearchRequest()
			{
				ChannelIds = new List<string> { channelId },
				SearchString = searchString,
				Sort = sortInfos,
				Start = 0,
				Limit = 8
			};

			ContentSearchResult result = await _client.Contents.SearchByChannelAsync(channelId, request);
			Assert.True(result.Results.Count > 0);
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldTrashAndUntrashRandomContent()
		{
			string contentId = _fixture.GetRandomContentId("*.jpg", 20);
			Assert.False(string.IsNullOrEmpty(contentId));

			var contentDetailViewItem = await _client.Contents.GetAsync(contentId);

			// Trash
			await _client.Contents.DeactivateAsync(contentId);

			await Assert.ThrowsAsync<ApiException<ContentNotFoundException>>(async () => await _client.Contents.GetAsync(contentId));

			// UnTrash
			var reactivatedContent = await _client.Contents.ReactivateAsync(contentId, resolve: false, timeout: 60000);
			Assert.True(reactivatedContent != null);
		}

		[Trait("Stack", "Contents")]
		public async Task ShouldUpdateMetadata()
		{
			string contentId = _fixture.GetRandomContentId("*.jpg", 20);

			ContentDetailViewItem content = await _client.Contents.GetAsync(contentId);

			var updateRequest = new ContentsMetadataUpdateRequest
			{
				ContentIds = new List<string> { content.Id },
				ChangeCommands = new List<MetadataValuesChangeCommandBase>
				{
					new MetadataValuesSchemaUpsertCommand
					{
							SchemaId = "Drive",
							Value = new MetadataDictionary
							{
								{ "Location", "testlocation" }
							}
					}
				}
			};

			BusinessProcessViewItem result = await _client.Contents.UpdateMetadataManyAsync(updateRequest);
			BusinessProcessWaitResult waitResult = await result.Wait4MetadataAsync(_client.ListItems);

			Assert.True(waitResult.HasStateHit);
		}

		[Fact]
		[Trait("Stack", "Contents")]
		public async Task ShouldUpdatePermissions()
		{
			string contentId = _fixture.GetRandomContentId("*.jpg", 20);

			Assert.False(string.IsNullOrEmpty(contentId));

			ContentDetailViewItem contentDetail = await _client.Contents.GetAsync(contentId);
			Assert.True(contentDetail.EntityType == EntityType.Content);

			var contentPermissionSetIds = new List<string>() { "aaa" + new Random().Next(0, 999).ToString(), "bbb" + new Random().Next(0, 999).ToString() };
			contentDetail.ContentPermissionSetIds = contentPermissionSetIds;

			BusinessProcessViewItem result = await _client.Contents.UpdatePermissionsManyAsync(new List<UpdateContentPermissionsRequest> { new UpdateContentPermissionsRequest { ContentId = contentDetail.Id, ContentPermissionSetIds = contentDetail.ContentPermissionSetIds } });
			await result.Wait4StateAsync("Completed", _client.BusinessProcesses);

			contentDetail = await _client.Contents.GetAsync(contentId);
			var currentContentPermissionSetIds = contentDetail.ContentPermissionSetIds.Select(i => i).ToList();

			Assert.True(contentPermissionSetIds.Except(currentContentPermissionSetIds).Count() == 0);
			Assert.True(currentContentPermissionSetIds.Except(contentPermissionSetIds).Count() == 0);
		}
	}
}
