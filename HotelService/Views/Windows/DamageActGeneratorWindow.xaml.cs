using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Data.Entity;
using HotelService.Data;
using HotelService.Utils;
using System.Globalization;
// Добавьте using для System.Diagnostics, если его еще нет
using System.Diagnostics;

namespace HotelService.Views.Windows
{
    public partial class DamageActGeneratorWindow : Window
    {
        private readonly int _reportId;
        private DamageReport _damageReport;

        public DamageActGeneratorWindow(int reportId)
        {
            InitializeComponent();
            _reportId = reportId;
            LoadDamageReportData();

            // По умолчанию устанавливаем текущую дату
            ActDatePicker.SelectedDate = DateTime.Today;
        }

        private void LoadDamageReportData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _damageReport = context.DamageReport
                        .Include(dr => dr.DamageType)
                        .Include(dr => dr.Room)
                        .Include(dr => dr.Guest)
                        .Include(dr => dr.Booking)
                        .FirstOrDefault(dr => dr.ReportId == _reportId);

                    if (_damageReport == null)
                    {
                        MessageBox.Show("Отчет о повреждении не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    ActNumberTextBox.Text = _damageReport.ReportId.ToString();
                    DamageDescriptionTextBox.Text = _damageReport.Description;

                    if (_damageReport.Cost.HasValue)
                    {
                        TotalAmountTextBox.Text = _damageReport.Cost.Value.ToString("N2");
                    }

                    if (_damageReport.Guest != null)
                    {
                        string fullName = $"{_damageReport.Guest.LastName} {_damageReport.Guest.FirstName} {_damageReport.Guest.MiddleName}".Trim();
                        GuiltyPersonTextBox.Text = fullName;
                    }

                    if (App.CurrentUser != null)
                    {
                        string receiverName = $"{App.CurrentUser.LastName} {App.CurrentUser.FirstName} {App.CurrentUser.MiddleName}".Trim();
                        ReceiverNameTextBox.Text = receiverName;
                        ReceiverPositionTextBox.Text = "Администратор"; // Или другая логика для определения должности
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

        private bool ValidateForm()
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(ActNumberTextBox.Text)) errors.Add("Не указан номер акта");
            if (ActDatePicker.SelectedDate == null) errors.Add("Не указана дата акта");
            if (string.IsNullOrWhiteSpace(GuiltyPersonTextBox.Text)) errors.Add("Не указано ФИО виновного лица");
            if (string.IsNullOrWhiteSpace(TotalAmountTextBox.Text)) errors.Add("Не указана общая сумма");
            else if (!decimal.TryParse(TotalAmountTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out _)) errors.Add("Общая сумма указана некорректно");
            if (string.IsNullOrWhiteSpace(ReceiverPositionTextBox.Text)) errors.Add("Не указана должность принявшего");
            if (string.IsNullOrWhiteSpace(ReceiverNameTextBox.Text)) errors.Add("Не указано ФИО принявшего");
            if (string.IsNullOrWhiteSpace(DamageDescriptionTextBox.Text)) errors.Add("Не указано описание повреждения");
            // Дополнительная валидация для DamagedItemsTextBox, если это поле обязательно
            // if (string.IsNullOrWhiteSpace(DamagedItemsTextBox.Text)) errors.Add("Не указаны испорченные вещи");


            if (errors.Count > 0)
            {
                ValidationMessageTextBlock.Text = string.Join(Environment.NewLine, errors);
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            ValidationMessageTextBlock.Visibility = Visibility.Collapsed;
            return true;
        }

        private string GenerateActDocument()
        {
            try
            {
                string hotelName = "Улан-Удэ"; // Рекомендуется вынести в настройки или получить из другого источника

                string actNumber = ActNumberTextBox.Text;
                DateTime actDate = ActDatePicker.SelectedDate.Value;
                string guiltyPersonFIO = GuiltyPersonTextBox.Text;
                string damageDescription = DamageDescriptionTextBox.Text;
                decimal totalAmount = decimal.Parse(TotalAmountTextBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture);
                string receiverPosition = ReceiverPositionTextBox.Text;
                string receiverFIO = ReceiverNameTextBox.Text;
                string damagedItems = DamagedItemsTextBox.Text; // Убедитесь, что это поле используется

                // Вызываем обновленный метод из DamageActTemplateGenerator
                string tempFilePath = DamageActTemplateGenerator.CreateTemporaryActFile(
                    hotelName,
                    actNumber,
                    actDate,
                    guiltyPersonFIO,
                    damageDescription,
                    totalAmount,
                    receiverPosition,
                    receiverFIO,
                    damagedItems);

                return tempFilePath;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при формировании акта: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        // Метод GetMonthName здесь больше не нужен, так как он есть в DamageActTemplateGenerator
        // и будет использоваться там при формировании документа.

        private void SaveActDocument(string tempFilePath)
        {
            var saveFileDialog = new SaveFileDialog
            {
                FileName = Path.GetFileName(tempFilePath), // Имя файла будет уже с .docx
                DefaultExt = ".docx", // Изменено на .docx
                Filter = "Документы Word (*.docx)|*.docx|Все файлы (*.*)|*.*", // Изменено на .docx
                Title = "Сохранить акт о повреждении"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(tempFilePath, saveFileDialog.FileName, true);
                    SaveActPathToReport(saveFileDialog.FileName);

                    MessageBox.Show("Акт успешно сохранен.", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    if (MessageBox.Show("Открыть сформированный акт?", "Вопрос",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    {
                        // Используем Process.Start для открытия файла
                        Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                    }
                    DialogResult = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при сохранении акта: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // Попытаемся удалить временный файл
                    try
                    {
                        if (File.Exists(tempFilePath))
                        {
                            File.Delete(tempFilePath);
                        }
                    }
                    catch (Exception exDel)
                    {
                        // Ошибку удаления временного файла можно залогировать, но не обязательно показывать пользователю
                        Debug.WriteLine($"Ошибка при удалении временного файла: {exDel.Message}");
                    }
                }
            }
            else
            {
                // Если пользователь отменил сохранение, также удаляем временный файл
                try
                {
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
                catch (Exception exDel)
                {
                    Debug.WriteLine($"Ошибка при удалении временного файла: {exDel.Message}");
                }
            }
        }

        private void SaveActPathToReport(string actPath)
        {
            try
            {
                using (var context = new HotelServiceEntities())
                {
                    var report = context.DamageReport.Find(_reportId);
                    if (report != null)
                    {
                        // Если у вашей модели DamageReport есть поле для пути к файлу, раскомментируйте и используйте его
                        // report.ActFilePath = actPath; 
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении отчета: {ex.Message}", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                string tempFilePath = GenerateActDocument();
                if (!string.IsNullOrEmpty(tempFilePath))
                {
                    SaveActDocument(tempFilePath);
                }
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close(); // Добавил Close() для согласованности с CloseButton_Click
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