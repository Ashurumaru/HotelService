using System;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using System.Data.Entity;
using HotelService.Data;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace HotelService.Views.Windows
{
    public partial class DamageReportViewWindow : Window
    {
        private readonly int _reportId;
        private DamageReport _damageReport;
        private bool _isDataLoaded = false;

        public DamageReportViewWindow(int reportId)
        {
            InitializeComponent();
            _reportId = reportId;
            LoadData();

            if (!_isDataLoaded)
            {
                Close();
            }

            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                EditButton.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    var report = context.DamageReport
                        .Include(dr => dr.DamageType)
                        .Include(dr => dr.TaskStatus)
                        .Include(dr => dr.Room)
                        .Include(dr => dr.Guest)
                        .Include(dr => dr.Booking)
                        .FirstOrDefault(dr => dr.ReportId == _reportId);

                    if (report == null)
                    {
                        MessageBox.Show("Отчет о повреждении не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    _damageReport = report;
                    _isDataLoaded = true;

                    DisplayReportData();
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

        private void DisplayReportData()
        {
            ReportNumberTextBlock.Text = _damageReport.ReportId.ToString();
            ReportIdTextBlock.Text = _damageReport.ReportId.ToString();

            DamageTypeTextBlock.Text = _damageReport.DamageType?.TypeName ?? "Не указан";
            ReportDateTextBlock.Text = _damageReport.ReportDate.ToString("dd.MM.yyyy");
            RoomNumberTextBlock.Text = _damageReport.Room?.RoomNumber ?? "Не указан";

            if (_damageReport.Cost.HasValue)
            {
                CostTextBlock.Text = string.Format("{0:N2} ₽", _damageReport.Cost.Value);
            }
            else
            {
                CostTextBlock.Text = "Не определена";
            }

            StatusTextBlock.Text = _damageReport.TaskStatus?.StatusName ?? "Не указан";
            StatusNameTextBlock.Text = _damageReport.TaskStatus?.StatusName ?? "Не указан";

            // Set status color based on status
            if (_damageReport.TaskStatus != null)
            {
                SolidColorBrush statusBrush;
                switch (_damageReport.StatusId)
                {
                    case 1: // Assuming 1 means "New"
                        statusBrush = FindResource("InfoColor") as SolidColorBrush;
                        break;
                    case 2: // Assuming 2 means "In Progress"
                        statusBrush = FindResource("WarningColor") as SolidColorBrush;
                        break;
                    case 3: // Assuming 3 means "Completed"
                        statusBrush = FindResource("SuccessColor") as SolidColorBrush;
                        break;
                    case 4: // Assuming 4 means "Cancelled"
                        statusBrush = FindResource("ErrorColor") as SolidColorBrush;
                        break;
                    default:
                        statusBrush = FindResource("AccentColor") as SolidColorBrush;
                        break;
                }

                var border = (StatusTextBlock.Parent as Border);
                if (border != null)
                {
                    border.Background = statusBrush;
                }
            }

            if (_damageReport.Booking != null)
            {
                BookingTextBlock.Text = $"Бронирование #{_damageReport.Booking.BookingId}";
            }
            else
            {
                BookingTextBlock.Text = "Не привязано к бронированию";
            }

            if (_damageReport.Guest != null)
            {
                string shortName = _damageReport.Guest.LastName;

                if (!string.IsNullOrEmpty(_damageReport.Guest.FirstName) && _damageReport.Guest.FirstName.Length > 0)
                    shortName += " " + _damageReport.Guest.FirstName[0] + ".";

                if (!string.IsNullOrEmpty(_damageReport.Guest.MiddleName) && _damageReport.Guest.MiddleName.Length > 0)
                    shortName += _damageReport.Guest.MiddleName[0] + ".";

                GuestTextBlock.Text = shortName;
            }
            else
            {
                GuestTextBlock.Text = "Не указан";
            }

            // Extract severity from description
            string severityLevel = "Незначительное";
            if (!string.IsNullOrEmpty(_damageReport.Description))
            {
                var match = Regex.Match(_damageReport.Description, @"Степень повреждения:\s*(\w+)");
                if (match.Success && match.Groups.Count > 1)
                {
                    severityLevel = match.Groups[1].Value;
                }

                // Clean description by removing the severity prefix
                string cleanDescription = Regex.Replace(_damageReport.Description, @"Степень повреждения:\s*\w+\.\s*", "");
                DescriptionTextBlock.Text = cleanDescription;
            }
            else
            {
                DescriptionTextBlock.Text = "Описание отсутствует";
            }

            SeverityLevelTextBlock.Text = severityLevel;
            SeverityTextBlock.Text = $"Степень: {severityLevel}";

            // Set severity color based on level
            SolidColorBrush severityBrush;
            switch (severityLevel.ToLower())
            {
                case "критическое":
                    severityBrush = FindResource("ErrorColor") as SolidColorBrush;
                    break;
                case "серьезное":
                    severityBrush = FindResource("WarningColor") as SolidColorBrush;
                    break;
                case "среднее":
                    severityBrush = FindResource("InfoColor") as SolidColorBrush;
                    break;
                default: // Незначительное
                    severityBrush = FindResource("PrimaryLightColor") as SolidColorBrush;
                    break;
            }

            var severityBorder = (SeverityTextBlock.Parent as Border);
            if (severityBorder != null)
            {
                severityBorder.Background = severityBrush;
            }

            NotesTextBlock.Text = !string.IsNullOrEmpty(_damageReport.Notes) ? _damageReport.Notes : "Нет примечаний";
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (_damageReport?.Booking != null)
            {
                var editWindow = new DamageReportEditWindow(_damageReport.BookingId.Value, _damageReport.ReportId);
                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            else
            {
                MessageBox.Show("Невозможно редактировать отчет, не привязанный к бронированию.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CloseButton2_Click(object sender, RoutedEventArgs e)
        {
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
            Close();
        }
    }
}