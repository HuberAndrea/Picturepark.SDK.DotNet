﻿#pragma warning disable SA1201 // Elements must appear in the correct order

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Picturepark.SDK.V1.Tests.Contracts;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Tests.Fixtures;
using Newtonsoft.Json;
using Picturepark.SDK.V1.Contract.Attributes;
using Picturepark.SDK.V1.Contract.Attributes.Providers;

namespace Picturepark.SDK.V1.Tests
{
	public class SchemaTests : IClassFixture<SDKClientFixture>
	{
		private readonly SDKClientFixture _fixture;
		private readonly PictureparkClient _client;

		public SchemaTests(SDKClientFixture fixture)
		{
			_fixture = fixture;
			_client = _fixture.Client;
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public async Task ShouldCreateAllTypesSchemaFromClass()
		{
			/// Act
			var allTypes = _client.Schemas.GenerateSchemaFromPOCO(typeof(AllDataTypesContract));
			foreach (var schema in allTypes)
			{
				await _client.Schemas.CreateOrUpdateAsync(schema, true);
			}
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public void ShouldInvokeFilterProvider()
		{
			/// Act
			var allTypes = _client.Schemas.GenerateSchemaFromPOCO(typeof(ClassWithSimpleRelationAndFilterProvider));

			/// Assert
			var type = allTypes.Single(t => t.Id == nameof(ClassWithSimpleRelationAndFilterProvider));
			var field = (FieldSingleRelation)type.Fields.Single(f => f.Id == "relationField");
			var filter = (TermFilter)field.RelationTypes.First().Filter;

			Assert.Equal("contentType", filter.Field);
			Assert.Equal("Bitmap", filter.Term);
		}

		[PictureparkSchemaType(SchemaType.Content)]
		public class ClassWithSimpleRelationAndFilterProvider
		{
			[PictureparkContentRelation("RelationName", typeof(RelationFieldFilterProvider))]
			public SimpleRelation RelationField { get; set; }

			public class RelationFieldFilterProvider : IFilterProvider
			{
				public FilterBase GetFilter()
				{
					return new TermFilter { Field = "contentType", Term = "Bitmap" };
				}
			}
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public void ShouldInvoke()
		{
			/// Act
			var allTypes = _client.Schemas.GenerateSchemaFromPOCO(typeof(ClassWithSimpleRelationAndSchemaIndexingInfoProvider));

			/// Assert
			var type = allTypes.Single(t => t.Id == nameof(ClassWithSimpleRelationAndSchemaIndexingInfoProvider));
			var field = (FieldSingleRelation)type.Fields.Single(f => f.Id == "relationField");
			var indexingInfo = field.SchemaIndexingInfo;

			Assert.Equal("relationInfo", indexingInfo.Fields.First().Id);
			Assert.Equal(11, indexingInfo.Fields.First().Boost);
		}

		[PictureparkSchemaType(SchemaType.Content)]
		public class ClassWithSimpleRelationAndSchemaIndexingInfoProvider
		{
			[PictureparkContentRelation("RelationName", "{ 'kind': 'TermFilter', 'field': 'contentType', term: 'Bitmap' }")]
			[PictureparkSchemaIndexing(typeof(RelationFieldSchemaIndexingInfoProvider))]
			public SimpleRelation RelationField { get; set; }

			public class RelationFieldSchemaIndexingInfoProvider : ISchemaIndexingInfoProvider
			{
				public SchemaIndexingInfo GetSchemaIndexingInfo()
				{
					return new SchemaIndexingInfo
					{
						Fields = new List<FieldIndexingInfo>
						{
							new FieldIndexingInfo
							{
								Id = "relationInfo",
								Boost = 11
							}
						}
					};
				}
			}
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public async Task ShouldCreateFromClass()
		{
			var person = _client.Schemas.GenerateSchemaFromPOCO(typeof(Person));

			// Expect child schemas and referenced schemas to exist
			Assert.Equal(person.Count, 8);

			foreach (var schema in person)
			{
				if (await _client.Schemas.ExistsAsync(schema.Id) == false)
				{
					await _client.Schemas.CreateAsync(schema, true);
				}
			}

			var generatedPersonSchema = await _client.Schemas.GetAsync("Person");
			Assert.Contains(generatedPersonSchema.Types, i => i == SchemaType.List || i == SchemaType.Struct);

			var personShot = _client.Schemas.GenerateSchemaFromPOCO(typeof(PersonShot));

			// Expect child schemas and referenced schemas to exist
			Assert.Equal(personShot.Count, 9);

			foreach (var schema in personShot)
			{
				if (await _client.Schemas.ExistsAsync(schema.Id) == false)
				{
					await _client.Schemas.CreateAsync(schema, true);
				}
			}
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public async Task ShouldDelete()
		{
			var tags = _client.Schemas.GenerateSchemaFromPOCO(typeof(Tag));

			Assert.Equal(tags.Count, 1);

			// Modify schema id before submit
			var tag = tags.First();
			tag.Id = "SchemaToDelete" + new Random().Next(0, 999999);

			await _client.Schemas.CreateAsync(tag, false);

			string schemaId = tag.Id;

			SchemaDetail schemaDetail = await _client.Schemas.GetAsync(schemaId);
			await _client.Schemas.DeleteAsync(schemaDetail.Id);

			await Assert.ThrowsAsync<ApiException>(async () => await _client.Schemas.GetAsync(schemaId));
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public async Task ShouldExist()
		{
			string schemaId = _fixture.GetRandomSchemaId(20);

			bool schemaExists = await _client.Schemas.ExistsAsync(schemaId);
			Assert.True(schemaExists);

			schemaExists = await _client.Schemas.ExistsAsync("abcabcabcabc");
			Assert.False(schemaExists);
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public async Task ShouldGet()
		{
			var request = new SchemaSearchRequest() { Start = 0, Limit = 100 };
			var result = await _client.Schemas.SearchAsync(request);
			Assert.True(result.Results.Any());

			List<string> schemaIds = result.Results.Select(i => i.Id).OrderBy(i => i).ToList();
			var schemaIdsOk = new List<string>();
			var schemaIdsNotOk = new List<string>();

			foreach (var schemaId in schemaIds)
			{
				try
				{
					var schema = await _client.Schemas.GetAsync(schemaId);
					schemaIdsOk.Add(schema.Id);
				}
				catch
				{
					schemaIdsNotOk.Add(schemaId);
				}
			}

			Assert.False(schemaIdsNotOk.Count > 0);
			Assert.True(schemaIdsOk.Count > 0);
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public async Task ShouldGetJsonValidationSchema()
		{
			var schemaId = _fixture.GetRandomSchemaId(20);
			Assert.False(string.IsNullOrEmpty(schemaId));

			var result = await _client.JsonSchemas.GetAsync(schemaId);
			var stringResult = result.ToString(Formatting.Indented);
			Assert.NotNull(stringResult);
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public async Task ShouldCreateSchemaAndValidateFilter()
		{
			await ShouldCreateFromClassGeneric<SoccerPlayer>();

			var generatedSoccerPlayerSchema = await _client.Schemas.GetAsync("SoccerPlayer");

			string filterString = "{\"kind\":\"TermFilter\",\"field\":\"contentType\",\"term\":\"FC Aarau\"}";

			var jsonConvertedField = generatedSoccerPlayerSchema.Fields[0].ToJson();

			bool containsString = jsonConvertedField.Contains(filterString);

			Assert.Equal(true, containsString);
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public async Task ShouldCreateSchemaAndValidateMultiline()
		{
			await ShouldCreateFromClassGeneric<Person>();

			string multilineString = "\"multiLine\":true";

			var generatedSoccerPlayerSchema = await _client.Schemas.GetAsync("Person");

			var jsonConvertedField = generatedSoccerPlayerSchema.Fields[0].ToJson();

			bool containsString = jsonConvertedField.Contains(multilineString);

			Assert.Equal(true, containsString);
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public async Task ShouldCreateSchemaAndValidateMaxRecursion()
		{
			await ShouldCreateFromClassGeneric<Person>();
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public async Task ShouldSearch()
		{
			var searchRequest = new SchemaSearchRequest
			{
				Limit = 12,
				SearchString = "D*"
			};

			var result = await _client.Schemas.SearchAsync(searchRequest);
			Assert.True(result.Results.Any());
		}

		[Fact]
		[Trait("Stack", "Schema")]
		public async Task ShouldUpdate()
		{
			string schemaId = _fixture.GetRandomSchemaId(20);
			SchemaDetail schemaDetail = await _client.Schemas.GetAsync(schemaId);

			string language = "es";

			schemaDetail.Names.Remove(language);
			schemaDetail.Names.Add(language, schemaId);

			await _client.Schemas.UpdateAsync(schemaDetail, false);

			SchemaDetail updatedSchema = await _client.Schemas.GetAsync(schemaId);

			updatedSchema.Names.TryGetValue(language, out string outString);
			Assert.True(outString == schemaId);
		}

		public async Task ShouldCreateFromClassGeneric<T>() where T : class
		{
			var childSchemas = _client.Schemas.GenerateSchemaFromPOCO(typeof(T));

			foreach (var schema in childSchemas)
			{
				if (await _client.Schemas.ExistsAsync(schema.Id) == false)
				{
					await _client.Schemas.CreateAsync(schema, true);
				}
			}

			var schemaId = typeof(T).Name;
			var generatedPersonSchema = await _client.Schemas.GetAsync(schemaId);
			Assert.Contains(generatedPersonSchema.Types, i => i == SchemaType.List || i == SchemaType.Struct);
		}
	}
}