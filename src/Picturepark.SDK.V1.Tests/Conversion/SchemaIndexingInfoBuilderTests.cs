﻿#pragma warning disable SA1201 // Elements must appear in the correct order

using System;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Contract.Attributes;
using Picturepark.SDK.V1.Contract.SystemTypes;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Picturepark.SDK.V1.Builders;
using Xunit;

namespace Picturepark.SDK.V1.Tests.Conversion
{
    public class SchemaIndexingInfoBuilderTests
    {
        [Fact]
        [Trait("Stack", "Schema")]
        public void ShouldAddIndexOnPropertyPath()
        {
            //// Arrange
            var builder = new SchemaIndexingInfoBuilder<Parent>();

            //// Act
            var info = builder
                .AddIndex(p => p.Child.FirstName)
                .Build();

            var json = JsonConvert.SerializeObject(info, Formatting.Indented);

            //// Assert
            Assert.Contains(info.Fields.Single(f => f.Id == "child").RelatedSchemaIndexing.Fields, f => f.Id == "firstName");
        }

        [Fact]
        [Trait("Stack", "Schema")]
        public void ShouldAddIndexOnCollectionPath()
        {
            //// Arrange
            var builder = new SchemaIndexingInfoBuilder<Parent>();

            //// Act
            var info = builder
                .AddIndex(p => p.Children.Select(c => c.FirstName))
                .Build();

            var json = JsonConvert.SerializeObject(info, Formatting.Indented);

            //// Assert
            Assert.Contains(info.Fields.Single(f => f.Id == "children").RelatedSchemaIndexing.Fields, f => f.Id == "firstName");

            Assert.Null(info.Fields.Single(f => f.Id == "children")
                .RelatedSchemaIndexing.Fields.Single(f => f.Id == "firstName")
                .RelatedSchemaIndexing);
        }

        [Fact]
        [Trait("Stack", "Schema")]
        public void ShouldAddMultipleIndexes()
        {
            //// Arrange
            var builder = new SchemaIndexingInfoBuilder<Parent>();

            //// Act
            var info = builder
                .AddIndex(p => p.Foo)
                .AddIndex(p => p.Bar)
                .Build();

            var json = JsonConvert.SerializeObject(info, Formatting.Indented);

            //// Assert
            Assert.Contains(info.Fields, f => f.Id == "foo");
            Assert.Contains(info.Fields, f => f.Id == "bar");

            Assert.Null(info.Fields.Single(f => f.Id == "foo").RelatedSchemaIndexing);
            Assert.Null(info.Fields.Single(f => f.Id == "bar").RelatedSchemaIndexing);
        }

        [Fact]
        [Trait("Stack", "Schema")]
        public void ShouldAddDefaultIndexes()
        {
            //// Arrange
            var builder = new SchemaIndexingInfoBuilder<Parent>();

            //// Act
            var info = builder
                .AddDefaultIndexes(p => p.Child, 1)
                .Build();

            var json = JsonConvert.SerializeObject(info, Formatting.Indented);

            //// Assert
            Assert.Contains(info.Fields.Single(f => f.Id == "child").RelatedSchemaIndexing.Fields, f => f.Id == "firstName");

            Assert.Contains(info.Fields.Single(f => f.Id == "child").RelatedSchemaIndexing.Fields, f => f.Id == "lastName");
            Assert.DoesNotContain(info.Fields.Single(f => f.Id == "child").RelatedSchemaIndexing.Fields, f => f.Id == "dateOfBirth");

            Assert.Null(info.Fields.Single(f => f.Id == "child")
                .RelatedSchemaIndexing.Fields.Single(f => f.Id == "lastName").RelatedSchemaIndexing);
            Assert.Null(info.Fields.Single(f => f.Id == "child")
                .RelatedSchemaIndexing.Fields.Single(f => f.Id == "firstName").RelatedSchemaIndexing);
        }

        [Fact]
        [Trait("Stack", "Schema")]
        public void ShouldAddDefaultIndexesOfRootType()
        {
            //// Arrange
            var builder = new SchemaIndexingInfoBuilder<Child>();

            //// Act
            var info = builder
                .AddDefaultIndexes(1)
                .Build();

            var json = JsonConvert.SerializeObject(info, Formatting.Indented);

            //// Assert
            Assert.Contains(info.Fields, f => f.Id == "firstName");
            Assert.Contains(info.Fields, f => f.Id == "lastName");
            Assert.DoesNotContain(info.Fields, f => f.Id == "dateOfBirth");

            Assert.Null(info.Fields.Single(f => f.Id == "firstName").RelatedSchemaIndexing);
            Assert.Null(info.Fields.Single(f => f.Id == "lastName").RelatedSchemaIndexing);
        }

        public class Parent
        {
            [PictureparkTagbox("{ 'kind': 'TermFilter', 'field': 'contentType', Term: 'FC Aarau' }")]
            public Child Child { get; set; }

            public Child[] Children { get; set; }

            public string Foo { get; set; }

            public string Bar { get; set; }
        }

        [KnownType(typeof(SpecialChild))]
        [PictureparkSchemaType(SchemaType.Struct)]
        public class Child : Relation
        {
            [PictureparkSearch(Index = true, Boost = 1.2, SimpleSearch = true)]
            public string FirstName { get; set; }

            [PictureparkSearch(Index = true, Boost = 1.3, SimpleSearch = true)]
            public string LastName { get; set; }

            public DateTime DateOfBirth { get; set; }
        }

        public class SpecialChild : Child
        {
            [PictureparkSearch(Index = true, Boost = 1.4, SimpleSearch = true)]
            public string Speciality { get; set; }
        }

        // inheritance tests
        [Fact]
        [Trait("Stack", "Schema")]
        public void ShouldAddIndexOnPropertyPathWithInheritance()
        {
            //// Arrange
            var builder = new SchemaIndexingInfoBuilder<Parent>();

            //// Act
            var info = builder
                .AddIndex(p => ((SpecialChild)p.Child).Speciality)
                .Build();

            var json = JsonConvert.SerializeObject(info, Formatting.Indented);

            //// Assert
            Assert.Contains(info.Fields.Single(f => f.Id == "child").RelatedSchemaIndexing.Fields, f => f.Id == "speciality");
        }

        [Fact]
        [Trait("Stack", "Schema")]
        public void ShouldAddIndexOnCollectionPathWithInheritance()
        {
            //// Arrange
            var builder = new SchemaIndexingInfoBuilder<Parent>();

            //// Act
            var info = builder
                .AddIndex(p => p.Children.OfType<SpecialChild>().Select(c => c.Speciality))
                .Build();

            var json = JsonConvert.SerializeObject(info, Formatting.Indented);

            //// Assert
            Assert.Contains(info.Fields.Single(f => f.Id == "children").RelatedSchemaIndexing.Fields, f => f.Id == "speciality");

            Assert.Null(info.Fields.Single(f => f.Id == "children")
                .RelatedSchemaIndexing.Fields.Single(f => f.Id == "speciality")
                .RelatedSchemaIndexing);
        }

        [Fact]
        [Trait("Stack", "Schema")]
        public void ShouldAddDefaultIndexesWithInheritance()
        {
            //// Arrange
            var builder = new SchemaIndexingInfoBuilder<Parent>();

            //// Act
            var info = builder
                .AddDefaultIndexes(p => p.Child, 1)
                .Build();

            var json = JsonConvert.SerializeObject(info, Formatting.Indented);

            //// Assert
            Assert.Contains(info.Fields.Single(f => f.Id == "child").RelatedSchemaIndexing.Fields, f => f.Id == "speciality");
        }
    }
}
