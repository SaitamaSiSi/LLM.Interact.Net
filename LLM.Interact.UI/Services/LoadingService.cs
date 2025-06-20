using System;

namespace LLM.Interact.UI.Services
{
    public static class LoadingService
    {
        public static event Action<bool, string>? LoadingStateChanged;

        public static void Show(string message = "加载中...")
        {
            LoadingStateChanged?.Invoke(true, message);
        }

        public static void Hide()
        {
            LoadingStateChanged?.Invoke(false, string.Empty);
        }

        public static void SetMessage(string message)
        {
            LoadingStateChanged?.Invoke(true, message);
        }
    }

}
