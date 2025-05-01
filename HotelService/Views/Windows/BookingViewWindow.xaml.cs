using System;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Data.Entity;
using System.Collections.Generic;
using HotelService.Data;
using System.Windows.Media;
using System.Windows.Controls;

namespace HotelService.Views.Windows
{
    public partial class BookingViewWindow : Window
    {
        private readonly int _bookingId;
        private Booking _booking;
        private decimal _totalPaid = 0;
        private decimal _totalCharges = 0;
        private const decimal LOYALTY_EARN_RATE = 0.05m; // 5% of payment amount
        private const decimal LOYALTY_EXCHANGE_RATE = 1.0m; // 1 point = 1 ruble
        private const int SILVER_THRESHOLD = 1000;
        private const int GOLD_THRESHOLD = 2000;
        private const int PLATINUM_THRESHOLD = 5000;

        // Transaction type constants (these should match your database)
        private const int TRANSACTION_TYPE_EARNING = 1;      // Earning points
        private const int TRANSACTION_TYPE_REDEEMING = 2;    // Redeeming points
        private const int TRANSACTION_TYPE_ADJUSTMENT = 3;   // Manual adjustment
        private const int TRANSACTION_TYPE_PAYMENT_EARN = 4; // Earning from payment
        private const int TRANSACTION_TYPE_CANCELLATION = 5; // Cancellation transaction

        // State tracking for filtering
        private int? _selectedTransactionTypeFilter;
        private DateTime? _startDateFilter;
        private DateTime? _endDateFilter;

        // Представления для связанных данных
        private class ServiceViewModel
        {
            public string ServiceName { get; set; }
            public string CategoryName { get; set; }
            public DateTime ItemDate { get; set; }
            public int Quantity { get; set; }
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
        }

        private class DamageReportViewModel
        {
            public string DamageTypeName { get; set; }
            public DateTime ReportDate { get; set; }
            public string SeverityText { get; set; }
            public string StatusName { get; set; }
            public decimal? EstimatedCost { get; set; }
        }

        private class PaymentViewModel
        {
            public int PaymentId { get; set; }
            public DateTime PaymentDate { get; set; }
            public string PaymentType { get; set; } = "Платеж";
            public string PaymentMethodName { get; set; }
            public decimal Amount { get; set; }
            public string StatusName { get; set; }
            public string Notes { get; set; }
            public int SourceId { get; set; } // 1 = Payment, 2 = Service, 3 = Damage
            public int? SourceItemId { get; set; } // ID of the related service or damage report
        }
        private class LoyaltyTransactionViewModel
        {
            public int TransactionId { get; set; }
            public DateTime TransactionDate { get; set; }
            public int TypeId { get; set; }
            public string TypeName { get; set; }
            public int Points { get; set; }
            public string FormattedPoints => Points >= 0 ? $"+{Points}" : Points.ToString();
            public string Description { get; set; }
            public int? BookingId { get; set; }
            public int? PaymentId { get; set; }
            public string BookingInfo => BookingId.HasValue ? $"Бронирование #{BookingId}" : "-";
            public bool CanCancel { get; set; }

            // For UI formatting - to color the points
            public bool IsPositive => Points >= 0;
        }

        private class ChargeViewModel
        {
            public string Description { get; set; }
            public DateTime Date { get; set; }
            public int Quantity { get; set; } = 1;
            public decimal UnitPrice { get; set; }
            public decimal TotalPrice { get; set; }
            public string Status { get; set; } = "Неоплачено";
            public int SourceId { get; set; } // 1 = Room charge, 2 = Service, 3 = Damage
            public int? SourceItemId { get; set; } // ID of the related service or damage report
        }

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
                    // Загрузка основной информации о бронировании
                    _booking = context.Booking
                        .Include(b => b.Guest)
                        .Include(b => b.Room)
                        .Include(b => b.Room.RoomType)
                        .Include(b => b.BookingStatus)
                        .Include(b => b.BookingSource)
                        .Include(b => b.FinancialStatus)
                        .FirstOrDefault(b => b.BookingId == _bookingId);

