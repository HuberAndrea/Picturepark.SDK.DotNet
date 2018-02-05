﻿using Picturepark.SDK.V1.Contract.Attributes;
using Picturepark.SDK.V1.Contract;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Picturepark.SDK.V1.Contract.Attributes.Analyzer;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace Picturepark.SDK.V1.Conversion
{
    /// <summary>Converts .NET types to Picturepark schemas.</summary>
    public class ClassToSchemaConverter
    {
        private readonly IContractResolver _contractResolver;
        private readonly List<string> _ignoredProperties = new List<string> { "refId", "_relId", "_relationType", "_targetContext", "_targetId" };

        public ClassToSchemaConverter()
            : this(new CamelCasePropertyNamesContractResolver())
        {
        }

        public ClassToSchemaConverter(IContractResolver contractResolver)
        {
            _contractResolver = contractResolver;
        }

        /// <summary>Converts a .NET type and its dependencies to a list of Picturepark schema definitions.</summary>
        /// <param name="type">The type to generate definitions for.</param>
        /// <param name="generateRelatedSchemas">Generates related schemas as well. E.g. referenced pocos in lists.</param>
        /// <returns>List of schemas</returns>
        public Task<ICollection<SchemaDetail>> GenerateAsync(Type type, bool generateRelatedSchemas = true)
        {
            return GenerateAsync(type, new List<SchemaDetail>(), generateRelatedSchemas);
        }

        /// <summary>Converts a .NET type and its dependencies to a list of Picturepark schema definitions.</summary>
        /// <param name="type">The type to generate definitions for.</param>
        /// <param name="schemaDetails">Existing list of schemas. Pass if you need to convert several pocos and they reference the same dependant schemas (used to exclude existing schemas).</param>
        /// <param name="generateRelatedSchemas">Generates related schemas as well. E.g. referenced pocos in lists.</param>
        /// <returns>List of schemas</returns>
        public Task<ICollection<SchemaDetail>> GenerateAsync(Type type, IEnumerable<SchemaDetail> schemaDetails, bool generateRelatedSchemas = true)
        {
            var properties = GetProperties(type);

            var schemaList = schemaDetails.ToList();
            CreateSchemas(type, properties, string.Empty, schemaList, 0, generateRelatedSchemas);

            var sortedList = new List<SchemaDetail>();
            foreach (var schemaItem in schemaList)
            {
                var dependencyList = schemaList.FindAll(s => s.Dependencies.Any(d => d.Id == schemaItem.Id));

                int? index = null;
                if (dependencyList.Any())
                {
                    foreach (var dependency in dependencyList)
                    {
                        var dependencyIndex = sortedList.FindIndex(s => s.Id == dependency.Id);
                        if (dependencyIndex == -1)
                            continue;

                        if (!index.HasValue || index.Value > dependencyIndex)
                            index = dependencyIndex;
                    }
                }

                if (index.HasValue)
                    sortedList.Insert(index.Value, schemaItem);
                else
                    sortedList.Add(schemaItem);
            }

            return Task.FromResult((ICollection<SchemaDetail>)sortedList);
        }

        private SchemaDetail CreateSchemas(Type contractType, List<ContractPropertyInfo> properties, string parentSchemaId, List<SchemaDetail> schemaList, int levelOfCall = 0, bool generateDependencySchema = true)
        {
            var schemaId = contractType.Name;

            var typeAttributes = contractType.GetTypeInfo()
                .GetCustomAttributes(typeof(PictureparkSchemaTypeAttribute), true)
                .Select(i => i as PictureparkSchemaTypeAttribute)
                .ToList();

            if (!typeAttributes.Any())
                throw new Exception("No PictureparkSchemaTypeAttribute set on class: " + contractType.Name);

            var types = typeAttributes
                .Select(typeAttribute => typeAttribute.SchemaType)
                .ToList();

            var schemaItem = new SchemaDetail
            {
                Id = schemaId,
                Fields = new List<FieldBase>(),
                FieldsOverwrite = new List<FieldOverwriteBase>(),
                ParentSchemaId = parentSchemaId,
                Names = new TranslatedStringDictionary { { "x-default", schemaId } },
                Descriptions = new TranslatedStringDictionary(),
                Types = types,
                DisplayPatterns = new List<DisplayPattern>()
            };

            ApplyDisplayPatternAttributes(schemaItem, contractType);
            ApplyNameTranslationAttributes(schemaItem, contractType);
            ApplyDescriptionTranslationAttributes(schemaItem, contractType);

            var customTypes = properties.FindAll(c => c.IsCustomType);
            if (customTypes.Any())
            {
                foreach (var customType in customTypes)
                {
                    var referencedSchemaId = customType.TypeName;

                    if (schemaList.Any(d => d.Id == referencedSchemaId))
                        continue;

                    // Exclusion, if the customType is the contractType (it would create itself again with zero fields)
                    if (customType.FullName == contractType.FullName)
                        continue;

                    var subLevelOfcall = levelOfCall + 1;

                    Type type = Type.GetType($"{customType.FullName}, {customType.AssemblyFullName}");

                    var isSystemSchema = type.GetTypeInfo().GetCustomAttributes(typeof(PictureparkSystemSchemaAttribute), true).Any();
                    if (isSystemSchema == false)
                    {
                        var dependencySchema = CreateSchemas(type, customType.TypeProperties, string.Empty, schemaList, subLevelOfcall, generateDependencySchema);

                        // the schema can be alredy added as dependency
                        if (schemaItem.Dependencies.Any(d => d.Id == referencedSchemaId) == false)
                            schemaItem.Dependencies.Add(dependencySchema);

                        // the schema can be alredy created
                        if (schemaList.Any(d => d.Id == referencedSchemaId) == false && generateDependencySchema)
                            schemaList.Add(dependencySchema);
                    }
                }
            }

            foreach (var property in properties)
            {
                if (property.IsOverwritten)
                {
                    var fieldOverwrite = GetFieldOverwrite(property);
                    schemaItem.FieldsOverwrite.Add(fieldOverwrite);
                }
                else
                {
                    var fieldData = GetField(property);

                    schemaItem.Fields.Add(fieldData);
                }
            }

            if (generateDependencySchema || levelOfCall == 0)
            {
                // the schema can be already created
                if (schemaList.Find(s => s.Id == schemaItem.Id) == null)
                    schemaList.Add(schemaItem);
            }

            // Create schemas for all subtypes
            var subtypes = contractType.GetTypeInfo().Assembly.GetTypes().Where(t => t.GetTypeInfo().IsSubclassOf(contractType));
            foreach (var subtype in subtypes)
            {
                CreateSchemas(subtype, GetProperties(subtype), schemaId, schemaList);
            }

            return schemaItem;
        }

        private FieldOverwriteBase GetFieldOverwrite(ContractPropertyInfo property)
        {
            var tagboxAttribute = property.PictureparkAttributes
                .OfType<PictureparkTagboxAttribute>()
                .SingleOrDefault();

            var listItemCreateTemplateAttribute = property.PictureparkAttributes
                .OfType<PictureparkListItemCreateTemplateAttribute>()
                .SingleOrDefault();

            if (property.IsArray)
            {
                if (property.IsReference)
                {
                    return new FieldOverwriteMultiTagbox
                    {
                        Id = property.Name,
                        Filter = tagboxAttribute?.Filter,
                        Required = property.PictureparkAttributes.OfType<PictureparkRequiredAttribute>().Any(),
                        ListItemCreateTemplate = listItemCreateTemplateAttribute?.ListItemCreateTemplate,
                        OverwriteListItemCreateTemplate = !string.IsNullOrEmpty(listItemCreateTemplateAttribute?.ListItemCreateTemplate)
                    };
                }
                else
                {
                    throw new InvalidOperationException("Only Tagbox properties can be overriden.");
                }
            }
            else
            {
                if (property.IsReference)
                {
                    return new FieldOverwriteSingleTagbox
                    {
                        Id = property.Name,
                        Filter = tagboxAttribute?.Filter,
                        Required = property.PictureparkAttributes.OfType<PictureparkRequiredAttribute>().Any(),
                        ListItemCreateTemplate = listItemCreateTemplateAttribute?.ListItemCreateTemplate,
                        OverwriteListItemCreateTemplate =
                            !string.IsNullOrEmpty(listItemCreateTemplateAttribute?.ListItemCreateTemplate)
                    };
                }
                else
                {
                    throw new InvalidOperationException("Only Tagbox properties can be overriden.");
                }
            }
        }

        private void ApplyDescriptionTranslationAttributes(SchemaDetail schemaDetail, Type type)
        {
            var descriptionTranslationAttributes = type.GetTypeInfo()
                .GetCustomAttributes(typeof(PictureparkDescriptionTranslationAttribute), true)
                .Select(i => i as PictureparkDescriptionTranslationAttribute)
                .ToList();

            foreach (var translationAttribute in descriptionTranslationAttributes)
            {
                schemaDetail.Descriptions[translationAttribute.LanguageAbbreviation] = translationAttribute.Translation;
            }
        }

        private void ApplyNameTranslationAttributes(SchemaDetail schemaDetail, Type type)
        {
            var nameTranslationAttributes = type.GetTypeInfo()
                .GetCustomAttributes(typeof(PictureparkNameTranslationAttribute), true)
                .Select(i => i as PictureparkNameTranslationAttribute)
                .ToList();

            foreach (var translationAttribute in nameTranslationAttributes)
            {
                schemaDetail.Names[translationAttribute.LanguageAbbreviation] = translationAttribute.Translation;
            }
        }

        private void ApplyDisplayPatternAttributes(SchemaDetail schemaDetail, Type type)
        {
            var displayPatternAttributes = type.GetTypeInfo()
                .GetCustomAttributes(typeof(PictureparkDisplayPatternAttribute), true)
                .Select(i => i as PictureparkDisplayPatternAttribute)
                .ToList();

            foreach (var displayPatternAttribute in displayPatternAttributes)
            {
                var displayPattern = new DisplayPattern
                {
                    DisplayPatternType = displayPatternAttribute.Type,
                    TemplateEngine = displayPatternAttribute.TemplateEngine,
                    Templates = new TranslatedStringDictionary { { "x-default", displayPatternAttribute.DisplayPattern } }
                };

                schemaDetail.DisplayPatterns.Add(displayPattern);

                //// TODO: Implement fallback for not provided patterns?
            }
        }

        private List<ContractPropertyInfo> GetProperties(Type type)
        {
            var contactPropertiesInfo = new List<ContractPropertyInfo>();

            var objectContract = _contractResolver.ResolveContract(type) as JsonObjectContract;
            if (objectContract != null)
            {
                foreach (var property in objectContract.Properties.Where(p => p.DeclaringType == type))
                {
                    var typeInfo = property.PropertyType.GetTypeInfo();
                    var name = property.PropertyName;

                    // Check if name is overridden by JsonProperty attribute
                    var attributes = property.AttributeProvider.GetAttributes(false);
                    var jsonProperty = attributes.OfType<JsonPropertyAttribute>().FirstOrDefault();
                    if (jsonProperty != null)
                        name = jsonProperty.PropertyName;

                    // Skip ignored properties
                    if (_ignoredProperties.Contains(name))
                        continue;

                    var propertyInfo = new ContractPropertyInfo()
                    {
                        Name = name,
                        IsOverwritten = type.GetTypeInfo().BaseType?.GetRuntimeProperty(property.UnderlyingName) != null
                    };

                    if (IsSimpleType(property.PropertyType))
                    {
                        HandleSimpleTypes(property, propertyInfo);
                    }
                    else
                    {
                        // either list or dictionary
                        if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(typeInfo))
                        {
                            if (typeInfo.ImplementedInterfaces.Contains(typeof(IDictionary)) ||
                                (typeInfo.GenericTypeArguments.Any() && typeInfo.GenericTypeArguments.First().GetTypeInfo().ImplementedInterfaces.Contains(typeof(IDictionary))))
                            {
                                propertyInfo.IsArray = typeInfo.ImplementedInterfaces.Contains(typeof(IList));
                                propertyInfo.IsDictionary = true;
                                propertyInfo.TypeName = property.PropertyType.Name;
                            }
                            else
                            {
                                var propertyGenericArg = typeInfo.GenericTypeArguments.First();

                                if (IsSimpleType(propertyGenericArg))
                                {
                                    HandleSimpleTypes(property, propertyInfo);
                                }
                                else
                                {
                                    propertyInfo.IsCustomType = true;
                                    propertyInfo.TypeName = propertyGenericArg.Name;
                                    propertyInfo.FullName = propertyGenericArg.FullName;
                                    propertyInfo.AssemblyFullName = propertyGenericArg.GetTypeInfo().Assembly.FullName;

                                    // Check for prevention of an infinite loop
                                    if (propertyGenericArg.FullName != type.FullName)
                                    {
                                        propertyInfo.TypeProperties.AddRange(
                                            GetProperties(propertyGenericArg));
                                    }
                                }

                                propertyInfo.IsArray = true;

                                if (attributes.OfType<PictureparkReferenceAttribute>().Any() ||
                                    property.PropertyType.GenericTypeArguments.FirstOrDefault().GetTypeInfo().GetCustomAttribute<PictureparkReferenceAttribute>() != null)
                                {
                                    propertyInfo.IsReference = true;
                                }
                            }
                        }
                        else
                        {
                            propertyInfo.IsCustomType = true;
                            propertyInfo.TypeName = property.PropertyType.Name;
                            propertyInfo.FullName = property.PropertyType.FullName;
                            propertyInfo.AssemblyFullName = typeInfo.Assembly.FullName;

                            if (typeInfo.GetCustomAttribute<PictureparkReferenceAttribute>() != null)
                            {
                                propertyInfo.IsReference = true;
                            }

                            // Check for prevention of an infinite loop
                            if (property.PropertyType.FullName != type.FullName)
                            {
                                propertyInfo.TypeProperties.AddRange(
                                    GetProperties(property.PropertyType));
                            }
                        }
                    }

                    var searchAttributes = property.AttributeProvider
                        .GetAttributes(true)
                        .Where(i => i.GetType().GetTypeInfo().ImplementedInterfaces.Any(j => j == typeof(IPictureparkAttribute)))
                        .Select(i => i as IPictureparkAttribute)
                        .ToList();

                    propertyInfo.PictureparkAttributes = searchAttributes;

                    contactPropertiesInfo.Add(propertyInfo);
                }
            }

            return contactPropertiesInfo;
        }

        private void HandleSimpleTypes(JsonProperty property, ContractPropertyInfo propertyInfo)
        {
            var typeInfo = property.PropertyType.GetTypeInfo();
            propertyInfo.IsSimpleType = true;

            // it's a case of: nullable / enum type property
            if (typeInfo.GenericTypeArguments != null && typeInfo.GenericTypeArguments.Length > 0)
            {
                var propertyGenericArg = property.PropertyType.GenericTypeArguments.First();
                var underlyingType = Nullable.GetUnderlyingType(propertyGenericArg);
                propertyGenericArg = underlyingType ?? propertyGenericArg;

                propertyInfo.FullName = propertyGenericArg.FullName;
                propertyInfo.AssemblyFullName = propertyGenericArg.GetTypeInfo().Assembly.FullName;

                if (propertyGenericArg.GetTypeInfo().IsEnum)
                {
                    propertyInfo.IsEnum = true;
                    propertyInfo.TypeName = propertyGenericArg.Name;
                }
                else
                {
                    if (propertyGenericArg == typeof(DateTimeOffset))
                    {
                        propertyInfo.TypeName = TypeCode.DateTime.ToString();
                    }
                    else
                    {
                        // TODO: Find better solution for this
                        propertyInfo.TypeName = typeof(Type)
                            .GetRuntimeMethod("GetTypeCode", new[] { typeof(Type) })
                            .Invoke(null, new object[] { propertyGenericArg })
                            .ToString();
                    }
                }
            }
            else
            {
                if (property.PropertyType == typeof(DateTimeOffset))
                {
                    propertyInfo.TypeName = TypeCode.DateTime.ToString();
                }
                else
                {
                    propertyInfo.TypeName = typeof(Type)
                        .GetRuntimeMethod("GetTypeCode", new[] { typeof(Type) })
                        .Invoke(null, new object[] { property.PropertyType })
                        .ToString();
                }

                propertyInfo.FullName = property.PropertyType.FullName;
                propertyInfo.AssemblyFullName = typeInfo.Assembly.FullName;
            }
        }

        private FieldBase GetField(ContractPropertyInfo property)
        {
            FieldBase fieldData = null;

            if (property.IsDictionary)
            {
                if (property.TypeName == "TranslatedStringDictionary")
                {
                    fieldData = new FieldTranslatedString
                    {
                        Required = false,
                        Fixed = false,
                        Index = true,
                        SimpleSearch = true,
                        MultiLine = false,
                        Boost = 1,
                        Analyzers = new List<AnalyzerBase>
                        {
                            new LanguageAnalyzer
                            {
                                SimpleSearch = true
                            }
                        }
                    };
                }
                else if (property.IsArray)
                {
                    fieldData = new FieldDictionaryArray();
                }
                else
                {
                    fieldData = new FieldDictionary();
                }
            }
            else if (property.IsEnum)
            {
                Type enumType = Type.GetType($"{property.FullName}, {property.AssemblyFullName}");

                // TODO: Handle enums
            }
            else if (property.IsSimpleType)
            {
                if (!Enum.TryParse(property.TypeName, out TypeCode typeCode))
                {
                    throw new Exception($"Parsing to TypeCode enumarated object failed for string value: {property.TypeName}.");
                }

                if (property.IsArray)
                {
                    switch (typeCode)
                    {
                        case TypeCode.String:
                            fieldData = new FieldStringArray
                            {
                                Index = true
                            };
                            break;

                        case TypeCode.DateTime:
                            fieldData = new FieldDateTimeArray
                            {
                                Index = true
                            };
                            break;

                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            fieldData = new FieldLongArray
                            {
                                Index = true
                            };
                            break;

                        default:
                            throw new Exception($"TypeCode {typeCode} is not supported.");
                    }
                }
                else
                {
                    var stringInfos = property.PictureparkAttributes.OfType<PictureparkStringAttribute>().SingleOrDefault();

                    switch (typeCode)
                    {
                        case TypeCode.String:
                            fieldData = new FieldString
                            {
                                Index = true,
                                SimpleSearch = true,
                                Boost = 1,
                                Analyzers = new List<AnalyzerBase>
                                {
                                    new SimpleAnalyzer
                                    {
                                        SimpleSearch = true
                                    }
                                },
                                MultiLine = stringInfos?.MultiLine ?? false
                            };
                            break;
                        case TypeCode.DateTime:
                            fieldData = new FieldDateTime
                            {
                                Index = true
                            };
                            break;
                        case TypeCode.Boolean:
                            fieldData = new FieldBoolean
                            {
                                Index = true
                            };
                            break;
                        case TypeCode.Int16:
                        case TypeCode.Int32:
                        case TypeCode.Int64:
                            fieldData = new FieldLong
                            {
                                Index = true
                            };
                            break;
                        case TypeCode.Decimal:
                        case TypeCode.Double:
                        case TypeCode.Single:
                            fieldData = new FieldDecimal
                            {
                                Index = true
                            };
                            break;
                        default:
                            throw new Exception($"TypeCode {typeCode} is not supported.");
                    }
                }
            }
            else
            {
                var schemaIndexing = property.PictureparkAttributes.OfType<PictureparkSchemaIndexingAttribute>().SingleOrDefault();
                var schemaItemInfos = property.PictureparkAttributes.OfType<PictureparkTagboxAttribute>().SingleOrDefault();
                var listItemCreateTemplateAttribute = property.PictureparkAttributes.OfType<PictureparkListItemCreateTemplateAttribute>().SingleOrDefault();
                var relationInfos = property.PictureparkAttributes.OfType<PictureparkContentRelationAttribute>().ToList();
                var maxRecursionInfos = property.PictureparkAttributes.OfType<PictureparkMaximumRecursionAttribute>().SingleOrDefault();

                var relationTypes = new List<RelationType>();
                if (relationInfos.Any())
                {
                    relationTypes = relationInfos.Select(i => new RelationType
                    {
                        Id = i.Name,
                        Filter = i.Filter,
                        TargetDocType = i.TargetDocType,
                        Names = new TranslatedStringDictionary { { "x-default", i.Name } }
                    }).ToList();
                }

                if (property.IsArray)
                {
                    if (relationInfos.Any())
                    {
                        fieldData = new FieldMultiRelation
                        {
                            Index = true,
                            RelationTypes = relationTypes,
                            SchemaId = property.TypeName,
                            SchemaIndexingInfo = schemaIndexing?.SchemaIndexingInfo
                        };
                    }
                    else if (property.IsReference)
                    {
                        fieldData = new FieldMultiTagbox
                        {
                            Index = true,
                            SimpleSearch = true,
                            SchemaId = property.TypeName,
                            Filter = schemaItemInfos?.Filter,
                            SchemaIndexingInfo = schemaIndexing?.SchemaIndexingInfo,
                            ListItemCreateTemplate = listItemCreateTemplateAttribute?.ListItemCreateTemplate
                        };
                    }
                    else
                    {
                        fieldData = new FieldMultiFieldset
                        {
                            Index = true,
                            SimpleSearch = true,
                            SchemaId = property.TypeName,
                            SchemaIndexingInfo = schemaIndexing?.SchemaIndexingInfo
                        };
                    }
                }
                else
                {
                    if (relationInfos.Any())
                    {
                        fieldData = new FieldSingleRelation
                        {
                            Index = true,
                            SimpleSearch = true,
                            RelationTypes = relationTypes,
                            SchemaId = property.TypeName,
                            SchemaIndexingInfo = schemaIndexing?.SchemaIndexingInfo
                        };
                    }
                    else if (property.TypeName == "GeoPoint")
                    {
                        fieldData = new FieldGeoPoint
                        {
                            Index = true
                        };
                    }
                    else if (property.IsReference)
                    {
                        fieldData = new FieldSingleTagbox
                        {
                            Index = true,
                            SimpleSearch = true,
                            SchemaId = property.TypeName,
                            Filter = schemaItemInfos?.Filter,
                            SchemaIndexingInfo = schemaIndexing?.SchemaIndexingInfo,
                            ListItemCreateTemplate = listItemCreateTemplateAttribute?.ListItemCreateTemplate
                        };
                    }
                    else
                    {
                        fieldData = new FieldSingleFieldset
                        {
                            Index = true,
                            SimpleSearch = true,
                            SchemaId = property.TypeName,
                            SchemaIndexingInfo = schemaIndexing?.SchemaIndexingInfo
                        };
                    }
                }
            }

            if (fieldData == null)
                throw new Exception($"Could not find type for {property.Name}");

            foreach (var attribute in property.PictureparkAttributes)
            {
                if (attribute is PictureparkSearchAttribute searchAttribute)
                {
                    fieldData.Index = searchAttribute.Index;
                    fieldData.SimpleSearch = searchAttribute.SimpleSearch;
                    if (fieldData.GetType().GetRuntimeProperty("Boost") != null)
                    {
                        fieldData.GetType().GetRuntimeProperty("Boost").SetValue(fieldData, searchAttribute.Boost);
                    }
                }

                if (attribute is PictureparkRequiredAttribute)
                {
                    fieldData.Required = true;
                }

                if (attribute is PictureparkMaximumLengthAttribute maxLengthAttribute)
                {
                    fieldData.GetType().GetRuntimeProperty("MaximumLength").SetValue(fieldData, maxLengthAttribute.Length);
                }

                if (attribute is PictureparkPatternAttribute patternAttribute)
                {
                    fieldData.GetType().GetRuntimeProperty("Pattern").SetValue(fieldData, patternAttribute.Pattern);
                }

                if (attribute is PictureparkNameTranslationAttribute)
                {
                    var translationAttribute = attribute as PictureparkNameTranslationAttribute;
                    if (fieldData.Names == null)
                        fieldData.Names = new TranslatedStringDictionary();

                    fieldData.Names[translationAttribute.LanguageAbbreviation] = translationAttribute.Translation;
                }
            }

            var fieldName = property.Name;
            fieldData.Id = fieldName.ToLowerCamelCase();

            if (fieldData.Names == null)
            {
                fieldData.Names = new TranslatedStringDictionary
                {
                    ["x-default"] = fieldName
                };
            }

            var fieldAnalyzers = property.PictureparkAttributes
                .OfType<PictureparkAnalyzerAttribute>()
                .Select(a => a.CreateAnalyzer())
                .ToList();

            if (fieldAnalyzers.Any())
                fieldData.GetType().GetRuntimeProperty("Analyzers").SetValue(fieldData, fieldAnalyzers);

            return fieldData;
        }

        private bool IsSimpleType(Type type)
        {
            return
                type.GetTypeInfo().IsValueType ||
                type.GetTypeInfo().IsPrimitive ||
                new Type[]
                {
                    typeof(string),
                    typeof(decimal),
                    typeof(DateTime),
                    typeof(DateTimeOffset),
                    typeof(TimeSpan),
                    typeof(Guid)
                }.Contains(type) ||
                Convert.GetTypeCode(type) != TypeCode.Object;
        }
    }
}
