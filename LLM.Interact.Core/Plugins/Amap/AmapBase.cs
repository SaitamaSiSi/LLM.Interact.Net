using System.Collections.Generic;

namespace LLM.Interact.Core.Plugins.Amap
{
    public class AmapBase
    {
        protected string BaseUrl { get; set; } = "https://restapi.amap.com";
        protected string ApiUrl { get; set; } = string.Empty;
        protected string ApiKey { get; set; } = string.Empty;
        protected Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();

        protected string BuildGetParams()
        {
            string ret = "";
            if (Parameters.Count > 0)
            {
                ret = "?";
                foreach (var item in Parameters)
                {
                    ret += $"{item.Key}={item.Value}&";
                }
                ret.TrimEnd('&');
            }
            return ret;
        }
        protected string ToUrlString()
        {
            return ApiUrl + BuildGetParams();
        }
    }
}
