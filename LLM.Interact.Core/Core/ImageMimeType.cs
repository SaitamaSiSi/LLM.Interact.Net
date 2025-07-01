using System;
using System.Collections.Generic;

namespace LLM.Interact.Core.Core
{
    public class ImageMimeType
    {
        public static readonly Dictionary<string, string> MimeTypes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".png", "image/png" },
            { ".gif", "image/gif" },
            { ".pdf", "application/pdf" }
        };
    }
}
