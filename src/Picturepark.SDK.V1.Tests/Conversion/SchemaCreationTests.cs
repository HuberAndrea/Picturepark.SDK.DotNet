﻿#pragma warning disable SA1201 // Elements must appear in the correct order

using System;
using Newtonsoft.Json;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Contract.Attributes;
using Picturepark.SDK.V1.Tests.Contracts;
using Picturepark.SDK.V1.Tests.Fixtures;
using System.Linq;
using System.Threading.Tasks;
using Picturepark.SDK.V1.Builders;
using Picturepark.SDK.V1.Contract.Attributes.Analyzer;
using Picturepark.SDK.V1.Contract.Providers;
using Picturepark.SDK.V1.Providers;
using Xunit;

namespace Picturepark.SDK.V1.Tests.Conversion
{
    public class SchemaCreationTests : IClassFixture<ClientFixture>
    {
        private readonly ClientFixture _fixture;
        private readonly PictureparkClient _client;

        public SchemaCreationTests(ClientFixture fixture)
        {
            _fixture = fixture;
            _client = _fixture.Client;
        }

        [Fact]
        [Trait("Stack", "Schema")]
        public async Task ShouldInvokeFilterProvider()
        {
            // Act
            var allTypes = await _client.Schemas.GenerateSchemasAsync(typeof(ClassWithSimpleRelationAndFilterProvider));

            // Assert
            var type = allTypes.Single(t => t.Id == nameof(ClassWithSimpleRelationAndFilterProvider));
            var field = (FieldSingleRelation)type.Fields.Single(f => f.Id == "relationField");
            var filter = (TermFilter)field.RelationTypes.First().Filter;

            Assert.Equal("contentType", filter.Field);
            Assert.Equal("Bitmap", filter.Term);
        }

        [PictureparkSchema(SchemaType.Content)]
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
        public async Task ShouldInvokeSchemaIndexingInfoProvider()
        {
            // Act
            var allTypes = await _client.Schemas.GenerateSchemasAsync(typeof(ClassWithSimpleRelationAndSchemaIndexingInfoProvider));

            // Assert
            var type = allTypes.Single(t => t.Id == nameof(ClassWithSimpleRelationAndSchemaIndexingInfoProvider));
            var field = (FieldSingleRelation)type.Fields.Single(f => f.Id == "relationField");
            var indexingInfo = field.SchemaIndexingInfo;

            Assert.Equal("relationField", indexingInfo.Fields.First().Id);
            Assert.Equal(11, indexingInfo.Fields.First().Boost);
        }

        [PictureparkSchema(SchemaType.Content)]
        public class ClassWithSimpleRelationAndSchemaIndexingInfoProvider
        {
            [PictureparkContentRelation("RelationName", "{ 'kind': 'TermFilter', 'field': 'contentType', term: 'Bitmap' }")]
            [PictureparkSchemaIndexing(typeof(RelationFieldSchemaIndexingInfoProvider))]
            public SimpleRelation RelationField { get; set; }

            public class RelationFieldSchemaIndexingInfoProvider : SchemaIndexingInfoProvider<ClassWithSimpleRelationAndSchemaIndexingInfoProvider>
            {
                protected override SchemaIndexingInfoBuilder<ClassWithSimpleRelationAndSchemaIndexingInfoProvider> Setup(
                    SchemaIndexingInfoBuilder<ClassWithSimpleRelationAndSchemaIndexingInfoProvider> builder)
                {
                    return builder.AddIndexWithSimpleSearch(p => p.RelationField, 11);
                }
            }
        }

        [Fact]
        [Trait("Stack", "SchemaCreation")]
        public async Task ShouldIgnoreJsonProperty()
        {
            // Act
            var jsonTransformSchemas = await _client.Schemas.GenerateSchemasAsync(typeof(JsonTransform));

            // Assert
            var jsonTransformSchema = jsonTransformSchemas.First();

            Assert.DoesNotContain(jsonTransformSchema.Fields, i => i.Id == nameof(JsonTransform.IgnoredString));
            var schemaSimpleRelation = jsonTransformSchemas.First(i => i.Id == nameof(SimpleRelation));

            Assert.Contains(schemaSimpleRelation.Fields, i => i.Id == nameof(SimpleRelation.RelationInfo).ToLowerCamelCase());
            Assert.DoesNotContain(schemaSimpleRelation.Fields, i => i.Id == nameof(SimpleRelation.RelationId).ToLowerCamelCase());
            Assert.DoesNotContain(schemaSimpleRelation.Fields, i => i.Id == nameof(SimpleRelation.RelationType).ToLowerCamelCase());
            Assert.DoesNotContain(schemaSimpleRelation.Fields, i => i.Id == nameof(SimpleRelation.TargetDocType).ToLowerCamelCase());
            Assert.DoesNotContain(schemaSimpleRelation.Fields, i => i.Id == nameof(SimpleRelation.TargetId).ToLowerCamelCase());
        }

