﻿using Newtonsoft.Json.Linq;
using System;

namespace Picturepark.SDK.V1.Contract
{
    public partial class ListItemDetail
    {
        /// <summary>Converts the content of a list item detail to the given type.</summary>
        /// <typeparam name="T">The content type.</typeparam>
        /// <returns>The converted content.</returns>
        public T ConvertTo<T>()
        {
            return Content is T ? (T)Content : ((JObject)Content).ToObject<T>();
        }
    }
}
