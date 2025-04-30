using System;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using HotelService.Data;
using System.Collections.Generic;
using System.Globalization;
using System.Data.Entity;
using System.Windows.Media;

namespace HotelService.Views.Windows
{
    public partial class BookingEditWindow : Window
    {
        private readonly int? _bookingId;
        private Booking _booking;
        private Guest _selectedGuest;
        private Room _selectedRoom;
        private bool _isInitializing = true;
        private int _nights = 1;

        public BookingEditWindow(int? bookingId = null)
        {
            InitializeComponent();
            _bookingId = bookingId;

            if (_bookingId.HasValue)
            {
                WindowTitleTextBlock.Text = "Редактирование бронирования";
            }
            else
            {
                WindowTitleTextBlock.Text = "Создание нового бронирования";
            }

            LoadReferenceData();

            if (_bookingId.HasValue)
            {
                LoadBookingData();
            }
            else
            {
                InitializeNewBooking();
            }

            _isInitializing = false;
        }
        private void LoadReferenceData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    // Загрузка статусов бронирования
                    var statuses = context.BookingStatus.OrderBy(s => s.StatusId).ToList();
                    StatusComboBox.ItemsSource = statuses;
                    StatusComboBox.DisplayMemberPath = "StatusName";
                    StatusComboBox.SelectedValuePath = "StatusId";

                    // Загрузка источников бронирования
                    var sources = context.BookingSource.OrderBy(s => s.SourceId).ToList();
                    SourceComboBox.ItemsSource = sources;
                    SourceComboBox.DisplayMemberPath = "SourceName";
                    SourceComboBox.SelectedValuePath = "SourceId";

                    // Загрузка финансовых статусов
                    var financialStatuses = context.FinancialStatus.OrderBy(s => s.StatusId).ToList();
                    FinancialStatusComboBox.ItemsSource = financialStatuses;
                    FinancialStatusComboBox.DisplayMemberPath = "StatusName";
                    FinancialStatusComboBox.SelectedValuePath = "StatusId";

                    // Загрузка сотрудников
                    var users = context.User
                        .Where(u => u.RoleId == 1 || u.RoleId == 2) // Только админы и администраторы стойки
                        .OrderBy(u => u.LastName)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке справочных данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void LoadBookingData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _booking = context.Booking
                        .Include(b => b.Guest)
                        .Include(b => b.Room)
                        .Include(b => b.Room.RoomType)
                        .Include(b => b.BookingStatus)
                        .Include(b => b.BookingSource)
                        .Include(b => b.FinancialStatus)
                        .Include(b => b.User)
                        .FirstOrDefault(b => b.BookingId == _bookingId);

