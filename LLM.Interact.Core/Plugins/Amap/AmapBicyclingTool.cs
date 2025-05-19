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
    public sealed class AmapBicyclingTool : AmapBase
    {
        public AmapBicyclingTool()
        {
            ApiUrl = "v4/direction/bicycling";
            ApiKey = "";
        }

        [KernelFunction, Description("骑行路径规划用于规划骑行通勤方案，规划时会考虑天桥、单行线、封路等情况。最大支持 500km 的骑行路线规划")]
        public AmapCmpResponse MapsBicycling(
            [Description("出发点经纬度，坐标格式为：经度，纬度")] string origin,
            [Description("目的地经纬度，坐标格式为：经度，纬度")] string destination
            )
        {
            using (var httplient = new HttpClient { BaseAddress = new Uri(BaseUrl) })
            {
                httplient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("amap-tool", "1.0"));

                Parameters.TryAdd("key", ApiKey);
                Parameters.TryAdd("origin", origin);
                Parameters.TryAdd("destination", destination);
                Parameters.TryAdd("source", "ts_mcp");

                AmapCmpResponse cmpResponse = new AmapCmpResponse();
                using var response = httplient.GetAsync(ToUrlString()).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadFromJsonAsync<AmapPathPlanResponse>().GetAwaiter().GetResult();
                if (responseContent != null)
                {
                    if (responseContent.Errcode == 0)
                    {
                        var result = new
                        {
                            responseContent.Data,
                        };
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
                            Text = $"Direction bicycling failed: ${responseContent.Info},{responseContent.InfoCode}"
                        });
                    }
                }
                else
                {
                    cmpResponse.IsError = true;
                    cmpResponse.Content.Add(new ContentItem()
                    {
                        Text = "Direction bicycling failed: request failed"
                    });
                }
                return cmpResponse;
            }
        }
    }
}
