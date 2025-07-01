using Avalonia.Controls;
using Avalonia.Interactivity;
using LLM.Interact.Core.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LLM.Interact.Components.Media
{
    public partial class ImageCarousel : Window
    {
        private ObservableCollection<ImageModel> _remaining = [];

        public ImageCarousel(ImageCarouselParams @params)
        {
            InitializeComponent();

            title.Text = @params.Title;
            _remaining = [.. @params.Images];

            // 初始化轮播图
            UpdateCarousel();
        }

        /// <summary>
        /// 显示图片轮播弹窗
        /// </summary>
        public static async Task<List<ImageModel>> Show(Window owner, ImageCarouselParams @params)
        {
            var dialog = new ImageCarousel(@params);
            var result = await dialog.ShowDialog<List<ImageModel>>(owner);
            return result ?? [];
        }

        private void CloseDialog(object sender, RoutedEventArgs e)
        {
            Close(_remaining.ToList());
        }

        private void Previous(object sender, RoutedEventArgs e)
        {
            if (slides.SelectedIndex > 0)
            {
                slides.SelectedIndex--;
                UpdateImageInfo();
            }
        }

        private void Next(object sender, RoutedEventArgs e)
        {
            if (slides.SelectedIndex < slides.ItemCount - 1)
            {
                slides.SelectedIndex++;
                UpdateImageInfo();
            }
        }

        private void DeleteImage(object sender, RoutedEventArgs e)
        {
            if (slides.SelectedIndex >= 0 && _remaining.Count > slides.SelectedIndex)
            {
                _remaining.RemoveAt(slides.SelectedIndex);

                // 如果删除后没有图片，关闭弹窗
                if (_remaining.Count == 0)
                {
                    Close(new List<ImageModel>());
                }
                UpdateCarousel();
            }
        }

        private void DeleteAllImages(object sender, RoutedEventArgs e)
        {
            _remaining.Clear();
            Close(_remaining.ToList());
        }

        private void UpdateCarousel()
        {
            slides.ItemsSource = _remaining;
            if (_remaining.Count > 0)
            {
                slides.SelectedIndex = Math.Min(slides.SelectedIndex, _remaining.Count - 1);
                UpdateImageInfo();
            }
            else
            {
                // 没有图片时清空信息
                fileName.Text = "";
                ToolTip.SetTip(fileName, fileName.Text);
                fileType.Text = "";
                fileSize.Text = "";
                dimensions.Text = "";
            }
        }

        private void UpdateImageInfo()
        {
            if (slides.SelectedIndex >= 0 && slides.SelectedIndex < _remaining.Count)
            {
                var img = _remaining[slides.SelectedIndex];

                // 获取文件信息
                var fileInfo = new FileInfo(img.AbsolutePath);
                fileName.Text = Path.GetFileName(img.AbsolutePath);
                ToolTip.SetTip(fileName, fileName.Text);
                ImageMimeType.MimeTypes.TryGetValue(Path.GetExtension(img.AbsolutePath).ToLowerInvariant(), out string? mime);
                fileType.Text = $"{Path.GetExtension(img.AbsolutePath).ToUpper().TrimStart('.')}({mime})";
                fileSize.Text = FormatFileSize(fileInfo.Length);
                dimensions.Text = $"{img.Data?.PixelSize.Width}×{img.Data?.PixelSize.Height}";
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size = size / 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        private void OnSelectedIndexChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateImageInfo();
        }
    }
}
