﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Conversion;
using System.Net.Http;

namespace Picturepark.SDK.V1
{
	public partial class SchemaClient
	{
		private readonly BusinessProcessClient _businessProcessClient;

		public SchemaClient(BusinessProcessClient businessProcessesClient, IPictureparkClientSettings settings, HttpClient httpClient)
			: this(settings, httpClient)
		{
			BaseUrl = businessProcessesClient.BaseUrl;
			_businessProcessClient = businessProcessesClient;
		}

		public List<SchemaDetail> GenerateSchemaFromPOCO(Type type, List<SchemaDetail> schemaList = null, bool generateDependencySchema = true)
		{
			if (schemaList == null)
				schemaList = new List<SchemaDetail>();

			var schemaConverter = new ClassToSchemaConverter();
			return schemaConverter.Generate(type, schemaList, generateDependencySchema);
		}

		public async Task CreateOrUpdateAsync(SchemaDetail metadataSchema, bool enableForBinaryFiles)
		{
			if (await ExistsAsync(metadataSchema.Id))
			{
				await UpdateAsync(metadataSchema, enableForBinaryFiles);
			}
			else
			{
				await CreateAsync(metadataSchema, enableForBinaryFiles);
			}
		}

		public void CreateOrUpdate(SchemaDetail metadataSchema, bool enableForBinaryFiles)
		{
			Task.Run(async () => await CreateOrUpdateAsync(metadataSchema, enableForBinaryFiles)).GetAwaiter().GetResult();
		}

		public async Task CreateAsync(SchemaDetail metadataSchema, bool enableForBinaryFiles)
		{
			// Map schema to binary schemas
			if (enableForBinaryFiles && metadataSchema.Types.Contains(SchemaType.Layer))
			{
				var binarySchemas = new List<string>
				{
					nameof(FileMetadata),
					nameof(AudioMetadata),
					nameof(DocumentMetadata),
					nameof(ImageMetadata),
					nameof(VideoMetadata),
				};
				metadataSchema.ReferencedInContentSchemaIds = binarySchemas;
			}

			await CreateAsync(metadataSchema);
		}

		public void Create(SchemaDetail metadataSchema, bool enableForBinaryFiles)
		{
			Task.Run(async () => await CreateAsync(metadataSchema, enableForBinaryFiles)).GetAwaiter().GetResult();
		}

		/// <exception cref="ApiException">A server side error occurred.</exception>
		public async Task CreateAsync(SchemaDetail metadataSchema)
		{
			var businessProcess = await CreateAsync(new SchemaCreateRequest
			{
				Aggregations = metadataSchema.Aggregations,
				Descriptions = metadataSchema.Descriptions,
				DisplayPatterns = metadataSchema.DisplayPatterns,
				Fields = metadataSchema.Fields,
				Id = metadataSchema.Id,
				SchemaPermissionSetIds = metadataSchema.SchemaPermissionSetIds,
				Names = metadataSchema.Names,
				ParentSchemaId = metadataSchema.ParentSchemaId,
				Public = metadataSchema.Public,
				ReferencedInContentSchemaIds = metadataSchema.ReferencedInContentSchemaIds,
				Sort = metadataSchema.Sort,
				SortOrder = metadataSchema.SortOrder,
				Types = metadataSchema.Types,
				LayerSchemaIds = metadataSchema.LayerSchemaIds
			});

			await _businessProcessClient.WaitForCompletionAsync(businessProcess.Id);
		}

		/// <exception cref="ApiException">A server side error occurred.</exception>
		public void Create(SchemaDetail metadataSchema)
		{
			Task.Run(async () => await CreateAsync(metadataSchema)).GetAwaiter().GetResult();
		}

		/// <exception cref="ApiException">A server side error occurred.</exception>
		public async Task DeleteAsync(string schemaId)
		{
			var process = await DeleteCoreAsync(schemaId);
			await _businessProcessClient.WaitForCompletionAsync(process.Id);
		}

		/// <exception cref="ApiException">A server side error occurred.</exception>
		public void Delete(string schemaId)
		{
			Task.Run(async () => await DeleteAsync(schemaId)).GetAwaiter().GetResult();
		}

		/// <exception cref="ApiException">A server side error occurred.</exception>
		public async Task UpdateAsync(SchemaDetail schema, bool enableForBinaryFiles)
		{
			if (enableForBinaryFiles && schema.Types.Contains(SchemaType.Layer))
			{
				var binarySchemas = new List<string>
				{
					nameof(FileMetadata),
					nameof(AudioMetadata),
					nameof(DocumentMetadata),
					nameof(ImageMetadata),
					nameof(VideoMetadata),
				};
				schema.ReferencedInContentSchemaIds = binarySchemas;
			}

			await UpdateAsync(schema.Id, new SchemaUpdateRequest
			{
				Aggregations = schema.Aggregations,
				Descriptions = schema.Descriptions,
				DisplayPatterns = schema.DisplayPatterns,
				Fields = schema.Fields,
				SchemaPermissionSetIds = schema.SchemaPermissionSetIds,
				Names = schema.Names,
				Public = schema.Public,
				ReferencedInContentSchemaIds = schema.ReferencedInContentSchemaIds,
				Sort = schema.Sort,
				SortOrder = schema.SortOrder,
				Types = schema.Types,
				LayerSchemaIds = schema.LayerSchemaIds
			});
		}

		/// <exception cref="ApiException">A server side error occurred.</exception>
		public async Task UpdateAsync(string schemaId, SchemaUpdateRequest updateRequest)
		{
			var process = await UpdateCoreAsync(schemaId, updateRequest);
			await _businessProcessClient.WaitForCompletionAsync(process.Id);
		}

		/// <exception cref="ApiException">A server side error occurred.</exception>
		public void Update(string schemaId, SchemaUpdateRequest updateRequest)
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
	}
}
