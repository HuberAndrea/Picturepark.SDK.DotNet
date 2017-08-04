﻿using Newtonsoft.Json.Linq;

namespace Picturepark.SDK.V1.Contract.Extensions
{
	public static class ListItemExtensions
	{
		public static T ConvertToType<T>(this ListItem listItem, string schemaId)
		{
			return (listItem.Content as JObject).ToObject<T>();
		}
	}
}