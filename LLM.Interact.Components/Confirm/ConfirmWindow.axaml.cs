using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Threading.Tasks;

namespace LLM.Interact.Components.Confirm
{
    public partial class ConfirmWindow : Window
    {
        public ConfirmWindow()
        {
            InitializeComponent();
        }

        public ConfirmWindow(ConfirmParams @params)
        {
            InitializeComponent();
            Title = @params.Title;
            show_text.Text = @params.Message;
            confirm_btn.IsVisible = @params.ShowConfirm;
            cancel_btn.IsVisible = @params.ShowCancel;
        }

        /// <summary>
        /// 显示方法
        /// </summary>
        /// <param name="owner"></param>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public static async Task<bool> Show(Window owner, ConfirmParams @params)
        {
            var dialog = new ConfirmWindow(@params);
            var result = await dialog.ShowDialog<bool?>(owner);
            return result.HasValue && result.Value;
        }

        /// <summary>
        /// 确定事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnSaveClick(object sender, RoutedEventArgs e)
        {
            Close(true);
        }

        /// <summary>
        /// 取消事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnCancelClick(object sender, RoutedEventArgs e)
        {
            Close(false);
        }
    }
}
