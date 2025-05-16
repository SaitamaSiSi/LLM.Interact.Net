using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Amap
{
    public class Forecast
    {
        public string City { get; set; } = string.Empty;
        public List<Cast> Casts { get; set; } = new List<Cast>();
    }
}
