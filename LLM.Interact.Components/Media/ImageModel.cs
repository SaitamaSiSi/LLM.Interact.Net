using Avalonia.Media.Imaging;
using System.ComponentModel;

namespace LLM.Interact.Components.Media
{
    public class ImageModel : INotifyPropertyChanged
    {
        public long Id { get; set; }
        public string AbsolutePath { get; set; } = string.Empty;
        public Bitmap? Data { get; set; }
        public event PropertyChangedEventHandler? PropertyChanged;
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
