using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Globalization;
using HotelService.Data;
using System.Data.Entity;

namespace HotelService.Views.Windows
{
    public partial class ServiceEditWindow : Window
    {
        private readonly int _bookingId;
        private readonly int? _bookingItemId;
        private Booking _booking;
        private BookingItem _bookingItem;
        private bool _isEditing;
        private bool _isInitializing = true;

        public ServiceEditWindow(int bookingId, int? bookingItemId = null)
        {
            InitializeComponent();
            _bookingId = bookingId;
            _bookingItemId = bookingItemId;
            _isEditing = _bookingItemId.HasValue;

            if (_isEditing)
            {
                WindowTitleTextBlock.Text = "Редактирование услуги";
            }

            ServiceDatePicker.SelectedDate = DateTime.Today;
            LoadData();
            _isInitializing = false;
            CalculateTotalPrice();
        }

        private void LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _booking = context.Booking
                        .Include(b => b.Room)
                        .FirstOrDefault(b => b.BookingId == _bookingId);

                    if (_booking == null)
                    {
                        MessageBox.Show("Бронирование не найдено.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    var services = context.Service
                        .Include(s => s.ServiceCategory)
                        .OrderBy(s => s.ServiceName)
                        .ToList();

                    ServiceComboBox.ItemsSource = services;

                    var rooms = context.Room
                        .Where(r => r.RoomId == _booking.RoomId ||
                                   r.RoomStatusId == 1) // Assuming 1 is available status
                        .OrderBy(r => r.RoomNumber)
                        .ToList();

                    RoomComboBox.ItemsSource = rooms;

                    if (_booking.RoomId.HasValue)
                    {
                        RoomComboBox.SelectedValue = _booking.RoomId.Value;
                    }
                    else if (rooms.Any())
                    {
                        RoomComboBox.SelectedIndex = 0;
                    }

                    if (_isEditing && _bookingItemId.HasValue)
                    {
                        _bookingItem = context.BookingItem
                            .Include(bi => bi.Service)
                            .Include(bi => bi.Service.ServiceCategory)
                            .FirstOrDefault(bi => bi.ItemId == _bookingItemId.Value);

                        if (_bookingItem != null && _bookingItem.BookingId == _bookingId)
                        {
                            if (_bookingItem.ServiceId.HasValue)
                            {
                                ServiceComboBox.SelectedValue = _bookingItem.ServiceId.Value;
                                CategoryTextBlock.Text = _bookingItem.Service?.ServiceCategory?.CategoryName ?? "Не указана";
                                BasePriceTextBlock.Text = string.Format("{0:N2} ₽", _bookingItem.Service?.Price ?? 0);
                            }
                            else
                            {
                                ServiceComboBox.SelectedIndex = -1;
                            }

                            QuantityTextBox.Text = _bookingItem.Quantity.ToString();
                            UnitPriceTextBox.Text = _bookingItem.UnitPrice.ToString("F2");
                            ServiceDatePicker.SelectedDate = _bookingItem.ItemDate;

                            if (_bookingItem.RoomId.HasValue)
                            {
                                RoomComboBox.SelectedValue = _bookingItem.RoomId.Value;
                            }

                            NotesTextBox.Text = _bookingItem.Description;
                        }
                        else
                        {
                            MessageBox.Show("Услуга не найдена или не относится к данному бронированию.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            DialogResult = false;
                            Close();
                        }
                    }
                    else if (services.Any())
                    {
                        ServiceComboBox.SelectedIndex = 0;
                        var selectedService = ServiceComboBox.SelectedItem as Service;
                        if (selectedService != null)
                        {
                            CategoryTextBlock.Text = selectedService.ServiceCategory?.CategoryName ?? "Не указана";
                            BasePriceTextBlock.Text = string.Format("{0:N2} ₽", selectedService.Price);
                            UnitPriceTextBox.Text = selectedService.Price.ToString("F2");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void BrowseServicesButton_Click(object sender, RoutedEventArgs e)
        {
            var serviceSelectWindow = new ServiceSelectWindow();
            if (serviceSelectWindow.ShowDialog() == true)
            {
                var selectedService = serviceSelectWindow.SelectedService;
                if (selectedService != null)
                {
                    ServiceComboBox.SelectedValue = selectedService.ServiceId;
                    CategoryTextBlock.Text = selectedService.ServiceCategory?.CategoryName ?? "Не указана";
                    BasePriceTextBlock.Text = string.Format("{0:N2} ₽", selectedService.Price);
                    UnitPriceTextBox.Text = selectedService.Price.ToString("F2");
                }
            }
        }

        private void ServiceComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (_isInitializing) return;

            var selectedService = ServiceComboBox.SelectedItem as Service;
            if (selectedService != null)
            {
                CategoryTextBlock.Text = selectedService.ServiceCategory?.CategoryName ?? "Не указана";
                BasePriceTextBlock.Text = string.Format("{0:N2} ₽", selectedService.Price);
                UnitPriceTextBox.Text = selectedService.Price.ToString("F2");
                CalculateTotalPrice();
            }
            else
            {
                CategoryTextBlock.Text = "Не выбрана";
                BasePriceTextBlock.Text = "0.00 ₽";
            }
        }

        private void QuantityTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isInitializing) return;
            CalculateTotalPrice();
        }

        private void UnitPriceTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (_isInitializing) return;
            CalculateTotalPrice();
        }

        private void CalculateTotalPrice()
        {
            try
            {
                int quantity = 1;
                if (!string.IsNullOrEmpty(QuantityTextBox.Text))
                {
                    int.TryParse(QuantityTextBox.Text, out quantity);
                }

                decimal unitPrice = 0;
                if (!string.IsNullOrEmpty(UnitPriceTextBox.Text))
                {
                    decimal.TryParse(UnitPriceTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out unitPrice);
                }

                decimal totalPrice = quantity * unitPrice;
                TotalPriceTextBlock.Text = string.Format("{0:N2} ₽", totalPrice);
            }
            catch
            {
                TotalPriceTextBlock.Text = "0.00 ₽";
            }
        }

        private bool ValidateForm()
        {
            List<string> errors = new List<string>();

            if (ServiceComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать услугу.");
            }

            int quantity;
            if (!int.TryParse(QuantityTextBox.Text, out quantity) || quantity <= 0)
            {
                errors.Add("Количество должно быть положительным числом.");
            }

            decimal unitPrice;
            if (!decimal.TryParse(UnitPriceTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out unitPrice) || unitPrice <= 0)
            {
                errors.Add("Цена за единицу должна быть положительным числом.");
            }

            if (!ServiceDatePicker.SelectedDate.HasValue)
            {
                errors.Add("Необходимо выбрать дату предоставления услуги.");
            }

            if (RoomComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать номер.");
            }

            if (string.IsNullOrWhiteSpace(NotesTextBox.Text))
            {
                errors.Add("Необходимо добавить описание услуги.");
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

        private void SaveService()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    var serviceId = (int)ServiceComboBox.SelectedValue;
                    int quantity = int.Parse(QuantityTextBox.Text);
                    decimal unitPrice = decimal.Parse(UnitPriceTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture);
                    DateTime serviceDate = ServiceDatePicker.SelectedDate.Value;
                    int roomId = (int)RoomComboBox.SelectedValue;
                    string description = NotesTextBox.Text;

                    BookingItem bookingItemToSave;

                    if (_isEditing && _bookingItemId.HasValue)
                    {
                        bookingItemToSave = context.BookingItem.Find(_bookingItemId.Value);
                        if (bookingItemToSave == null || bookingItemToSave.BookingId != _bookingId)
                        {
                            MessageBox.Show("Услуга не найдена или не относится к данному бронированию.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        bookingItemToSave = new BookingItem();
                        bookingItemToSave.BookingId = _bookingId;
                        context.BookingItem.Add(bookingItemToSave);
                    }

                    bookingItemToSave.ServiceId = serviceId;
                    bookingItemToSave.Quantity = quantity;
                    bookingItemToSave.UnitPrice = unitPrice;
                    bookingItemToSave.ItemDate = serviceDate;
                    bookingItemToSave.RoomId = roomId;
                    bookingItemToSave.Description = description;

                    context.SaveChanges();

                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении услуги: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
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
            bool isMatch = regex.IsMatch(e.Text);

            if (!isMatch)
            {
                var textBox = sender as System.Windows.Controls.TextBox;
                if (textBox != null)
                {
                    string futureText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
                    isMatch = futureText.Count(c => c == ',' || c == '.') > 1;
                }
            }

            e.Handled = isMatch;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                SaveService();
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