using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace HotelService.Views.Windows
{
    public partial class ImageViewWindow : Window
    {
        public ImageViewWindow(ImageSource imageSource, string description = null)
        {
            InitializeComponent();

            DisplayImage.Source = imageSource;

            if (!string.IsNullOrWhiteSpace(description))
            {
                DescriptionTextBlock.Text = description;
                DescriptionTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CloseButton2_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}