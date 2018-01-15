﻿using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Picturepark.SDK.V1.Contract.Attributes.Providers
{
    public class BuilderBase
    {
        internal T Clone<T>(T obj)
        {
            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj));
        }

        internal List<T> Replace<T>(IEnumerable<T> items, T oldItem, T newItem)
        {
            var list = items.ToList();
            var index = list.IndexOf(oldItem);
            list.RemoveAt(index);
            list.Insert(index, newItem);
            return list;
        }
    }
}
