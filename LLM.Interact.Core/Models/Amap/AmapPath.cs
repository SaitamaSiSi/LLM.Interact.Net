using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Amap
{
    public class AmapPath
    {
        public string Path = string.Empty;
        public int Distance;
        public int Duration;
        public List<AmapStep> Steps = new List<AmapStep>();
    }
}
