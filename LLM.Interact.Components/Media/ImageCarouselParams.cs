using System.Collections.Generic;

namespace LLM.Interact.Components.Media
{
    public class ImageCarouselParams
    {
        public string Title { get; set; } = "图片预览";
        public List<ImageModel> Images { get; set; } = [];
    }
}
