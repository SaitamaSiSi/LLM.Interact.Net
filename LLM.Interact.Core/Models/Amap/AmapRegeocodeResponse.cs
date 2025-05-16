namespace LLM.Interact.Core.Models.Amap
{
    public class AmapRegeocodeResponse : AmapResponseBase
    {
        public Regeocode Regeocode { get; set; } = new Regeocode();
    }
}
