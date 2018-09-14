﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Conversion;
using System.Net.Http;
using System.Threading;

namespace Picturepark.SDK.V1
{
    public partial class SchemaClient
    {
        private readonly BusinessProcessClient _businessProcessClient;
        private readonly InfoClient _infoClient;

        public SchemaClient(BusinessProcessClient businessProcessesClient, InfoClient infoClient, IPictureparkServiceSettings settings, HttpClient httpClient)
            : this(settings, httpClient)
        {
            _businessProcessClient = businessProcessesClient;
            _infoClient = infoClient;
        }

        /// <summary>Generates the <see cref="SchemaDetail"/>s for the given type and the referenced types.</summary>
        /// <param name="type">The type.</param>
        /// <param name="schemaDetails">The existing schema details.</param>
        /// <param name="generateDependencySchema">Specifies whether to generate dependent schemas.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The collection of schema details.</returns>
        public async Task<ICollection<SchemaDetail>> GenerateSchemasAsync(
            Type type,
            IEnumerable<SchemaDetail> schemaDetails = null,
            bool generateDependencySchema = true,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var config = await _infoClient.GetAsync(cancellationToken).ConfigureAwait(false);
            var schemaConverter = new ClassToSchemaConverter(config.LanguageConfiguration.DefaultLanguage);
            return await schemaConverter.GenerateAsync(type, schemaDetails ?? new List<SchemaDetail>(), generateDependencySchema).ConfigureAwait(false);
        }

        /// <summary>Creates or updates the given <see cref="SchemaDetail"/>.</summary>
        /// <param name="schemaDetail">The schema detail.</param>
        /// <param name="enableForBinaryFiles">Specifies whether to enable the schema for binary files.</param>
        /// <param name="timeout">Maximum time to wait for the operation to complete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public async Task<ISchemaResult> CreateOrUpdateAsync(SchemaDetail schemaDetail, bool enableForBinaryFiles, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (await ExistsAsync(schemaDetail.Id, null, cancellationToken).ConfigureAwait(false))
            {
                return await UpdateAsync(schemaDetail, enableForBinaryFiles, timeout, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return await CreateAsync(schemaDetail, enableForBinaryFiles, timeout, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>Creates the given <see cref="SchemaDetail"/>.</summary>
        /// <param name="schemaDetail">The schema detail.</param>
        /// <param name="enableForBinaryFiles">Specifies whether to enable the schema for binary files.</param>
        /// <param name="timeout">Maximum time to wait for the operation to complete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        public async Task<SchemaCreateResult> CreateAsync(SchemaDetail schemaDetail, bool enableForBinaryFiles, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Map schema to binary schemas
            if (enableForBinaryFiles && schemaDetail.Types.Contains(SchemaType.Layer))
            {
                var binarySchemas = new List<string>
                {
                    nameof(FileMetadata),
                    nameof(AudioMetadata),
                    nameof(DocumentMetadata),
                    nameof(ImageMetadata),
                    nameof(VideoMetadata),
                };

                schemaDetail.ReferencedInContentSchemaIds = binarySchemas;
            }

            return await CreateAsync(schemaDetail, timeout, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Creates the given <see cref="SchemaDetail"/>.</summary>
        /// <param name="schemaDetail">The schema detail.</param>
        /// <param name="timeout">Maximum time to wait for the operation to complete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        /// <exception cref="ApiException">A server side error occurred.</exception>
        public async Task<SchemaCreateResult> CreateAsync(SchemaDetail schemaDetail, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var createRequest = new SchemaCreateRequest
            {
                Aggregations = schemaDetail.Aggregations,
                Descriptions = schemaDetail.Descriptions,
                DisplayPatterns = schemaDetail.DisplayPatterns,
                Fields = schemaDetail.Fields,
                Id = schemaDetail.Id,
                SchemaPermissionSetIds = schemaDetail.SchemaPermissionSetIds,
                Names = schemaDetail.Names,
                ParentSchemaId = schemaDetail.ParentSchemaId,
                Public = schemaDetail.Public,
                ReferencedInContentSchemaIds = schemaDetail.ReferencedInContentSchemaIds,
                Sort = schemaDetail.Sort,
                Types = schemaDetail.Types,
                LayerSchemaIds = schemaDetail.LayerSchemaIds
            };

            return await CreateAsync(createRequest, timeout, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Updates the given <see cref="SchemaDetail"/>.</summary>
        /// <param name="schemaDetail">The schema detail.</param>
        /// <param name="enableForBinaryFiles">Specifies whether to enable the schema for binary files.</param>
        /// <param name="timeout">Maximum time to wait for the operation to complete.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        /// <exception cref="ApiException">A server side error occurred.</exception>
        public async Task<SchemaUpdateResult> UpdateAsync(SchemaDetail schemaDetail, bool enableForBinaryFiles, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (enableForBinaryFiles && schemaDetail.Types.Contains(SchemaType.Layer))
            {
                var binarySchemas = new List<string>
                {
                    nameof(FileMetadata),
                    nameof(AudioMetadata),
                    nameof(DocumentMetadata),
                    nameof(ImageMetadata),
                    nameof(VideoMetadata),
                };

                schemaDetail.ReferencedInContentSchemaIds = binarySchemas;
            }

            var updateRequest = new SchemaUpdateRequest
            {
                Aggregations = schemaDetail.Aggregations,
                Descriptions = schemaDetail.Descriptions,
                DisplayPatterns = schemaDetail.DisplayPatterns,
                Fields = schemaDetail.Fields,
                SchemaPermissionSetIds = schemaDetail.SchemaPermissionSetIds,
                Names = schemaDetail.Names,
                Public = schemaDetail.Public,
                ReferencedInContentSchemaIds = schemaDetail.ReferencedInContentSchemaIds,
                Sort = schemaDetail.Sort,
                LayerSchemaIds = schemaDetail.LayerSchemaIds,
                FieldsOverwrite = schemaDetail.FieldsOverwrite
            };

            return await UpdateAsync(schemaDetail.Id, updateRequest, timeout, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Checks whether a schema ID already exists.</summary>
        /// <param name="schemaId">The schema ID.</param>
        /// <param name="fieldId">The optional field ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task.</returns>
        /// <exception cref="ApiException">A server side error occurred.</exception>
        public async Task<bool> ExistsAsync(string schemaId, string fieldId = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return (await ExistsCoreAsync(schemaId, null, cancellationToken).ConfigureAwait(false)).Exists;
        }
    }
}
