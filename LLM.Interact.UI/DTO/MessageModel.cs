using System;
using System.ComponentModel;

namespace LLM.Interact.UI.DTO
{
    public class MessageModel : INotifyPropertyChanged
    {
        public bool IsUserMessage { get; set; }
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