                    if (_booking == null)
                    {
                        MessageBox.Show("Бронирование не найдено.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    // Заполнение полей формы
                    _selectedGuest = _booking.Guest;
                    DisplayGuestInfo();

                    _selectedRoom = _booking.Room;
                    DisplayRoomInfo();

                    StatusComboBox.SelectedValue = _booking.BookingStatusId;
                    SourceComboBox.SelectedValue = _booking.SourceId;

                    CheckInDatePicker.SelectedDate = _booking.CheckInDate;
                    CheckOutDatePicker.SelectedDate = _booking.CheckOutDate;

                    _nights = (_booking.CheckOutDate.Date - _booking.CheckInDate.Date).Days;

                    AdultsTextBox.Text = _booking.Adults.ToString();
                    ChildrenTextBox.Text = _booking.Children.ToString();

                    TotalAmountTextBox.Text = _booking.TotalAmount.ToString("F2");

                    if (_booking.DepositAmount.HasValue)
                    {
                        DepositAmountTextBox.Text = _booking.DepositAmount.Value.ToString("F2");
                    }
                    else
                    {
                        DepositAmountTextBox.Text = "0.00";
                    }

                    DepositPaidCheckBox.IsChecked = _booking.DepositPaid;

                    if (_booking.IssueDate != DateTime.MinValue)
                    {
                        IssueDatePicker.SelectedDate = _booking.IssueDate;
                    }
                    else
                    {
                        IssueDatePicker.SelectedDate = DateTime.Today;
                    }

                    if (_booking.DueDate.HasValue)
                    {
                        DueDatePicker.SelectedDate = _booking.DueDate.Value;
                    }

                    if (_booking.FinancialStatusId.HasValue)
                    {
                        FinancialStatusComboBox.SelectedValue = _booking.FinancialStatusId.Value;
                    }

                    NotesTextBox.Text = _booking.Notes;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных бронирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void InitializeNewBooking()
        {
            CheckInDatePicker.SelectedDate = DateTime.Today;
            CheckOutDatePicker.SelectedDate = DateTime.Today.AddDays(1);
            IssueDatePicker.SelectedDate = DateTime.Today;
            DueDatePicker.SelectedDate = DateTime.Today.AddDays(14); // По умолчанию 14 дней на оплату

            StatusComboBox.SelectedIndex = 0;  // Первый статус (обычно "Подтверждено")
            SourceComboBox.SelectedIndex = 0;  // Первый источник
            FinancialStatusComboBox.SelectedIndex = 0; // Первый финансовый статус

            AdultsTextBox.Text = "1";
            ChildrenTextBox.Text = "0";

            TotalAmountTextBox.Text = "0.00";
            DepositAmountTextBox.Text = "0.00";
            DepositPaidCheckBox.IsChecked = false;
        }

        private void DisplayGuestInfo()
        {
            if (_selectedGuest != null)
            {
                string fullName = _selectedGuest.LastName + " " + _selectedGuest.FirstName + " " + _selectedGuest.MiddleName;
                GuestNameTextBlock.Text = fullName;
                GuestPhoneTextBlock.Text = _selectedGuest.Phone ?? "Не указан";
                GuestEmailTextBlock.Text = _selectedGuest.Email ?? "Не указан";
                GuestVIPTextBlock.Text = _selectedGuest.IsVIP ? "Да" : "Нет";

                if (_selectedGuest.IsVIP)
                {
                    GuestVIPTextBlock.Foreground = FindResource("AccentColor") as SolidColorBrush;
                }
                else
                {
                    GuestVIPTextBlock.Foreground = FindResource("TextSecondaryColor") as SolidColorBrush;
                }
            }
            else
            {
                GuestNameTextBlock.Text = "Не выбран";
                GuestPhoneTextBlock.Text = "—";
                GuestEmailTextBlock.Text = "—";
                GuestVIPTextBlock.Text = "Нет";
                GuestVIPTextBlock.Foreground = FindResource("TextSecondaryColor") as SolidColorBrush;
            }
        }

        private void DisplayRoomInfo()
        {
            if (_selectedRoom != null)
            {
                RoomNumberTextBlock.Text = _selectedRoom.RoomNumber;
                RoomTypeTextBlock.Text = _selectedRoom.RoomType?.TypeName ?? "Не указан";
                RoomFloorTextBlock.Text = _selectedRoom.FloorNumber.ToString();
                RoomCapacityTextBlock.Text = _selectedRoom.MaxOccupancy.ToString();
                RoomPriceTextBlock.Text = string.Format("{0:N2} ₽", _selectedRoom.BasePrice);

                if (!_isInitializing)
                {
                    CalculateTotalAmount();
                }
            }
            else
            {
                RoomNumberTextBlock.Text = "Не выбран";
                RoomTypeTextBlock.Text = "—";
                RoomFloorTextBlock.Text = "—";
                RoomCapacityTextBlock.Text = "—";
                RoomPriceTextBlock.Text = "—";
            }
        }

        private void CalculateTotalAmount()
        {
            if (_isInitializing || _selectedRoom == null ||
                CheckInDatePicker.SelectedDate == null ||
                CheckOutDatePicker.SelectedDate == null)
            {
                return;
            }

            try
            {
                DateTime checkIn = CheckInDatePicker.SelectedDate.Value;
                DateTime checkOut = CheckOutDatePicker.SelectedDate.Value;
                _nights = (checkOut.Date - checkIn.Date).Days;

                if (_nights <= 0)
                {
                    ValidationMessageTextBlock.Text = "Дата выезда должна быть позже даты заезда.";
                    ValidationMessageTextBlock.Visibility = Visibility.Visible;
                    TotalAmountTextBox.Text = "0.00";
                    return;
                }
                else
                {
                    ValidationMessageTextBlock.Visibility = Visibility.Collapsed;
                }

                decimal basePrice = _selectedRoom.BasePrice;
                decimal totalAmount = basePrice * _nights;

                // Логика для расчета дополнительных сборов за гостей
                int adults = 0;
                int.TryParse(AdultsTextBox.Text, out adults);

                int children = 0;
                int.TryParse(ChildrenTextBox.Text, out children);

                int totalGuests = adults + children;

                if (totalGuests > _selectedRoom.MaxOccupancy)
                {
                    int extraGuests = totalGuests - _selectedRoom.MaxOccupancy;
                    // Примерно 20% от базовой стоимости за каждого дополнительного гостя
                    decimal extraCharge = basePrice * 0.2m * extraGuests * _nights;
                    totalAmount += extraCharge;
                }

                TotalAmountTextBox.Text = totalAmount.ToString("F2");
            }
            catch
            {
                TotalAmountTextBox.Text = "0.00";
            }
        }

        private void SelectGuestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var guestSelectWindow = new GuestSelectWindow();
                if (guestSelectWindow.ShowDialog() == true)
                {
                    _selectedGuest = guestSelectWindow.SelectedGuest;
                    DisplayGuestInfo();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выборе гостя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SelectRoomButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем даты для проверки доступности
                DateTime? checkInDate = CheckInDatePicker.SelectedDate;
                DateTime? checkOutDate = CheckOutDatePicker.SelectedDate;

                if (!checkInDate.HasValue || !checkOutDate.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите даты заезда и выезда перед выбором номера.",
                        "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var roomSelectWindow = new RoomSelectWindow(checkInDate.Value, checkOutDate.Value, _bookingId);
                if (roomSelectWindow.ShowDialog() == true)
                {
                    _selectedRoom = roomSelectWindow.SelectedRoom;
                    DisplayRoomInfo();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выборе номера: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void CheckInDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitializing && CheckInDatePicker.SelectedDate.HasValue)
            {
                // Если дата выезда не выбрана или меньше даты заезда, устанавливаем дату выезда на следующий день
                if (!CheckOutDatePicker.SelectedDate.HasValue ||
                    CheckOutDatePicker.SelectedDate.Value <= CheckInDatePicker.SelectedDate.Value)
                {
                    CheckOutDatePicker.SelectedDate = CheckInDatePicker.SelectedDate.Value.AddDays(1);
                }

                CalculateTotalAmount();
            }
        }

        private void CheckOutDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitializing)
            {
                CalculateTotalAmount();
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9\,\.]+");

            // Проверяем, что вводятся только числа или десятичный разделитель
            bool isMatch = regex.IsMatch(e.Text);

            // Проверяем, не пытается ли пользователь добавить второй десятичный разделитель
            if (!isMatch)
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null)
                {
                    string futureText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
                    isMatch = futureText.Count(c => c == ',' || c == '.') > 1;
                }
            }

