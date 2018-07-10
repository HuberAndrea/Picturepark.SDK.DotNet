﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;
using Picturepark.SDK.V1.Tests.Contracts;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Tests.Fixtures;

namespace Picturepark.SDK.V1.Tests.Clients
{
    public class ListItemTests : IClassFixture<ListItemFixture>
    {
        private readonly ClientFixture _fixture;
        private readonly PictureparkClient _client;

        public ListItemTests(ListItemFixture fixture)
        {
            _fixture = fixture;
            _client = _fixture.Client;
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldAggregateObjects()
        {
            /// Arrange
            var fieldName = nameof(Country).ToLowerCamelCase() + "." + nameof(Country.RegionCode).ToLowerCamelCase();
            var request = new ListItemAggregationRequest
            {
                SearchString = "*",
                SchemaIds = new List<string> { nameof(Country) },
                Aggregators = new List<AggregatorBase>
                {
                    new TermsAggregator { Name = fieldName, Field = fieldName, Size = 20 }
                }
            };

            /// Act
            ObjectAggregationResult result = await _client.ListItems.AggregateAsync(request);
            AggregationResult aggregation = result.GetByName(fieldName);

            /// Assert
            Assert.NotNull(aggregation);
            Assert.Equal(20, aggregation.AggregationResultItems.Count);
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldCreateAndUpdateObject()
        {
            /// Arrange
            var objectName = "ThisObjectD" + new Random().Next(0, 999999);
            var createManyRequest = new ListItemCreateManyRequest()
            {
                AllowMissingDependencies = false,
                Requests = new List<ListItemCreateRequest>
                {
                    new ListItemCreateRequest
                    {
                        ContentSchemaId = nameof(Tag),
                        Content = new Tag { Name = objectName }
                    }
                }
            };

            var results = await _client.ListItems.CreateManyAsync(createManyRequest).ConfigureAwait(false);
            var result = results.First();

            /// Act
            var request = new ListItemUpdateRequest
            {
                Id = result.Id,
                Content = new Tag { Name = "Foo" }
            };
            await _client.ListItems.UpdateAsync(request).ConfigureAwait(false);

            /// Assert
            var newItem = await _client.ListItems.GetAsync(result.Id, new ListItemResolveBehaviour[] { ListItemResolveBehaviour.Content }).ConfigureAwait(false);
            Assert.Equal("Foo", newItem.ConvertTo<Tag>(nameof(Tag)).Name);
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldCreate()
        {
            /// Arrange
            var objectName = "ThisObjectB" + new Random().Next(0, 999999);
            var listItem = new ListItemCreateRequest
            {
                ContentSchemaId = nameof(Tag),
                Content = new Tag { Name = objectName }
            };

            /// Act
            ListItemDetail result = await _client.ListItems.CreateAsync(listItem);

            /// Assert
            Assert.False(string.IsNullOrEmpty(result.Id));
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldBatchUpdateFieldsByFilter()
        {
            /// Arrange
            var objectName = "ThisObjectB" + new Random().Next(0, 999999);
            var listItem = new ListItemCreateRequest
            {
                ContentSchemaId = nameof(Tag),
                Content = new Tag { Name = objectName }
            };
            ListItemDetail listItemDetail = await _client.ListItems.CreateAsync(listItem).ConfigureAwait(false);

            /// Act
            var updateRequest = new ListItemFieldsBatchUpdateFilterRequest
            {
                ListItemFilterRequest = new ListItemFilterRequest // TODO: ListItemFieldsFilterUpdateRequest.ListItemFilterRequest: Rename property to FilterRequest?
                {
                    Filter = new TermFilter { Field = "id", Term = listItemDetail.Id }
                },
                ChangeCommands = new List<MetadataValuesChangeCommandBase>
                {
                    new MetadataValuesSchemaUpdateCommand
                    {
                        SchemaId = nameof(Tag),
                        Value = new DataDictionary
                        {
                            { "name", "Foo" }
                        }
                    }
                }
            };

            /// Act
            var businessProcess = await _client.ListItems.BatchUpdateFieldsByFilterAsync(updateRequest).ConfigureAwait(false);
            var waitResult = await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id, TimeSpan.FromSeconds(10)).ConfigureAwait(false);

            ListItemDetail result = await _client.ListItems.GetAsync(listItemDetail.Id, new ListItemResolveBehaviour[] { ListItemResolveBehaviour.Content }).ConfigureAwait(false);

            /// Assert
            Assert.Equal("Foo", result.ConvertTo<Tag>(nameof(Tag)).Name);
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldCreateWithHelper()
        {
            /// Arrange
            var tag = new Tag
            {
                Name = "ThisObjectB" + new Random().Next(0, 999999)
            };

            /// Act
            var createdObjects = await _client.ListItems.CreateFromObjectAsync(tag, nameof(Tag));

            /// Assert
            Assert.Single(createdObjects);
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldCreateComplexObjectWithHelper()
        {
            /// Arrange
            await SchemaHelper.CreateSchemasIfNotExistentAsync<Person>(_client);

            // Reusable as reference
            var dog = new Dog
            {
                Name = "Dogname1",
                PlaysCatch = true
            };

            /// Act
            // Using Helper method
            var soccerPlayerTree = await _client.ListItems.CreateFromObjectAsync(
                new SoccerPlayer
                {
                    BirthDate = DateTime.Now,
                    EmailAddress = "xyyyy@teyyyyyyst.com",
                    Firstname = "Urxxxxs",
                    LastName = "xxxxxxxx",
                    Addresses = new List<Addresses>
                    {
                        new Addresses
                        {
                            Name = "Aarau",
                            SecurityPet = dog
                        }
                    },
                    OwnsPets = new List<Pet>
                    {
                        new Cat
                        {
                            Name = "Catname1",
                            ChasesLaser = true
                        },
                        dog
                    }
                }, nameof(SoccerPlayer)); // TODO: ListItemClient.CreateFromObjectAsync: We should add an attribute to the class with its schema name instead of passing it as parameter

            var soccerTrainerTree = await _client.ListItems.CreateFromObjectAsync(
                new SoccerTrainer
                {
                    BirthDate = DateTime.Now,
                    EmailAddress = "xyyyy@teyyyyyyst.com",
                    Firstname = "Urxxxxs",
                    LastName = "xxxxxxxx",
                    TrainerSince = new DateTime(2000, 1, 1)
                }, nameof(SoccerTrainer));

            var personTree = await _client.ListItems.CreateFromObjectAsync(
                new Person
                {
                    BirthDate = DateTime.Now,
                    EmailAddress = "xyyyy@teyyyyyyst.com",
                    Firstname = "Urxxxxs",
                    LastName = "xxxxxxxx"
                }, nameof(Person));

            /// Assert
            Assert.True(soccerPlayerTree.Any());
            Assert.True(soccerTrainerTree.Any());
            Assert.True(personTree.Any());
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldCreateObjectWithoutHelper()
        {
            /// Arrange
            await SchemaHelper.CreateSchemasIfNotExistentAsync<Person>(_client).ConfigureAwait(false);

            var originalPlayer = new SoccerPlayer
            {
                BirthDate = DateTime.Now,
                EmailAddress = "test@test.com",
                Firstname = "Urs",
                LastName = "Brogle"
            };

            /// Act
            var createRequest = new ListItemCreateRequest
            {
                ContentSchemaId = nameof(SoccerPlayer),
                Content = originalPlayer
            };

            var playerItem = await _client.ListItems.CreateAsync(createRequest, new ListItemResolveBehaviour[] { ListItemResolveBehaviour.Content }).ConfigureAwait(false);

            /// Assert
            Assert.NotNull(playerItem);

            var createdPlayer = playerItem.ConvertTo<SoccerPlayer>("SoccerPlayer");
            Assert.Equal("Urs", createdPlayer.Firstname);
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldGetObject()
        {
            /// Arrange
            var objectName = "ThisObjectC" + new Random().Next(0, 999999);

            /// Act
            var createRequest = new ListItemCreateRequest
            {
                ContentSchemaId = nameof(Tag),
                Content = new Tag { Name = objectName }
            };

            var listItem = await _client.ListItems.CreateAsync(createRequest).ConfigureAwait(false);
            var result = await _client.ListItems.GetAsync(listItem.Id).ConfigureAwait(false);

            /// Assert
            Assert.NotNull(result);
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldGetObjectResolved()
        {
            /// Arrange
            var request = new SchemaSearchRequest
            {
                Limit = 100,
                Filter = new TermFilter
                {
                    Field = "types",
                    Term = SchemaType.List.ToString()
                }
            };

            var result = await _client.Schemas.SearchAsync(request).ConfigureAwait(false);
            Assert.True(result.Results.Any());

            // For debugging: .Where(i => i.Id == "Esselabore1"))
            var tuple = result.Results
                .Select(i => new { schemaId = i.Id, objectId = _fixture.GetRandomObjectIdAsync(i.Id, 20).Result })
                .First(i => !string.IsNullOrEmpty(i.objectId));

            /// Act
            var listItem = await _client.ListItems.GetAsync(tuple.objectId, new ListItemResolveBehaviour[] { ListItemResolveBehaviour.Content, ListItemResolveBehaviour.LinkedListItems }).ConfigureAwait(false);

            /// Assert
            Assert.Equal(tuple.schemaId, listItem.ContentSchemaId);
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldGetManyObjects()
        {
            // Arrange
            var objectName1 = "ThisObjectC" + new Random().Next(0, 999999);
            var objectName2 = "ThisObjectC" + new Random().Next(0, 999999);

            var createRequest = new ListItemCreateManyRequest()
            {
                Requests = new List<ListItemCreateRequest>
                {
                    new ListItemCreateRequest
                    {
                        ContentSchemaId = nameof(Tag),
                        Content = new Tag { Name = objectName1 }
                    },
                    new ListItemCreateRequest
                    {
                        ContentSchemaId = nameof(Tag),
                        Content = new Tag { Name = objectName2 }
                    }
                }
            };

            var createdListItems = await _client.ListItems.CreateManyAsync(createRequest).ConfigureAwait(false);

            // Act
            var resultListItems = await _client.ListItems.GetManyAsync(createdListItems.Select(li => li.Id), new List<ListItemResolveBehaviour> { ListItemResolveBehaviour.Content }).ConfigureAwait(false);

            // Assert
            resultListItems.Should().NotBeNull().And.HaveCount(2);
            resultListItems.Select(li => li.Id).Should().BeEquivalentTo(createdListItems.ElementAt(0).Id, createdListItems.ElementAt(1).Id);
            resultListItems.Select(li => li.Content.As<Newtonsoft.Json.Linq.JObject>()["name"].ToString()).Should().BeEquivalentTo(objectName1, objectName2);
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldGetManyObjectsConverted()
        {
            // Arrange
            var objectName1 = "ThisObjectC" + new Random().Next(0, 999999);
            var objectName2 = "ThisObjectC" + new Random().Next(0, 999999);

            var createRequest = new ListItemCreateManyRequest()
            {
                Requests = new List<ListItemCreateRequest>
                {
                    new ListItemCreateRequest
                    {
                        ContentSchemaId = nameof(Tag),
                        Content = new Tag { Name = objectName1 }
                    },
                    new ListItemCreateRequest
                    {
                        ContentSchemaId = nameof(Tag),
                        Content = new Tag { Name = objectName2 }
                    }
                }
            };

            var createdListItems = await _client.ListItems.CreateManyAsync(createRequest).ConfigureAwait(false);

            // Act
            var resultListItems = await _client.ListItems.GetManyAndConvertToAsync<Tag>(createdListItems.Select(li => li.Id), nameof(Tag)).ConfigureAwait(false);

            // Assert
            resultListItems.Should().NotBeNull().And.HaveCount(2);
            resultListItems.Select(li => li.Name).Should().BeEquivalentTo(objectName1, objectName2);
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldSearchObjects()
        {
            /// Arrange
            // ---------------------------------------------------------------------------
            // Get a list of MetadataSchemaIds
            // ---------------------------------------------------------------------------
            var searchRequestSchema = new SchemaSearchRequest
            {
                Start = 0,
                Limit = 999,
                Filter = new TermFilter
                {
                    Field = "types",
                    Term = SchemaType.List.ToString()
                }
            };

            var searchResultSchema = await _client.Schemas.SearchAsync(searchRequestSchema);
            Assert.True(searchResultSchema.Results.Any());

            List<string> metadataSchemaIds = searchResultSchema.Results
                .Select(i => i.Id)
                .OrderBy(i => i)
                .ToList();

            var searchRequestObject = new ListItemSearchRequest() { Start = 0, Limit = 100 };
            var items = new List<ListItem>();
            List<string> failedMetadataSchemaIds = new List<string>();

            /// Act
            // ---------------------------------------------------------------------------
            // Loop over all metadataSchemaIds and make a search for each metadataSchemaId
            // ---------------------------------------------------------------------------
            foreach (var metadataSchemaId in metadataSchemaIds)
            {
                searchRequestObject.SchemaIds = new List<string> { metadataSchemaId };

                try
                {
                    var searchResultObject = await _client.ListItems.SearchAsync(searchRequestObject);
                    if (searchResultObject.Results.Any())
                    {
                        items.AddRange(searchResultObject.Results);
                    }
                }
                catch (Exception)
                {
                    failedMetadataSchemaIds.Add(metadataSchemaId);
                }
            }

            /// Assert
            Assert.True(!failedMetadataSchemaIds.Any());
            Assert.True(items.Any());
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldUpdate()
        {
            /// Arrange
            await SchemaHelper.CreateSchemasIfNotExistentAsync<Person>(_client).ConfigureAwait(false);

            // Create object
            var objectName = "ObjectToUpdate" + new Random().Next(0, 999999);
            var listItem = new ListItemCreateRequest
            {
                ContentSchemaId = nameof(SoccerPlayer),
                Content = new SoccerPlayer { Firstname = objectName, LastName = "Foo", EmailAddress = "abc@def.ch" }
            };
            var x = await _client.ListItems.CreateAsync(listItem).ConfigureAwait(false);

            // Search object
            var players = await _client.ListItems.SearchAsync(new ListItemSearchRequest
            {
                Limit = 20,
                SearchString = objectName,
                SchemaIds = new List<string> { "SoccerPlayer" }
            }).ConfigureAwait(false);

            var playerObjectId = players.Results.First().Id;
            var playerItem = await _client.ListItems.GetAsync(playerObjectId, new ListItemResolveBehaviour[] { ListItemResolveBehaviour.Content }).ConfigureAwait(false);

            /// Act
            var player = playerItem.ConvertTo<SoccerPlayer>(nameof(SoccerPlayer));
            player.Firstname = "xy jviorej ivorejvioe";

            await _client.ListItems.UpdateAsync(playerItem.Id, player).ConfigureAwait(false);
            var updatedPlayer = await _client.ListItems.GetAndConvertToAsync<SoccerPlayer>(playerItem.Id, nameof(SoccerPlayer)).ConfigureAwait(false);

            /// Assert
            Assert.Equal(player.Firstname, updatedPlayer.Firstname);
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldUpdateMany()
        {
            /// Arrange
            await SchemaHelper.CreateSchemasIfNotExistentAsync<SoccerPlayer>(_client).ConfigureAwait(false);

            var originalPlayer = new SoccerPlayer
            {
                BirthDate = DateTime.Now,
                EmailAddress = "test@test.com",
                Firstname = "Test",
                LastName = "Soccerplayer"
            };

            var createRequest = new ListItemCreateRequest
            {
                ContentSchemaId = nameof(SoccerPlayer),
                Content = originalPlayer
            };

            var playerItem = await _client.ListItems.CreateAsync(createRequest, new ListItemResolveBehaviour[] { ListItemResolveBehaviour.Content }).ConfigureAwait(false);

            /// Act
            var player = playerItem.ConvertTo<SoccerPlayer>(nameof(SoccerPlayer));
            player.Firstname = "xy jviorej ivorejvioe";

            var businessProcess = await _client.ListItems.UpdateManyAsync(new ListItemUpdateManyRequest
            {
                AllowMissingDependencies = false,
                Requests = new[]
                {
                    new ListItemUpdateRequest { Id = playerItem.Id, Content = player }
                }
            }).ConfigureAwait(false);

            await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id).ConfigureAwait(false);
            var updatedPlayer = await _client.ListItems.GetAndConvertToAsync<SoccerPlayer>(playerItem.Id, nameof(SoccerPlayer)).ConfigureAwait(false);

            /// Assert
            Assert.Equal(player.Firstname, updatedPlayer.Firstname);
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldTrashAndUntrashListItem()
        {
            /// Arrange
            var listItem = await _client.ListItems.CreateAsync(new ListItemCreateRequest
            {
                ContentSchemaId = nameof(Tag),
                Content = new Tag { Name = "ShouldTrashAndUntrashListItem" }
            }).ConfigureAwait(false);
            var listItemId = listItem.Id;

            Assert.False(string.IsNullOrEmpty(listItemId));
            var listItemDetail = await _client.ListItems.GetAsync(listItemId).ConfigureAwait(false);

            /// Act
            // Deactivate
            await _client.ListItems.DeactivateAsync(listItemId, new TimeSpan(0, 2, 0)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ListItemNotFoundException>(async () => await _client.ListItems.GetAsync(listItemId).ConfigureAwait(false)).ConfigureAwait(false);

            // Reactivate
            await _client.ListItems.ReactivateAsync(listItemId, new TimeSpan(0, 2, 0)).ConfigureAwait(false);

            /// Assert
            Assert.NotNull(await _client.ListItems.GetAsync(listItemId).ConfigureAwait(false));
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldTrashAndUntrashListItemMany()
        {
            /// Arrange
            var listItem1 = await _client.ListItems.CreateAsync(new ListItemCreateRequest
            {
                ContentSchemaId = nameof(Tag),
                Content = new Tag { Name = "ShouldTrashAndUntrashListItemMany1" }
            }).ConfigureAwait(false);

            var listItem2 = await _client.ListItems.CreateAsync(new ListItemCreateRequest
            {
                ContentSchemaId = nameof(Tag),
                Content = new Tag { Name = "ShouldTrashAndUntrashListItemMany2" }
            }).ConfigureAwait(false);

            var listItemDetail1 = await _client.ListItems.GetAsync(listItem1.Id).ConfigureAwait(false);

            var listItemDetail2 = await _client.ListItems.GetAsync(listItem2.Id).ConfigureAwait(false);

            /// Act
            // Deactivate
            var deactivateRequest = new ListItemDeactivateRequest
            {
                ListItemIds = new List<string> { listItem1.Id, listItem2.Id }
            };

            var businessProcess = await _client.ListItems.DeactivateManyAsync(deactivateRequest).ConfigureAwait(false);
            await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id).ConfigureAwait(false);

            await Assert.ThrowsAsync<ListItemNotFoundException>(async () => await _client.ListItems.GetAsync(listItem1.Id).ConfigureAwait(false)).ConfigureAwait(false);
            await Assert.ThrowsAsync<ListItemNotFoundException>(async () => await _client.ListItems.GetAsync(listItem2.Id).ConfigureAwait(false)).ConfigureAwait(false);

            // Reactivate
            var reactivateRequest = new ListItemReactivateRequest
            {
                ListItemIds = new List<string> { listItem1.Id, listItem2.Id }
            };

            businessProcess = await _client.ListItems.ReactivateManyAsync(reactivateRequest).ConfigureAwait(false);
            await _client.BusinessProcesses.WaitForCompletionAsync(businessProcess.Id).ConfigureAwait(false);

            /// Assert
            Assert.NotNull(await _client.ListItems.GetAsync(listItem1.Id).ConfigureAwait(false));
            Assert.NotNull(await _client.ListItems.GetAsync(listItem2.Id).ConfigureAwait(false));
        }

        [Fact]
        [Trait("Stack", "ListItem")]
        public async Task ShouldUseDisplayLanguageToResolveDisplayValues()
        {
            /// Arange
            var schema = await SchemaHelper.CreateSchemasIfNotExistentAsync<DisplayLanguageTestItems>(_client).ConfigureAwait(false);

            var listItem1 = new DisplayLanguageTestItems()
            {
                Value1 = "value1",
                Value2 = "value2"
            };

            var detail = await _client.ListItems.CreateAsync(new ListItemCreateRequest() { ContentSchemaId = schema.Id, Content = listItem1 }).ConfigureAwait(false);

            /// Act
            var englishClient = _fixture.GetLocalizedPictureparkClient("en");
            var receivedItem1 = await englishClient.ListItems.GetAsync(detail.Id, new[] { ListItemResolveBehaviour.Content }).ConfigureAwait(false);

            var germanClient = _fixture.GetLocalizedPictureparkClient("de");
            var receivedItem2 = await germanClient.ListItems.GetAsync(detail.Id, new[] { ListItemResolveBehaviour.Content }).ConfigureAwait(false);

            /// Assert
            receivedItem1.DisplayValues[DisplayPatternType.Name.ToString().ToLowerCamelCase()].Should().Be("value2");
            receivedItem2.DisplayValues[DisplayPatternType.Name.ToString().ToLowerCamelCase()].Should().Be("value1");
        }
    }
}