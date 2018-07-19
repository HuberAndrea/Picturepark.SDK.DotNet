﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.IO;
using System.Net.Http;
using FluentAssertions;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Tests.Fixtures;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Picturepark.SDK.V1.Tests.Contracts;

namespace Picturepark.SDK.V1.Tests.Clients
{
    public class ContentTests : IClassFixture<ContentFixture>
    {
        private readonly ClientFixture _fixture;
        private readonly PictureparkClient _client;

        public ContentTests(ContentFixture fixture)
        {
            _fixture = fixture;
            _client = _fixture.Client;
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldTransferOwnership()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 50).ConfigureAwait(false);

            // Act
            var previousContent = await _client.Contents.GetAsync(contentId).ConfigureAwait(false);
            var previousOwner = await _client.Users.GetByOwnerTokenAsync(previousContent.OwnerTokenId).ConfigureAwait(false);
            var searchResult = await _client.Users.SearchAsync(new UserSearchRequest { Limit = 10, UserRightsFilter = new List<UserRight> { UserRight.ManageContent } }).ConfigureAwait(false);

            var newUser = searchResult.Results.First(u => u.Id != previousOwner.Id);
            var request = new ContentOwnershipTransferRequest
            {
                TransferUserId = newUser.Id
            };
            await _client.Contents.TransferOwnershipAsync(contentId, request).ConfigureAwait(false);

            var newContent = await _client.Contents.GetAsync(contentId).ConfigureAwait(false);
            var newOwner = await _client.Users.GetByOwnerTokenAsync(newContent.OwnerTokenId).ConfigureAwait(false);

