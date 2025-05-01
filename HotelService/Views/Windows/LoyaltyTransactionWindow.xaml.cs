using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using HotelService.Data;
using System.Data.Entity;

namespace HotelService.Views.Windows
{
    public partial class LoyaltyTransactionWindow : Window
    {
        private readonly int _guestId;
        private readonly int? _bookingId;
        private readonly int _transactionTypeId;
        private Guest _guest;
        private Booking _booking;

        // Constants for loyalty program
        private const decimal LOYALTY_EXCHANGE_RATE = 1.0m; // 1 point = 1 ruble
        private const int SILVER_THRESHOLD = 1000;
        private const int GOLD_THRESHOLD = 2000;
        private const int PLATINUM_THRESHOLD = 5000;

        // Transaction type constants
        private const int TRANSACTION_TYPE_EARNING = 1;      // Earning points
        private const int TRANSACTION_TYPE_REDEEMING = 2;    // Redeeming points
        private const int TRANSACTION_TYPE_ADJUSTMENT = 3;   // Manual adjustment

        public LoyaltyTransactionWindow(int guestId, int? bookingId, int transactionTypeId)
        {
            InitializeComponent();
            _guestId = guestId;
            _bookingId = bookingId;
            _transactionTypeId = transactionTypeId;

            SetWindowTitle();
            LoadInitialData();
        }

        private void SetWindowTitle()
        {
            switch (_transactionTypeId)
            {
                case TRANSACTION_TYPE_EARNING:
                    WindowTitleTextBlock.Text = "Начисление баллов лояльности";
                    SaveButton.Content = "Начислить";
                    break;

                case TRANSACTION_TYPE_REDEEMING:
                    WindowTitleTextBlock.Text = "Списание баллов лояльности";
                    SaveButton.Content = "Списать";
                    break;

                case TRANSACTION_TYPE_ADJUSTMENT:
                    WindowTitleTextBlock.Text = "Корректировка баллов лояльности";
                    SaveButton.Content = "Сохранить";
                    break;

                default:
                    WindowTitleTextBlock.Text = "Операция с баллами лояльности";
                    break;
            }
        }

        private void LoadInitialData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    // Load guest info
                    _guest = context.Guest
                        .FirstOrDefault(g => g.GuestId == _guestId);

