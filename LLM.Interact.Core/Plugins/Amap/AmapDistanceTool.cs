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
    public sealed class AmapDistanceTool : AmapBase
    {
        public AmapDistanceTool()
        {
            ApiUrl = "v3/distance";
            ApiKey = "";
        }

        [KernelFunction, Description("距离测量 API 可以测量两个经纬度坐标之间的距离,支持驾车、步行以及球面距离测量")]
        public AmapCmpResponse MapsRegeocode(
            [Description("起点经度，纬度，可以传多个坐标，使用竖线隔离，比如120,30|120,31，坐标格式为：经度，纬度")] string origins,
            [Description("终点经度，纬度，坐标格式为：经度，纬度")] string destination,
            [Description("距离测量类型,1代表驾车距离测量，0代表直线距离测量，3步行距离测量")] string type = "1"
            )
        {
            using (var httplient = new HttpClient { BaseAddress = new Uri(BaseUrl) })
            {
                httplient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("amap-tool", "1.0"));

                Parameters.TryAdd("key", ApiKey);
                Parameters.TryAdd("origins", origins);
                Parameters.TryAdd("destination", destination);
                Parameters.TryAdd("type", string.IsNullOrEmpty(type) ? "1" : type);
                Parameters.TryAdd("source", "ts_mcp");

                AmapCmpResponse cmpResponse = new AmapCmpResponse();
                using var response = httplient.GetAsync(ToUrlString()).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadFromJsonAsync<AmapDistanceResponse>().GetAwaiter().GetResult();
                if (responseContent != null)
                {
                    if (responseContent.Status != 1)
                    {
                        var result = new
                        {
                            responseContent.Results,
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
                            Text = $"Direction Distance failed: ${responseContent.Info},{responseContent.InfoCode}"
                        });
                    }
                }
                else
                {
                    cmpResponse.IsError = true;
                    cmpResponse.Content.Add(new ContentItem()
                    {
                        Text = "Direction Distance failed: request failed"
                    });
                }
                return cmpResponse;
            }
        }
    }
}