            // Assert
            Assert.Equal(previousContent.Id, newContent.Id);
            Assert.NotEqual(previousContent.OwnerTokenId, newContent.OwnerTokenId);
            Assert.NotEqual(previousOwner.Id, newOwner.Id);
            Assert.Equal(newUser.Id, newOwner.Id);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldGetMany()
        {
            // Arrange
            var randomContents = await _fixture.GetRandomContentsAsync(".jpg", 2).ConfigureAwait(false);
            var contentIds = randomContents.Results.Select(i => i.Id).ToList();

            // Act
            var contents = await _client.Contents.GetManyAsync(contentIds).ConfigureAwait(false);

            // Assert
            Assert.Equal(2, contents.Count);
            Assert.Equal(contentIds[0], contents.ToList()[0].Id);
            Assert.Equal(contentIds[1], contents.ToList()[1].Id);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldTransferOwnershipMany()
        {
            // Arrange
            var randomContents = await _fixture.GetRandomContentsAsync(".jpg", 2).ConfigureAwait(false);
            var contentIds = randomContents.Results.Select(i => i.Id).ToList();

            // Act
            var previousContents = await _client.Contents.GetManyAsync(contentIds).ConfigureAwait(false);
            var previousOwner = await _client.Users.GetByOwnerTokenAsync(previousContents.ToList()[0].OwnerTokenId).ConfigureAwait(false);

            // Search user with ManageContent UserRight
            var searchResult = await _client.Users.SearchAsync(new UserSearchRequest
            {
                Limit = 10,
                UserRightsFilter = new List<UserRight> { UserRight.ManageContent }
            }).ConfigureAwait(false);

            var newUser = searchResult.Results.First(u => u.Id != previousOwner.Id);
            var manyRequest = new ContentOwnershipTransferManyRequest
            {
                Items = contentIds.Select(id => new ContentOwnershipTransferItem
                {
                    ContentId = id,
                    TransferUserId = newUser.Id
                }).ToList()
            };

            await _client.Contents.TransferOwnershipManyAsync(manyRequest).ConfigureAwait(false);

            var newContents = await _client.Contents.GetManyAsync(contentIds).ConfigureAwait(false);
            var newOwner1 = await _client.Users.GetByOwnerTokenAsync(newContents.ToList()[0].OwnerTokenId).ConfigureAwait(false);
            var newOwner2 = await _client.Users.GetByOwnerTokenAsync(newContents.ToList()[1].OwnerTokenId).ConfigureAwait(false);

            // Assert
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
            var ranges = new List<NumericRangeForAggregator>
            {
                new NumericRangeForAggregator { From = null, To = 499, Names = new TranslatedStringDictionary { { "en", "Aggregator2a" } } },
                new NumericRangeForAggregator { From = 500, To = 5000, Names = new TranslatedStringDictionary { { "en", "Aggregator2b" } } }
            };

            var numRangeAggregator = new NumericRangeAggregator()
            {
                Name = "NumberAggregator",
                Field = "Original.Width",
                Ranges = ranges
            };

            request.Aggregators.Add(numRangeAggregator);
            await _client.Contents.AggregateAsync(request).ConfigureAwait(false);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldAggregateByChannel()
        {
            // Arrange
            var channelId = "rootChannel";
            var request = new ContentAggregationRequest
            {
                ChannelId = channelId,
                SearchString = string.Empty
            };

            // Act
            var result = await _client.Contents.AggregateOnChannelAsync(request).ConfigureAwait(false);

            // Assert
            var originalWidthResults = result.AggregationResults
                .SingleOrDefault(i => i.Name == "Original Width");

            Assert.NotNull(originalWidthResults);
            Assert.True(originalWidthResults.AggregationResultItems.Count > 0);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldAggregateByChannelWithTermsAggregator()
        {
            // Arrange
            var channelId = "rootChannel";
            var request = new ContentAggregationRequest
            {
                ChannelId = channelId,
                SearchString = string.Empty,
                Aggregators = new List<AggregatorBase>
                {
                    new TermsAggregator { Name = "Permissions", Field = "permissionSetIds", Size = 10 }
                }
            };

            // Act
            var result = await _client.Contents.AggregateAsync(request).ConfigureAwait(false);

            // Assert
            var permissionSetResults = result.AggregationResults
                .SingleOrDefault(i => i.Name == "Permissions");

            Assert.NotNull(permissionSetResults);
            Assert.True(permissionSetResults.AggregationResultItems.Count > 0);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldCreateDownloadLinkForSingleFile()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 50).ConfigureAwait(false);
            var createDownloadLinkRequest = new ContentDownloadLinkCreateRequest
            {
                Contents = new List<ContentDownloadRequestItem>
                {
                    new ContentDownloadRequestItem { ContentId = contentId, OutputFormatId = "Original" }
                }
            };

            // Act
            var result = await _client.Contents.CreateDownloadLinkAsync(createDownloadLinkRequest).ConfigureAwait(false);
            Assert.NotNull(result.DownloadUrl);

            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(result.DownloadUrl).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var fileName = response.Content.Headers.ContentDisposition.FileName.Replace("\"", string.Empty);
                Assert.EndsWith(".jpg", fileName);

                var filePath = Path.Combine(_fixture.TempDirectory, fileName);

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var fileStream = File.Create(filePath))
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);

                    // Assert
                    Assert.True(stream.Length > 10);
                }
            }
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldCreateDownloadLinkForMultipeFiles()
        {
            // Arrange
            var randomContents = await _fixture.GetRandomContentsAsync(".jpg", 2).ConfigureAwait(false);
            var contentIds = randomContents.Results.Select(i => i.Id).ToList();

            var createDownloadLinkRequest = new ContentDownloadLinkCreateRequest
            {
                Contents = new List<ContentDownloadRequestItem>
                {
                    new ContentDownloadRequestItem { ContentId = contentIds[0], OutputFormatId = "Original" },
                    new ContentDownloadRequestItem { ContentId = contentIds[1], OutputFormatId = "Original" }
                }
            };

            // Act
            var result = await _client.Contents.CreateDownloadLinkAsync(createDownloadLinkRequest).ConfigureAwait(false);
            Assert.NotNull(result.DownloadUrl);

            using (var httpClient = new HttpClient())
            using (var response = await httpClient.GetAsync(result.DownloadUrl).ConfigureAwait(false))
            {
                response.EnsureSuccessStatusCode();

                var fileName = response.Content.Headers.ContentDisposition.FileName;
                Assert.EndsWith(".zip", fileName);

                var filePath = Path.Combine(_fixture.TempDirectory, fileName);

                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var fileStream = File.Create(filePath))
                {
                    await stream.CopyToAsync(fileStream).ConfigureAwait(false);

                    // Assert
                    Assert.True(stream.Length > 10);
                }
            }
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldCreateContent()
        {
            // Arrange
            var request = new ContentCreateRequest
            {
                Content = JsonConvert.DeserializeObject(@"{ ""name"": ""foo"" }"),
                ContentSchemaId = "ContentItem",
                Metadata = new DataDictionary()
            };

            // Act
            var result = await _client.Contents.CreateAsync(request).ConfigureAwait(false);

            // Assert
            Assert.NotNull(result);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldCreateContents()
        {
            // Arrange
            var request1 = new ContentCreateRequest
            {
                Content = JsonConvert.DeserializeObject(@"{ ""name"": ""foo"" }"),
                ContentSchemaId = "ContentItem",
                Metadata = new DataDictionary()
            };

            var request2 = new ContentCreateRequest
            {
                Content = JsonConvert.DeserializeObject(@"{ ""name"": ""bar"" }"),
                ContentSchemaId = "ContentItem",
                Metadata = new DataDictionary()
            };

            // Act
            var result = await _client.Contents.CreateManyAsync(new ContentCreateManyRequest
            {
                AllowMissingDependencies = false,
                Items = new List<ContentCreateRequest> { request1, request2 }
            }).ConfigureAwait(false);

            var detail = await result.FetchDetail().ConfigureAwait(false);

            // Assert
            detail.FailedItems.Should().BeNullOrEmpty();
            detail.SucceededItems.Should().HaveCount(2);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldDownloadMultiple()
        {
            int maxNumberOfDownloadFiles = 3;
            string searchString = string.Empty;

            ContentSearchResult result = await _fixture.GetRandomContentsAsync(searchString, maxNumberOfDownloadFiles).ConfigureAwait(false);
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
                }).ConfigureAwait(false);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldDownloadSingle()
        {
            string contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            Assert.False(string.IsNullOrEmpty(contentId));
            ContentDetail contentDetail = await _client.Contents.GetAsync(contentId, new[] { ContentResolveBehaviour.Content }).ConfigureAwait(false);

            var fileMetadata = contentDetail.GetFileMetadata();
            var fileName = new Random().Next(0, 999999) + "-" + fileMetadata.FileName + ".jpg";
            var filePath = Path.Combine(_fixture.TempDirectory, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var response = await _client.Contents.DownloadAsync(contentId, "Original", null, null, "bytes=0-20000000").ConfigureAwait(false))
            {
                var stream = response.Stream;
                Assert.True(stream.CanRead);

                await response.Stream.WriteToFileAsync(filePath).ConfigureAwait(false);
                Assert.True(File.Exists(filePath));
            }
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldDownloadThumbnail()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);

            // Act
            using (var response = await _client.Contents.DownloadThumbnailAsync(contentId, ThumbnailSize.Medium).ConfigureAwait(false))
            {
                var stream = new MemoryStream();
                await response.Stream.CopyToAsync(stream).ConfigureAwait(false);

                // Assert
                Assert.True(stream.Length > 10);
                Assert.True(stream.CanRead);
            }
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldUpdateMetadata()
        {
            // Arrange
            var expectedName = "test" + new Random().Next(0, 999999);
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            var request = new ContentMetadataUpdateRequest
            {
                LayerSchemaIds = new List<string> { nameof(SimpleLayer) },
                Metadata = new DataDictionary
                {
                    {
                        nameof(SimpleLayer).ToLowerCamelCase(),
                        new Dictionary<string, object>
                        {
                            { "name", expectedName }
                        }
                    }
                }
            };

            // Act
            var response = await _client.Contents.UpdateMetadataAsync(contentId, request, new[] { ContentResolveBehaviour.Metadata }).ConfigureAwait(false);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(expectedName, response.Metadata.Get(nameof(SimpleLayer).ToLowerCamelCase())["name"]);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldUpdateMetadataMany()
        {
            // Arrange
            var randomContents = await _fixture.GetRandomContentsAsync(".jpg", 2).ConfigureAwait(false);
            var contentIds = randomContents.Results.Select(i => i.Id).ToList();

            var request1 = new ContentMetadataUpdateItem
            {
                Id = contentIds[0],
                LayerSchemaIds = new List<string> { nameof(SimpleLayer) },
                Metadata = new DataDictionary
                {
                    {
                        nameof(SimpleLayer),
                        new Dictionary<string, object>
                        {
                            { "name", "Content1" }
                        }
                    }
                }
            };

            var request2 = new ContentMetadataUpdateItem
            {
                Id = contentIds[1],
                LayerSchemaIds = new List<string> { nameof(SimpleLayer) },
                Metadata = new DataDictionary
                {
                    {
                        nameof(SimpleLayer),
                        new Dictionary<string, object>
                        {
                            { "name", "Content2" }
                        }
                    }
                }
            };

            // Act
            var result = await _client.Contents.UpdateMetadataManyAsync(new ContentMetadataUpdateManyRequest
            {
                AllowMissingDependencies = false,
                Items = new List<ContentMetadataUpdateItem> { request1, request2 }
            }).ConfigureAwait(false);

            // Assert
            Assert.Equal(BusinessProcessLifeCycle.Succeeded, result.LifeCycle);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldSetLayerAndResolveDisplayValues()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            var request = new ContentMetadataUpdateRequest
            {
                LayerSchemaIds = new List<string> { "PersonShot" },
                Metadata = new DataDictionary
                {
                    {
                        "PersonShot",
                        new Dictionary<string, object>
                        {
                            { "Description", "test description" }
                        }
                    }
                }
            };

            // Act
            var response = await _client.Contents.UpdateMetadataAsync(contentId, request, new[] { ContentResolveBehaviour.Metadata, ContentResolveBehaviour.InnerDisplayValueName }).ConfigureAwait(false);

            // Assert
            Assert.Equal("test description", ((JObject)response.Metadata["personShot"])["displayValue"]["name"].ToString());
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldMergeLayersOnMetadataUpdate()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            var request = new ContentMetadataUpdateRequest
            {
                LayerSchemaIds = new List<string> { nameof(PersonShot) },
                Metadata = new DataDictionary
                {
                    {
                        nameof(PersonShot),
                        new Dictionary<string, object>
                        {
                            { "Description", "test description" }
                        }
                    }
                }
            };

            await _client.Contents.UpdateMetadataAsync(contentId, request).ConfigureAwait(false);

            request = new ContentMetadataUpdateRequest
            {
                LayerSchemaIds = new List<string> { nameof(AllDataTypesContract) },
                Metadata = new DataDictionary
                {
                    {
                        nameof(AllDataTypesContract),
                        new Dictionary<string, object>
                        {
                            { "IntegerField", 12345 }
                        }
                    }
                },
                LayerSchemasUpdateOptions = UpdateOption.Merge
            };

            // Act
            var response = await _client.Contents.UpdateMetadataAsync(contentId, request, new[] { ContentResolveBehaviour.Metadata }).ConfigureAwait(false);

            // Assert
            Assert.Equal("test description", ((JObject)response.Metadata["personShot"])["description"].ToString());
            Assert.Equal(12345, ((JObject)response.Metadata["allDataTypesContract"])["integerField"].ToObject<int>());
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldReplaceLayersOnMetadataUpdate()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            var request = new ContentMetadataUpdateRequest
            {
                LayerSchemaIds = new List<string> { nameof(PersonShot) },
                Metadata = new DataDictionary
                {
                    {
                        nameof(PersonShot),
                        new Dictionary<string, object>
                        {
                            { "Description", "test description" }
                        }
                    }
                }
            };

            await _client.Contents.UpdateMetadataAsync(contentId, request).ConfigureAwait(false);

            request = new ContentMetadataUpdateRequest
            {
                LayerSchemaIds = new List<string> { nameof(AllDataTypesContract) },
                Metadata = new DataDictionary
                {
                    {
                        nameof(AllDataTypesContract),
                        new Dictionary<string, object>
                        {
                            { "IntegerField", 12345 }
                        }
                    }
                },
                LayerSchemasUpdateOptions = UpdateOption.Replace
            };

            // Act
            var response = await _client.Contents.UpdateMetadataAsync(contentId, request, new[] { ContentResolveBehaviour.Metadata }).ConfigureAwait(false);

            // Assert
            Assert.DoesNotContain("personShot", response.Metadata.Keys);
            Assert.Equal(12345, ((JObject)response.Metadata["allDataTypesContract"])["integerField"].ToObject<int>());
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldMergeFieldsOnMetadataUpdate()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            var request = new ContentMetadataUpdateRequest
            {
                LayerSchemaIds = new List<string> { nameof(AllDataTypesContract) },
                Metadata = new DataDictionary
                {
                    {
                        nameof(AllDataTypesContract),
                        new Dictionary<string, object>
                        {
                            { "IntegerField", 12345 }
                        }
                    }
                }
            };

            await _client.Contents.UpdateMetadataAsync(contentId, request).ConfigureAwait(false);

            request = new ContentMetadataUpdateRequest
            {
                LayerSchemaIds = new List<string> { nameof(AllDataTypesContract) },
                Metadata = new DataDictionary
                {
                    {
                        nameof(AllDataTypesContract),
                        new Dictionary<string, object>
                        {
                            { "StringField", "test string" }
                        }
                    }
                },
                SchemaFieldsUpdateOptions = UpdateOption.Merge
            };

            // Act
            var response = await _client.Contents.UpdateMetadataAsync(contentId, request, new[] { ContentResolveBehaviour.Metadata }).ConfigureAwait(false);

            // Assert
            Assert.Equal(12345, ((JObject)response.Metadata["allDataTypesContract"])["integerField"].ToObject<int>());
            Assert.Equal("test string", ((JObject)response.Metadata["allDataTypesContract"])["stringField"].ToString());
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldReplaceFieldsOnMetadataUpdate()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            var request = new ContentMetadataUpdateRequest
            {
                LayerSchemaIds = new List<string> { nameof(AllDataTypesContract) },
                Metadata = new DataDictionary
                {
                    {
                        nameof(AllDataTypesContract),
                        new Dictionary<string, object>
                        {
                            { "IntegerField", 12345 }
                        }
                    }
                }
            };

            await _client.Contents.UpdateMetadataAsync(contentId, request).ConfigureAwait(false);

            request = new ContentMetadataUpdateRequest
            {
                LayerSchemaIds = new List<string> { nameof(AllDataTypesContract) },
                Metadata = new DataDictionary
                {
                    {
                        nameof(AllDataTypesContract),
                        new Dictionary<string, object>
                        {
                            { "StringField", "test string" }
                        }
                    }
                },
                SchemaFieldsUpdateOptions = UpdateOption.Replace
            };

            // Act
            var response = await _client.Contents.UpdateMetadataAsync(contentId, request, new[] { ContentResolveBehaviour.Metadata }).ConfigureAwait(false);

            // Assert
            Assert.Null(((JObject)response.Metadata["allDataTypesContract"])["integerField"]);
            Assert.Equal("test string", ((JObject)response.Metadata["allDataTypesContract"])["stringField"].ToString());
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldBatchUpdateFieldsByFilter()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            var request = new ContentFieldsBatchUpdateFilterRequest
            {
                ContentFilterRequest = new ContentFilterRequest
                {
                    ChannelId = "rootChannel",
                    Filter = new TermFilter { Field = "id", Term = contentId }
                },
                ChangeCommands = new List<MetadataValuesChangeCommandBase>
                {
                    new MetadataValuesSchemaUpsertCommand
                    {
                        SchemaId = nameof(SimpleLayer),
                        Value = new DataDictionary
                        {
                            { "name", "testlocation" }
                        }
                    }
                }
            };

            // Act
            var result = await _client.Contents.BatchUpdateFieldsByFilterAsync(request).ConfigureAwait(false);

            // Assert
            Assert.True(result.LifeCycle == BusinessProcessLifeCycle.Succeeded);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldBatchUpdateFieldsByIds()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);

            var content = await _client.Contents.GetAsync(contentId).ConfigureAwait(false);
            var updateRequest = new ContentFieldsBatchUpdateRequest
            {
                ContentIds = new List<string> { content.Id },
                ChangeCommands = new List<MetadataValuesChangeCommandBase>
                {
                    new MetadataValuesSchemaUpsertCommand
                    {
                        SchemaId = nameof(SimpleLayer),
                        Value = new DataDictionary
                        {
                            { "name", "testlocation" }
                        }
                    }
                }
            };

            // Act
            var result = await _client.Contents.BatchUpdateFieldsByIdsAsync(updateRequest).ConfigureAwait(false);

            // Assert
            Assert.True(result.LifeCycle == BusinessProcessLifeCycle.Succeeded);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldThrowExceptionWhenContentNotFound()
        {
            var contentId = "foobar.baz";
            await Assert.ThrowsAsync<ContentNotFoundException>(async () =>
            {
                await _client.Contents.GetAsync(contentId).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldDownloadSingleResized()
        {
            // Download a resized version of an image file
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);

            Assert.False(string.IsNullOrEmpty(contentId));
            ContentDetail contentDetail = await _client.Contents.GetAsync(contentId, new[] { ContentResolveBehaviour.Content }).ConfigureAwait(false);

            var fileMetadata = contentDetail.GetFileMetadata();
            var fileName = new Random().Next(0, 999999) + "-" + fileMetadata.FileName + ".jpg";
            var filePath = Path.Combine(_fixture.TempDirectory, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var response = await _client.Contents.DownloadAsync(contentId, "Original", 200, 200).ConfigureAwait(false))
            {
                await response.Stream.WriteToFileAsync(filePath).ConfigureAwait(false);
            }

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldDownloadSingleThumbnail()
        {
            // Download a resized version of an image file
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            Assert.False(string.IsNullOrEmpty(contentId));

            var fileName = new Random().Next(0, 999999) + "-" + contentId + ".jpg";
            var filePath = Path.Combine(_fixture.TempDirectory, fileName);

            if (File.Exists(filePath))
                File.Delete(filePath);

            using (var response = await _client.Contents.DownloadThumbnailAsync(contentId, ThumbnailSize.Small).ConfigureAwait(false))
            {
                await response.Stream.WriteToFileAsync(filePath).ConfigureAwait(false);
            }

            Assert.True(File.Exists(filePath));
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldGet()
        {
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            Assert.False(string.IsNullOrEmpty(contentId));

            ContentDetail result = await _client.Contents.GetAsync(contentId, new[] { ContentResolveBehaviour.InnerDisplayValueList }).ConfigureAwait(false);
            Assert.NotNull(result.Id);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldGetDocumentMetadata()
        {
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);

            if (string.IsNullOrEmpty(contentId))
                contentId = await _fixture.GetRandomContentIdAsync(".docx", 20).ConfigureAwait(false);

            Assert.False(string.IsNullOrEmpty(contentId));

            ContentDetail result = await _client.Contents.GetAsync(contentId, new[] { ContentResolveBehaviour.Content }).ConfigureAwait(false);

            FileMetadata fileMetadata = result.GetFileMetadata();
            Assert.False(string.IsNullOrEmpty(fileMetadata.FileName));
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldGetWithResolvedObjects()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);

            // Act
            var contentDetail = await _client.Contents.GetAsync(contentId, new[] { ContentResolveBehaviour.Metadata, ContentResolveBehaviour.LinkedListItems }).ConfigureAwait(false);

            // Assert
            Assert.Equal(contentId, contentDetail.Id);
            Assert.NotNull(contentDetail); // TODO: Add better asserts
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldGetWithoutResolvedObjects()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);

            // Act
            var contentDetail = await _client.Contents.GetAsync(contentId).ConfigureAwait(false);

            // Assert
            Assert.Equal(contentId, contentDetail.Id);
            Assert.NotNull(contentDetail); // TODO: Add better asserts
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldSearch()
        {
            // Arrange
            var channelId = "rootChannel";
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
                ChannelId = channelId,
                SearchString = "*",
                Sort = sortInfos,
                Filter = filter,
                Start = 0
            };

            // Act
            ContentSearchResult result = await _client.Contents.SearchAsync(request).ConfigureAwait(false);

            // Assert
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
                ChannelId = channelId,
                SearchString = searchString,
                Sort = sortInfos,
                Start = 0,
                Limit = 8
            };

            ContentSearchResult result = await _client.Contents.SearchAsync(request).ConfigureAwait(false);
            Assert.True(result.Results.Count > 0);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldDeactivateAndReactivateContent()
        {
            // Arrange
            var request = new ContentCreateRequest
            {
                Content = JsonConvert.DeserializeObject(@"{ ""name"": ""contentToTrash"" }"),
                ContentSchemaId = "ContentItem",
                Metadata = new DataDictionary()
            };

            // Act
            var content = await _client.Contents.CreateAsync(request).ConfigureAwait(false);

            // Deactivate
            await _client.Contents.DeactivateAsync(content.Id).ConfigureAwait(false);
            await Assert.ThrowsAsync<ContentNotFoundException>(async () => await _client.Contents.GetAsync(content.Id).ConfigureAwait(false)).ConfigureAwait(false);

            // Reactivate
            await _client.Contents.ReactivateAsync(content.Id, timeout: TimeSpan.FromMinutes(1)).ConfigureAwait(false);

            // Assert
            Assert.NotNull(await _client.Contents.GetAsync(content.Id).ConfigureAwait(false));
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldDeactivateAndReactivateContentMany()
        {
            // Arrange
            var content1 = await _client.Contents.CreateAsync(new ContentCreateRequest
            {
                Content = JsonConvert.DeserializeObject(@"{ ""name"": ""contentToTrashMany1"" }"),
                ContentSchemaId = "ContentItem",
                Metadata = new DataDictionary()
            }).ConfigureAwait(false);

            var content2 = await _client.Contents.CreateAsync(new ContentCreateRequest
            {
                Content = JsonConvert.DeserializeObject(@"{ ""name"": ""contentToTrashMany2"" }"),
                ContentSchemaId = "ContentItem",
                Metadata = new DataDictionary()
            }).ConfigureAwait(false);

            var contentIds = new List<string> { content1.Id, content2.Id };

            // Deactivate
            var deactivationRequest = new ContentDeactivateRequest
            {
                ContentIds = contentIds
            };

            var businessProcess = await _client.Contents.DeactivateManyAsync(deactivationRequest).ConfigureAwait(false);
            await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id).ConfigureAwait(false);

            await Assert.ThrowsAsync<ContentNotFoundException>(async () => await _client.Contents.GetAsync(contentIds[0]).ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ContentNotFoundException>(async () => await _client.Contents.GetAsync(contentIds[1]).ConfigureAwait(false)).ConfigureAwait(false);

            // Reactivate
            var reactivateRequest = new ContentReactivateRequest
            {
                ContentIds = contentIds
            };

            businessProcess = await _client.Contents.ReactivateManyAsync(reactivateRequest).ConfigureAwait(false);
            await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id).ConfigureAwait(false);

            // Assert
            Assert.NotNull(await _client.Contents.GetAsync(contentIds[0]).ConfigureAwait(false));
            Assert.NotNull(await _client.Contents.GetAsync(contentIds[1]).ConfigureAwait(false));
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldUpdateFile()
        {
            string contentId = await _fixture.GetRandomContentIdAsync(".jpg -0030_JabLtzJl8bc", 20).ConfigureAwait(false);

            // Create transfer
            var filePaths = new FileLocations[]
            {
                Path.Combine(_fixture.ExampleFilesBasePath, "0030_JabLtzJl8bc.jpg")
            };
            string transferName = nameof(ShouldUpdateFile) + "-" + new Random().Next(1000, 9999);
            var createTransferResult = await _client.Transfers.CreateAndWaitForCompletionAsync(transferName, filePaths).ConfigureAwait(false);

            // Upload file
            var uploadOptions = new UploadOptions
            {
                SuccessDelegate = Console.WriteLine,
                ErrorDelegate = Console.WriteLine
            };

            await _client.Transfers.UploadFilesAsync(createTransferResult.Transfer, filePaths, uploadOptions).ConfigureAwait(false);

            // Search filetransfers to get id
            var request = new FileTransferSearchRequest() { Limit = 20, SearchString = "*", Filter = new TermFilter { Field = "transferId", Term = createTransferResult.Transfer.Id } };
            FileTransferSearchResult result = await _client.Transfers.SearchFilesAsync(request).ConfigureAwait(false);

            Assert.Equal(1, result.TotalResults);

            var updateRequest = new ContentFileUpdateRequest
            {
                FileTransferId = result.Results.First().Id
            };

            var businessProcess = await _client.Contents.UpdateFileAsync(contentId, updateRequest).ConfigureAwait(false);
            var waitResult = await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id).ConfigureAwait(false);

            Assert.True(waitResult.LifeCycleHit == BusinessProcessLifeCycle.Succeeded);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldUpdatePermissions()
        {
            // Arrange
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            var contentDetail = await _client.Contents.GetAsync(contentId).ConfigureAwait(false);
            var permissionSetId = await _fixture.GetRandomContentPermissionSetIdAsync(20).ConfigureAwait(false);

            var contentPermissionSetIds = new List<string>
            {
                permissionSetId
            };

            var request = new ContentPermissionsUpdateRequest
            {
                ContentPermissionSetIds = contentPermissionSetIds
            };

            // Act
            await _client.Contents.UpdatePermissionsAsync(contentDetail.Id, request).ConfigureAwait(false);

            var currentContentDetail = await _client.Contents.GetAsync(contentId).ConfigureAwait(false);
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
            var contentId = await _fixture.GetRandomContentIdAsync(".jpg", 20).ConfigureAwait(false);
            var contentDetail = await _client.Contents.GetAsync(contentId).ConfigureAwait(false);
            var permissionSetId = await _fixture.GetRandomContentPermissionSetIdAsync(20).ConfigureAwait(false);

            var contentPermissionSetIds = new List<string>
            {
                permissionSetId
            };

            var manyRequest = new ContentPermissionsUpdateManyRequest
            {
                Items = new List<ContentPermissionsUpdateItem>
                {
                    new ContentPermissionsUpdateItem
                    {
                        ContentId = contentDetail.Id,
                        ContentPermissionSetIds = contentPermissionSetIds
                    }
                }
            };

            // Act
            await _client.Contents.UpdatePermissionsManyAsync(manyRequest).ConfigureAwait(false);

            var currentContentDetail = await _client.Contents.GetAsync(contentId).ConfigureAwait(false);
            var currentContentPermissionSetIds = currentContentDetail.ContentPermissionSetIds.Select(i => i).ToList();

            // Assert
            currentContentPermissionSetIds.Should().BeEquivalentTo(contentPermissionSetIds);
        }

        [Fact]
        [Trait("Stack", "Contents")]
        public async Task ShouldUseDisplayLanguageToResolveDisplayPatterns()
        {
            // Arrange
            var schemaId = $"DisplayLanguageContentSchema{Guid.NewGuid():N}";
            var contentSchema = new SchemaDetail
            {
                Id = schemaId,
                Types = new List<SchemaType> { SchemaType.Content },
                Fields = new List<FieldBase>
                {
                    new FieldString { Id = "value1" },
                    new FieldString { Id = "value2" }
                },
                DisplayPatterns = new List<DisplayPattern>
                {
                    new DisplayPattern
                    {
                        DisplayPatternType = DisplayPatternType.Name,
                        TemplateEngine = TemplateEngine.DotLiquid,
                        Templates = new TranslatedStringDictionary
                        {
                            { "en", $"{{{{data.{schemaId.ToLowerCamelCase()}.value1}}}}" },
                            { "de", $"{{{{data.{schemaId.ToLowerCamelCase()}.value2}}}}" }
                        }
                    }
                }
            };

            await _client.Schemas.CreateAsync(contentSchema, TimeSpan.FromMinutes(1)).ConfigureAwait(false);

            var content = new ContentCreateRequest
            {
                ContentSchemaId = schemaId,
                Content = new
                {
                    value1 = "value1",
                    value2 = "value2"
                }
            };

            var detail = await _client.Contents.CreateAsync(content).ConfigureAwait(false);

            // Act
            var englishClient = _fixture.GetLocalizedPictureparkClient("en");
            var englishContent = await englishClient.Contents.GetAsync(detail.Id, new[] { ContentResolveBehaviour.Content }).ConfigureAwait(false);

            var germanClient = _fixture.GetLocalizedPictureparkClient("de");
            var germanContent = await germanClient.Contents.GetAsync(detail.Id, new[] { ContentResolveBehaviour.Content }).ConfigureAwait(false);

            // Assert
            englishContent.DisplayValues[DisplayPatternType.Name.ToString().ToLowerCamelCase()].Should().Be("value1");
            germanContent.DisplayValues[DisplayPatternType.Name.ToString().ToLowerCamelCase()].Should().Be("value2");
        }
    }
}
