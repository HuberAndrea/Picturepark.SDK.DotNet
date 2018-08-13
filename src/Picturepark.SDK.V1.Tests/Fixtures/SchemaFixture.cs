﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Picturepark.SDK.V1.Contract;

namespace Picturepark.SDK.V1.Tests.Fixtures
{
    public class SchemaFixture : ClientFixture
    {
        internal static readonly string CountrySchemaId = "Country";
        internal static readonly IReadOnlyList<string> SystemSchemaIds = new[] { CountrySchemaId };

        private readonly ConcurrentQueue<string> _createdSchemaIds = new ConcurrentQueue<string>();

        public async Task<IReadOnlyList<SchemaDetail>> RandomizeSchemaIdsAndCreate(IEnumerable<SchemaDetail> schemas)
        {
            var schemaSuffix = new Random().Next(0, 1000000);
            var createSchemaTasks = schemas.Select(async s =>
            {
                AppendSchemaIdSuffix(s, schemaSuffix);
                return await Client.Schemas.CreateAsync(s, true, TimeSpan.FromMinutes(1)).ConfigureAwait(false);
            });

            var createdSchemas = (await Task.WhenAll(createSchemaTasks)).Select(r => r.Schema).ToArray();

            foreach (var createdSchema in createdSchemas)
            {
                _createdSchemaIds.Enqueue(createdSchema.Id);
            }

            return createdSchemas;
        }

        public override void Dispose()
        {
            while (_createdSchemaIds.TryDequeue(out var id))
            {
                var exists = Client.Schemas.ExistsAsync(id).GetAwaiter().GetResult();
                if (exists)
                    Client.Schemas.DeleteAsync(id, TimeSpan.FromMinutes(1)).GetAwaiter().GetResult();
            }

            base.Dispose();
        }

        internal void AppendSchemaIdSuffix(SchemaDetail schema, int schemaSuffix)
        {
            if (!SystemSchemaIds.Contains(schema.Id))
            {
                schema.Id = schema.Id + schemaSuffix;

                foreach (var key in schema.Names.Keys.ToList())
                {
                    schema.Names[key] = schema.Names[key] + " " + schemaSuffix;
                }
            }

            if (!string.IsNullOrEmpty(schema.ParentSchemaId) && !SystemSchemaIds.Contains(schema.ParentSchemaId))
            {
                schema.ParentSchemaId = schema.ParentSchemaId + schemaSuffix;
            }

            foreach (var field in schema.Fields.OfType<FieldSingleTagbox>().Where(f => !SystemSchemaIds.Contains(f.SchemaId)))
            {
                field.SchemaId = field.SchemaId + schemaSuffix;
            }

            foreach (var field in schema.Fields.OfType<FieldMultiTagbox>().Where(f => !SystemSchemaIds.Contains(f.SchemaId)))
            {
                field.SchemaId = field.SchemaId + schemaSuffix;
            }

            foreach (var field in schema.Fields.OfType<FieldSingleFieldset>().Where(f => !SystemSchemaIds.Contains(f.SchemaId)))
            {
                field.SchemaId = field.SchemaId + schemaSuffix;
            }

            foreach (var field in schema.Fields.OfType<FieldMultiFieldset>().Where(f => !SystemSchemaIds.Contains(f.SchemaId)))
            {
                field.SchemaId = field.SchemaId + schemaSuffix;
            }

            foreach (var field in schema.Fields.OfType<FieldSingleRelation>().Where(f => !SystemSchemaIds.Contains(f.SchemaId)))
            {
                field.SchemaId = field.SchemaId + schemaSuffix;
            }

            foreach (var field in schema.Fields.OfType<FieldMultiRelation>().Where(f => !SystemSchemaIds.Contains(f.SchemaId)))
            {
                field.SchemaId = field.SchemaId + schemaSuffix;
            }
        }
    }
}