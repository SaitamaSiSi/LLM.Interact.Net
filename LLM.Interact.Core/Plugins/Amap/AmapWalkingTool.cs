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
    public sealed class AmapWalkingTool : AmapBase
    {
        public AmapWalkingTool()
        {
            ApiUrl = "v3/direction/walking";
            ApiKey = "";
        }

        [KernelFunction, Description("步行路径规划 API 可以根据输入起点终点经纬度坐标规划100km 以内的步行通勤方案，并且返回通勤方案的数据")]
        public AmapCmpResponse MapsDirectionWalking(
            [Description("出发点经度，纬度，坐标格式为：经度，纬度")] string origin,
            [Description("目的地经度，纬度，坐标格式为：经度，纬度")] string destination
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
                    if (responseContent.Status != 1)
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
                            Text = $"Direction Walking failed: ${responseContent.Info},{responseContent.InfoCode}"
                        });
                    }
                }
                else
                {
                    cmpResponse.IsError = true;
                    cmpResponse.Content.Add(new ContentItem()
                    {
                        Text = "Direction Walking failed: request failed"
                    });
                }
                return cmpResponse;
            }
        }
    }
}
