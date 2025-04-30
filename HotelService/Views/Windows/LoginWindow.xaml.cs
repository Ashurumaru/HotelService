using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Animation;
using HotelService.Data;
using System.Windows.Media;

namespace HotelService.Views.Windows
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            UsernameTextBox.Focus();
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
            Application.Current.Shutdown();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            AttemptLogin();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                PasswordBox.Focus();
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AttemptLogin();
            }
        }

        private void AttemptLogin()
        {
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;

            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Введите имя пользователя и пароль");
                return;
            }
            

            try
            {
                using (var context = new HotelServiceEntities())
                {
                    var user = context.User.FirstOrDefault(u => u.Username == username);

                    if (user == null)
                    {
                        ShowError("Пользователь не найден");
                        return;
                    }

                    if (user.Password != password)
                    {
                        ShowError("Неверный пароль");
                        return;
                    }

                    App.CurrentUser = user;

                    MainWindow mainWindow = new MainWindow();
                    mainWindow.Show();
                    Close();
                }
            }
            catch (Exception ex)
            {
                ShowError("Ошибка подключения к базе данных");

                MessageBox.Show($"Ошибка: {ex.Message}", "Отладка",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowError(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;

            DoubleAnimation animation = new DoubleAnimation
            {
                From = -4,
                To = 4,
                Duration = TimeSpan.FromMilliseconds(50),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3)
            };

            ErrorMessageTextBlock.RenderTransform = new TranslateTransform();
            ErrorMessageTextBlock.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation);
        }
    }
}