            e.Handled = isMatch;
        }

        private void TotalAmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitializing && string.IsNullOrEmpty(TotalAmountTextBox.Text))
            {
                TotalAmountTextBox.Text = "0.00";
            }
        }

        private bool ValidateBooking()
        {
            List<string> errors = new List<string>();

            if (_selectedGuest == null)
            {
                errors.Add("Необходимо выбрать гостя.");
            }

            if (_selectedRoom == null)
            {
                errors.Add("Необходимо выбрать номер.");
            }

            if (!CheckInDatePicker.SelectedDate.HasValue)
            {
                errors.Add("Необходимо выбрать дату заезда.");
            }

            if (!CheckOutDatePicker.SelectedDate.HasValue)
            {
                errors.Add("Необходимо выбрать дату выезда.");
            }

            if (CheckInDatePicker.SelectedDate.HasValue && CheckOutDatePicker.SelectedDate.HasValue &&
                CheckInDatePicker.SelectedDate.Value >= CheckOutDatePicker.SelectedDate.Value)
            {
                errors.Add("Дата выезда должна быть позже даты заезда.");
            }

            if (StatusComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать статус бронирования.");
            }

            if (SourceComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать источник бронирования.");
            }

            int adults;
            if (!int.TryParse(AdultsTextBox.Text, out adults) || adults <= 0)
            {
                errors.Add("Количество взрослых должно быть положительным числом.");
            }

            int children;
            if (!int.TryParse(ChildrenTextBox.Text, out children) || children < 0)
            {
                errors.Add("Количество детей должно быть неотрицательным числом.");
            }

            if (adults + children <= 0)
            {
                errors.Add("Общее количество гостей должно быть больше нуля.");
            }

            decimal totalAmount;
            if (!decimal.TryParse(TotalAmountTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out totalAmount) || totalAmount < 0)
            {
                errors.Add("Общая стоимость должна быть неотрицательным числом.");
            }

            decimal depositAmount;
            if (!decimal.TryParse(DepositAmountTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out depositAmount) || depositAmount < 0)
            {
                errors.Add("Сумма депозита должна быть неотрицательным числом.");
            }

            if (depositAmount > totalAmount)
            {
                errors.Add("Сумма депозита не может превышать общую стоимость.");
            }

            if (!IssueDatePicker.SelectedDate.HasValue)
            {
                errors.Add("Необходимо указать дату бронирования.");
            }

            if (errors.Count > 0)
            {
                ValidationMessageTextBlock.Text = string.Join("\n", errors);
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            ValidationMessageTextBlock.Visibility = Visibility.Collapsed;
            return true;
        }

        private void SaveBooking()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    Booking bookingToSave;

                    if (_bookingId.HasValue)
                    {
                        // Редактирование существующего бронирования
                        bookingToSave = context.Booking.Find(_bookingId.Value);
                        if (bookingToSave == null)
                        {
                            MessageBox.Show("Бронирование не найдено в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        // Создание нового бронирования
                        bookingToSave = new Booking();
                        context.Booking.Add(bookingToSave);
                    }

                    bookingToSave.GuestId = _selectedGuest.GuestId;
                    bookingToSave.RoomId = _selectedRoom.RoomId;
                    bookingToSave.BookingStatusId = (int)StatusComboBox.SelectedValue;
                    bookingToSave.SourceId = (int)SourceComboBox.SelectedValue;

                    bookingToSave.CheckInDate = CheckInDatePicker.SelectedDate.Value;
                    bookingToSave.CheckOutDate = CheckOutDatePicker.SelectedDate.Value;

                    bookingToSave.Adults = int.Parse(AdultsTextBox.Text);
                    bookingToSave.Children = int.Parse(ChildrenTextBox.Text);

                    bookingToSave.TotalAmount = decimal.Parse(TotalAmountTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture);
                    bookingToSave.DepositAmount = decimal.Parse(DepositAmountTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture);
                    bookingToSave.DepositPaid = DepositPaidCheckBox.IsChecked ?? false;

                    bookingToSave.IssueDate = IssueDatePicker.SelectedDate.Value;

                    if (DueDatePicker.SelectedDate.HasValue)
                    {
                        bookingToSave.DueDate = DueDatePicker.SelectedDate.Value;
                    }

                    if (FinancialStatusComboBox.SelectedValue != null)
                    {
                        bookingToSave.FinancialStatusId = (int)FinancialStatusComboBox.SelectedValue;
                    }

                    // При создании нового бронирования сохраняем информацию о создателе
                    if (!_bookingId.HasValue && App.CurrentUser != null)
                    {
                        bookingToSave.CreatedBy = App.CurrentUser.UserId;
                    }

                    bookingToSave.Notes = NotesTextBox.Text;

                    context.SaveChanges();

                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении бронирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateBooking())
            {
                SaveBooking();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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
            DialogResult = false;
            Close();
        }
    }
}