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

namespace HotelService.Views.Windows
{
    public partial class PaymentEditWindow : Window
    {
        private readonly int _bookingId;
        private readonly int? _paymentId;
        private readonly decimal _suggestedAmount;
        private Booking _booking;
        private decimal _remainingAmount = 0;

        // Loyalty program constants
        private const decimal LOYALTY_EARN_RATE = 0.05m; // 5% of payment amount
        private const decimal LOYALTY_EXCHANGE_RATE = 1.0m; // 1 point = 1 ruble
        private const int TRANSACTION_TYPE_EARNING = 1; // Earning points from payment
        private const int TRANSACTION_TYPE_REDEEMING = 2; // Redeeming points for payment

        // Loyalty state variables
        private bool _useLoyaltyPoints = false;
        private int _availablePoints = 0;
        private int _pointsToRedeem = 0;
        private decimal _pointsValue = 0;

        public PaymentEditWindow(int bookingId, decimal suggestedAmount = 0, int? paymentId = null)
        {
            InitializeComponent();
            _bookingId = bookingId;
            _paymentId = paymentId;
            _suggestedAmount = suggestedAmount;

            if (_paymentId.HasValue)
            {
                WindowTitleTextBlock.Text = "Редактирование оплаты";
                SaveButton.Content = "Сохранить";
            }
            else
            {
                WindowTitleTextBlock.Text = "Добавление новой оплаты";
                SaveButton.Content = "Добавить";
            }

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    // Load booking information
                    _booking = context.Booking
                        .Include("Guest")
                        .Include("Room")
                        .FirstOrDefault(b => b.BookingId == _bookingId);

                    if (_booking == null)
                    {
                        MessageBox.Show("Бронирование не найдено.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    // Load payment methods for combobox
                    var paymentMethods = context.PaymentMethod
                        .OrderBy(pm => pm.MethodName)
                        .ToList();

                    PaymentMethodComboBox.ItemsSource = paymentMethods;
                    if (paymentMethods.Any())
                        PaymentMethodComboBox.SelectedIndex = 0;

                    // Load financial statuses for combobox
                    var statuses = context.FinancialStatus
                        .OrderBy(s => s.StatusId)
                        .ToList();

                    StatusComboBox.ItemsSource = statuses;
                    if (statuses.Any())
                        StatusComboBox.SelectedIndex = 0;

                    // Set default date and time
                    PaymentDatePicker.SelectedDate = DateTime.Today;
                    PaymentTimePicker.Text = DateTime.Now.ToString("HH:mm");

                    // Display booking information
                    BookingNumberTextBlock.Text = _booking.BookingId.ToString();

                    if (_booking.Guest != null)
                    {
                        string guestName = $"{_booking.Guest.LastName} {_booking.Guest.FirstName}";
                        if (!string.IsNullOrEmpty(_booking.Guest.MiddleName))
                            guestName += $" {_booking.Guest.MiddleName}";

                        GuestNameTextBlock.Text = guestName;

                        // Load loyalty points information
                        LoadLoyaltyPointsInfo(_booking.Guest);
                    }
                    else
                    {
                        GuestNameTextBlock.Text = "Гость не указан";
                        LoyaltyPointsPanel.Visibility = Visibility.Collapsed;
                    }

                    // Calculate remaining amount
                    decimal totalAmount = _booking.TotalAmount;

                    // Handle null result from Sum() by using DefaultIfEmpty(0).Sum()
                    decimal paidAmount = context.Payment
                        .Where(p => p.BookingId == _bookingId)  // Считаем все платежи
                        .Select(p => p.Amount)
                        .DefaultIfEmpty(0)
                        .Sum();

                    // Если редактируем существующий платеж, добавляем его сумму обратно к остатку
                    if (_paymentId.HasValue)
                    {
                        var currentPayment = context.Payment.Find(_paymentId.Value);
                        if (currentPayment != null)
                        {
                            // При редактировании не учитываем сумму текущего платежа в уже оплаченном
                            paidAmount -= currentPayment.Amount;
                        }
                    }

                    _remainingAmount = Math.Max(0, totalAmount - paidAmount);

                    TotalAmountTextBlock.Text = string.Format("{0:N2} ₽", totalAmount);
                    PaidAmountTextBlock.Text = string.Format("{0:N2} ₽", paidAmount);
                    RemainingAmountTextBlock.Text = string.Format("{0:N2} ₽", _remainingAmount);

                    // If this is a suggested amount (from the booking view) or a new payment,
                    // set the amount to the remaining amount or suggested amount
                    if (!_paymentId.HasValue)
                    {
                        if (_suggestedAmount > 0 && _suggestedAmount <= _remainingAmount)
                            AmountTextBox.Text = _suggestedAmount.ToString("F2");
                        else
                            AmountTextBox.Text = _remainingAmount.ToString("F2");
                    }

                    // If editing existing payment, load its data
                    if (_paymentId.HasValue)
                    {
                        var payment = context.Payment
                            .FirstOrDefault(p => p.PaymentId == _paymentId.Value);

                        if (payment != null)
                        {
                            PaymentMethodComboBox.SelectedValue = payment.PaymentMethodId;
                            StatusComboBox.SelectedValue = payment.StatusId;
                            PaymentDatePicker.SelectedDate = payment.PaymentDate.Date;
                            PaymentTimePicker.Text = payment.PaymentDate.ToString("HH:mm");
                            AmountTextBox.Text = payment.Amount.ToString("F2");
                            NotesTextBox.Text = payment.Notes;
                        }
                        else
                        {
                            MessageBox.Show("Платеж не найден.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            DialogResult = false;
                            Close();
                            return;
                        }
                    }

                    // Update the total payment amount display
                    UpdateTotalPaymentAmount();
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

        private void LoadLoyaltyPointsInfo(Guest guest)
        {
            if (guest == null)
            {
                LoyaltyPointsPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // Show loyalty panel
            LoyaltyPointsPanel.Visibility = Visibility.Visible;

            // Store available points
            _availablePoints = guest.CurrentPoints;

            // Display available points
            AvailablePointsTextBlock.Text = $"{_availablePoints} баллов";

            // Display exchange rate
            PointsExchangeRateTextBlock.Text = $"1 балл = {LOYALTY_EXCHANGE_RATE:N2} ₽";

            // Initially hide loyalty details until checkbox is checked
            LoyaltyInfoGrid.Visibility = Visibility.Collapsed;
            UseLoyaltyPointsCheckBox.IsChecked = false;
            _useLoyaltyPoints = false;

            // Reset points to redeem
            PointsToRedeemTextBox.Text = "0";
            _pointsToRedeem = 0;
            _pointsValue = 0;
            PointsValueTextBlock.Text = "0.00 ₽";
        }

        private void UseLoyaltyPointsCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            _useLoyaltyPoints = UseLoyaltyPointsCheckBox.IsChecked ?? false;
            LoyaltyInfoGrid.Visibility = _useLoyaltyPoints ? Visibility.Visible : Visibility.Collapsed;

            // When toggling off, reset points to redeem
            if (!_useLoyaltyPoints)
            {
                PointsToRedeemTextBox.Text = "0";
                _pointsToRedeem = 0;
                _pointsValue = 0;
                PointsValueTextBlock.Text = "0.00 ₽";
            }

            // Update total payment display
            UpdateTotalPaymentAmount();
        }

        private void PointsToRedeemTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Parse points to redeem
            if (int.TryParse(PointsToRedeemTextBox.Text, out int points))
            {
                // Limit to available points
                if (points > _availablePoints)
                {
                    points = _availablePoints;
                    PointsToRedeemTextBox.Text = points.ToString();
                }

                // Store in field
                _pointsToRedeem = points;

                // Calculate and display value
                _pointsValue = points * LOYALTY_EXCHANGE_RATE;
                if (PointsValueTextBlock != null)
                {
                    PointsValueTextBlock.Text = $"{_pointsValue:N2} ₽";

                }

                // Update total payment amount
                UpdateTotalPaymentAmount();
            }
            else
            {
                _pointsToRedeem = 0;
                _pointsValue = 0;
                PointsValueTextBlock.Text = "0.00 ₽";
            }
        }

        private void MaxPointsButton_Click(object sender, RoutedEventArgs e)
        {
            // Calculate how many points would be needed to cover the remaining amount
            int pointsNeeded = (int)Math.Ceiling(_remainingAmount / LOYALTY_EXCHANGE_RATE);

            // Limit to available points
            int pointsToUse = Math.Min(pointsNeeded, _availablePoints);

            // Set the value
            PointsToRedeemTextBox.Text = pointsToUse.ToString();
        }

        private void AmountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTotalPaymentAmount();
        }

        private void UpdateTotalPaymentAmount()
        {
            decimal moneyAmount = 0;

            // Parse money amount
            if (AmountTextBox != null)
            {
                decimal.TryParse(AmountTextBox.Text.Replace(',', '.'),
                          NumberStyles.Any, CultureInfo.InvariantCulture, out moneyAmount);
                // Calculate total payment
                decimal totalPayment = moneyAmount + _pointsValue;

                // Display total
                if (TotalPaymentAmountTextBlock != null)
                {
                    TotalPaymentAmountTextBlock.Text = $"{totalPayment:N2} ₽";

                }

                // Validate against remaining amount
                if (totalPayment > _remainingAmount)
                {
                    TotalPaymentAmountTextBlock.Foreground = FindResource("WarningColor") as System.Windows.Media.Brush;
                }
                else
                {
                    if (TotalPaymentAmountTextBlock != null)
                    {
                        TotalPaymentAmountTextBlock.Foreground = FindResource("SuccessColor") as System.Windows.Media.Brush;

                    }
                }
            }

           
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                SavePayment();
            }
        }

        private bool ValidateInput()
        {
            // Clear previous error message
            ErrorMessageTextBlock.Visibility = Visibility.Collapsed;

            // Check payment method
            if (PaymentMethodComboBox.SelectedItem == null)
            {
                ShowError("Выберите способ оплаты");
                return false;
            }

            // Check status
            if (StatusComboBox.SelectedItem == null)
            {
                ShowError("Выберите статус оплаты");
                return false;
            }

            // Check date
            if (!PaymentDatePicker.SelectedDate.HasValue)
            {
                ShowError("Выберите дату оплаты");
                return false;
            }

            // Check time format
            TimeSpan timeValue;
            if (!TimeSpan.TryParse(PaymentTimePicker.Text, out timeValue))
            {
                ShowError("Некорректный формат времени. Используйте формат ЧЧ:ММ");
                return false;
            }

            // Check amount - at least one of money or points must be greater than 0
            decimal moneyAmount;
            if (!decimal.TryParse(AmountTextBox.Text.Replace(',', '.'),
                NumberStyles.Any, CultureInfo.InvariantCulture, out moneyAmount))
            {
                ShowError("Введите корректную сумму платежа");
                return false;
            }

            // Check that at least one payment method is used
            if (moneyAmount <= 0 && _pointsToRedeem <= 0)
            {
                ShowError("Необходимо указать сумму платежа денежными средствами или баллами лояльности");
                return false;
            }

            // Check if total exceeds the remaining amount
            decimal totalPayment = moneyAmount + _pointsValue;
            if (totalPayment > _remainingAmount)
            {
                // This is a warning, not an error - ask for confirmation
                MessageBoxResult result = MessageBox.Show(
                    $"Общая сумма платежа ({totalPayment:N2} ₽) превышает оставшуюся к оплате сумму ({_remainingAmount:N2} ₽).\n\n" +
                    "Вы уверены, что хотите продолжить?",
                    "Предупреждение", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.No)
                {
                    return false;
                }
            }

            return true;
        }

        private void ShowError(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
        }

        private void SavePayment()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    // Start a transaction to ensure atomic operations
                    using (var transaction = context.Database.BeginTransaction())
                    {
                        try
                        {
                            // First verify that the booking exists
                            var booking = context.Booking
                                .Include(b => b.Guest)
                                .FirstOrDefault(b => b.BookingId == _bookingId);

                            if (booking == null)
                            {
                                MessageBox.Show($"Бронирование №{_bookingId} не найдено в базе данных.", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // Parse money amount
                            decimal moneyAmount;
                            if (!decimal.TryParse(AmountTextBox.Text.Replace(',', '.'),
                                NumberStyles.Any, CultureInfo.InvariantCulture, out moneyAmount))
                            {
                                MessageBox.Show("Неверный формат суммы платежа.", "Ошибка",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                                return;
                            }

                            // Combine date and time
                            TimeSpan timeValue;
                            if (!TimeSpan.TryParse(PaymentTimePicker.Text, out timeValue))
                            {
                                timeValue = DateTime.Now.TimeOfDay; // Default to current time if parse fails
                            }
                            DateTime paymentDateTime = PaymentDatePicker.SelectedDate.Value.Date.Add(timeValue);

                            // Begin with cash/card payment if amount > 0
                            if (moneyAmount > 0)
                            {
                                Payment payment;
                                if (_paymentId.HasValue)
                                {
                                    // Edit existing payment
                                    payment = context.Payment.Find(_paymentId.Value);
                                    if (payment == null)
                                    {
                                        MessageBox.Show("Платеж не найден в базе данных.", "Ошибка",
                                            MessageBoxButton.OK, MessageBoxImage.Error);
                                        return;
                                    }
                                }
                                else
                                {
                                    // Create new payment
                                    payment = new Payment();
                                    payment.BookingId = _bookingId;
                                    context.Payment.Add(payment);
                                }

                                // Set payment properties
                                payment.PaymentMethodId = (int)PaymentMethodComboBox.SelectedValue;
                                payment.StatusId = (int)StatusComboBox.SelectedValue;
                                payment.PaymentDate = paymentDateTime;
                                payment.Amount = moneyAmount;
                                payment.Notes = NotesTextBox.Text;

                                // Save to get payment ID
                                context.SaveChanges();

                                // Add loyalty points for this payment if guest exists
                                if (booking.Guest != null && moneyAmount > 0)
                                {
                                    // Calculate points to earn - round down to integer
                                    int pointsToEarn = (int)(moneyAmount * LOYALTY_EARN_RATE);

                                    if (pointsToEarn > 0)
                                    {
                                        // Create loyalty transaction
                                        var loyaltyTransaction = new LoyaltyTransaction
                                        {
                                            GuestId = booking.Guest.GuestId,
                                            TypeId = TRANSACTION_TYPE_EARNING,
                                            Points = pointsToEarn,
                                            BookingId = _bookingId,
                                            Description = $"Начисление баллов за оплату №{payment.PaymentId} на сумму {moneyAmount:N2} ₽.",
                                            TransactionDate = paymentDateTime
                                        };

                                        context.LoyaltyTransaction.Add(loyaltyTransaction);

                                        // Update guest points
                                        booking.Guest.CurrentPoints += pointsToEarn;

                                        context.SaveChanges();
                                    }
                                }
                            }

                            // Process loyalty points redemption if needed
                            if (_useLoyaltyPoints && _pointsToRedeem > 0 && booking.Guest != null)
                            {
                                // First verify guest has enough points
                                var guest = context.Guest.Find(booking.Guest.GuestId);
                                if (guest == null || guest.CurrentPoints < _pointsToRedeem)
                                {
                                    MessageBox.Show("Недостаточно баллов лояльности для выполнения операции.",
                                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                // Create loyalty transaction (negative points for redemption)
                                var loyaltyTransaction = new LoyaltyTransaction
                                {
                                    GuestId = guest.GuestId,
                                    TypeId = TRANSACTION_TYPE_REDEEMING,
                                    Points = -_pointsToRedeem,
                                    BookingId = _bookingId,
                                    Description = $"Использование баллов для оплаты бронирования №{_bookingId} на сумму {_pointsValue:N2} ₽.",
                                    TransactionDate = paymentDateTime
                                };

                                context.LoyaltyTransaction.Add(loyaltyTransaction);

                                // Update guest points
                                guest.CurrentPoints -= _pointsToRedeem;

                                context.SaveChanges();
                            }

                            // Update booking financial status
                            UpdateBookingFinancialStatus(context, _bookingId);

                            // Commit the transaction
                            transaction.Commit();

                            // Show success message with point earn details
                            string pointsMessage = "";
                            if (booking.Guest != null && moneyAmount > 0)
                            {
                                int pointsEarned = (int)(moneyAmount * LOYALTY_EARN_RATE);
                                if (pointsEarned > 0)
                                {
                                    pointsMessage = $"\n\nНачислено {pointsEarned} баллов лояльности.";
                                }
                            }

                            if (_useLoyaltyPoints && _pointsToRedeem > 0)
                            {
                                pointsMessage += $"\nСписано {_pointsToRedeem} баллов лояльности на сумму {_pointsValue:N2} ₽.";
                            }

                            MessageBox.Show($"Платеж успешно сохранен.{pointsMessage}", "Информация",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                            DialogResult = true;
                            Close();
                        }
                        catch (Exception ex)
                        {
                            // Rollback transaction on error
                            transaction.Rollback();
                            throw; // Re-throw to be caught by outer handler
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении платежа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void UpdateBookingFinancialStatus(HotelServiceEntities context, int bookingId)
        {
            try
            {
                var booking = context.Booking.Find(bookingId);
                if (booking != null)
                {
                    // Calculate points value if any were used
                    decimal pointsValueForPayment = _useLoyaltyPoints ? _pointsValue : 0;

                    // Get total paid amount - handle null results with DefaultIfEmpty
                    decimal totalPaid = context.Payment
                        .Where(p => p.BookingId == bookingId && p.StatusId == 1) // Assuming status 1 = Confirmed
                        .Select(p => p.Amount)
                        .DefaultIfEmpty(0)
                        .Sum();

                    // Add points value to total paid amount
                    totalPaid += pointsValueForPayment;

                    // Check if deposit is paid
                    booking.DepositPaid = booking.DepositAmount.HasValue && totalPaid >= booking.DepositAmount.Value;

                    // Determine financial status
                    if (totalPaid >= booking.TotalAmount)
                    {
                        booking.FinancialStatusId = 3; // Fully paid
                    }
                    else if (booking.DepositPaid)
                    {
                        booking.FinancialStatusId = 2; // Deposit paid
                    }
                    else
                    {
                        booking.FinancialStatusId = 1; // Unpaid
                    }

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении финансового статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            // Allow only digits and decimal separator
            Regex regex = new Regex(@"[^0-9\,\.]+");
            e.Handled = regex.IsMatch(e.Text);

            // Check if we already have a decimal separator
            if (!regex.IsMatch(e.Text))
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null && (e.Text == "," || e.Text == "."))
                {
                    // Check if text already contains a decimal separator
                    if (textBox.Text.Contains(",") || textBox.Text.Contains("."))
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
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