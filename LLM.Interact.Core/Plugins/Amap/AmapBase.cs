using System.Collections.Generic;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace LLM.Interact.Core.Plugins.Amap
{
    public class AmapBase
    {
        protected string BaseUrl { get; set; } = "https://restapi.amap.com";
        protected string ApiUrl { get; set; } = string.Empty;
        protected string ApiKey { get; set; } = string.Empty;
        protected Dictionary<string, string> Parameters { get; set; } = new Dictionary<string, string>();
        protected JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            IndentSize = 2,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // 禁用 Unicode 转义
        };

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

        public class AmapCmpResponse
        {
            public List<ContentItem> Content { get; set; } = new List<ContentItem>();
            public bool IsError { get; set; }
        }

        public class ContentItem
        {
            public string Type { get; set; } = "text";
            public string Text { get; set; } = string.Empty;
        }
    }
}
