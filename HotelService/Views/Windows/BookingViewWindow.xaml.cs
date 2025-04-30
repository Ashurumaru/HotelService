using System;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class BookingViewWindow : Window
    {
        private readonly int _bookingId;
        private Booking _booking;

        public BookingViewWindow(int bookingId)
        {
            InitializeComponent();
            _bookingId = bookingId;
            LoadBookingData();
        }

        private void LoadBookingData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _booking = context.Booking
                        .Include("Guest")
                        .Include("Room")
                        .Include("Room.RoomType")
                        .Include("BookingStatus")
                        .Include("BookingSource")
                        .FirstOrDefault(b => b.BookingId == _bookingId);

                    if (_booking == null)
                    {
                        MessageBox.Show("Бронирование не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    PopulateBookingData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void PopulateBookingData()
        {
            BookingNumberTextBlock.Text = _booking.BookingId.ToString();
            BookingTitleTextBlock.Text = $"Бронирование #{_booking.BookingId}";

            StatusTextBlock.Text = _booking.BookingStatus.StatusName;
            RoomTextBlock.Text = $"Номер {_booking.Room.RoomNumber}";

            int nights = (_booking.CheckOutDate.Date - _booking.CheckInDate.Date).Days;
            NightsTextBlock.Text = $"{nights} {GetNightsText(nights)}";
            NightsCountTextBlock.Text = nights.ToString();

            DateRangeTextBlock.Text = $"{_booking.CheckInDate:dd.MM.yyyy} - {_booking.CheckOutDate:dd.MM.yyyy}";

            // Информация о госте
            if (_booking.Guest != null)
            {
                GuestNameTextBlock.Text = _booking.Guest.LastName + "" + _booking.Guest.FirstName + "" + _booking.Guest.MiddleName;
                GuestPhoneTextBlock.Text = _booking.Guest.Phone ?? "Не указан";
                GuestEmailTextBlock.Text = _booking.Guest.Email ?? "Не указан";
                GuestAddressTextBlock.Text = _booking.Guest.Address ?? "Не указан";

                GuestVIPTextBlock.Text = _booking.Guest.IsVIP ? "Да" : "Нет";
                GuestVIPTextBlock.Foreground = _booking.Guest.IsVIP
                    ? FindResource("AccentColor") as System.Windows.Media.SolidColorBrush
                    : FindResource("TextSecondaryColor") as System.Windows.Media.SolidColorBrush;

                GuestPointsTextBlock.Text = _booking.Guest.CurrentPoints.ToString();
            }
            else
            {
                GuestNameTextBlock.Text = "Гость не указан";
                GuestPhoneTextBlock.Text = "Не указан";
                GuestEmailTextBlock.Text = "Не указан";
                GuestAddressTextBlock.Text = "Не указан";
                GuestVIPGrid.Visibility = Visibility.Collapsed;
                GuestPointsTextBlock.Text = "0";
            }

            // Детали бронирования
            SourceTextBlock.Text = _booking.BookingSource != null ? _booking.BookingSource.SourceName : "Не указан";
            CheckInDateTextBlock.Text = _booking.CheckInDate.ToString("dd.MM.yyyy");
            CheckOutDateTextBlock.Text = _booking.CheckOutDate.ToString("dd.MM.yyyy");
            AdultsTextBlock.Text = _booking.Adults.ToString();
            ChildrenTextBlock.Text = _booking.Children.ToString();

            RoomNumberTextBlock.Text = _booking.Room.RoomNumber;
            RoomTypeTextBlock.Text = _booking.Room?.RoomType?.TypeName ?? "Не указан";

            // Финансовая информация
            TotalAmountTextBlock.Text = string.Format("{0:N2} ₽", _booking.TotalAmount);
            DepositAmountTextBlock.Text = string.Format("{0:N2} ₽", _booking.DepositAmount);

            DepositPaidTextBlock.Text = _booking.DepositPaid ? "Да" : "Нет";
            DepositPaidTextBlock.Foreground = _booking.DepositPaid
                ? FindResource("SuccessColor") as System.Windows.Media.SolidColorBrush
                : FindResource("WarningColor") as System.Windows.Media.SolidColorBrush;

            NotesTextBlock.Text = string.IsNullOrEmpty(_booking.Notes) ? "Нет примечаний" : _booking.Notes;
        }

        private string GetNightsText(int nights)
        {
            if (nights % 10 == 1 && nights % 100 != 11)
                return "ночь";
            else if ((nights % 10 == 2 || nights % 10 == 3 || nights % 10 == 4) &&
                    (nights % 100 < 10 || nights % 100 > 20))
                return "ночи";
            else
                return "ночей";
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