                    if (_guest != null)
                    {
                        string fullName = $"{_guest.LastName} {_guest.FirstName} {_guest.MiddleName}";
                        GuestNameTextBlock.Text = fullName.Trim();
                        CurrentPointsTextBlock.Text = $"{_guest.CurrentPoints} баллов";

                    }
                    else
                    {
                        MessageBox.Show("Гость не найден в базе данных.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    // Load booking info if provided
                    if (_bookingId.HasValue)
                    {
                        _booking = context.Booking
                            .FirstOrDefault(b => b.BookingId == _bookingId.Value);

                        if (_booking != null)
                        {
                            BookingInfoTextBlock.Text = $"№{_booking.BookingId} от {_booking.IssueDate:dd.MM.yyyy}";
                        }
                        else
                        {
                            BookingInfoTextBlock.Text = "Не указано";
                        }
                    }
                    else
                    {
                        BookingInfoTextBlock.Text = "Не указано";
                    }

                    // Load transaction types based on operation type
                    var transactionTypes = context.TransactionType.ToList();

                    switch (_transactionTypeId)
                    {
                        case TRANSACTION_TYPE_EARNING:
                            // Filter for earning-type transactions only
                            transactionTypes = transactionTypes
                                .Where(tt => tt.TypeId == TRANSACTION_TYPE_EARNING ||
                                       (tt.TypeName != null &&
                                        (tt.TypeName.Contains("Начисление") ||
                                         tt.TypeName.Contains("Добавление") ||
                                         tt.TypeName.Contains("Earn"))))
                                .ToList();
                            break;

                        case TRANSACTION_TYPE_REDEEMING:
                            // Filter for redemption-type transactions only
                            transactionTypes = transactionTypes
                                .Where(tt => tt.TypeId == TRANSACTION_TYPE_REDEEMING ||
                                       (tt.TypeName != null &&
                                        (tt.TypeName.Contains("Списание") ||
                                         tt.TypeName.Contains("Использование") ||
                                         tt.TypeName.Contains("Redeem"))))
                                .ToList();
                            break;

                        case TRANSACTION_TYPE_ADJUSTMENT:
                            // For adjustments, show all transaction types
                            break;
                    }

                    TransactionTypeComboBox.ItemsSource = transactionTypes;

                    // Try to select the appropriate default
                    var defaultType = transactionTypes.FirstOrDefault(t => t.TypeId == _transactionTypeId);
                    if (defaultType != null)
                        TransactionTypeComboBox.SelectedItem = defaultType;
                    else if (transactionTypes.Any())
                        TransactionTypeComboBox.SelectedIndex = 0;
                }

                // Set default date to today
                TransactionDatePicker.SelectedDate = DateTime.Today;

                // Set default description based on operation type
                switch (_transactionTypeId)
                {
                    case TRANSACTION_TYPE_EARNING:
                        DescriptionTextBox.Text = "Начисление баллов лояльности.";
                        break;

                    case TRANSACTION_TYPE_REDEEMING:
                        DescriptionTextBox.Text = "Списание баллов лояльности.";
                        break;

                    case TRANSACTION_TYPE_ADJUSTMENT:
                        DescriptionTextBox.Text = "Корректировка баланса баллов.";
                        break;
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

        private string GetLoyaltyStatusText(int points)
        {
            if (points >= PLATINUM_THRESHOLD)
                return "Платиновый";
            else if (points >= GOLD_THRESHOLD)
                return "Золотой";
            else if (points >= SILVER_THRESHOLD)
                return "Серебряный";
            else
                return "Стандарт";
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void PointsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Update equivalent value in rubles
            if (int.TryParse(PointsTextBox.Text, out int points))
            {
                decimal equivalentValue = points * LOYALTY_EXCHANGE_RATE;
                if (EquivalentValueTextBlock != null)
                {
                    EquivalentValueTextBlock.Text = $"{equivalentValue:N2} ₽";
                }
            }
            else
            {
                EquivalentValueTextBlock.Text = "0.00 ₽";
            }

            // Validate against current balance for redemption
            if (_transactionTypeId == TRANSACTION_TYPE_REDEEMING && _guest != null)
            {
                if (points > _guest.CurrentPoints)
                {
                    ShowValidationError($"Невозможно списать больше баллов, чем имеется на счету ({_guest.CurrentPoints}).");
                }
                else
                {
                    HideValidationError();
                }
            }
        }

        private void TransactionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // When type changes, suggest an appropriate description
            if (TransactionTypeComboBox.SelectedItem is TransactionType selectedType)
            {
                string baseDescription = "";

                switch (_transactionTypeId)
                {
                    case TRANSACTION_TYPE_EARNING:
                        baseDescription = "Начисление баллов лояльности";
                        break;

                    case TRANSACTION_TYPE_REDEEMING:
                        baseDescription = "Списание баллов лояльности";
                        break;

                    case TRANSACTION_TYPE_ADJUSTMENT:
                        baseDescription = "Корректировка баланса баллов";
                        break;
                }

                DescriptionTextBox.Text = $"{baseDescription}. Тип: {selectedType.TypeName}.";
            }
        }

        private bool ValidateInput()
        {
            HideValidationError();

            if (TransactionTypeComboBox.SelectedItem == null)
            {
                ShowValidationError("Выберите тип операции");
                return false;
            }

            if (!int.TryParse(PointsTextBox.Text, out int points) || points <= 0)
            {
                ShowValidationError("Введите корректное количество баллов (больше 0)");
                return false;
            }

            if (!TransactionDatePicker.SelectedDate.HasValue)
            {
                ShowValidationError("Выберите дату операции");
                return false;
            }

            // Additional validation for redemption - can't redeem more than available
            if (_transactionTypeId == TRANSACTION_TYPE_REDEEMING && _guest != null)
            {
                if (points > _guest.CurrentPoints)
                {
                    ShowValidationError($"Невозможно списать {points} баллов. Доступно: {_guest.CurrentPoints}");
                    return false;
                }
            }

            return true;
        }

        private void ShowValidationError(string message)
        {
            ValidationMessageTextBlock.Text = message;
            ValidationMessageTextBlock.Visibility = Visibility.Visible;
        }

        private void HideValidationError()
        {
            ValidationMessageTextBlock.Visibility = Visibility.Collapsed;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput())
                return;

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    int points = int.Parse(PointsTextBox.Text);

                    // For redemptions, points should be negative
                    if (_transactionTypeId == TRANSACTION_TYPE_REDEEMING)
                    {
                        points = -points;
                    }

                    // For adjustments, check if it's positive or negative
                    if (_transactionTypeId == TRANSACTION_TYPE_ADJUSTMENT)
                    {
                        // Just keep the original sign
                    }

                    // Create new loyalty transaction
                    var transaction = new LoyaltyTransaction
                    {
                        GuestId = _guestId,
                        TypeId = (int)TransactionTypeComboBox.SelectedValue,
                        Points = points,
                        BookingId = _bookingId,
                        Description = DescriptionTextBox.Text,
                        TransactionDate = TransactionDatePicker.SelectedDate.Value
                    };

                    context.LoyaltyTransaction.Add(transaction);

                    // Update guest points
                    var guest = context.Guest.Find(_guestId);
                    if (guest != null)
                    {
                        guest.CurrentPoints += points;

                        // Ensure points don't go negative
                        if (guest.CurrentPoints < 0)
                        {
                            ShowValidationError("Операция отменена: баланс баллов не может стать отрицательным.");
                            return;
                        }
                    }

                    context.SaveChanges();

                    string actionText = _transactionTypeId == TRANSACTION_TYPE_EARNING
                        ? "начислены"
                        : (_transactionTypeId == TRANSACTION_TYPE_REDEEMING ? "списаны" : "скорректированы");

                    MessageBox.Show($"Баллы успешно {actionText}. Новый баланс: {guest.CurrentPoints} баллов.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
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