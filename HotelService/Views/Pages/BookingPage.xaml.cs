using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;
using System.ComponentModel;
using System.Windows.Data;

namespace HotelService.Views.Pages
{
    public partial class BookingPage : Page
    {
        private HotelServiceEntities _context;
        private List<BookingViewModel> _allBookings;
        private ICollectionView _bookingsView;

        private string _searchText = "";
        private int? _selectedStatusId = null;
        private DateTime? _startDate = null;
        private DateTime? _endDate = null;

        public BookingPage()
        {
            InitializeComponent();
            LoadData();
        }

        // Create a view model class to hold the properties we want to display
        public class BookingViewModel
        {
            public int BookingId { get; set; }
            public string GuestFullName { get; set; }
            public string RoomNumber { get; set; }
            public DateTime CheckInDate { get; set; }
            public DateTime CheckOutDate { get; set; }
            public int Adults { get; set; }
            public int Children { get; set; }
            public string Status { get; set; }
            public decimal TotalAmount { get; set; }
            public int GuestId { get; set; }
            public int? RoomId { get; set; }
            public int BookingStatusId { get; set; }

            // Constructor to map from Booking to BookingViewModel
            public BookingViewModel(Booking booking)
            {
                BookingId = booking.BookingId;
                GuestFullName = booking.Guest != null ?
                    $"{booking.Guest.LastName} {booking.Guest.FirstName} {booking.Guest.MiddleName}".Trim() :
                    "Гость не указан";
                RoomNumber = booking.Room != null ? booking.Room.RoomNumber : "Н/Д";
                CheckInDate = booking.CheckInDate;
                CheckOutDate = booking.CheckOutDate;
                Adults = booking.Adults;
                Children = booking.Children;
                Status = booking.BookingStatus != null ? booking.BookingStatus.StatusName : "Неизвестно";
                TotalAmount = booking.TotalAmount;
                GuestId = booking.GuestId;
                RoomId = booking.RoomId;
                BookingStatusId = booking.BookingStatusId;
            }
        }

        private void LoadData()
        {
            try
            {
                _context = new HotelServiceEntities();
                if (StatusFilterComboBox.Items.Count <= 1)
                {
                    var statuses = _context.BookingStatus.ToList();
                    StatusFilterComboBox.Items.Clear();
                    StatusFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все статусы", IsSelected = true });
                    foreach (var status in statuses)
                    {
                        StatusFilterComboBox.Items.Add(status);
                    }
                    StatusFilterComboBox.DisplayMemberPath = "StatusName";
                    StatusFilterComboBox.SelectedValuePath = "StatusId";
                    StatusFilterComboBox.SelectedIndex = 0;
                }

                var bookingsQuery = _context.Booking
                    .Include(b => b.Guest)
                    .Include(b => b.Room)
                    .Include(b => b.BookingStatus)
                    .Include(b => b.BookingSource)
                    .AsNoTracking();
                _allBookings = bookingsQuery.ToList().Select(b => new BookingViewModel(b)).ToList();
                _bookingsView = CollectionViewSource.GetDefaultView(_allBookings);
                _bookingsView.Filter = ApplyFilters;
                BookingsDataGrid.ItemsSource = _bookingsView;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                if (_allBookings == null)
                    _allBookings = new List<BookingViewModel>();

                if (_bookingsView == null)
                {
                    _bookingsView = CollectionViewSource.GetDefaultView(_allBookings);
                    BookingsDataGrid.ItemsSource = _bookingsView;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private bool ApplyFilters(object item)
        {
            if (!(item is BookingViewModel booking))
                return false;

            bool matchesStatus = !_selectedStatusId.HasValue || _selectedStatusId == 0 ||
                                 booking.BookingStatusId == _selectedStatusId.Value;

            bool matchesStartDate = !_startDate.HasValue ||
                                   booking.CheckInDate >= _startDate.Value.Date;

            bool matchesEndDate = !_endDate.HasValue ||
                                 booking.CheckOutDate <= _endDate.Value.Date.AddDays(1).AddSeconds(-1);

            bool matchesSearch = string.IsNullOrEmpty(_searchText) ||
                                booking.GuestFullName.ToLower().Contains(_searchText.ToLower());

            return matchesStatus && matchesStartDate && matchesEndDate && matchesSearch;
        }



        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                if (comboBox.SelectedIndex == 0)
                {
                    _selectedStatusId = null;
                }
                else if (comboBox.SelectedItem is BookingStatus status)
                {
                    _selectedStatusId = status.StatusId;
                }
                else if (comboBox.SelectedItem is ComboBoxItem comboItem)
                {
                    string statusName = comboItem.Content?.ToString();
                    if (statusName == "Подтверждено")
                        _selectedStatusId = 1;
                    else if (statusName == "Ожидает подтверждения")
                        _selectedStatusId = 2;
                    else if (statusName == "Заезд")
                        _selectedStatusId = 3;
                    else if (statusName == "Выезд")
                        _selectedStatusId = 4;
                    else if (statusName == "Отменено")
                        _selectedStatusId = 5;
                    else
                        _selectedStatusId = comboBox.SelectedIndex;
                }

                if (_bookingsView != null)
                {
                    _bookingsView.Refresh();
                    
                }
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == StartDatePicker)
                _startDate = StartDatePicker.SelectedDate;
            else if (sender == EndDatePicker)
                _endDate = EndDatePicker.SelectedDate;

            if (_bookingsView != null)
            {
                _bookingsView.Refresh();
                

            }
        }

        private void SearchGuestTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchGuestTextBox.Text?.Trim();

            if (searchText != null && (searchText.Length >= 3 || searchText.Length == 0))
            {
                _searchText = searchText;

                if (_bookingsView != null)
                {
                    _bookingsView.Refresh();
                }
            }
        }

        private void AddBookingButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для создания бронирований.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Windows.BookingEditWindow();
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditBookingButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для редактирования бронирований.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var booking = (sender as Button)?.DataContext as BookingViewModel;
            if (booking == null) return;

            var dialog = new Windows.BookingEditWindow(booking.BookingId);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void ViewBookingButton_Click(object sender, RoutedEventArgs e)
        {
            var booking = (sender as Button)?.DataContext as BookingViewModel;
            if (booking == null) return;

            var dialog = new Windows.BookingViewWindow(booking.BookingId);
            dialog.ShowDialog();
        }

        private void CancelBookingButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для отмены бронирований.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var booking = (sender as Button)?.DataContext as BookingViewModel;
            if (booking == null) return;

            if (booking.BookingStatusId == 5)
            {
                MessageBox.Show("Данное бронирование уже отменено.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                $"Вы действительно хотите отменить бронирование №{booking.BookingId}?",
                "Подтверждение отмены", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        var bookingToCancel = context.Booking.Find(booking.BookingId);
                        if (bookingToCancel != null)
                        {
                            bookingToCancel.BookingStatusId = 5;
                            context.SaveChanges();
                            MessageBox.Show("Бронирование успешно отменено.", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при отмене бронирования: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BookingsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (BookingsDataGrid.SelectedItem != null)
            {
                var booking = BookingsDataGrid.SelectedItem as BookingViewModel;
                if (booking != null)
                {
                    var dialog = new Windows.BookingViewWindow(booking.BookingId);
                    dialog.ShowDialog();
                }
            }
        }

        private void BookingsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Дополнительная логика при выборе строки в таблице, если потребуется
        }
    }
}