        [Fact]
        [Trait("Stack", "SchemaCreation")]
        public async Task ShouldUseRenamedJsonProperty()
        {
            // Act
            var jsonTransformSchemas = await _client.Schemas.GenerateSchemasAsync(typeof(JsonTransform));

            // Assert
            var jsonTransformSchema = jsonTransformSchemas.First(i => i.Id == nameof(JsonTransform));

            Assert.DoesNotContain(jsonTransformSchema.Fields, i => i.Id == nameof(JsonTransform.OldName).ToLowerCamelCase());
            Assert.Contains(jsonTransformSchema.Fields, i => i.Id == "_newName");
        }

        [PictureparkSchema(SchemaType.Struct)]
        public class JsonTransform
        {
            [JsonIgnore]
            public string IgnoredString { get; set; }

            [JsonProperty("_newName")]
            public string OldName { get; set; }

            [PictureparkContentRelation(
                "RelationName",
                "{ 'kind': 'TermFilter', 'field': 'contentType', term: 'Bitmap' }"
            )]
            public SimpleRelation RelationField { get; set; }
        }

        [Fact]
        [Trait("Stack", "SchemaCreation")]
        public async Task ShouldNotAllowRelationsMarkedAsSortable()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async() => await _client.Schemas.GenerateSchemasAsync(typeof(ClassSortableRelation)));
        }

        [PictureparkSchema(SchemaType.List)]
        public class ClassSortableRelation
        {
            [PictureparkContentRelation(
                "RelationName",
                "{ 'kind': 'TermFilter', 'field': 'contentType', term: 'Bitmap' }"
            )]
            [PictureparkSort]
            public SimpleRelation Relation { get; set; }
        }

        [Fact]
        [Trait("Stack", "SchemaCreation")]
        public async Task ShouldNotAllowGeopointsMarkedAsSortable()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await _client.Schemas.GenerateSchemasAsync(typeof(ClassSortableGeopoint)));
        }

        [PictureparkSchema(SchemaType.List)]
        public class ClassSortableGeopoint
        {
            [PictureparkSort]
            public GeoPoint Location { get; set; }
        }

        [Fact]
        [Trait("Stack", "SchemaCreation")]
        public async Task ShouldMarkFieldAsSortableWhenMarkedWithSortAttribute()
        {
            var schema = await _client.Schemas.GenerateSchemasAsync(typeof(ClassSortableString));
            Assert.True(schema.First().Fields.First().Sortable);
        }

        [PictureparkSchema(SchemaType.List)]
        public class ClassSortableString
        {
            [PictureparkSort]
            public string Title { get; set; }
        }

        [Fact]
        [Trait("Stack", "SchemaCreation")]
        public async Task ShouldNotAllowAnalyzerWithoutIndexOrSearch()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await _client.Schemas.GenerateSchemasAsync(typeof(ClassAnalyzerWithoutIndexAndSearch)));
        }

        [PictureparkSchema(SchemaType.List)]
        public class ClassAnalyzerWithoutIndexAndSearch
        {
            [PictureparkSimpleAnalyzer(Index = false, SimpleSearch = false)]
            public string Title { get; set; }
        }

        [Fact]
        [Trait("Stack", "SchemaCreation")]
        public async Task ShouldNotAllowMultipleDisplayPatternsOfSameTypeAndLanguage()
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                async () => await _client.Schemas.GenerateSchemasAsync(typeof(ClassWithMultipleDisplayPatternsForEnglishName)));
        }

        [PictureparkSchema(SchemaType.List)]
        [PictureparkDisplayPattern(DisplayPatternType.Name, TemplateEngine.DotLiquid, "{{pattern}}", "en")]
        [PictureparkDisplayPattern(DisplayPatternType.Name, TemplateEngine.DotLiquid, "{{pattern}}", "en")]
        public class ClassWithMultipleDisplayPatternsForEnglishName
        {
        }
    }
}
