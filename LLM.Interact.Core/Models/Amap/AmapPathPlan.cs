using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Amap
{
    public class AmapPathPlan
    {
        public string Origin = string.Empty;
        public string Destination = string.Empty;
        public List<AmapPath> Paths = new List<AmapPath>();
    }
}
