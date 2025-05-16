using System.Collections.Generic;

namespace LLM.Interact.Core.Models.Amap
{
    public class Poi
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public string Location = string.Empty;
        public string Address = string.Empty;
        public string BusinessArea = string.Empty; // BUSINESS_AREA
        public string CityName = string.Empty;
        public string Type = string.Empty;
        public string TypeCode = string.Empty;
        public string Alias = string.Empty;
        public List<Photo> Photos = new List<Photo>();

    }
}
