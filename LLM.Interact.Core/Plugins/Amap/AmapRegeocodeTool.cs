using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Net.Http.Json;
using System.Text.Json;

namespace LLM.Interact.Core.Plugins.Amap
{
    public sealed class AmapRegeocodeTool : AmapBase
    {
        public AmapRegeocodeTool()
        {
            ApiUrl = "v3/geocode/regeo";
            ApiKey = "";
        }

        [KernelFunction, Description("将一个高德经纬度坐标转换为行政区划地址信息")]
        public AmapCmpResponse MapsRegeocode(
            [Description("经纬度")] string location
            )
        {
            using (var httplient = new HttpClient { BaseAddress = new Uri(BaseUrl) })
            {
                httplient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("amap-tool", "1.0"));

                Parameters.TryAdd("key", ApiKey);
                Parameters.TryAdd("location", location);
                Parameters.TryAdd("source", "ts_mcp");

                AmapCmpResponse cmpResponse = new AmapCmpResponse();
                using var response = httplient.GetAsync(ToUrlString()).GetAwaiter().GetResult();
                var responseContent = response.Content.ReadFromJsonAsync<AmapRegeocodeResponse>().GetAwaiter().GetResult();
                if (responseContent != null)
                {
                    if (responseContent.Status != 1)
                    {
                        var result = new
                        {
                            responseContent.Regeocode.AddressComponent.Province,
                            responseContent.Regeocode.AddressComponent.City,
                            responseContent.Regeocode.AddressComponent.District,
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
                            Text = $"RGeocoding failed: ${responseContent.Info},{responseContent.InfoCode}"
                        });
                    }
                }
                else
                {
                    cmpResponse.IsError = true;
                    cmpResponse.Content.Add(new ContentItem()
                    {
                        Text = "RGeocoding failed: request failed"
                    });
                }
                return cmpResponse;
            }
        }

        private class AddressComponent
        {
            public string Province { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string District { get; set; } = string.Empty;
        }
        private class Regeocode
        {
            public AddressComponent AddressComponent { get; set; } = new AddressComponent();
        }
        private class AmapRegeocodeResponse : AmapResponseBase
        {
            public Regeocode Regeocode { get; set; } = new Regeocode();
        }

    }
}
