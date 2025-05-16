using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Linq;
using LLM.Interact.Core.Models.Amap;

namespace LLM.Interact.Core.Plugins.Amap
{
    public sealed class AmapSearchDetialTool : AmapBase
    {
        public AmapSearchDetialTool()
        {
            ApiUrl = "v3/place/detail";
            ApiKey = "";
        }

        [KernelFunction, Description("查询关键词搜或者周边搜获取到的POI ID的详细信息")]
        public AmapCmpResponse MapsRegeocode(
            [Description("关键词搜或者周边搜获取到的POI ID")] string id
            )
        {
            using (var httplient = new HttpClient { BaseAddress = new Uri(BaseUrl) })
            {
                httplient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("amap-tool", "1.0"));

                Parameters.TryAdd("key", ApiKey);
                Parameters.TryAdd("id", id);
                Parameters.TryAdd("source", "ts_mcp");

                AmapCmpResponse cmpResponse = new AmapCmpResponse();
                using var response = httplient.GetAsync(ToUrlString()).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadFromJsonAsync<AmapSearchDetialResponse>().GetAwaiter().GetResult();
                if (responseContent != null)
                {
                    if (responseContent.Status != 1)
                    {
                        var poi = responseContent.Pois.FirstOrDefault();
                        var result = new
                        {
                            poi.Id,
                            poi.Name,
                            poi.Location,
                            poi.Address,
                            poi.BusinessArea,
                            City = poi.CityName,
                            poi.Type,
                            poi.Alias,
                            Photos = poi.Photos.Count > 0 ? poi.Photos.FirstOrDefault() : null,
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
                            Text = $"Get poi detail failed: ${responseContent.Info},{responseContent.InfoCode}"
                        });
                    }
                }
                else
                {
                    cmpResponse.IsError = true;
                    cmpResponse.Content.Add(new ContentItem()
                    {
                        Text = "Get poi detail failed: request failed"
                    });
                }
                return cmpResponse;
            }
        }
    }
}
