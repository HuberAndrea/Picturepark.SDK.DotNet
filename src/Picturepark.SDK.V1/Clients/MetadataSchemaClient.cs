﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Picturepark.SDK.V1.Authentication;
using Picturepark.SDK.V1.Clients;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Conversion;

namespace Picturepark.SDK.V1
{
    public class MetadataSchemaClient : MetadataSchemasClientBase
    {
        private readonly BusinessProcessClient _businessProcessesClient;

        public MetadataSchemaClient(BusinessProcessClient businessProcessesClient, IAuthClient authClient)
            : base(authClient)
        {
            BaseUrl = businessProcessesClient.BaseUrl;
            _businessProcessesClient = businessProcessesClient;
        }

        public List<MetadataSchemaDetailViewItem> GenerateSchemaFromPOCO(Type type, List<MetadataSchemaDetailViewItem> schemaList, bool generateDependencySchema = true)
        {
            var schemaConverter = new ClassToSchemaConverter();
            return schemaConverter.Generate(type, schemaList, generateDependencySchema);
        }

        public async Task CreateOrUpdateAsync(MetadataSchemaDetailViewItem metadataSchema)
        {
            if (await ExistsAsync(metadataSchema.Id))
            {
                await UpdateAsync(metadataSchema);
            }
            else
            {
                await CreateAsync(metadataSchema);
            }
        }

        public void CreateOrUpdate(MetadataSchemaDetailViewItem metadataSchema)
        {
            Task.Run(async () => await CreateOrUpdateAsync(metadataSchema)).GetAwaiter().GetResult();
        }

        public async Task CreateAsync(MetadataSchemaDetailViewItem metadataSchema, bool enableForBinaryFiles)
        {
            // Map schema to binary schemas
            if (enableForBinaryFiles && metadataSchema.Types.Contains(MetadataSchemaType.AssetLayer))
            {
                var binarySchemas = new List<string>
                {
                    nameof(FileMetadata),
                    nameof(AudioMetadata),
                    nameof(DocumentMetadata),
                    nameof(ImageMetadata),
                    nameof(VideoMetadata),
                };
                metadataSchema.ReferencedInMetadataSchemaIds = binarySchemas;
            }

            await CreateAsync(metadataSchema);
        }

        public void Create(MetadataSchemaDetailViewItem metadataSchema, bool enableForBinaryFiles)
        {
            Task.Run(async () => await CreateAsync(metadataSchema, enableForBinaryFiles)).GetAwaiter().GetResult();
        }

        /// <exception cref="ApiException">A server side error occurred.</exception>
        public async Task CreateAsync(MetadataSchemaDetailViewItem metadataSchema)
        {
            var process = await CreateAsync(new MetadataSchemaCreateRequest
            {
                Aggregations = metadataSchema.Aggregations,
                Descriptions = new TranslatedStringDictionary { }, //// TODO
                DisplayPatterns = metadataSchema.DisplayPatterns,
                Fields = metadataSchema.Fields,
                FullTextFields = metadataSchema.FullTextFields,
                Id = metadataSchema.Id,
                MetadataPermissionSetIds = metadataSchema.MetadataPermissionSetIds,
                Names = metadataSchema.Names,
                ParentMetadataSchemaId = metadataSchema.ParentMetadataSchemaId,
                Public = metadataSchema.Public,
                ReferencedInMetadataSchemaIds = metadataSchema.ReferencedInMetadataSchemaIds,
                Sort = metadataSchema.Sort,
                SortOrder = metadataSchema.SortOrder,
                Types = metadataSchema.Types
            });
            await WaitForCompletionAsync(process);
        }

        /// <exception cref="ApiException">A server side error occurred.</exception>
        public void Create(MetadataSchemaDetailViewItem metadataSchema)
        {
            Task.Run(async () => await CreateAsync(metadataSchema)).GetAwaiter().GetResult();
        }

        /// <exception cref="ApiException">A server side error occurred.</exception>
        public async Task DeleteAsync(string schemaId)
        {
            var process = await DeleteCoreAsync(schemaId);
            await WaitForCompletionAsync(process);
        }

        /// <exception cref="ApiException">A server side error occurred.</exception>
        public void Delete(string schemaId)
        {
            Task.Run(async () => await DeleteAsync(schemaId)).GetAwaiter().GetResult();
        }

        /// <exception cref="ApiException">A server side error occurred.</exception>
        public async Task UpdateAsync(MetadataSchemaDetailViewItem metadataSchema)
        {
            await UpdateAsync(metadataSchema.Id, new MetadataSchemaUpdateRequest
            {
                Aggregations = metadataSchema.Aggregations,
                Descriptions = new TranslatedStringDictionary { }, //// TODO
                DisplayPatterns = metadataSchema.DisplayPatterns,
                Fields = metadataSchema.Fields,
                FullTextFields = metadataSchema.FullTextFields,
                MetadataPermissionSetIds = metadataSchema.MetadataPermissionSetIds,
                Names = metadataSchema.Names,
                Public = metadataSchema.Public,
                ReferencedInMetadataSchemaIds = metadataSchema.ReferencedInMetadataSchemaIds,
                Sort = metadataSchema.Sort,
                SortOrder = metadataSchema.SortOrder,
                Types = metadataSchema.Types,
                MetadataSchemaIds = metadataSchema.MetadataSchemaIds
            });
        }

        /// <exception cref="ApiException">A server side error occurred.</exception>
        public async Task UpdateAsync(string schemaId, MetadataSchemaUpdateRequest updateRequest)
        {
            var process = await UpdateCoreAsync(schemaId, updateRequest);
            await WaitForCompletionAsync(process);
        }

        /// <exception cref="ApiException">A server side error occurred.</exception>
        public void Update(string schemaId, MetadataSchemaUpdateRequest updateRequest)
        {
            Task.Run(async () => await UpdateAsync(schemaId, updateRequest)).GetAwaiter().GetResult();
        }

        /// <exception cref="ApiException">A server side error occurred.</exception>
        public async Task<bool> ExistsAsync(string schemaId)
        {
            return (await ExistsAsync(schemaId, null)).Exists;
        }

        public bool Exists(string schemaId)
        {
            return Task.Run(async () => await ExistsAsync(schemaId)).GetAwaiter().GetResult();
        }

        private async Task WaitForCompletionAsync(BusinessProcessViewItem process)
        {
            var wait = await process.Wait4StateAsync("Completed", _businessProcessesClient);

            var errors = wait.BusinessProcess.StateHistory?
                .Where(i => i.Error != null)
                .Select(i => i.Error)
                .ToList();

            if (errors != null && errors.Any())
            {
                // TODO: Deserialize and create Aggregate exception
                throw new Exception(errors.First().Exception);
            }
        }
    }
}