                    if (_booking == null)
                    {
                        MessageBox.Show("Бронирование не найдено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    // Загрузка связанных данных
                    LoadBookingServices(context);
                    LoadDamageReports(context);
                    LoadFinancialData(context);
                    LoadLoyaltyTransactions(context);
                    // Отображение данных
                    if (_booking.Guest != null)
                    {
                        GuestPointsTextBlock.Text = _booking.Guest.CurrentPoints.ToString();
                    }
                    else
                    {
                        GuestPointsTextBlock.Text = "0";
                    }
                    DisplayBookingData();
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

        private void LoadLoyaltyProgramInfo(HotelServiceEntities context)
        {
            try
            {
                if (_booking?.Guest == null)
                {
                    return;
                }

                // Load guest details
                var guest = context.Guest.Find(_booking.Guest.GuestId);
                if (guest == null) return;

                // Display guest name
                string fullName = $"{guest.LastName} {guest.FirstName} {guest.MiddleName}".Trim();
                LoyaltyGuestNameTextBlock.Text = fullName;

                // Display current points balance
                LoyaltyPointsBalanceTextBlock.Text = $"{guest.CurrentPoints} баллов";

                // Display loyalty status and next level info
                UpdateLoyaltyStatusDisplay(guest.CurrentPoints);

                // Display exchange rate
                LoyaltyExchangeRateTextBlock.Text = $"100 баллов = {(100 * LOYALTY_EXCHANGE_RATE):N2} ₽";

                // Display earning rate
                LoyaltyEarnRateTextBlock.Text = $"{LOYALTY_EARN_RATE:P0} от суммы оплаты";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке информации о программе лояльности: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Load loyalty transactions with filtering
        private void LoadLoyaltyTransactions(HotelServiceEntities context)
        {
            try
            {
                if (_booking?.Guest == null)
                {
                    LoyaltyTransactionsDataGrid.ItemsSource = null;
                    return;
                }

                // Base query for transactions
                var query = context.LoyaltyTransaction
                    .Include(lt => lt.TransactionType)
                    .Where(lt => lt.GuestId == _booking.Guest.GuestId)
                    .AsQueryable();

                // Apply filters if set
                if (_selectedTransactionTypeFilter.HasValue)
                {
                    query = query.Where(lt => lt.TypeId == _selectedTransactionTypeFilter.Value);
                }

                if (_startDateFilter.HasValue)
                {
                    query = query.Where(lt => lt.TransactionDate >= _startDateFilter.Value);
                }

                if (_endDateFilter.HasValue)
                {
                    var endDate = _endDateFilter.Value.AddDays(1).AddSeconds(-1); // End of the day
                    query = query.Where(lt => lt.TransactionDate <= endDate);
                }

                // Load and map to view model
                var transactions = query
                    .OrderByDescending(lt => lt.TransactionDate)
                    .ToList()
                    .Select(lt => new LoyaltyTransactionViewModel
                    {
                        TransactionId = lt.TransactionId,
                        TransactionDate = lt.TransactionDate,
                        TypeId = lt.TypeId,
                        TypeName = lt.TransactionType?.TypeName ?? "Неизвестно",
                        Points = lt.Points,
                        Description = lt.Description,
                        BookingId = lt.BookingId,
                        // Can only cancel recent manual transactions, not automatic ones
                        CanCancel = (DateTime.Now - lt.TransactionDate).TotalDays < 7 &&
                                   (lt.TypeId == TRANSACTION_TYPE_EARNING || lt.TypeId == TRANSACTION_TYPE_ADJUSTMENT)
                    })
                    .ToList();

                // Update data grid
                LoyaltyTransactionsDataGrid.ItemsSource = transactions;

                // Update summary counters
                int totalEarned = transactions.Where(t => t.Points > 0).Sum(t => t.Points);
                int totalRedeemed = transactions.Where(t => t.Points < 0).Sum(t => Math.Abs(t.Points));

            }
            catch (Exception ex)
            {
                LoyaltyTransactionsDataGrid.ItemsSource = null;

                MessageBox.Show($"Ошибка при загрузке транзакций лояльности: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Update loyalty status display
        private void UpdateLoyaltyStatusDisplay(int currentPoints)
        {
            string status;
            string nextLevelText = "";
            Brush statusColor;

            if (currentPoints >= PLATINUM_THRESHOLD)
            {
                status = "Платиновый";
                nextLevelText = "Максимальный уровень достигнут";
                statusColor = FindResource("PrimaryColor") as SolidColorBrush;
            }
            else if (currentPoints >= GOLD_THRESHOLD)
            {
                status = "Золотой";
                int pointsToNextLevel = PLATINUM_THRESHOLD - currentPoints;
                nextLevelText = $"До Платинового: {pointsToNextLevel} баллов";
                statusColor = FindResource("AccentColor") as SolidColorBrush;
            }
            else if (currentPoints >= SILVER_THRESHOLD)
            {
                status = "Серебряный";
                int pointsToNextLevel = GOLD_THRESHOLD - currentPoints;
                nextLevelText = $"До Золотого: {pointsToNextLevel} баллов";
                statusColor = Brushes.Silver;
            }
            else
            {
                status = "Стандарт";
                int pointsToNextLevel = SILVER_THRESHOLD - currentPoints;
                nextLevelText = $"До Серебряного: {pointsToNextLevel} баллов";
                statusColor = FindResource("TextTertiaryColor") as SolidColorBrush;
            }
        }

        // Filter change handlers
        private void TransactionTypeFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
          

            using (var context = new HotelServiceEntities())
            {
                LoadLoyaltyTransactions(context);
            }
        }

        private void DateFilter_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
          
            using (var context = new HotelServiceEntities())
            {
                LoadLoyaltyTransactions(context);
            }
        }

        private void ClearFiltersButton_Click(object sender, RoutedEventArgs e)
        {
          

            _selectedTransactionTypeFilter = null;
            _startDateFilter = null;
            _endDateFilter = null;

            using (var context = new HotelServiceEntities())
            {
                LoadLoyaltyTransactions(context);
            }
        }

        // Transaction action handlers
        private void AddPointsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для начисления баллов лояльности.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_booking?.Guest == null)
            {
                MessageBox.Show("Невозможно начислить баллы. Гость не указан в бронировании.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var loyaltyWindow = new LoyaltyTransactionWindow(_booking.Guest.GuestId, _bookingId, TRANSACTION_TYPE_EARNING);
            if (loyaltyWindow.ShowDialog() == true)
            {
                using (var context = new HotelServiceEntities())
                {
                    LoadLoyaltyProgramInfo(context);
                    LoadLoyaltyTransactions(context);
                }
            }
        }

        private void RedeemPointsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для списания баллов лояльности.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

        

            var loyaltyWindow = new LoyaltyTransactionWindow(_booking.Guest.GuestId, _bookingId, TRANSACTION_TYPE_REDEEMING);
            if (loyaltyWindow.ShowDialog() == true)
            {
                using (var context = new HotelServiceEntities())
                {
                    LoadLoyaltyProgramInfo(context);
                    LoadLoyaltyTransactions(context);
                }
            }
        }

        private void AdjustPointsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для корректировки баллов лояльности. Операция доступна только для администратора системы.",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_booking?.Guest == null)
            {
                MessageBox.Show("Невозможно корректировать баллы. Гость не указан в бронировании.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var loyaltyWindow = new LoyaltyTransactionWindow(_booking.Guest.GuestId, _bookingId, TRANSACTION_TYPE_ADJUSTMENT);
            if (loyaltyWindow.ShowDialog() == true)
            {
                using (var context = new HotelServiceEntities())
                {
                    LoadLoyaltyProgramInfo(context);
                    LoadLoyaltyTransactions(context);
                }
            }
        }

        private void ViewTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            var transaction = (sender as Button)?.DataContext as LoyaltyTransactionViewModel;
            if (transaction == null) return;

            // Format transaction details
            string typeColorIndicator = transaction.Points >= 0 ? "✓" : "✗";
            string typeColor = transaction.Points >= 0 ? "зеленым" : "красным";

            string details = $"Транзакция баллов лояльности № {transaction.TransactionId}\n\n" +
                             $"Дата и время: {transaction.TransactionDate:dd.MM.yyyy HH:mm:ss}\n" +
                             $"Тип операции: {transaction.TypeName} ({typeColorIndicator})\n" +
                             $"Количество баллов: {transaction.FormattedPoints} (отмечено {typeColor})\n" +
                             $"Связанное бронирование: {(transaction.BookingId.HasValue ? $"№{transaction.BookingId}" : "Нет")}\n\n" +
                             $"Описание:\n{transaction.Description ?? "Нет описания"}";

            MessageBox.Show(details, "Информация о транзакции", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void CancelTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для отмены транзакций. Операция доступна только для администратора системы.",
                    "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var transaction = (sender as Button)?.DataContext as LoyaltyTransactionViewModel;
            if (transaction == null) return;

            // Confirm cancellation
            var result = MessageBox.Show(
                $"Вы уверены, что хотите отменить транзакцию №{transaction.TransactionId}?\n\n" +
                $"Тип: {transaction.TypeName}\n" +
                $"Баллы: {transaction.FormattedPoints}\n" +
                $"Дата: {transaction.TransactionDate:dd.MM.yyyy HH:mm}\n\n" +
                "Будет создана новая транзакция с противоположным значением баллов.",
                "Подтверждение отмены", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                using (var context = new HotelServiceEntities())
                {
                    // Get the transaction to cancel
                    var originalTransaction = context.LoyaltyTransaction.Find(transaction.TransactionId);
                    if (originalTransaction == null)
                    {
                        MessageBox.Show("Транзакция не найдена в базе данных.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Create cancellation transaction
                    var cancellationTransaction = new LoyaltyTransaction
                    {
                        GuestId = originalTransaction.GuestId,
                        TypeId = TRANSACTION_TYPE_CANCELLATION,
                        Points = -originalTransaction.Points, // Reverse the points
                        BookingId = originalTransaction.BookingId,
                        Description = $"Отмена транзакции №{originalTransaction.TransactionId} от {originalTransaction.TransactionDate:dd.MM.yyyy HH:mm}. " +
                                     $"Причина: отмена через интерфейс администратора.",
                        TransactionDate = DateTime.Now
                    };

                    context.LoyaltyTransaction.Add(cancellationTransaction);

                    // Update guest points balance
                    var guest = context.Guest.Find(originalTransaction.GuestId);
                    if (guest != null)
                    {
                        guest.CurrentPoints += cancellationTransaction.Points;

                        // Prevent negative balance
                        if (guest.CurrentPoints < 0)
                        {
                            MessageBox.Show("Невозможно отменить транзакцию, так как это приведет к отрицательному балансу баллов.",
                                "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    context.SaveChanges();

                    MessageBox.Show("Транзакция успешно отменена. Баланс баллов обновлен.",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Reload data
                    LoadLoyaltyProgramInfo(context);
                    LoadLoyaltyTransactions(context);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отмене транзакции: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

      
        private void LoadFinancialData(HotelServiceEntities context)
        {
            try
            {
                if (_booking == null) return;

                // Reset total values
                _totalPaid = 0;
                _totalCharges = 0;

                // Load main booking financial information
                LoadMainFinancialInfo();

                // Load all payments
                LoadPaymentsData(context);

                // Load all charges
                LoadChargesData(context);

                // Calculate and update summary
                UpdateFinancialSummary();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке финансовых данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadMainFinancialInfo()
        {
            // Update booking financial information
            TotalAmountTextBlock.Text = string.Format("{0:N2} ₽", _booking.TotalAmount);

            if (_booking.DepositAmount.HasValue)
            {
                DepositAmountTextBlock.Text = string.Format("{0:N2} ₽", _booking.DepositAmount.Value);
            }
            else
            {
                DepositAmountTextBlock.Text = "0.00 ₽";
            }

            DepositPaidTextBlock.Text = _booking.DepositPaid ? "Да" : "Нет";
            DepositPaidTextBlock.Foreground = _booking.DepositPaid
                ? FindResource("SuccessColor") as SolidColorBrush
                : FindResource("WarningColor") as SolidColorBrush;

        }

        private void LoadPaymentsData(HotelServiceEntities context)
        {
            try
            {
                var payments = new List<PaymentViewModel>();

                // First, try to get payments directly without joins to diagnose
                var allPayments = context.Payment
                    .Where(p => p.BookingId == _bookingId)
                    .ToList();

                // Log diagnostic information
                int paymentCount = allPayments.Count;

                // Now load with left joins to ensure we get all payments even if method or status is missing
                var regularPayments = (from p in context.Payment
                                       join pm in context.PaymentMethod on p.PaymentMethodId equals pm.PaymentMethodId into pmJoin
                                       from pm in pmJoin.DefaultIfEmpty()
                                       join fs in context.FinancialStatus on p.StatusId equals fs.StatusId into fsJoin
                                       from fs in fsJoin.DefaultIfEmpty()
                                       where p.BookingId == _bookingId
                                       orderby p.PaymentDate descending
                                       select new PaymentViewModel
                                       {
                                           PaymentId = p.PaymentId,
                                           Amount = p.Amount,
                                           PaymentDate = p.PaymentDate,
                                           PaymentType = "Платеж",
                                           PaymentMethodName = pm != null ? pm.MethodName : "Неизвестно",
                                           StatusName = fs != null ? fs.StatusName : "Неизвестно",
                                           Notes = p.Notes,
                                           SourceId = 1,
                                           SourceItemId = null
                                       }).ToList();

                payments.AddRange(regularPayments);

                // Считаем сумму всех платежей (без учета статуса)
                // Для финансовой вкладки считаем все платежи учтенными
                _totalPaid = allPayments.Sum(p => p.Amount);

               

                // Update payment DataGrid
                PaymentsDataGrid.ItemsSource = payments;
                NoPaymentsTextBlock.Visibility = payments.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch (Exception ex)
            {
                PaymentsDataGrid.ItemsSource = new List<PaymentViewModel>();
                NoPaymentsTextBlock.Visibility = Visibility.Visible;
                MessageBox.Show($"Ошибка при загрузке платежей: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadChargesData(HotelServiceEntities context)
        {
            try
            {
                var charges = new List<ChargeViewModel>();

                // Main room charge
                var nights = (_booking.CheckOutDate.Date - _booking.CheckInDate.Date).Days;
                decimal roomRate = _booking.Room?.BasePrice ?? 0;

                var roomCharge = new ChargeViewModel
                {
                    Description = $"Проживание ({nights} {GetNightsText(nights)})",
                    Date = _booking.CheckInDate,
                    Quantity = nights,
                    UnitPrice = roomRate,
                    TotalPrice = roomRate * nights,
                    Status = "Основное бронирование",
                    SourceId = 1
                };

                charges.Add(roomCharge);
                _totalCharges += roomCharge.TotalPrice;

                // Additional services
                try
                {
                    var services = (from bi in context.BookingItem
                                    join s in context.Service on bi.ServiceId equals s.ServiceId into sJoin
                                    from service in sJoin.DefaultIfEmpty()
                                    where bi.BookingId == _bookingId && bi.ServiceId != null
                                    select new ChargeViewModel
                                    {
                                        Description = service.ServiceName,
                                        Date = bi.ItemDate,
                                        Quantity = bi.Quantity,
                                        UnitPrice = bi.UnitPrice,
                                        TotalPrice = bi.Quantity * bi.UnitPrice,
                                        Status = "Дополнительная услуга",
                                        SourceId = 2,
                                        SourceItemId = bi.ItemId
                                    }).ToList();

                    charges.AddRange(services);
                    _totalCharges += services.Sum(s => s.TotalPrice);
                }
                catch { /* Ignore errors for this part */ }

                // Damage reports
                try
                {
                    var damages = (from dr in context.DamageReport
                                   join dt in context.DamageType on dr.DamageTypeId equals dt.DamageTypeId
                                   where dr.BookingId == _bookingId && dr.Cost.HasValue
                                   select new ChargeViewModel
                                   {
                                       Description = $"Повреждение: {dt.TypeName}",
                                       Date = dr.ReportDate,
                                       UnitPrice = dr.Cost.Value,
                                       TotalPrice = dr.Cost.Value,
                                       Status = "Возмещение ущерба",
                                       SourceId = 3,
                                       SourceItemId = dr.ReportId
                                   }).ToList();

                    charges.AddRange(damages);
                    _totalCharges += damages.Sum(d => d.TotalPrice);
                }
                catch { /* Ignore errors for this part */ }

                // Update charges DataGrid
                ChargesDataGrid.ItemsSource = charges;
                NoChargesTextBlock.Visibility = charges.Any() ? Visibility.Collapsed : Visibility.Visible;

                // Update total charges
                TotalChargesTextBlock.Text = string.Format("{0:N2} ₽", _totalCharges);
            }
            catch (Exception ex)
            {
                ChargesDataGrid.ItemsSource = new List<ChargeViewModel>();
                NoChargesTextBlock.Visibility = Visibility.Visible;
                MessageBox.Show($"Ошибка при загрузке начислений: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateFinancialSummary()
        {
            // Update summary section
            TotalChargesTextBlock.Text = string.Format("{0:N2} ₽", _totalCharges);
            TotalPaidSummaryTextBlock.Text = string.Format("{0:N2} ₽", _totalPaid);

            decimal remainingAmount = Math.Max(0, _totalCharges - _totalPaid);
            RemainingAmountSummaryTextBlock.Text = string.Format("{0:N2} ₽", remainingAmount);

            // Set color based on payment status
            SolidColorBrush textColor;
            if (remainingAmount <= 0)
            {
                textColor = FindResource("SuccessColor") as SolidColorBrush;
            }
            else if (_totalPaid > 0)
            {
                textColor = FindResource("WarningColor") as SolidColorBrush;
            }
            else
            {
                textColor = FindResource("ErrorColor") as SolidColorBrush;
            }

            RemainingAmountSummaryTextBlock.Foreground = textColor;
        }

        private void LoadBookingServices(HotelServiceEntities context)
        {
            try
            {
                var invoiceItems = (from i in context.Booking
                                    join ii in context.BookingItem on i.BookingId equals ii.BookingId
                                    join s in context.Service on ii.ServiceId equals s.ServiceId into sJoin
                                    from service in sJoin.DefaultIfEmpty()
                                    join sc in context.ServiceCategory on service.CategoryId equals sc.CategoryId into scJoin
                                    from category in scJoin.DefaultIfEmpty()
                                    where i.BookingId == _bookingId && ii.ServiceId != null
                                    select new ServiceViewModel
                                    {
                                        ServiceName = service.ServiceName,
                                        CategoryName = category.CategoryName,
                                        ItemDate = ii.ItemDate,
                                        Quantity = ii.Quantity,
                                        UnitPrice = ii.UnitPrice,
                                        TotalPrice = ii.Quantity * ii.UnitPrice
                                    }).ToList();

                ServicesDataGrid.ItemsSource = invoiceItems;
                NoServicesTextBlock.Visibility = invoiceItems.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                ServicesDataGrid.ItemsSource = new List<ServiceViewModel>();
                NoServicesTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void LoadDamageReports(HotelServiceEntities context)
        {
            // Загрузка отчетов о повреждениях
            // Адаптируйте под вашу структуру данных
            try
            {
                var damageReports = (from dr in context.DamageReport
                                     join dt in context.DamageType on dr.DamageTypeId equals dt.DamageTypeId
                                     join ts in context.TaskStatus on dr.StatusId equals ts.StatusId
                                     where dr.BookingId == _bookingId
                                     select new DamageReportViewModel
                                     {
                                         DamageTypeName = dt.TypeName,
                                         ReportDate = dr.ReportDate,
                                         StatusName = ts.StatusName,
                                         EstimatedCost = dr.Cost
                                     }).ToList();

                DamageReportsDataGrid.ItemsSource = damageReports;
                NoDamageReportsTextBlock.Visibility = damageReports.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                DamageReportsDataGrid.ItemsSource = new List<DamageReportViewModel>();
                NoDamageReportsTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void DisplayBookingData()
        {
            // Основная информация о бронировании
            BookingNumberTextBlock.Text = _booking.BookingId.ToString();
            BookingTitleTextBlock.Text = $"Бронирование #{_booking.BookingId}";

            StatusTextBlock.Text = _booking.BookingStatus?.StatusName ?? "Не указан";

            string roomText = "Номер не назначен";
            if (_booking.Room != null)
            {
                roomText = $"Номер {_booking.Room.RoomNumber}";
            }
            RoomTextBlock.Text = roomText;

            // Расчет количества ночей
            int nights = (_booking.CheckOutDate.Date - _booking.CheckInDate.Date).Days;
            NightsTextBlock.Text = $"{nights} {GetNightsText(nights)}";
            NightsCountTextBlock.Text = nights.ToString();

            DateRangeTextBlock.Text = $"{_booking.CheckInDate:dd.MM.yyyy} - {_booking.CheckOutDate:dd.MM.yyyy}";

            // Информация о госте
            if (_booking.Guest != null)
            {
                string fullName = _booking.Guest.LastName + " " + _booking.Guest.FirstName + " " + _booking.Guest.MiddleName;
                string shortName = _booking.Guest.LastName;

                if (!string.IsNullOrEmpty(_booking.Guest.FirstName) && _booking.Guest.FirstName.Length > 0)
                    shortName += " " + _booking.Guest.FirstName[0] + ".";

                if (!string.IsNullOrEmpty(_booking.Guest.MiddleName) && _booking.Guest.MiddleName.Length > 0)
                    shortName += _booking.Guest.MiddleName[0] + ".";

                GuestNameTextBlock.Text = fullName;
                GuestShortNameTextBlock.Text = shortName;
                GuestPhoneTextBlock.Text = _booking.Guest.Phone ?? "Не указан";
                GuestEmailTextBlock.Text = _booking.Guest.Email ?? "Не указан";
                GuestAddressTextBlock.Text = _booking.Guest.Address ?? "Не указан";

                GuestVIPTextBlock.Text = _booking.Guest.IsVIP ? "Да" : "Нет";
                GuestVIPTextBlock.Foreground = _booking.Guest.IsVIP
                    ? FindResource("AccentColor") as SolidColorBrush
                    : FindResource("TextSecondaryColor") as SolidColorBrush;

                GuestPointsTextBlock.Text = _booking.Guest.CurrentPoints.ToString();
            }
            else
            {
                GuestNameTextBlock.Text = "Гость не указан";
                GuestShortNameTextBlock.Text = "Не указан";
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

            if (_booking.Room != null)
            {
                RoomNumberTextBlock.Text = _booking.Room.RoomNumber;
                RoomTypeTextBlock.Text = _booking.Room.RoomType?.TypeName ?? "Не указан";
            }
            else
            {
                RoomNumberTextBlock.Text = "Не назначен";
                RoomTypeTextBlock.Text = "—";
            }

            // Финансовая информация
            TotalAmountTextBlock.Text = string.Format("{0:N2} ₽", _booking.TotalAmount);

            if (_booking.DepositAmount.HasValue)
            {
                DepositAmountTextBlock.Text = string.Format("{0:N2} ₽", _booking.DepositAmount.Value);
            }
            else
            {
                DepositAmountTextBlock.Text = "0.00 ₽";
            }

            DepositPaidTextBlock.Text = _booking.DepositPaid ? "Да" : "Нет";
            DepositPaidTextBlock.Foreground = _booking.DepositPaid
                ? FindResource("SuccessColor") as SolidColorBrush
                : FindResource("WarningColor") as SolidColorBrush;

           
            // Примечания
            if (!string.IsNullOrEmpty(_booking.Notes))
            {
                NotesTextBlock.Text = _booking.Notes;
                NoNotesTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                NotesTextBlock.Visibility = Visibility.Collapsed;
                NoNotesTextBlock.Visibility = Visibility.Visible;
            }

            // Проверка прав доступа для кнопок
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                EditBookingButton.Visibility = Visibility.Collapsed;
                AddServiceButton.Visibility = Visibility.Collapsed;
                AddDamageButton.Visibility = Visibility.Collapsed;
            }

            // Скрываем текстовые заглушки, если есть данные
            NoPaymentsTextBlock.Visibility = PaymentsDataGrid.Items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            NoServicesTextBlock.Visibility = ServicesDataGrid.Items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            NoDamageReportsTextBlock.Visibility = DamageReportsDataGrid.Items.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
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

        private void AddServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для добавления услуг.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var serviceEditWindow = new ServiceEditWindow(_bookingId);
            if (serviceEditWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        LoadBookingServices(context);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении списка услуг: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void EditServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для редактирования услуг.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var serviceViewModel = (sender as Button)?.DataContext as ServiceViewModel;
            if (serviceViewModel == null) return;

            try
            {
                using (var context = new HotelServiceEntities())
                {
                    // Получаем все записи услуг для данного бронирования
                    var bookingItems = context.BookingItem
                        .Include(bi => bi.Service)
                        .Where(bi => bi.BookingId == _bookingId && bi.ServiceId != null)
                        .ToList();

                    // Ищем нужную запись по совпадению названия услуги
                    var bookingItem = bookingItems.FirstOrDefault(bi =>
                        bi.Service != null &&
                        bi.Service.ServiceName == serviceViewModel.ServiceName &&
                        bi.Quantity == serviceViewModel.Quantity &&
                        bi.UnitPrice == serviceViewModel.UnitPrice);

                    if (bookingItem != null)
                    {
                        var serviceEditWindow = new ServiceEditWindow(_bookingId, bookingItem.ItemId);
                        if (serviceEditWindow.ShowDialog() == true)
                        {
                            LoadBookingServices(context);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Не удалось найти данную услугу в базе данных.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании услуги: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteServiceButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для удаления услуг.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var serviceViewModel = (sender as Button)?.DataContext as ServiceViewModel;
            if (serviceViewModel == null) return;

            MessageBoxResult result = MessageBox.Show(
                $"Вы действительно хотите удалить услугу \"{serviceViewModel.ServiceName}\"?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        // Получаем все записи услуг для данного бронирования
                        var bookingItems = context.BookingItem
                            .Include(bi => bi.Service)
                            .Where(bi => bi.BookingId == _bookingId && bi.ServiceId != null)
                            .ToList();

                        // Ищем нужную запись по совпадению названия услуги
                        var bookingItem = bookingItems.FirstOrDefault(bi =>
                            bi.Service != null &&
                            bi.Service.ServiceName == serviceViewModel.ServiceName &&
                            bi.Quantity == serviceViewModel.Quantity &&
                            bi.UnitPrice == serviceViewModel.UnitPrice);

                        if (bookingItem != null)
                        {
                            context.BookingItem.Remove(bookingItem);
                            context.SaveChanges();

                            LoadBookingServices(context);

                            MessageBox.Show("Услуга успешно удалена.", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Не удалось найти данную услугу в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении услуги: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddDamageButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для добавления отчетов о повреждениях.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var damageReportEditWindow = new DamageReportEditWindow(_bookingId);
            if (damageReportEditWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        LoadDamageReports(context);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении списка отчетов: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewDamageReportButton_Click(object sender, RoutedEventArgs e)
        {
            var reportViewModel = (sender as Button)?.DataContext as DamageReportViewModel;
            if (reportViewModel == null) return;

            try
            {
                using (var context = new HotelServiceEntities())
                {
                    // Получаем все отчеты для данного бронирования
                    var damageReports = context.DamageReport
                        .Include(dr => dr.DamageType)
                        .Where(dr => dr.BookingId == _bookingId)
                        .ToList();

                    // Используем метод расширения для работы с датами, а не LINQ to Entities
                    var damageReport = damageReports.FirstOrDefault(dr =>
                        dr.DamageType.TypeName == reportViewModel.DamageTypeName &&
                        dr.ReportDate.Year == reportViewModel.ReportDate.Year &&
                        dr.ReportDate.Month == reportViewModel.ReportDate.Month &&
                        dr.ReportDate.Day == reportViewModel.ReportDate.Day);

                    if (damageReport != null)
                    {
                        var damageReportViewWindow = new DamageReportViewWindow(damageReport.ReportId);
                        damageReportViewWindow.ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось найти данный отчет в базе данных.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при просмотре отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EditDamageReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для редактирования отчетов о повреждениях.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var reportViewModel = (sender as Button)?.DataContext as DamageReportViewModel;
            if (reportViewModel == null) return;

            try
            {
                using (var context = new HotelServiceEntities())
                {
                    // Получаем все отчеты для данного бронирования
                    var damageReports = context.DamageReport
                        .Include(dr => dr.DamageType)
                        .Where(dr => dr.BookingId == _bookingId)
                        .ToList();

                    // Используем метод расширения для работы с датами, а не LINQ to Entities
                    var damageReport = damageReports.FirstOrDefault(dr =>
                        dr.DamageType.TypeName == reportViewModel.DamageTypeName &&
                        dr.ReportDate.Year == reportViewModel.ReportDate.Year &&
                        dr.ReportDate.Month == reportViewModel.ReportDate.Month &&
                        dr.ReportDate.Day == reportViewModel.ReportDate.Day);

                    if (damageReport != null)
                    {
                        var damageReportEditWindow = new DamageReportEditWindow(_bookingId, damageReport.ReportId);
                        if (damageReportEditWindow.ShowDialog() == true)
                        {
                            LoadDamageReports(context);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Не удалось найти данный отчет в базе данных.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при редактировании отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void AddPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для добавления оплаты.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var context = new HotelServiceEntities())
                {
                    var paymentMethods = context.PaymentMethod.OrderBy(pm => pm.MethodName).ToList();

                    if (paymentMethods.Count == 0)
                    {
                        MessageBox.Show("В системе не настроены способы оплаты. Обратитесь к администратору системы.",
                            "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Calculate the remaining amount
                    decimal remainingAmount = Math.Max(0, _totalCharges - _totalPaid);

                    var paymentWindow = new PaymentEditWindow(_bookingId, remainingAmount);
                    if (paymentWindow.ShowDialog() == true)
                    {
                        // Reload financial data
                        LoadFinancialData(context);

                        // Reload booking to update its status
                        _booking = context.Booking
                            .Include(b => b.FinancialStatus)
                            .FirstOrDefault(b => b.BookingId == _bookingId);

                        if (_booking != null)
                        {
                            LoadMainFinancialInfo();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подготовке к добавлению оплаты: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewPaymentButton_Click(object sender, RoutedEventArgs e)
        {
            var payment = (sender as Button)?.DataContext as PaymentViewModel;
            if (payment == null) return;

            try
            {
                using (var context = new HotelServiceEntities())
                {
                    // Check payment type and display appropriate information
                    if (payment.SourceId == 1) // Regular payment
                    {
                        var paymentDetails = context.Payment
                            .Include(p => p.PaymentMethod)
                            .Include(p => p.FinancialStatus)
                            .FirstOrDefault(p => p.PaymentId == payment.PaymentId);

                        if (paymentDetails != null)
                        {
                            var message = $"Детали платежа #{paymentDetails.PaymentId}:\n\n" +
                                $"Дата: {paymentDetails.PaymentDate:dd.MM.yyyy HH:mm}\n" +
                                $"Способ оплаты: {paymentDetails.PaymentMethod?.MethodName ?? "Неизвестно"}\n" +
                                $"Сумма: {paymentDetails.Amount:N2} ₽\n" +
                                $"Статус: {paymentDetails.FinancialStatus?.StatusName ?? "Неизвестно"}\n" +
                                $"Примечание: {paymentDetails.Notes ?? "Отсутствует"}";

                            MessageBox.Show(message, "Информация о платеже", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Информация о платеже не найдена.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении информации о платеже: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeletePaymentButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для удаления оплаты.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var payment = (sender as Button)?.DataContext as PaymentViewModel;
            if (payment == null) return;

            // Check if this is a regular payment that can be deleted
            if (payment.SourceId != 1)
            {
                MessageBox.Show("Этот платеж нельзя удалить. Он связан с другими данными системы.",
                    "Операция невозможна", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                "Вы действительно хотите удалить данный платеж? Это действие невозможно отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        var paymentToDelete = context.Payment.Find(payment.PaymentId);
                        if (paymentToDelete != null)
                        {
                            // Delete the payment
                            context.Payment.Remove(paymentToDelete);
                            context.SaveChanges();

                            // Update booking financial status
                            UpdateBookingFinancialStatus(context, _bookingId);

                            // Reload financial data
                            LoadFinancialData(context);

                            MessageBox.Show("Платеж успешно удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Платеж не найден в базе данных.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении платежа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void AddLoyaltyPointsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для начисления баллов лояльности.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_booking?.Guest == null)
            {
                MessageBox.Show("Невозможно начислить баллы. Гость не указан в бронировании.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Show dialog to add loyalty points
            var loyaltyWindow = new LoyaltyPointsAddWindow(_booking.GuestId, _bookingId);
            if (loyaltyWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        // Reload data
                        _booking.Guest = context.Guest.Find(_booking.GuestId);
                        LoadLoyaltyTransactions(context);

                        // Update guest points in main tab too
                        GuestPointsTextBlock.Text = _booking.Guest.CurrentPoints.ToString();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении данных лояльности: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Add a method to view loyalty transaction details
        private void ViewLoyaltyTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            var transaction = (sender as Button)?.DataContext as LoyaltyTransactionViewModel;
            if (transaction == null) return;

            string message = $"Транзакция #{transaction.TransactionId}\n\n" +
                $"Дата: {transaction.TransactionDate:dd.MM.yyyy HH:mm}\n" +
                $"Тип: {transaction.TypeName}\n" +
                $"Баллы: {transaction.Points}\n" +
                $"Описание: {transaction.Description ?? "Нет описания"}";

            MessageBox.Show(message, "Детали транзакции", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        private void UpdateBookingFinancialStatus(HotelServiceEntities context, int bookingId)
        {
            try
            {
                // First retrieve the booking with a fresh context to avoid caching issues
                var booking = context.Booking.Find(bookingId);
                if (booking != null)
                {
                    // Get count of all payments to check if any exist
                    int paymentCount = context.Payment
                        .Count(p => p.BookingId == bookingId);

                    // Get total paid amount - consider all confirmed payments (status 1)
                    decimal totalPaid = 0;

                    // Use direct SQL query to avoid EF quirks if necessary
                    var payments = context.Payment
                        .Where(p => p.BookingId == bookingId)
                        .ToList();

                    // Calculate sum manually to avoid EF issues
                    if (payments.Any())
                    {
                        totalPaid = payments
                            .Where(p => p.StatusId == 1) // Status 1 = Confirmed
                            .Sum(p => p.Amount);
                    }

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
                else
                {
                    MessageBox.Show($"Бронирование #{bookingId} не найдено при обновлении финансового статуса.",
                        "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении финансового статуса: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void EditBookingButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new BookingEditWindow(_bookingId);
            if (editWindow.ShowDialog() == true)
            {
                LoadBookingData();
            }
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