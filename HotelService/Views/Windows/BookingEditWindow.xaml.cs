using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class BookingEditWindow : Window
    {
        private HotelServiceEntities _context;
        private Booking _booking;
        private bool _isNewBooking;
        private bool _isInitializing = true;
        private decimal _roomBasePrice = 0;
        private int _nights = 0;

        public BookingEditWindow(int? bookingId = null)
        {
            InitializeComponent();

            _isNewBooking = bookingId == null;
            this.DataContext = this;

            if (_isNewBooking)
            {
                WindowTitleTextBlock.Text = "Создание нового бронирования";
                _booking = new Booking
                {
                    CheckInDate = DateTime.Now.Date,
                    CheckOutDate = DateTime.Now.Date.AddDays(1),
                    Adults = 1,
                    Children = 0
                };
            }
            else
            {
                WindowTitleTextBlock.Text = $"Редактирование бронирования #{bookingId}";
                LoadBooking(bookingId.Value);
            }

            LoadComboBoxData();
            InitializeFields();

            _isInitializing = false;
            CalculateTotalAmount();
        }

        public bool IsNewBooking
        {
            get { return _isNewBooking; }
        }

        private void LoadBooking(int bookingId)
        {
            try
            {
                _context = new HotelServiceEntities();
                _booking = _context.Booking
                    .Include(b => b.Guest)
                    .Include(b => b.Room)
                    .Include(b => b.Room.RoomType)
                    .Include(b => b.BookingStatus)
                    .Include(b => b.BookingSource)
                    .FirstOrDefault(b => b.BookingId == bookingId);

                if (_booking == null)
                {
                    MessageBox.Show("Бронирование не найдено.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке бронирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void LoadComboBoxData()
        {
            try
            {
                _context = new HotelServiceEntities();

                var statuses = _context.BookingStatus.ToList();
                StatusComboBox.ItemsSource = statuses;

                var sources = _context.BookingSource.ToList();
                SourceComboBox.ItemsSource = sources;

                var guests = _context.Guest.Select(g => new
                {
                    g.GuestId,
                    FullName = g.LastName + " " + g.FirstName + " " + g.MiddleName
                }).ToList();
                GuestComboBox.ItemsSource = guests;

                LoadAvailableRooms();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadAvailableRooms()
        {
            if (CheckInDatePicker.SelectedDate == null || CheckOutDatePicker.SelectedDate == null)
                return;

            try
            {
                DateTime checkIn = CheckInDatePicker.SelectedDate.Value;
                DateTime checkOut = CheckOutDatePicker.SelectedDate.Value;
                int adultsCount = int.Parse((AdultsComboBox.SelectedItem as ComboBoxItem).Content.ToString());

                var occupiedRoomIds = _context.Booking
                    .Where(b => b.BookingId != (_isNewBooking ? 0 : _booking.BookingId)) 
                    .Where(b => b.BookingStatusId != 5) 
                    .Where(b => (checkIn < b.CheckOutDate && checkOut > b.CheckInDate)) 
                    .Select(b => b.RoomId)
                    .ToList();

                var availableRooms = _context.Room
                    .Include(r => r.RoomType)
                    .Where(r => r.RoomStatusId == 1) 
                    .Where(r => r.MaxOccupancy >= adultsCount) 
                    .Where(r => !occupiedRoomIds.Contains(r.RoomId))
                    .Select(r => new
                    {
                        r.RoomId,
                        r.RoomNumber,
                        r.RoomType.TypeName,
                        r.MaxOccupancy,
                        r.BasePrice,
                        r.FloorNumber,
                        DisplayName = $"№{r.RoomNumber} - {r.RoomType.TypeName} ({r.MaxOccupancy} чел.)"
                    })
                    .ToList();

                if (!_isNewBooking && _booking.RoomId.HasValue)
                {
                    var currentRoom = _context.Room
                        .Include(r => r.RoomType)
                        .Where(r => r.RoomId == _booking.RoomId)
                        .Select(r => new
                        {
                            r.RoomId,
                            r.RoomNumber,
                            r.RoomType.TypeName,
                            r.MaxOccupancy,
                            r.BasePrice,
                            r.FloorNumber,
                            DisplayName = $"№{r.RoomNumber} - {r.RoomType.TypeName} ({r.MaxOccupancy} чел.) [текущий]"
                        })
                        .FirstOrDefault();

                    if (currentRoom != null && !availableRooms.Any(r => r.RoomId == currentRoom.RoomId))
                    {
                        availableRooms.Add(currentRoom);
                    }
                }

                RoomComboBox.ItemsSource = availableRooms;

                if (availableRooms.Count == 0)
                {
                    ShowError("На выбранные даты нет доступных номеров.");
                }
                else
                {
                    ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке доступных номеров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeFields()
        {
            CheckInDatePicker.SelectedDate = _booking.CheckInDate;
            CheckOutDatePicker.SelectedDate = _booking.CheckOutDate;

            AdultsComboBox.SelectedIndex = _booking.Adults - 1; 
            ChildrenComboBox.SelectedIndex = _booking.Children; 

            NotesTextBox.Text = _booking.Notes;

            if (!_isNewBooking)
            {
                GuestComboBox.SelectedValue = _booking.GuestId;
                UpdateGuestInfo(_booking.GuestId);

                StatusComboBox.SelectedValue = _booking.BookingStatusId;
                SourceComboBox.SelectedValue = _booking.SourceId;

                if (_booking.RoomId.HasValue)
                {
                    RoomComboBox.SelectedValue = _booking.RoomId;
                    UpdateRoomInfo(_booking.RoomId.Value);
                }
            }
            else
            {
                StatusComboBox.SelectedValue = 2; 

                SourceComboBox.SelectedValue = 1;
            }
        }

        private void UpdateGuestInfo(int guestId)
        {
            try
            {
                var guest = _context.Guest.Find(guestId);
                if (guest != null)
                {
                    PhoneTextBlock.Text = string.IsNullOrEmpty(guest.Phone) ? "-" : guest.Phone;
                    EmailTextBlock.Text = string.IsNullOrEmpty(guest.Email) ? "-" : guest.Email;
                    VipStatusTextBlock.Text = guest.IsVIP ? "Да" : "Нет";
                    LoyaltyPointsTextBlock.Text = $"{guest.CurrentPoints} баллов";
                }
                else
                {
                    ResetGuestInfo();
                }
            }
            catch
            {
                ResetGuestInfo();
            }
        }

        private void ResetGuestInfo()
        {
            PhoneTextBlock.Text = "-";
            EmailTextBlock.Text = "-";
            VipStatusTextBlock.Text = "Нет";
            LoyaltyPointsTextBlock.Text = "0 баллов";
        }

        private void UpdateRoomInfo(int roomId)
        {
            try
            {
                var room = _context.Room
                    .Include(r => r.RoomType)
                    .FirstOrDefault(r => r.RoomId == roomId);

                if (room != null)
                {
                    RoomTypeTextBlock.Text = room.RoomType.TypeName;
                    OccupancyTextBlock.Text = $"{room.MaxOccupancy} чел.";
                    FloorTextBlock.Text = room.FloorNumber.ToString();
                    PriceTextBlock.Text = $"{room.BasePrice:N2} ₽";
                    _roomBasePrice = room.BasePrice;
                    CalculateTotalAmount();
                }
                else
                {
                    ResetRoomInfo();
                }
            }
            catch
            {
                ResetRoomInfo();
            }
        }

        private void ResetRoomInfo()
        {
            RoomTypeTextBlock.Text = "-";
            OccupancyTextBlock.Text = "-";
            FloorTextBlock.Text = "-";
            PriceTextBlock.Text = "-";
            _roomBasePrice = 0;
            CalculateTotalAmount();
        }

        private void CalculateTotalAmount()
        {
            if (_isInitializing) return;

            try
            {
                if (CheckInDatePicker.SelectedDate.HasValue && CheckOutDatePicker.SelectedDate.HasValue)
                {
                    _nights = (int)(CheckOutDatePicker.SelectedDate.Value - CheckInDatePicker.SelectedDate.Value).TotalDays;
                    NightsTextBlock.Text = _nights.ToString();

                    if (_roomBasePrice > 0 && _nights > 0)
                    {
                        decimal totalAmount = _roomBasePrice * _nights;
                        TotalAmountTextBlock.Text = $"{totalAmount:N2} ₽";
                    }
                    else
                    {
                        NightsTextBlock.Text = "0";
                        TotalAmountTextBlock.Text = "0.00 ₽";
                    }
                }
            }
            catch
            {
                NightsTextBlock.Text = "0";
                TotalAmountTextBlock.Text = "0.00 ₽";
            }
        }

        private void DatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CheckInDatePicker.SelectedDate.HasValue && CheckOutDatePicker.SelectedDate.HasValue)
            {
                if (CheckOutDatePicker.SelectedDate <= CheckInDatePicker.SelectedDate)
                {
                    ShowError("Дата выезда должна быть позже даты заезда.");
                    CheckOutDatePicker.SelectedDate = CheckInDatePicker.SelectedDate.Value.AddDays(1);
                    return;
                }
            }

            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;
            LoadAvailableRooms();
            CalculateTotalAmount();
        }

        private void GuestsCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;
            LoadAvailableRooms();
        }

        private void GuestComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (GuestComboBox.SelectedValue != null)
            {
                int guestId = (int)GuestComboBox.SelectedValue;
                UpdateGuestInfo(guestId);
            }
            else
            {
                ResetGuestInfo();
            }
        }

        private void RoomComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            if (RoomComboBox.SelectedValue != null)
            {
                int roomId = (int)RoomComboBox.SelectedValue;
                UpdateRoomInfo(roomId);
            }
            else
            {
                ResetRoomInfo();
            }
        }

        private void SelectGuestButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция выбора гостя будет реализована позже.", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SelectRoomButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция выбора номера будет реализована позже.", "Информация",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                SaveBooking();
            }
        }

        private bool ValidateForm()
        {
            if (GuestComboBox.SelectedValue == null)
            {
                ShowError("Необходимо выбрать гостя.");
                return false;
            }

            if (!CheckInDatePicker.SelectedDate.HasValue)
            {
                ShowError("Необходимо выбрать дату заезда.");
                return false;
            }

            if (!CheckOutDatePicker.SelectedDate.HasValue)
            {
                ShowError("Необходимо выбрать дату выезда.");
                return false;
            }

            if (CheckOutDatePicker.SelectedDate <= CheckInDatePicker.SelectedDate)
            {
                ShowError("Дата выезда должна быть позже даты заезда.");
                return false;
            }

            if (AdultsComboBox.SelectedItem == null)
            {
                ShowError("Необходимо указать количество взрослых.");
                return false;
            }

            if (StatusComboBox.SelectedValue == null)
            {
                ShowError("Необходимо выбрать статус бронирования.");
                return false;
            }

            if (SourceComboBox.SelectedValue == null)
            {
                ShowError("Необходимо выбрать источник бронирования.");
                return false;
            }

            if (RoomComboBox.SelectedValue == null)
            {
                ShowError("Необходимо выбрать номер.");
                return false;
            }

            return true;
        }

        private void SaveBooking()
        {
            try
            {
                _booking.GuestId = (int)GuestComboBox.SelectedValue;
                _booking.RoomId = (int)RoomComboBox.SelectedValue;
                _booking.CheckInDate = CheckInDatePicker.SelectedDate.Value;
                _booking.CheckOutDate = CheckOutDatePicker.SelectedDate.Value;
                _booking.Adults = int.Parse((AdultsComboBox.SelectedItem as ComboBoxItem).Content.ToString());
                _booking.Children = int.Parse((ChildrenComboBox.SelectedItem as ComboBoxItem).Content.ToString());
                _booking.BookingStatusId = (int)StatusComboBox.SelectedValue;
                _booking.SourceId = (int)SourceComboBox.SelectedValue;
                _booking.Notes = NotesTextBox.Text;

                decimal totalAmount = _roomBasePrice * _nights;
                _booking.TotalAmount = totalAmount;

                using (var context = new HotelServiceEntities())
                {
                    if (_isNewBooking)
                    {
                        context.Booking.Add(_booking);
                    }
                    else
                    {
                        var bookingToUpdate = context.Booking.Find(_booking.BookingId);
                        if (bookingToUpdate != null)
                        {
                            context.Entry(bookingToUpdate).CurrentValues.SetValues(_booking);
                        }
                    }

                    context.SaveChanges();
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении бронирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowError(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
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
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}