using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HotelService.Views.Windows
{
    public partial class MainWindow : Window
    {
        private Button _currentActiveButton;

        public MainWindow()
        {
            InitializeComponent();
            LoadUserInfo();

            _currentActiveButton = BookingButton;

            LoadDashboardContent();
        }

        private void LoadUserInfo()
        {
            if (App.CurrentUser != null)
            {
                UserNameTextBlock.Text = App.CurrentUser.LastName + " " + App.CurrentUser.FirstName + " " + App.CurrentUser.MiddleName;
                UserRoleTextBlock.Text = GetRoleName(App.CurrentUser.RoleId);

                if (!string.IsNullOrEmpty(App.CurrentUser.FirstName) && !string.IsNullOrEmpty(App.CurrentUser.LastName))
                {
                    string firstInitial = App.CurrentUser.FirstName.Substring(0, 1).ToUpper();
                    string lastInitial = App.CurrentUser.LastName.Substring(0, 1).ToUpper();
                    UserInitialsTextBlock.Text = $"{firstInitial}{lastInitial}";
                }
                else
                {
                    UserInitialsTextBlock.Text = "ГО"; 
                }
            }
            else
            {
                UserNameTextBlock.Text = "Неизвестный пользователь";
                UserRoleTextBlock.Text = "Гость";
                UserInitialsTextBlock.Text = "ГО";
            }
        }

        private string GetRoleName(int roleId)
        {
            switch (roleId)
            {
                case 1:
                    return "Администратор системы";
                case 2:
                    return "Администратор стойки регистрации";
                case 3:
                    return "Сотрудник хозяйственной службы";
                default:
                    return "Пользователь";
            }
        }

        #region Обработчики событий кнопок управления окном

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

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Maximized;
            MaximizeButton.Visibility = Visibility.Collapsed;
            RestoreButton.Visibility = Visibility.Visible;
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Normal;
            MaximizeButton.Visibility = Visibility.Visible;
            RestoreButton.Visibility = Visibility.Collapsed;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        #endregion

        #region Обработчики событий основного меню

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedButton = sender as Button;

            if (clickedButton != null && _currentActiveButton != clickedButton)
            {
                _currentActiveButton.Style = (Style)FindResource("MenuButtonStyle");

                clickedButton.Style = (Style)FindResource("ActiveMenuButtonStyle");

                _currentActiveButton = clickedButton;

                if (clickedButton == BookingButton)
                    LoadBookingContent();
                else if (clickedButton == GuestsButton)
                    LoadGuestsContent();
                else if (clickedButton == RoomsButton)
                    LoadRoomsContent();
                else if (clickedButton == CheckInButton)
                    LoadCheckInContent();
                else if (clickedButton == PaymentsButton)
                    LoadPaymentsContent();
                else if (clickedButton == ReportsButton)
                    LoadReportsContent();
                else if (clickedButton == UsersButton)
                    LoadUsersContent();
                else if (clickedButton == HandBookButton)
                    LoadSettingsContent();
            }
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show(
                "Вы действительно хотите выйти из системы?",
                "Подтверждение выхода",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                App.CurrentUser = null;

                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();

                Close();
            }
        }

        #endregion

        #region Методы загрузки контента

        private void LoadDashboardContent()
        {
            // Временная реализация для демонстрации
            // В реальной системе здесь должно быть:
            // MainContent.Content = new DashboardView();

            TextBlock tempContent = new TextBlock
            {
                Text = "Панель управления",
                FontSize = 24,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#322A28")),
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            MainContent.Content = tempContent;
        }

        private void LoadBookingContent()
        {
            // Временная реализация
            TextBlock tempContent = new TextBlock
            {
                Text = "Управление бронированием",
                FontSize = 24,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#322A28")),
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            MainContent.Content = tempContent;
        }

        private void LoadGuestsContent()
        {
            // Временная реализация
            TextBlock tempContent = new TextBlock
            {
                Text = "Гости и клиенты",
                FontSize = 24,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#322A28")),
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            MainContent.Content = tempContent;
        }

        private void LoadRoomsContent()
        {
            // Временная реализация
            TextBlock tempContent = new TextBlock
            {
                Text = "Номерной фонд",
                FontSize = 24,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#322A28")),
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            MainContent.Content = tempContent;
        }

        private void LoadCheckInContent()
        {
            // Временная реализация
            TextBlock tempContent = new TextBlock
            {
                Text = "Стойка регистрации",
                FontSize = 24,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#322A28")),
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            MainContent.Content = tempContent;
        }

        private void LoadPaymentsContent()
        {
            // Временная реализация
            TextBlock tempContent = new TextBlock
            {
                Text = "Платежи и счета",
                FontSize = 24,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#322A28")),
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            MainContent.Content = tempContent;
        }

        private void LoadReportsContent()
        {
            // Временная реализация
            TextBlock tempContent = new TextBlock
            {
                Text = "Отчеты и аналитика",
                FontSize = 24,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#322A28")),
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            MainContent.Content = tempContent;
        }

        private void LoadUsersContent()
        {
            // Временная реализация
            TextBlock tempContent = new TextBlock
            {
                Text = "Пользователи системы",
                FontSize = 24,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#322A28")),
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            MainContent.Content = tempContent;
        }

        private void LoadSettingsContent()
        {
            // Временная реализация
            TextBlock tempContent = new TextBlock
            {
                Text = "Настройки системы",
                FontSize = 24,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#322A28")),
                FontWeight = FontWeights.SemiBold,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            MainContent.Content = tempContent;
        }

        #endregion
    }
}