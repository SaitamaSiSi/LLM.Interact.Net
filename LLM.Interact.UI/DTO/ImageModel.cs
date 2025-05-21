using System;
using System.ComponentModel;
using Yitter.IdGenerator;

namespace LLM.Interact.UI.DTO
{
    public class ImageModel : INotifyPropertyChanged
    {
        public long ID { get; set; } = YitIdHelper.NextId();
        public string AbsolutePath { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
