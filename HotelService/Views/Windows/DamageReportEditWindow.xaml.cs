using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using System.Globalization;
using HotelService.Data;
using System.Data.Entity;
using System.Windows.Controls;

namespace HotelService.Views.Windows
{
    public partial class DamageReportEditWindow : Window
    {
        private readonly int _bookingId;
        private readonly int? _reportId;
        private Booking _booking;
        private DamageReport _damageReport;
        private Guest _selectedGuest;
        private bool _isEditing;

        public DamageReportEditWindow(int bookingId, int? reportId = null)
        {
            InitializeComponent();
            _bookingId = bookingId;
            _reportId = reportId;
            _isEditing = _reportId.HasValue;

            if (_isEditing)
            {
                WindowTitleTextBlock.Text = "Редактирование отчета о повреждении";
            }

            ReportDatePicker.SelectedDate = DateTime.Today;
            SeverityComboBox.SelectedIndex = 0;

            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _booking = context.Booking
                        .Include(b => b.Guest)
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

                    if (_booking.Guest != null)
                    {
                        _selectedGuest = _booking.Guest;
                        GuestTextBox.Text = $"{_booking.Guest.LastName} {_booking.Guest.FirstName} {_booking.Guest.MiddleName}".Trim();
                    }

                    var damageTypes = context.DamageType.OrderBy(dt => dt.TypeName).ToList();
                    DamageTypeComboBox.ItemsSource = damageTypes;

                    if (damageTypes.Any())
                    {
                        DamageTypeComboBox.SelectedIndex = 0;
                    }

                    var statuses = context.TaskStatus.OrderBy(ts => ts.StatusId).ToList();
                    StatusComboBox.ItemsSource = statuses;

                    if (statuses.Any())
                    {
                        StatusComboBox.SelectedIndex = 0;
                    }

                    var rooms = context.Room.OrderBy(r => r.RoomNumber).ToList();
                    RoomComboBox.ItemsSource = rooms;

                    if (_booking.RoomId.HasValue)
                    {
                        RoomComboBox.SelectedValue = _booking.RoomId.Value;
                    }
                    else if (rooms.Any())
                    {
                        RoomComboBox.SelectedIndex = 0;
                    }

                    if (_isEditing && _reportId.HasValue)
                    {
                        _damageReport = context.DamageReport
                            .Include(dr => dr.DamageType)
                            .Include(dr => dr.TaskStatus)
                            .Include(dr => dr.Room)
                            .Include(dr => dr.GuestId)
                            .FirstOrDefault(dr => dr.ReportId == _reportId.Value);

                        if (_damageReport != null && _damageReport.BookingId == _bookingId)
                        {
                            DamageTypeComboBox.SelectedValue = _damageReport.DamageTypeId;
                            StatusComboBox.SelectedValue = _damageReport.StatusId;
                            ReportDatePicker.SelectedDate = _damageReport.ReportDate;
                            RoomComboBox.SelectedValue = _damageReport.RoomId;

                            if (_damageReport.Cost.HasValue)
                            {
                                CostTextBox.Text = _damageReport.Cost.Value.ToString("F2");
                            }

                            DescriptionTextBox.Text = _damageReport.Description;
                            NotesTextBox.Text = _damageReport.Notes;

                            if (_damageReport.Guest != null)
                            {
                                _selectedGuest = _damageReport.Guest;
                                GuestTextBox.Text = $"{_damageReport.Guest.LastName} {_damageReport.Guest.FirstName} {_damageReport.Guest.MiddleName}".Trim();
                            }

                            // Set severity level
                            // Assuming severity is encoded in the description or handled separately
                            // This is a simplified approach; adjust based on your actual data model
                            if (_damageReport.Description.Contains("Критическое"))
                                SeverityComboBox.SelectedIndex = 3;
                            else if (_damageReport.Description.Contains("Серьезное"))
                                SeverityComboBox.SelectedIndex = 2;
                            else if (_damageReport.Description.Contains("Среднее"))
                                SeverityComboBox.SelectedIndex = 1;
                            else
                                SeverityComboBox.SelectedIndex = 0;
                        }
                        else
                        {
                            MessageBox.Show("Отчет о повреждении не найден или не относится к данному бронированию.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            DialogResult = false;
                            Close();
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

        private void SelectGuestButton_Click(object sender, RoutedEventArgs e)
        {
            var guestSelectWindow = new GuestSelectWindow();
            if (guestSelectWindow.ShowDialog() == true)
            {
                _selectedGuest = guestSelectWindow.SelectedGuest;
                if (_selectedGuest != null)
                {
                    GuestTextBox.Text = $"{_selectedGuest.LastName} {_selectedGuest.FirstName} {_selectedGuest.MiddleName}".Trim();
                }
            }
        }

        private bool ValidateForm()
        {
            List<string> errors = new List<string>();

            if (DamageTypeComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать тип повреждения.");
            }

            if (StatusComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать статус отчета.");
            }

            if (!ReportDatePicker.SelectedDate.HasValue)
            {
                errors.Add("Необходимо выбрать дату отчета.");
            }

            if (RoomComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать номер комнаты.");
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                errors.Add("Необходимо добавить описание повреждения.");
            }

            if (!string.IsNullOrWhiteSpace(CostTextBox.Text))
            {
                if (!decimal.TryParse(CostTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal cost) || cost < 0)
                {
                    errors.Add("Стоимость ущерба должна быть неотрицательным числом.");
                }
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

        private void SaveDamageReport()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    int damageTypeId = (int)DamageTypeComboBox.SelectedValue;
                    int statusId = (int)StatusComboBox.SelectedValue;
                    DateTime reportDate = ReportDatePicker.SelectedDate.Value;
                    int roomId = (int)RoomComboBox.SelectedValue;
                    string description = DescriptionTextBox.Text;
                    string notes = NotesTextBox.Text;
                    string severityText = (SeverityComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Незначительное";

                    // Add severity to description if not already included
                    if (!description.Contains($"Степень повреждения: {severityText}"))
                    {
                        description = $"Степень повреждения: {severityText}. {description}";
                    }

                    decimal? cost = null;
                    if (!string.IsNullOrWhiteSpace(CostTextBox.Text))
                    {
                        cost = decimal.Parse(CostTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture);
                    }

                    DamageReport reportToSave;

                    if (_isEditing && _reportId.HasValue)
                    {
                        reportToSave = context.DamageReport.Find(_reportId.Value);
                        if (reportToSave == null || reportToSave.BookingId != _bookingId)
                        {
                            MessageBox.Show("Отчет о повреждении не найден или не относится к данному бронированию.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        reportToSave = new DamageReport();
                        reportToSave.BookingId = _bookingId;
                        context.DamageReport.Add(reportToSave);
                    }

                    reportToSave.DamageTypeId = damageTypeId;
                    reportToSave.StatusId = statusId;
                    reportToSave.ReportDate = reportDate;
                    reportToSave.RoomId = roomId;
                    reportToSave.Description = description;
                    reportToSave.Notes = notes;
                    reportToSave.Cost = cost;

                    if (_selectedGuest != null)
                    {
                        reportToSave.GuestId = _selectedGuest.GuestId;
                    }
                    else if (_booking.Guest != null)
                    {
                        reportToSave.GuestId = _booking.Guest.GuestId;
                    }

                    context.SaveChanges();

                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении отчета о повреждении: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
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
                SaveDamageReport();
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