﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using System.Net.Http;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Contract.Extensions;
using Picturepark.SDK.V1.Tests.Fixtures;
using Newtonsoft.Json;
using Picturepark.SDK.V1.Tests.Contracts;
#pragma warning disable 1587

namespace Picturepark.SDK.V1.Tests.Clients
{
    public class ContentTests : IClassFixture<ClientFixture>
    {
        private readonly ClientFixture _fixture;
        private readonly PictureparkClient _client;

        public ContentTests(ClientFixture fixture)
        {
            _fixture = fixture;
            _client = _fixture.Client;
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldTransferOwnership()
        {
            /// Arrange
            var contentId = _fixture.GetRandomContentId(".jpg", 50);

            /// Act
            var previousContent = await _client.Contents.GetAsync(contentId);
            var previousOwner = await _client.Users.GetByOwnerTokenAsync(previousContent.OwnerTokenId);
            var searchResult = await _client.Users.SearchAsync(new UserSearchRequest { Limit = 10, UserRightsFilter = new List<UserRight> { UserRight.ManageContent } });

            var newUser = searchResult.Results.First(u => u.Id != previousOwner.Id);
            var request = new ContentOwnershipTransferRequest
            {
                ContentId = contentId,
                TransferUserId = newUser.Id
            };
            await _client.Contents.TransferOwnershipAsync(contentId, request);

            var newContent = await _client.Contents.GetAsync(contentId);
            var newOwner = await _client.Users.GetByOwnerTokenAsync(newContent.OwnerTokenId);

            /// Assert
            Assert.Equal(previousContent.Id, newContent.Id);
            Assert.NotEqual(previousContent.OwnerTokenId, newContent.OwnerTokenId);
            Assert.NotEqual(previousOwner.Id, newOwner.Id);
            Assert.Equal(newUser.Id, newOwner.Id);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldGetMany()
        {
            /// Arrange
            var contentIds = new string[]
            {
                _fixture.GetRandomContentId(".jpg", 50),
                _fixture.GetRandomContentId(".jpg", 50)
            };

            /// Act
            var contents = await _client.Contents.GetManyAsync(contentIds, true);

            /// Assert
            Assert.Equal(2, contents.Count);
            Assert.Equal(contentIds[0], contents.ToList()[0].Id);
            Assert.Equal(contentIds[1], contents.ToList()[1].Id);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldTransferOwnershipMany()
        {
            /// Arrange
            var contentIds = new string[]
            {
                _fixture.GetRandomContentId(".jpg", 50),
                _fixture.GetRandomContentId(".jpg", 50)
            };

            /// Act
            var previousContents = await _client.Contents.GetManyAsync(contentIds, true);
            var previousOwner = await _client.Users.GetByOwnerTokenAsync(previousContents.ToList()[0].OwnerTokenId);

            // Search user with ManageContent UserRight
            var searchResult = await _client.Users.SearchAsync(new UserSearchRequest
            {
                Limit = 10,
                UserRightsFilter = new List<UserRight> { UserRight.ManageContent }
            });

            var newUser = searchResult.Results.First(u => u.Id != previousOwner.Id);
            var request = new ContentsOwnershipTransferRequest
            {
                ContentIds = contentIds.ToList(),
                TransferUserId = newUser.Id
            };
            var businessProcess = await _client.Contents.TransferOwnershipManyAsync(request);
            await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id);

            var newContents = await _client.Contents.GetManyAsync(contentIds, true);
            var newOwner1 = await _client.Users.GetByOwnerTokenAsync(newContents.ToList()[0].OwnerTokenId);
            var newOwner2 = await _client.Users.GetByOwnerTokenAsync(newContents.ToList()[1].OwnerTokenId);

            /// Assert
            Assert.Equal(previousContents.ToList()[0].Id, newContents.ToList()[0].Id);
            Assert.Equal(previousContents.ToList()[1].Id, newContents.ToList()[1].Id);

            Assert.Equal(newOwner1.Id, newOwner2.Id);
            Assert.Equal(newUser.Id, newOwner1.Id);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldAggregateWithAggregators()
        {
            var request = new ContentAggregationRequest
            {
                SearchString = string.Empty,
                Aggregators = new List<AggregatorBase>
                {
                    new TermsAggregator { Name = "Aggregator1", Field = "contentType", Size = 10 }
                }
            };

            // Second Aggregator
            var ranges = new List<NumericRange>
            {
                new NumericRange { From = null, To = 499,  Names = new TranslatedStringDictionary { { "en", "Aggregator2a" } } },
                new NumericRange { From = 500, To = 5000, Names = new TranslatedStringDictionary { { "en", "Aggregator2b" } } }
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
        public async Task ShouldAggregateByChannel()
        {
            /// Arrange
            var channelId = "rootChannel";
            var request = new ContentAggregationRequest
            {
                SearchString = string.Empty
            };

            /// Act
            var result = await _client.Contents.AggregateByChannelAsync(channelId, request);

            /// Assert
            var originalWidthResults = result.AggregationResults
                .SingleOrDefault(i => i.Name == "Original Width");

            Assert.NotNull(originalWidthResults);
            Assert.True(originalWidthResults.AggregationResultItems.Count > 0);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldAggregateByChannelWithTermsAggregator()
        {
            /// Arrange
            var channelId = "rootChannel";
            var request = new ContentAggregationRequest
            {
                SearchString = string.Empty,
                Aggregators = new List<AggregatorBase>
                {
                    new TermsAggregator { Name = "Permissions", Field = "permissionSetIds", Size = 10 }
                }
            };

            /// Act
            var result = await _client.Contents.AggregateByChannelAsync(channelId, request);

            /// Assert
            var permissionSetResults = result.AggregationResults
                .SingleOrDefault(i => i.Name == "Permissions");

            Assert.NotNull(permissionSetResults);
            Assert.True(permissionSetResults.AggregationResultItems.Count > 0);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldCreateDownloadLinkForSingleFile()
        {
            /// Arrange
            var contentId = _fixture.GetRandomContentId(".jpg", 50);
            var createDownloadLinkRequest = new ContentDownloadLinkCreateRequest
            {
                Contents = new List<ContentDownloadRequestItem>
                {
                    new ContentDownloadRequestItem { ContentId = contentId, OutputFormatId = "Original" }
                }
            };

            /// Act
            var result = await _client.Contents.CreateDownloadLinkAsync(createDownloadLinkRequest);
            Assert.NotNull(result.DownloadUrl);

            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(result.DownloadUrl))
            {
                response.EnsureSuccessStatusCode();

                var fileName = response.Content.Headers.ContentDisposition.FileName.Replace("\"", string.Empty);
                Assert.EndsWith(".jpg", fileName);

                var filePath = Path.Combine(_fixture.TempDirectory, fileName);

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(filePath))
                {
                    stream.CopyTo(fileStream);

                    /// Assert
                    Assert.True(stream.Length > 10);
                }
            }
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldCreateDownloadLinkForMultipeFiles()
        {
            /// Arrange
            var contentId1 = _fixture.GetRandomContentId(".jpg", 50);
            var contentId2 = _fixture.GetRandomContentId(".jpg", 50);

            var createDownloadLinkRequest = new ContentDownloadLinkCreateRequest
            {
                Contents = new List<ContentDownloadRequestItem>
                {
                    new ContentDownloadRequestItem { ContentId = contentId1, OutputFormatId = "Original" },
                    new ContentDownloadRequestItem { ContentId = contentId2, OutputFormatId = "Original" }
                }
            };

            /// Act
            var result = await _client.Contents.CreateDownloadLinkAsync(createDownloadLinkRequest);
            Assert.NotNull(result.DownloadUrl);

            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(result.DownloadUrl))
            {
                response.EnsureSuccessStatusCode();

                var fileName = response.Content.Headers.ContentDisposition.FileName;
                Assert.EndsWith(".zip", fileName);

                var filePath = Path.Combine(_fixture.TempDirectory, fileName);

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = File.Create(filePath))
                {
                    stream.CopyTo(fileStream);

                    /// Assert
                    Assert.True(stream.Length > 10);
                }
            }
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldCreateContents()
        {
            /// Arrange
            var schemas = await _client.Schemas.GenerateSchemasAsync(typeof(ContentItem));
            foreach (var schema in schemas)
            {
                await _client.Schemas.CreateOrUpdateAndWaitForCompletionAsync(schema, false);
            }

            var request = new ContentCreateRequest
            {
                Content = JsonConvert.DeserializeObject(@"{ ""name"": ""foo"" }"),
                ContentSchemaId = "ContentItem",
                Metadata = new DataDictionary()
            };

            /// Act
            var result = await _client.Contents.CreateContentAsync(request, true);

            /// Assert
            string contentId = _fixture.GetRandomContentId(".jpg", 20);
            Assert.False(string.IsNullOrEmpty(contentId));
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
            string contentId = _fixture.GetRandomContentId(".jpg", 20);
            Assert.False(string.IsNullOrEmpty(contentId));
            ContentDetail contentDetail = await _client.Contents.GetAsync(contentId);

            var fileMetadata = contentDetail.GetFileMetadata();
            var fileName = new Random().Next(0, 999999).ToString() + "-" + fileMetadata.FileName + ".jpg";
            var filePath = Path.Combine(_fixture.TempDirectory, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var response = await _client.Contents.DownloadAsync(contentId, "Original", null, null, "bytes=0-20000000"))
            {
                var stream = response.Stream;
                Assert.Equal(true, stream.CanRead);

                await response.Stream.WriteToFileAsync(filePath);
                Assert.True(File.Exists(filePath));
            }
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldDownloadThumbnail()
        {
            /// Arrange
            var contentId = _fixture.GetRandomContentId(".jpg", 20);

            /// Act
            using (var response = await _client.Contents.DownloadThumbnailAsync(contentId, ThumbnailSize.Medium))
            {
                var stream = new MemoryStream();
                await response.Stream.CopyToAsync(stream);

                /// Assert
                Assert.True(stream.Length > 10);
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldUpdateMetadata()
        {
            /// Arrange
            var contentId = _fixture.GetRandomContentId(".jpg", 20);
            var request = new ContentMetadataUpdateRequest
			{
                Id = contentId,
                SchemaIds = new List<string> { "Drive" },
                Metadata = new DataDictionary
                {
                    {
                        "Drive",
                        new Dictionary<string, object>
                        {
                            { "location", "test" }
                        }
                    }
                }
            };

            /// Act
            var response = await _client.Contents.UpdateMetadataAsync(contentId, request, true);

            /// Assert
            Assert.True(true);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldUpdateMetadataByFilter()
        {
            /// Arrange
            var contentId = _fixture.GetRandomContentId(".jpg", 20);
            var request = new FilterContentsMetadataUpdateRequest
            {
                ContentFilterRequest = new ContentFilterRequest
                {
                    Filter = new TermFilter { Field = "id", Term = contentId }
                },
                ChangeCommands = new List<MetadataValuesChangeCommandBase>
                {
                    new MetadataValuesSchemaUpsertCommand
                    {
                        SchemaId = "Drive",
                        Value = new DataDictionary
                        {
                            { "location", "testlocation" }
                        }
                    }
                }
            };

            /// Act
            var businessProcess = await _client.Contents.UpdateMetadataByFilterAsync(request);
            var waitResult = await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id);

            /// Assert
            Assert.True(waitResult.HasLifeCycleHit);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldUpdateMetadataMany()
        {
            /// Arrange
            var contentId = _fixture.GetRandomContentId(".jpg", 20);

            var content = await _client.Contents.GetAsync(contentId);
            var updateRequest = new ContentsMetadataUpdateRequest
            {
                ContentIds = new List<string> { content.Id },
                ChangeCommands = new List<MetadataValuesChangeCommandBase>
                {
                    new MetadataValuesSchemaUpsertCommand
                    {
                        SchemaId = "Drive",
                        Value = new DataDictionary
                        {
                            { "location", "testlocation" }
                        }
                    }
                }
            };

            /// Act
            var businessProcess = await _client.Contents.UpdateMetadataManyAsync(updateRequest);
            var waitResult = await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id);

            /// Assert
            Assert.True(waitResult.HasLifeCycleHit);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldThrowExceptionWhenContentNotFound()
        {
            var contentId = "foobar.baz";
            await Assert.ThrowsAsync(typeof(ContentNotFoundException), async () =>
            {
                await _client.Contents.GetAsync(contentId);
            });
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldDownloadSingleResized()
        {
            // Download a resized version of an image file
            string contentId = _fixture.GetRandomContentId(".jpg", 20);
            Assert.False(string.IsNullOrEmpty(contentId));
            ContentDetail contentDetail = await _client.Contents.GetAsync(contentId);

            var fileMetadata = contentDetail.GetFileMetadata();
            var fileName = new Random().Next(0, 999999).ToString() + "-" + fileMetadata.FileName + ".jpg";
            var filePath = Path.Combine(_fixture.TempDirectory, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var response = await _client.Contents.DownloadAsync(contentId, "Original", 200, 200))
            {
                await response.Stream.WriteToFileAsync(filePath);
            }

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldDownloadSingleThumbnail()
        {
            // Download a resized version of an image file
            string contentId = _fixture.GetRandomContentId(".jpg", 20);
            Assert.False(string.IsNullOrEmpty(contentId));

            var fileName = new Random().Next(0, 999999).ToString() + "-" + contentId + ".jpg";
            var filePath = Path.Combine(_fixture.TempDirectory, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var response = await _client.Contents.DownloadThumbnailAsync(contentId, ThumbnailSize.Small))
            {
                await response.Stream.WriteToFileAsync(filePath);
            }

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldGet()
        {
            string contentId = _fixture.GetRandomContentId(".jpg", 20);
            Assert.False(string.IsNullOrEmpty(contentId));

            ContentDetail result = await _client.Contents.GetAsync(contentId, false, new[] { DisplayPatternType.List });
            Assert.NotNull(result.Id);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldGetDocumentMetadata()
        {
            string contentId = _fixture.GetRandomContentId(".doc", 20);
            if (string.IsNullOrEmpty(contentId))
                contentId = _fixture.GetRandomContentId(".docx", 20);
            Assert.False(string.IsNullOrEmpty(contentId));

            ContentDetail result = await _client.Contents.GetAsync(contentId);

            FileMetadata fileMetadata = result.GetFileMetadata();
            Assert.False(string.IsNullOrEmpty(fileMetadata.FileName));
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldGetWithResolvedObjects()
        {
            /// Arrange
            var contentId = _fixture.GetRandomContentId(".jpg", 20);

            /// Act
            var contentDetail = await _client.Contents.GetAsync(contentId, true);

			/// Assert
			Assert.Equal(contentId, contentDetail.Id);
			Assert.NotNull(contentDetail); // TODO: Add better asserts
		}

		[Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldGetWithoutResolvedObjects()
        {
            /// Arrange
            var contentId = _fixture.GetRandomContentId(".jpg", 20);

            /// Act
            var contentDetail = await _client.Contents.GetAsync(contentId, false);

			/// Assert
			Assert.Equal(contentId, contentDetail.Id);
			Assert.NotNull(contentDetail); // TODO: Add better asserts
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldSearch()
        {
            /// Arrange
            var channelIds = new List<string> { "rootChannel" };
            var searchFieldPath =
                nameof(ContentDetail.Audit).ToLowerCamelCase() + "." +
                nameof(UserAudit.CreationDate).ToLowerCamelCase();

            var sortInfos = new List<SortInfo>
            {
                new SortInfo { Direction = SortDirection.Asc, Field = searchFieldPath }
            };

            var filter = new TermFilter { Field = "contentSchemaId", Term = "ImageMetadata" };
            var request = new ContentSearchRequest
            {
                ChannelIds = channelIds,
                SearchString = "*",
                Sort = sortInfos,
                Filter = filter,
                Start = 0
            };

            /// Act
            ContentSearchResult result = await _client.Contents.SearchAsync(request);

            /// Assert
            Assert.True(result.Results.Count > 0);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldSearchByChannel()
        {
            string channelId = "rootChannel";
            string searchString = "*";

            var sortInfos = new List<SortInfo>
            {
                new SortInfo { Direction = SortDirection.Asc, Field = "audit.creationDate" }
            };

            var request = new ContentSearchRequest
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
            /// Arrange
            var contentId = _fixture.GetRandomContentId(".jpg", 20);
            Assert.False(string.IsNullOrEmpty(contentId));

            var contentDetail = await _client.Contents.GetAsync(contentId); // Should not throw

            /// Act

            // Deactivate
            await _client.Contents.DeactivateAsync(contentId);
            await Assert.ThrowsAsync<ContentNotFoundException>(async () => await _client.Contents.GetAsync(contentId));

            // Reactivate
            var reactivatedContent = await _client.Contents.ReactivateAsync(contentId, resolve: false, timeout: TimeSpan.FromMinutes(1));

            /// Assert
            Assert.True(reactivatedContent != null);
            Assert.NotNull(await _client.Contents.GetAsync(contentId));
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldTrashAndUntrashRandomContentMany()
        {
            /// Arrange
            var contentIds = new List<string>
            {
                _fixture.GetRandomContentId(".jpg", 20),
                _fixture.GetRandomContentId(".jpg", 20)
            };

            var response = await _client.Contents.GetManyAsync(contentIds, true); // Should not throw

            /// Act

            // Deactivate
            var deactivationRequest = new ContentDeactivateRequest
            {
                ContentIds = contentIds
            };

            var businessProcess = await _client.Contents.DeactivateManyAsync(deactivationRequest);
            await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id);

            await Assert.ThrowsAsync<ContentNotFoundException>(async () => await _client.Contents.GetAsync(contentIds[0]));
            await Assert.ThrowsAsync<ContentNotFoundException>(async () => await _client.Contents.GetAsync(contentIds[1]));

            // Reactivate
            var reactivateRequest = new ContentReactivateRequest
            {
                ContentIds = contentIds
            };

            businessProcess = await _client.Contents.ReactivateManyAsync(reactivateRequest);
            await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id);

            /// Assert
            Assert.NotNull(await _client.Contents.GetAsync(contentIds[0]));
            Assert.NotNull(await _client.Contents.GetAsync(contentIds[1]));
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldUpdateFile()
        {
            string contentId = _fixture.GetRandomContentId(".jpg -0030_JabLtzJl8bc", 20);

            // Create transfer
            var filePaths = new List<string>
            {
                Path.Combine(_fixture.ExampleFilesBasePath, "0030_JabLtzJl8bc.jpg")
            };
            var directoryPath = Path.GetDirectoryName(filePaths.First());
            string transferName = nameof(ShouldUpdateFile) + "-" + new Random().Next(1000, 9999);
            var createTransferResult = await _client.Transfers.CreateAndWaitForCompletionAsync(transferName, filePaths.Select(Path.GetFileName).ToList());

            // Upload file
            var uploadOptions = new UploadOptions
            {
                SuccessDelegate = Console.WriteLine,
                ErrorDelegate = Console.WriteLine
            };

            await _client.Transfers.UploadFilesAsync(createTransferResult.Transfer, filePaths, uploadOptions);

            // Search filetransfers to get id
            var request = new FileTransferSearchRequest() { Limit = 20, SearchString = "*", Filter = new TermFilter { Field = "transferId", Term = createTransferResult.Transfer.Id } };
            FileTransferSearchResult result = await _client.Transfers.SearchFilesAsync(request);

            Assert.Equal(result.TotalResults, 1);

            var updateRequest = new ContentFileUpdateRequest
            {
                ContentId = contentId,
                FileTransferId = result.Results.First().Id
            };

            var businessProcess = await _client.Contents.UpdateFileAsync(contentId, updateRequest);
            var waitResult = await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id);

            Assert.True(waitResult.HasLifeCycleHit);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldUpdatePermissions()
        {
            // Arrange
            var contentId = _fixture.GetRandomContentId(".jpg", 20);
            var contentDetail = await _client.Contents.GetAsync(contentId);

            var contentPermissionSetIds = new List<string>
            {
                "aaa" + new Random().Next(0, 999),
                "bbb" + new Random().Next(0, 999)
            };

            var request = new ContentPermissionsUpdateRequest
			{
                ContentId = contentDetail.Id,
                ContentPermissionSetIds = contentPermissionSetIds
            };

            // Act
            var result = await _client.Contents.UpdatePermissionsAsync(contentDetail.Id, request, true);

            var currentContentDetail = await _client.Contents.GetAsync(contentId);
            var currentContentPermissionSetIds = currentContentDetail.ContentPermissionSetIds.Select(i => i).ToList();

            // Assert
            Assert.True(!contentPermissionSetIds.Except(currentContentPermissionSetIds).Any());
            Assert.True(!currentContentPermissionSetIds.Except(contentPermissionSetIds).Any());
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldUpdatePermissionsMany()
        {
            // Arrange
            var contentId = _fixture.GetRandomContentId(".jpg", 20);
            var contentDetail = await _client.Contents.GetAsync(contentId);

            var contentPermissionSetIds = new List<string>
            {
                "aaa" + new Random().Next(0, 999),
                "bbb" + new Random().Next(0, 999)
            };

            var request = new ContentPermissionsUpdateRequest
			{
                ContentId = contentDetail.Id,
                ContentPermissionSetIds = contentPermissionSetIds
            };

            /// Act
            var businessProcess = await _client.Contents.UpdatePermissionsManyAsync(new List<ContentPermissionsUpdateRequest> { request });
            await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id);

            var currentContentDetail = await _client.Contents.GetAsync(contentId);
            var currentContentPermissionSetIds = currentContentDetail.ContentPermissionSetIds.Select(i => i).ToList();

            /// Assert
            Assert.True(!contentPermissionSetIds.Except(currentContentPermissionSetIds).Any());
            Assert.True(!currentContentPermissionSetIds.Except(contentPermissionSetIds).Any());
        }
    }
}
