using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Amap
{
    public class AmapGeoResponse : AmapResponseBase
    {
        public List<Geocode> Geocodes { get; set; } = new List<Geocode>();
    }
}
