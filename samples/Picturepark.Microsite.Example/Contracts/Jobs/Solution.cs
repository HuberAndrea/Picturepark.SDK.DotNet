﻿using Newtonsoft.Json;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Contract.Attributes;
using Picturepark.SDK.V1.Contract.Interfaces;

namespace Picturepark.Microsite.Example.Contracts.Jobs
{
	[PictureparkSchemaType(SchemaType.List)]
	[PictureparkSchemaType(SchemaType.Struct)]
	[PictureparkDisplayPattern(DisplayPatternType.Name, TemplateEngine.DotLiquid, "{{data.solution.name}}")]
	[PictureparkDisplayPattern(DisplayPatternType.List, TemplateEngine.DotLiquid, "{{data.solution.name}}")]
	[PictureparkDisplayPattern(DisplayPatternType.Thumbnail, TemplateEngine.DotLiquid, "{{data.solution.name}}")]
	[PictureparkDisplayPattern(DisplayPatternType.Detail, TemplateEngine.DotLiquid, "{{data.solution.name}}")]
	public class Solution : IReference
	{
		public TranslatedStringDictionary Name { get; set; }

		[PictureparkNameTranslation("x-default", "Description")]
		[PictureparkNameTranslation("en", "Description")]
		[PictureparkNameTranslation("de", "Beschreibung")]
		public TranslatedStringDictionary Description { get; set; }

		[JsonIgnore]
		public string refId { get; set; }
	}
}
