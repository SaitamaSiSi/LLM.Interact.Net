using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Amap
{
    public class AmapWeatherResponse : AmapResponseBase
    {
        public List<Forecast> ForeCasts { get; set; } = new List<Forecast>();
    }
}
