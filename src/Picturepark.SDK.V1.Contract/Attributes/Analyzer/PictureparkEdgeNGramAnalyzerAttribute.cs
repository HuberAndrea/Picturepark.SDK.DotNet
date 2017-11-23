﻿using System;

namespace Picturepark.SDK.V1.Contract.Attributes.Analyzer
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PictureparkEdgeNGramAnalyzerAttribute : PictureparkAnalyzerAttribute
    {
        public override AnalyzerBase CreateAnalyzer()
        {
            return new EdgeNGramAnalyzer { SimpleSearch = SimpleSearch };
        }
    }
}
