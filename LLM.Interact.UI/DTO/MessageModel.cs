using System;
using System.ComponentModel;
using Yitter.IdGenerator;

namespace LLM.Interact.UI.DTO
{
    public class MessageModel : INotifyPropertyChanged
    {
        public long ID { get; set; } = YitIdHelper.NextId();
        public bool IsUserMessage { get; set; }
        private string _content = "";
        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }
        }
        public DateTime Timestamp { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
