namespace LLM.Interact.Core.Models.Amap
{
    public class AmapIpLocationResponse : AmapResponseBase
    {
        public string Province { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Adcode { get; set; } = string.Empty;
        public string Rectangle { get; set; } = string.Empty;
    }
}
