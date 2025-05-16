using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Amap
{
    public class AmapTextSearchResponse : AmapResponseBase
    {
        public Suggestion Suggestion = new Suggestion();
        public List<Poi> Pois = new List<Poi>();
    }
}
