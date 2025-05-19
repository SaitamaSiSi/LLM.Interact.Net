namespace LLM.Interact.Core.Models.Amap
{
    public class AmapResponseBase
    {
        /// <summary>
        /// V3使用
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// V4使用
        /// </summary>
        public int Errcode { get; set; }

        public string Info { get; set; } = string.Empty;
        public string InfoCode { get; set; } = string.Empty;
    }
}
