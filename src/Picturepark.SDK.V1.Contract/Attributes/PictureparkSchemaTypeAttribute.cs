﻿using System;

namespace Picturepark.SDK.V1.Contract.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class PictureparkSchemaTypeAttribute : Attribute, IPictureparkAttribute
    {
        public PictureparkSchemaTypeAttribute(SchemaType metadataType)
        {
            SchemaType = metadataType;
        }

        public SchemaType SchemaType { get; }
    }
}
