using Newtonsoft.Json;
using Picturepark.SDK.V1.Contract;
using Picturepark.SDK.V1.Contract.Attributes;
using Picturepark.SDK.V1.Contract.SystemTypes;

namespace Picturepark.Microsite.Example.Contracts
{
	public class Legend : Relation
	{
		public TranslatedStringDictionary LegendTitle { get; set; }
        public TranslatedStringDictionary LegendDescription { get; set; }
    }
}
