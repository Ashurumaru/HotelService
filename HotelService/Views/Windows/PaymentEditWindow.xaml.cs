using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class PaymentEditWindow : Window
    {
        private readonly int _bookingId;
        private readonly int? _paymentId;
        private Booking _booking;
        private decimal _remainingAmount;

        public Payment CreatedPayment { get; private set; }

        public PaymentEditWindow(int bookingId, int? paymentId = null)
        {
            InitializeComponent();
            _bookingId = bookingId;
            _paymentId = paymentId;

            if (_paymentId.HasValue)
            {
                WindowTitleTextBlock.Text = "Редактирование оплаты";
            }
            else
            {
                WindowTitleTextBlock.Text = "Добавление новой оплаты";
            }

            LoadBookingData();
            LoadReferenceData();

            if (_paymentId.HasValue)
            {
                LoadPaymentData();
            }
            else
            {
                InitializeNewPayment();
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
                        .Include("Guest")
                        .FirstOrDefault(b => b.BookingId == _bookingId);

                    if (_booking == null)
                    {
                        MessageBox.Show("Бронирование не найдено.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    // Получаем сумму уже произведенных оплат
                    decimal paidAmount = context.Payment
                        .Where(p => p.BookingId == _bookingId && p.StatusId != 3) // Исключаем отмененные платежи
                        .Sum(p => (decimal?)p.Amount) ?? 0;

                    _remainingAmount = _booking.TotalAmount - paidAmount;

                    // Заполняем информацию о бронировании
                    BookingIdTextBlock.Text = _booking.BookingId.ToString();

                    string guestName = "Не указан";
                    if (_booking.Guest != null)
                    {
                        guestName = $"{_booking.Guest.LastName} {_booking.Guest.FirstName} {_booking.Guest.MiddleName}".Trim();
                    }
                    GuestNameTextBlock.Text = guestName;

                    TotalAmountTextBlock.Text = string.Format("{0:N2} ₽", _booking.TotalAmount);
                    RemainingAmountTextBlock.Text = string.Format("{0:N2} ₽", _remainingAmount);

                    // Устанавливаем цвет остатка в зависимости от суммы
                    if (_remainingAmount <= 0)
                    {
                        RemainingAmountTextBlock.Foreground = (SolidColorBrush)FindResource("SuccessColor");
                    }
                    else if (_remainingAmount < _booking.TotalAmount)
                    {
                        RemainingAmountTextBlock.Foreground = (SolidColorBrush)FindResource("WarningColor");
                    }
                    else
                    {
                        RemainingAmountTextBlock.Foreground = (SolidColorBrush)FindResource("TextSecondaryColor");
                    }
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

        private void LoadReferenceData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    // Загрузка способов оплаты
                    var paymentMethods = context.PaymentMethod.OrderBy(pm => pm.MethodName).ToList();
                    PaymentMethodComboBox.ItemsSource = paymentMethods;
                    PaymentMethodComboBox.DisplayMemberPath = "MethodName";
                    PaymentMethodComboBox.SelectedValuePath = "PaymentMethodId";

                    if (paymentMethods.Count > 0)
                    {
                        PaymentMethodComboBox.SelectedIndex = 0;
                    }

                    // Загрузка статусов оплаты
                    var statuses = context.FinancialStatus.OrderBy(s => s.StatusId).ToList();
                    StatusComboBox.ItemsSource = statuses;
                    StatusComboBox.DisplayMemberPath = "StatusName";
                    StatusComboBox.SelectedValuePath = "StatusId";

                    if (statuses.Count > 0)
                    {
                        StatusComboBox.SelectedIndex = 0; // Обычно "Оплачено"
                    }
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

        private void LoadPaymentData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    var payment = context.Payment.Find(_paymentId);

                    if (payment == null)
                    {
                        MessageBox.Show("Оплата не найдена.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    AmountTextBox.Text = payment.Amount.ToString("F2");
                    PaymentMethodComboBox.SelectedValue = payment.PaymentMethodId;
                    StatusComboBox.SelectedValue = payment.StatusId;
                    NotesTextBox.Text = payment.Notes;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных оплаты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void InitializeNewPayment()
        {
            AmountTextBox.Text = _remainingAmount > 0 ? _remainingAmount.ToString("F2") : "0.00";

            // Устанавливаем фокус на поле суммы
            AmountTextBox.Focus();
            AmountTextBox.SelectAll();
        }

        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9\,\.]+");

            // Проверяем, что вводятся только числа или десятичный разделитель
            bool isMatch = regex.IsMatch(e.Text);

            // Проверяем, не пытается ли пользователь добавить второй десятичный разделитель
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

        private void AmountTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            // Обновляем кнопку сохранения в зависимости от введенной суммы
            decimal amount;
            if (decimal.TryParse(AmountTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out amount))
            {
                SaveButton.IsEnabled = amount > 0;
            }
            else
            {
                SaveButton.IsEnabled = false;
            }
        }

        private bool ValidatePayment()
        {
            List<string> errors = new List<string>();

            decimal amount;
            if (!decimal.TryParse(AmountTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out amount) || amount <= 0)
            {
                errors.Add("Сумма оплаты должна быть положительным числом.");
            }

            if (PaymentMethodComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать способ оплаты.");
            }

            if (StatusComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать статус оплаты.");
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

        private void SavePayment()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    Payment paymentToSave;

                    if (_paymentId.HasValue)
                    {
                        // Редактирование существующей оплаты
                        paymentToSave = context.Payment.Find(_paymentId.Value);
                        if (paymentToSave == null)
                        {
                            MessageBox.Show("Оплата не найдена в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        // Создание новой оплаты
                        paymentToSave = new Payment
                        {
                            BookingId = _bookingId,
                            PaymentDate = DateTime.Now
                        };
                        context.Payment.Add(paymentToSave);
                    }

                    decimal amount = decimal.Parse(AmountTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture);

                    paymentToSave.Amount = amount;
                    paymentToSave.PaymentMethodId = (int)PaymentMethodComboBox.SelectedValue;
                    paymentToSave.StatusId = (int)StatusComboBox.SelectedValue;
                    paymentToSave.Notes = NotesTextBox.Text.Trim();

                    // Обновляем финансовый статус бронирования
                    var booking = context.Booking.Find(_bookingId);
                    if (booking != null)
                    {
                        // Получаем общую сумму оплат после добавления текущей
                        decimal paidAmount = context.Payment
                            .Where(p => p.BookingId == _bookingId && p.StatusId != 3 && p.PaymentId != _paymentId)
                            .Sum(p => (decimal?)p.Amount) ?? 0;

                        paidAmount += amount;

                        // Обновляем финансовый статус бронирования
                        if (paidAmount >= booking.TotalAmount)
                        {
                            booking.FinancialStatusId = 1; // "Оплачено"
                        }
                        else if (paidAmount > 0)
                        {
                            booking.FinancialStatusId = 2; // "Частично оплачено"
                        }
                        else
                        {
                            booking.FinancialStatusId = 3; // "Не оплачено"
                        }

                        // Если используется предоплата, отмечаем, что депозит оплачен
                        if (!booking.DepositPaid && booking.DepositAmount.HasValue && booking.DepositAmount.Value > 0 &&
                            paidAmount >= booking.DepositAmount.Value)
                        {
                            booking.DepositPaid = true;
                        }
                    }

                    context.SaveChanges();
                    CreatedPayment = paymentToSave;
                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении оплаты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidatePayment())
            {
                SavePayment();
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