using Microsoft.SemanticKernel;
using Newtonsoft.Json.Linq;
using System.ComponentModel;

namespace LLM.Interact.Core.Plugins
{
    public class MenuPlugin
    {
        [KernelFunction, Description("Provides a list of specials from the menu.")]
        public object GetSpecials()
        {
            // 创建动态对象保留原始键名和格式
            var specials = new JObject();
            specials["Special Soup"] = "Clam Chowder";
            specials["Special Salad"] = "Cobb Salad";
            specials["Special Drink"] = "Chai Tea";
            return specials;
        }

        [KernelFunction, Description("Provides the price of the requested menu item.")]
        public object GetItemPrice([Description("The name of the menu item.")] string menuItem)
        {
            double price = 9.99;
            return new
            {
                price
            };
        }
    }
}
