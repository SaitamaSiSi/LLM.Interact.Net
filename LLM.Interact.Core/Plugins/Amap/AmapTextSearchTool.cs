using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Net.Http.Json;
using System.Text.Json;
using LLM.Interact.Core.Models.Amap;

namespace LLM.Interact.Core.Plugins.Amap
{
    public sealed class AmapTextSearchTool : AmapBase
    {
        public AmapTextSearchTool()
        {
            ApiUrl = "v3/place/text";
            ApiKey = "";
        }

        [KernelFunction, Description("关键词搜，根据用户传入关键词，搜索出相关的POI")]
        public AmapCmpResponse MapsRegeocode(
            [Description("搜索关键词")] string keywords,
            [Description("查询城市")] string city = "",
            [Description("POI类型，比如加油站")] string types = ""
            )
        {
            using (var httplient = new HttpClient { BaseAddress = new Uri(BaseUrl) })
            {
                httplient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("amap-tool", "1.0"));

                Parameters.TryAdd("key", ApiKey);
                Parameters.TryAdd("keywords", keywords);
                Parameters.TryAdd("city", string.IsNullOrEmpty(city) ? "" : city);
                Parameters.TryAdd("types", string.IsNullOrEmpty(types) ? "" : types);
                Parameters.TryAdd("citylimit", "false");
                Parameters.TryAdd("source", "ts_mcp");

                AmapCmpResponse cmpResponse = new AmapCmpResponse();
                using var response = httplient.GetAsync(ToUrlString()).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadFromJsonAsync<AmapTextSearchResponse>().GetAwaiter().GetResult();
                if (responseContent != null)
                {
                    if (responseContent.Status != 1)
                    {
                        var result = new
                        {
                            responseContent.Suggestion,
                            responseContent.Pois,
                        }
                    ;
                        cmpResponse.Content.Add(new ContentItem
                        {
                            Text = JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true })
                        });
                    }
                    else
                    {
                        cmpResponse.IsError = true;
                        cmpResponse.Content.Add(new ContentItem()
                        {
                            Text = $"Text Search failed: ${responseContent.Info},{responseContent.InfoCode}"
                        });
                    }
                }
                else
                {
                    cmpResponse.IsError = true;
                    cmpResponse.Content.Add(new ContentItem()
                    {
                        Text = "Text Search failed: request failed"
                    });
                }
                return cmpResponse;
            }
        }
    }
}
