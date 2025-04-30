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
            public string PaymentMethodName { get; set; }
            public decimal Amount { get; set; }
            public string StatusName { get; set; }
            public string Notes { get; set; }
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
                    LoadPayments(context);

                    // Отображение данных
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

        private void LoadPayments(HotelServiceEntities context)
        {
            try
            {
                var payments = (from p in context.Payment
                                join pm in context.PaymentMethod on p.PaymentMethodId equals pm.PaymentMethodId
                                join fs in context.FinancialStatus on p.StatusId equals fs.StatusId
                                where p.BookingId == _bookingId
                                orderby p.PaymentDate descending
                                select new PaymentViewModel
                                {
                                    PaymentId = p.PaymentId,
                                    Amount = p.Amount,
                                    PaymentDate = p.PaymentDate,
                                    PaymentMethodName = pm.MethodName,
                                    StatusName = fs.StatusName,
                                    Notes = p.Notes
                                }).ToList();

                PaymentsDataGrid.ItemsSource = payments;
                NoPaymentsTextBlock.Visibility = payments.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                PaymentsDataGrid.ItemsSource = new List<PaymentViewModel>();
                NoPaymentsTextBlock.Visibility = Visibility.Visible;
            }
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

            FinancialStatusTextBlock.Text = _booking.FinancialStatus?.StatusName ?? "Не указан";

            // Даты создания и оплаты
            if (_booking.IssueDate != DateTime.MinValue)
            {
                IssueDateTextBlock.Text = _booking.IssueDate.ToString("dd.MM.yyyy");
            }
            else
            {
                IssueDateTextBlock.Text = "Не указана";
            }

            if (_booking.DueDate.HasValue)
            {
                DueDateTextBlock.Text = _booking.DueDate.Value.ToString("dd.MM.yyyy");
            }
            else
            {
                DueDateTextBlock.Text = "Не указан";
            }

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

                    var paymentWindow = new PaymentEditWindow(_bookingId);
                    if (paymentWindow.ShowDialog() == true)
                    {
                        LoadPayments(context);

                        _booking = context.Booking
                            .Include(b => b.FinancialStatus)
                            .FirstOrDefault(b => b.BookingId == _bookingId);

                        if (_booking != null)
                        {
                            FinancialStatusTextBlock.Text = _booking.FinancialStatus?.StatusName ?? "Не указан";
                            DepositPaidTextBlock.Text = _booking.DepositPaid ? "Да" : "Нет";
                            DepositPaidTextBlock.Foreground = _booking.DepositPaid
                                ? FindResource("SuccessColor") as SolidColorBrush
                                : FindResource("WarningColor") as SolidColorBrush;
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
            if (PaymentsDataGrid.SelectedItem is PaymentViewModel selectedPayment)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        var payment = context.Payment
                            .Include(p => p.PaymentMethod)
                            .Include(p => p.FinancialStatus)
                            .FirstOrDefault(p => p.PaymentId == selectedPayment.PaymentId);

                        if (payment != null)
                        {
                            // Здесь можно открыть окно для просмотра деталей платежа
                            // Или показать информацию в диалоговом окне
                            var message = $"Детали платежа #{payment.PaymentId}:\n\n" +
                                $"Дата: {payment.PaymentDate:dd.MM.yyyy}\n" +
                                $"Способ оплаты: {payment.PaymentMethod?.MethodName ?? "Неизвестно"}\n" +
                                $"Сумма: {payment.Amount:N2} ₽\n" +
                                $"Статус: {payment.FinancialStatus?.StatusName ?? "Неизвестно"}\n" +
                                $"Примечание: {payment.Notes ?? "Отсутствует"}";

                            MessageBox.Show(message, "Информация о платеже", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Информация о платеже не найдена.", "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при получении информации о платеже: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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

            if (PaymentsDataGrid.SelectedItem is PaymentViewModel selectedPayment)
            {
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
                            var payment = context.Payment.Find(selectedPayment.PaymentId);
                            if (payment != null)
                            {
                                // Удаляем платеж
                                context.Payment.Remove(payment);
                                context.SaveChanges();

                                // Обновляем финансовый статус бронирования
                                UpdateBookingFinancialStatus(context, _bookingId);

                                // Перезагружаем список платежей
                                LoadPayments(context);

                                // Обновляем информацию о бронировании
                                var booking = context.Booking
                                    .Include(b => b.FinancialStatus)
                                    .FirstOrDefault(b => b.BookingId == _bookingId);

                                if (booking != null)
                                {
                                    FinancialStatusTextBlock.Text = booking.FinancialStatus?.StatusName ?? "Не указан";
                                    DepositPaidTextBlock.Text = booking.DepositPaid ? "Да" : "Нет";
                                    DepositPaidTextBlock.Foreground = booking.DepositPaid
                                        ? FindResource("SuccessColor") as SolidColorBrush
                                        : FindResource("WarningColor") as SolidColorBrush;
                                }

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
        }

        private void UpdateBookingFinancialStatus(HotelServiceEntities context, int bookingId)
        {
            // Метод для обновления финансового статуса бронирования после удаления платежа
            try
            {
                var booking = context.Booking.Find(bookingId);
                if (booking != null)
                {
                    // Получаем сумму всех платежей для данного бронирования
                    decimal totalPaid = context.Payment
                        .Where(p => p.BookingId == bookingId && p.StatusId == 1) // Предполагается, что статус 1 = "Подтверждено"
                        .Sum(p => p.Amount);

                    // Проверяем, оплачен ли депозит
                    booking.DepositPaid = booking.DepositAmount.HasValue && totalPaid >= booking.DepositAmount.Value;

                    // Определяем финансовый статус
                    if (totalPaid >= booking.TotalAmount)
                    {
                        booking.FinancialStatusId = 3; // Полностью оплачено
                    }
                    else if (booking.DepositPaid)
                    {
                        booking.FinancialStatusId = 2; // Оплачен депозит
                    }
                    else
                    {
                        booking.FinancialStatusId = 1; // Неоплачено
                    }

                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении финансового статуса: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
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