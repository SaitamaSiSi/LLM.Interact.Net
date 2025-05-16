using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Generic;

namespace LLM.Interact.Core.Plugins.Amap
{
    public sealed class AmapGeoTool : AmapBase
    {
        public AmapGeoTool()
        {
            ApiUrl = "v3/geocode/geo";
            ApiKey = "";
        }

        [KernelFunction, Description("将详细的结构化地址转换为经纬度坐标。支持对地标性名胜景区、建筑物名称解析为经纬度坐标")]
        public AmapCmpResponse MapsRegeocode(
            [Description("待解析的结构化地址信息")] string address,
            [Description("指定查询的城市")] string city
            )
        {
            using (var httplient = new HttpClient { BaseAddress = new Uri(BaseUrl) })
            {
                httplient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("amap-tool", "1.0"));

                Parameters.TryAdd("key", ApiKey);
                Parameters.TryAdd("address", address);
                Parameters.TryAdd("city", city);
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
                            Results = responseContent.Geocodes
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
                            Text = $"Geocoding failed: ${responseContent.Info},{responseContent.InfoCode}"
                        });
                    }
                }
                else
                {
                    cmpResponse.IsError = true;
                    cmpResponse.Content.Add(new ContentItem()
                    {
                        Text = "Geocoding failed: request failed"
                    });
                }
                return cmpResponse;
            }
        }

        private class Geocode
        {
            public string Country { get; set; } = string.Empty;
            public string Province { get; set; } = string.Empty;
            public string City { get; set; } = string.Empty;
            public string CityCode { get; set; } = string.Empty;
            public string District { get; set; } = string.Empty;
            public string Street { get; set; } = string.Empty;
            public string Number { get; set; } = string.Empty;
            public string Adcode { get; set; } = string.Empty;
            public string Location { get; set; } = string.Empty;
            public string Level { get; set; } = string.Empty;
        }
        private class AmapRegeocodeResponse : AmapResponseBase
        {
            public List<Geocode> Geocodes { get; set; } = new List<Geocode>();
        }

    }
}
