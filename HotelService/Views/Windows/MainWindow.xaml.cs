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
                }
                else
                {
                }
            }
            else
            {
                UserNameTextBlock.Text = "Неизвестный пользователь";
                UserRoleTextBlock.Text = "Гость";
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
                else if (clickedButton == ReportsButton)
                    LoadReportsContent();
                else if (clickedButton == UsersButton)
                    LoadUsersContent();
                else if (clickedButton == HandBookButton)
                    LoadHandBookContent();
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

        private void LoadBookingContent()
        {
            MainContent.Content = new Pages.BookingPage();
        }

        private void LoadGuestsContent()
        {
            //MainContent.Content = new Pages.GuestsPage();
        }

        private void LoadRoomsContent()
        {
            // MainContent.Content = new Pages.RoomsPage();
            MessageBox.Show("Модуль 'Номерной фонд' находится в разработке.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadCheckInContent()
        {
            // MainContent.Content = new Pages.CheckInPage();
            MessageBox.Show("Модуль 'Стойка регистрации' находится в разработке.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadPaymentsContent()
        {
            // MainContent.Content = new Pages.PaymentsPage();
            MessageBox.Show("Модуль 'Платежи и счета' находится в разработке.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadReportsContent()
        {
            // MainContent.Content = new Pages.ReportsPage();
            MessageBox.Show("Модуль 'Отчеты и аналитика' находится в разработке.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadUsersContent()
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав доступа к модулю 'Пользователи системы'.",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);

                _currentActiveButton.Style = (Style)FindResource("MenuButtonStyle");
                BookingButton.Style = (Style)FindResource("ActiveMenuButtonStyle");
                _currentActiveButton = BookingButton;

                LoadBookingContent();
                return;
            }

            // MainContent.Content = new Pages.UsersPage();
            MessageBox.Show("Модуль 'Пользователи системы' находится в разработке.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadHandBookContent()
        {
            // Проверка прав доступа - только администратор системы и администратор стойки
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав доступа к модулю 'Справочники'.",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);

                // Возвращаем выделение на предыдущую кнопку
                _currentActiveButton.Style = (Style)FindResource("MenuButtonStyle");
                BookingButton.Style = (Style)FindResource("ActiveMenuButtonStyle");
                _currentActiveButton = BookingButton;

                LoadBookingContent();
                return;
            }

            MainContent.Content = new Pages.HandbookPage();
          
        }

        private void LoadDashboardContent()
        {
            // Загружаем страницу бронирований по умолчанию
            MainContent.Content = new Pages.BookingPage();
        }
        #endregion
    }
}