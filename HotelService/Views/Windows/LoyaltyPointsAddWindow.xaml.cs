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
    public partial class LoyaltyPointsAddWindow : Window
    {
        private readonly int _guestId;
        private readonly int _bookingId;
        private Guest _guest;

        public LoyaltyPointsAddWindow(int guestId, int bookingId)
        {
            InitializeComponent();
            _guestId = guestId;
            _bookingId = bookingId;

            LoadInitialData();
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
                        CurrentPointsTextBlock.Text = _guest.CurrentPoints.ToString();
                    }

                    // Load transaction types
                    var transactionTypes = context.TransactionType
                        .OrderBy(t => t.TypeName)
                        .ToList();

                    TransactionTypeComboBox.ItemsSource = transactionTypes;

                    if (transactionTypes.Any())
                    {
                        // Try to select a default "earn points" type
                        var defaultType = transactionTypes.FirstOrDefault(t =>
                            t.TypeName.Contains("Начисление") ||
                            t.TypeName.Contains("Заработано") ||
                            t.TypeName.Contains("Earn"));

                        if (defaultType != null)
                            TransactionTypeComboBox.SelectedItem = defaultType;
                        else
                            TransactionTypeComboBox.SelectedIndex = 0;
                    }
                }

                // Set default date to today
                TransactionDatePicker.SelectedDate = DateTime.Today;
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

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private bool ValidateInput()
        {
            if (TransactionTypeComboBox.SelectedItem == null)
            {
                ValidationMessageTextBlock.Text = "Выберите тип транзакции";
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            int points;
            if (!int.TryParse(PointsTextBox.Text, out points) || points <= 0)
            {
                ValidationMessageTextBlock.Text = "Введите корректное количество баллов (больше 0)";
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            if (!TransactionDatePicker.SelectedDate.HasValue)
            {
                ValidationMessageTextBlock.Text = "Выберите дату транзакции";
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            ValidationMessageTextBlock.Visibility = Visibility.Collapsed;
            return true;
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
                    // Create new loyalty transaction
                    var transaction = new LoyaltyTransaction
                    {
                        GuestId = _guestId,
                        TypeId = (int)TransactionTypeComboBox.SelectedValue,
                        Points = int.Parse(PointsTextBox.Text),
                        BookingId = _bookingId,
                        Description = DescriptionTextBox.Text,
                        TransactionDate = TransactionDatePicker.SelectedDate.Value
                    };

                    context.LoyaltyTransaction.Add(transaction);

                    // Update guest points
                    var guest = context.Guest.Find(_guestId);
                    if (guest != null)
                    {
                        guest.CurrentPoints += transaction.Points;
                    }

                    context.SaveChanges();

                    MessageBox.Show($"Баллы успешно начислены. Новый баланс: {guest.CurrentPoints} баллов.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    DialogResult = true;
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