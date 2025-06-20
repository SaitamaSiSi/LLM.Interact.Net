using Avalonia.Controls;
using Avalonia.Threading;

namespace LLM.Interact.Components.Loading
{
    public partial class LoadingOverlay : UserControl
    {
        public string Message
        {
            get
            {
                return LoadingText.Text ?? string.Empty;
            }
            set
            {
                if (LoadingText.Text != value)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        LoadingText.Text = Message;
                    });
                }
            }
        }

        public bool IsActivied
        {
            get
            {
                return IsVisible;
            }
            set
            {
                IsVisible = value;
            }
        }

        public LoadingOverlay()
        {
            InitializeComponent();
        }
    }
}