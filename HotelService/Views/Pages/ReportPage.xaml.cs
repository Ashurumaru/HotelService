using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using System.Linq;
using System.Data.Entity;
using HotelService.Data;
using Xceed.Words.NET;
using System.Globalization;
using Xceed.Document.NET;

namespace HotelService.Views.Pages
{
    public partial class ReportPage : Page
    {
        public ReportPage()
        {
            InitializeComponent();

            // Инициализируем текущие значения для элементов выбора даты
            DailyReportDatePicker.SelectedDate = DateTime.Today;
            WeeklyStartDatePicker.SelectedDate = GetStartOfWeek(DateTime.Today);
            WeeklyEndDatePicker.SelectedDate = GetStartOfWeek(DateTime.Today).AddDays(6);

            // Инициализируем выпадающие списки для выбора месяца и года
            InitializeMonthYearSelectors();

            // Инициализируем элементы для финансовых отчетов
            DailyIncomeReportDatePicker.SelectedDate = DateTime.Today;
            WeeklyIncomeStartDatePicker.SelectedDate = GetStartOfWeek(DateTime.Today);
            WeeklyIncomeEndDatePicker.SelectedDate = GetStartOfWeek(DateTime.Today).AddDays(6);

            // Установка текущего месяца в комбобоксе доходов
            int currentMonth = DateTime.Today.Month;
            foreach (ComboBoxItem item in IncomeMonthComboBox.Items)
            {
                if (int.Parse(item.Tag.ToString()) == currentMonth)
                {
                    item.IsSelected = true;
                    break;
                }
            }

            // Заполнение комбобокса годами для отчетов о доходах
            int currentYear = DateTime.Today.Year;
            for (int year = currentYear - 2; year <= currentYear + 1; year++)
            {
                ComboBoxItem yearItem = new ComboBoxItem
                {
                    Content = year.ToString(),
                    Tag = year
                };
                IncomeYearComboBox.Items.Add(yearItem);

                if (year == currentYear)
                {
                    yearItem.IsSelected = true;
                }
            }

            // Обновляем отображение периода для отчетов о доходах
            UpdateIncomeMonthlyDateRange();

            // Инициализируем элементы для отчетов о платежах
            PaymentSummaryStartDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            PaymentSummaryEndDatePicker.SelectedDate = DateTime.Today;

            GuestDemographicsStartDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            GuestDemographicsEndDatePicker.SelectedDate = DateTime.Today;

            // Same period for booking sources report
            BookingSourcesStartDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            BookingSourcesEndDatePicker.SelectedDate = DateTime.Today;

            // First day of current year to today for guest groups report (annual analysis)
            GuestGroupsStartDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
            GuestGroupsEndDatePicker.SelectedDate = DateTime.Today;

            DailyCleaningReportDatePicker.SelectedDate = DateTime.Today;

            // Current week for cleaning assignments
            CleaningAssignmentsStartDatePicker.SelectedDate = GetStartOfWeek(DateTime.Today);
            CleaningAssignmentsEndDatePicker.SelectedDate = GetStartOfWeek(DateTime.Today).AddDays(6);


        }


        private void InitializeMonthYearSelectors()
        {
            // Установка текущего месяца в комбобоксе
            int currentMonth = DateTime.Today.Month;
            foreach (ComboBoxItem item in MonthComboBox.Items)
            {
                if (int.Parse(item.Tag.ToString()) == currentMonth)
                {
                    item.IsSelected = true;
                    break;
                }
            }

            // Заполнение комбобокса годами (текущий год и несколько предыдущих)
            int currentYear = DateTime.Today.Year;
            for (int year = currentYear - 2; year <= currentYear + 1; year++)
            {
                ComboBoxItem yearItem = new ComboBoxItem
                {
                    Content = year.ToString(),
                    Tag = year
                };
                YearComboBox.Items.Add(yearItem);

                if (year == currentYear)
                {
                    yearItem.IsSelected = true;
                }
            }

            // Обновляем отображение периода
            UpdateMonthlyDateRange();
        }

        private void MonthYearChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateMonthlyDateRange();
        }

        private void UpdateMonthlyDateRange()
        {
            if (MonthComboBox.SelectedItem == null || YearComboBox.SelectedItem == null)
                return;

            int month = int.Parse(((ComboBoxItem)MonthComboBox.SelectedItem).Tag.ToString());
            int year = int.Parse(((ComboBoxItem)YearComboBox.SelectedItem).Tag.ToString());

            DateTime startDate = new DateTime(year, month, 1);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            MonthlyDateRangeText.Text = $"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
        }

        private DateTime GetStartOfWeek(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.AddDays(-1 * diff).Date;
        }

        #region Экспорт отчетов о загрузке

        private void ExportDailyOccupancyReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!DailyReportDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите дату для отчета.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime reportDate = DailyReportDatePicker.SelectedDate.Value;

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Отчет_ежедневной_загрузки_{reportDate:yyyy-MM-dd}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GenerateDailyOccupancyReport(context, reportDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportWeeklyOccupancyReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!WeeklyStartDatePicker.SelectedDate.HasValue || !WeeklyEndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите даты начала и конца недели.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = WeeklyStartDatePicker.SelectedDate.Value;
                DateTime endDate = WeeklyEndDatePicker.SelectedDate.Value;

                // Проверка корректности выбранного периода
                TimeSpan period = endDate - startDate;
                if (period.Days != 6)
                {
                    MessageBox.Show("Период должен составлять ровно 7 дней (неделю).", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Отчет_еженедельной_загрузки_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GenerateWeeklyOccupancyReport(context, startDate, endDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportMonthlyOccupancyReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (MonthComboBox.SelectedItem == null || YearComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите месяц и год.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int month = int.Parse(((ComboBoxItem)MonthComboBox.SelectedItem).Tag.ToString());
                int year = int.Parse(((ComboBoxItem)YearComboBox.SelectedItem).Tag.ToString());

                DateTime startDate = new DateTime(year, month, 1);
                DateTime endDate = startDate.AddMonths(1).AddDays(-1);

                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Отчет_ежемесячной_загрузки_{monthName}_{year}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GenerateMonthlyOccupancyReport(context, startDate, endDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Генерация отчетов

        private class OccupancyData
        {
            public DateTime Date { get; set; }
            public int TotalRooms { get; set; }
            public int OccupiedRooms { get; set; }
            public int FreeRooms { get; set; }
            public double OccupancyPercentage { get; set; }
            public Dictionary<string, int> RoomStatusCounts { get; set; }
        }

        private OccupancyData GetOccupancyData(HotelServiceEntities context, DateTime date)
        {
            // Получаем общее количество номеров
            int totalRooms = context.Room.Count();

            // Получаем количество занятых номеров на указанную дату
            int occupiedRooms = context.Booking
                .Where(b => b.CheckInDate <= date && b.CheckOutDate > date &&
                       b.BookingStatusId != 5)
                .Count();

            // Вычисляем количество свободных номеров
            int freeRooms = totalRooms - occupiedRooms;

            // Вычисляем процент загрузки
            double occupancyPercentage = totalRooms > 0 ? (double)occupiedRooms / totalRooms * 100 : 0;

            // Получаем статистику по статусам номеров
            var roomStatusCounts = context.Room
                .GroupBy(r => r.RoomStatus.StatusName)
                .ToDictionary(g => g.Key, g => g.Count());

            return new OccupancyData
            {
                Date = date,
                TotalRooms = totalRooms,
                OccupiedRooms = occupiedRooms,
                FreeRooms = freeRooms,
                OccupancyPercentage = occupancyPercentage,
                RoomStatusCounts = roomStatusCounts
            };
        }

        private List<OccupancyData> GetOccupancyDataForPeriod(HotelServiceEntities context, DateTime startDate, DateTime endDate)
        {
            List<OccupancyData> result = new List<OccupancyData>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                result.Add(GetOccupancyData(context, date));
            }

            return result;
        }

        private void GenerateDailyOccupancyReport(HotelServiceEntities context, DateTime reportDate, string filePath)
        {
            // Получаем данные о загрузке на указанную дату
            OccupancyData data = GetOccupancyData(context, reportDate);

            using (var doc = DocX.Create(filePath))
            {
                // Настройка параметров документа
                SetupDocument(doc);

                // Добавляем заголовок отчета
                var title = doc.InsertParagraph()
                    .Append($"ОТЧЕТ О ЗАГРУЗКЕ ГОСТИНИЦЫ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                var subtitle = doc.InsertParagraph()
                    .Append($"на {reportDate:dd.MM.yyyy}")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Создаем таблицу для основных показателей
                var mainTable = doc.AddTable(5, 2);
                mainTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                mainTable.Alignment = Xceed.Document.NET.Alignment.center;
                mainTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Заголовок таблицы
                var mainTableTitle = doc.InsertParagraph()
                    .Append("Основные показатели загрузки")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                mainTableTitle.SpacingBefore(10).SpacingAfter(5);

                // Настраиваем заголовок таблицы
                SetupTableHeader(mainTable, "Показатель", "Значение");

                // Заполняем строки таблицы данными
                int rowIndex = 1;

                AddRowToTable(mainTable, rowIndex++, "Общее количество номеров", data.TotalRooms.ToString());
                AddRowToTable(mainTable, rowIndex++, "Занятые номера", data.OccupiedRooms.ToString());
                AddRowToTable(mainTable, rowIndex++, "Свободные номера", data.FreeRooms.ToString());
                AddRowToTable(mainTable, rowIndex++, "Процент загрузки", $"{data.OccupancyPercentage:F2}%");

                // Добавляем таблицу в документ
                doc.InsertParagraph().InsertTableAfterSelf(mainTable);

                // Создаем таблицу для статусов номеров
                var statusTableTitle = doc.InsertParagraph()
                    .Append("Распределение номеров по статусам")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                statusTableTitle.SpacingBefore(15).SpacingAfter(5);

                var statusTable = doc.AddTable(data.RoomStatusCounts.Count + 1, 2);
                statusTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                statusTable.Alignment = Xceed.Document.NET.Alignment.center;
                statusTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Настраиваем заголовок таблицы статусов
                SetupTableHeader(statusTable, "Статус номера", "Количество");

                // Заполняем таблицу статусов
                rowIndex = 1;
                foreach (var status in data.RoomStatusCounts)
                {
                    AddRowToTable(statusTable, rowIndex++, status.Key, status.Value.ToString());
                }

                // Добавляем таблицу статусов в документ
                doc.InsertParagraph().InsertTableAfterSelf(statusTable);

                // Добавляем нижний колонтитул с датой формирования отчета
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Добавляем подпись
                AddSignature(doc);

                // Сохраняем документ
                doc.Save();
            }
        }

        private void GenerateWeeklyOccupancyReport(HotelServiceEntities context, DateTime startDate, DateTime endDate, string filePath)
        {
            // Получаем данные о загрузке за неделю
            List<OccupancyData> weeklyData = GetOccupancyDataForPeriod(context, startDate, endDate);

            using (var doc = DocX.Create(filePath))
            {
                // Настройка параметров документа
                SetupDocument(doc);

                // Добавляем заголовок отчета
                var title = doc.InsertParagraph()
                    .Append($"ЕЖЕНЕДЕЛЬНЫЙ ОТЧЕТ О ЗАГРУЗКЕ ГОСТИНИЦЫ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                var subtitle = doc.InsertParagraph()
                    .Append($"за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Создаем таблицу для данных по дням
                var dailyTable = doc.AddTable(weeklyData.Count + 1, 5);
                dailyTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                dailyTable.Alignment = Xceed.Document.NET.Alignment.center;
                dailyTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Добавляем заголовок
                var tableTitle = doc.InsertParagraph()
                    .Append("Показатели загрузки по дням недели")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                tableTitle.SpacingBefore(10).SpacingAfter(5);

                // Настраиваем заголовок таблицы
                dailyTable.Rows[0].Cells[0].Paragraphs[0].Append("Дата")
                    .Font("Times New Roman").FontSize(11).Bold();
                dailyTable.Rows[0].Cells[1].Paragraphs[0].Append("День недели")
                    .Font("Times New Roman").FontSize(11).Bold();
                dailyTable.Rows[0].Cells[2].Paragraphs[0].Append("Занятые номера")
                    .Font("Times New Roman").FontSize(11).Bold();
                dailyTable.Rows[0].Cells[3].Paragraphs[0].Append("Свободные номера")
                    .Font("Times New Roman").FontSize(11).Bold();
                dailyTable.Rows[0].Cells[4].Paragraphs[0].Append("Процент загрузки")
                    .Font("Times New Roman").FontSize(11).Bold();

                // Заполняем таблицу данными по дням
                for (int i = 0; i < weeklyData.Count; i++)
                {
                    var data = weeklyData[i];
                    int rowIndex = i + 1;

                    dailyTable.Rows[rowIndex].Cells[0].Paragraphs[0].Append(data.Date.ToString("dd.MM.yyyy"))
                        .Font("Times New Roman").FontSize(11);
                    dailyTable.Rows[rowIndex].Cells[1].Paragraphs[0].Append(GetDayOfWeekName(data.Date))
                        .Font("Times New Roman").FontSize(11);
                    dailyTable.Rows[rowIndex].Cells[2].Paragraphs[0].Append(data.OccupiedRooms.ToString())
                        .Font("Times New Roman").FontSize(11);
                    dailyTable.Rows[rowIndex].Cells[3].Paragraphs[0].Append(data.FreeRooms.ToString())
                        .Font("Times New Roman").FontSize(11);
                    dailyTable.Rows[rowIndex].Cells[4].Paragraphs[0].Append($"{data.OccupancyPercentage:F2}%")
                        .Font("Times New Roman").FontSize(11);
                }

                // Добавляем таблицу в документ
                doc.InsertParagraph().InsertTableAfterSelf(dailyTable);

                // Добавляем сводную информацию
                var summaryTitle = doc.InsertParagraph()
                    .Append("Сводная информация за неделю")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                summaryTitle.SpacingBefore(15).SpacingAfter(5);

                var summaryTable = doc.AddTable(4, 2);
                summaryTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                summaryTable.Alignment = Xceed.Document.NET.Alignment.center;
                summaryTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Настраиваем заголовок сводной таблицы
                SetupTableHeader(summaryTable, "Показатель", "Значение");

                // Заполняем сводную таблицу
                double avgOccupancy = weeklyData.Average(d => d.OccupancyPercentage);
                int maxOccupiedRooms = weeklyData.Max(d => d.OccupiedRooms);
                int minOccupiedRooms = weeklyData.Min(d => d.OccupiedRooms);
                double stdDevOccupancy = CalculateStandardDeviation(weeklyData.Select(d => d.OccupancyPercentage));

                int rowIdx = 1;
                AddRowToTable(summaryTable, rowIdx++, "Средний % загрузки за неделю", $"{avgOccupancy:F2}%");
                AddRowToTable(summaryTable, rowIdx++, "Максимальное количество занятых номеров", maxOccupiedRooms.ToString());
                AddRowToTable(summaryTable, rowIdx++, "Минимальное количество занятых номеров", minOccupiedRooms.ToString());

                // Добавляем сводную таблицу в документ
                doc.InsertParagraph().InsertTableAfterSelf(summaryTable);

                // Добавляем нижний колонтитул с датой формирования отчета
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Добавляем подпись
                AddSignature(doc);

                // Сохраняем документ
                doc.Save();
            }
        }

        private void GenerateMonthlyOccupancyReport(HotelServiceEntities context, DateTime startDate, DateTime endDate, string filePath)
        {
            // Получаем данные о загрузке за месяц
            List<OccupancyData> monthlyData = GetOccupancyDataForPeriod(context, startDate, endDate);

            using (var doc = DocX.Create(filePath))
            {
                // Настройка параметров документа
                SetupDocument(doc);

                // Добавляем заголовок отчета
                var title = doc.InsertParagraph()
                    .Append($"ЕЖЕМЕСЯЧНЫЙ ОТЧЕТ О ЗАГРУЗКЕ ГОСТИНИЦЫ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(startDate.Month);
                var subtitle = doc.InsertParagraph()
                    .Append($"за {monthName} {startDate.Year} года")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Добавляем сводную информацию
                var summaryTitle = doc.InsertParagraph()
                    .Append("Сводная информация за месяц")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                summaryTitle.SpacingBefore(10).SpacingAfter(5);

                var summaryTable = doc.AddTable(5, 2);
                summaryTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                summaryTable.Alignment = Xceed.Document.NET.Alignment.center;
                summaryTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Настраиваем заголовок сводной таблицы
                SetupTableHeader(summaryTable, "Показатель", "Значение");

                // Заполняем сводную таблицу
                double avgOccupancy = monthlyData.Average(d => d.OccupancyPercentage);
                int maxOccupiedRooms = monthlyData.Max(d => d.OccupiedRooms);
                DateTime peakOccupancyDate = monthlyData.First(d => d.OccupiedRooms == maxOccupiedRooms).Date;
                int minOccupiedRooms = monthlyData.Min(d => d.OccupiedRooms);
                DateTime lowOccupancyDate = monthlyData.First(d => d.OccupiedRooms == minOccupiedRooms).Date;

                int rowIdx = 1;
                AddRowToTable(summaryTable, rowIdx++, "Средний % загрузки за месяц", $"{avgOccupancy:F2}%");
                AddRowToTable(summaryTable, rowIdx++, "Максимальное количество занятых номеров", maxOccupiedRooms.ToString());
                AddRowToTable(summaryTable, rowIdx++, "Дата пиковой загрузки", peakOccupancyDate.ToString("dd.MM.yyyy"));
                AddRowToTable(summaryTable, rowIdx++, "Минимальное количество занятых номеров", minOccupiedRooms.ToString());

                // Добавляем сводную таблицу в документ
                doc.InsertParagraph().InsertTableAfterSelf(summaryTable);

                // Создаем таблицу для данных по дням
                var weeklyGroupsTitle = doc.InsertParagraph()
                    .Append("Данные о загрузке по неделям")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                weeklyGroupsTitle.SpacingBefore(15).SpacingAfter(5);

                // Группируем данные по неделям
                var weeklyGroups = monthlyData.GroupBy(d => GetWeekOfMonth(d.Date));

                foreach (var weekGroup in weeklyGroups)
                {
                    var weekData = weekGroup.ToList();

                    var weekTitle = doc.InsertParagraph()
                        .Append($"Неделя {weekGroup.Key}: {weekData.First().Date:dd.MM.yyyy} - {weekData.Last().Date:dd.MM.yyyy}")
                        .Font("Times New Roman")
                        .FontSize(11);
                    weekTitle.SpacingBefore(5).SpacingAfter(2);

                    var weekTable = doc.AddTable(weekData.Count + 1, 4);
                    weekTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                    weekTable.Alignment = Xceed.Document.NET.Alignment.center;
                    weekTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                    // Настраиваем заголовок таблицы
                    weekTable.Rows[0].Cells[0].Paragraphs[0].Append("Дата")
                        .Font("Times New Roman").FontSize(11).Bold();
                    weekTable.Rows[0].Cells[1].Paragraphs[0].Append("Занятые номера")
                        .Font("Times New Roman").FontSize(11).Bold();
                    weekTable.Rows[0].Cells[2].Paragraphs[0].Append("Свободные номера")
                        .Font("Times New Roman").FontSize(11).Bold();
                    weekTable.Rows[0].Cells[3].Paragraphs[0].Append("Процент загрузки")
                        .Font("Times New Roman").FontSize(11).Bold();

                    // Заполняем таблицу данными по дням
                    for (int i = 0; i < weekData.Count; i++)
                    {
                        var data = weekData[i];
                        int rowIndex = i + 1;

                        weekTable.Rows[rowIndex].Cells[0].Paragraphs[0].Append($"{data.Date:dd.MM.yyyy} ({GetDayOfWeekNameShort(data.Date)})")
                            .Font("Times New Roman").FontSize(11);
                        weekTable.Rows[rowIndex].Cells[1].Paragraphs[0].Append(data.OccupiedRooms.ToString())
                            .Font("Times New Roman").FontSize(11);
                        weekTable.Rows[rowIndex].Cells[2].Paragraphs[0].Append(data.FreeRooms.ToString())
                            .Font("Times New Roman").FontSize(11);
                        weekTable.Rows[rowIndex].Cells[3].Paragraphs[0].Append($"{data.OccupancyPercentage:F2}%")
                            .Font("Times New Roman").FontSize(11);
                    }

                    // Добавляем таблицу в документ
                    doc.InsertParagraph().InsertTableAfterSelf(weekTable);
                }

                // Добавляем нижний колонтитул с датой формирования отчета
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Добавляем подпись
                AddSignature(doc);

                // Сохраняем документ
                doc.Save();
            }
        }

        #endregion

        #region Income and Payment Reports Data Classes

        private class DailyIncomeData
        {
            public DateTime Date { get; set; }
            public decimal TotalRevenue { get; set; }
            public decimal RoomRevenue { get; set; }
            public decimal ServiceRevenue { get; set; }
            public decimal Taxes { get; set; }
            public decimal ADR { get; set; } // Average Daily Rate
            public int OccupiedRooms { get; set; }
        }

        private class PaymentSummaryData
        {
            public decimal TotalPaymentsReceived { get; set; }
            public Dictionary<string, decimal> PaymentsByMethod { get; set; }
            public decimal OutstandingBalance { get; set; }
        }

        #endregion

        #region Financial Data Methods

        private DailyIncomeData GetIncomeData(HotelServiceEntities context, DateTime date)
        {
            // Получаем все активные бронирования на указанную дату
            var activeBookings = context.Booking
                .Where(b => b.CheckInDate <= date && b.CheckOutDate > date &&
                       b.BookingStatusId != 5) // Исключаем отмененные бронирования
                .ToList(); // выполняем запрос и получаем данные в память

            // Считаем количество занятых номеров
            int occupiedRooms = activeBookings.Count;

            // Теперь вычисляем доход от номеров
            decimal roomRevenue = 0;
            foreach (var booking in activeBookings)
            {
                int totalDays = (booking.CheckOutDate - booking.CheckInDate).Days;
                if (totalDays > 0)
                {
                    roomRevenue += booking.TotalAmount / totalDays; // Распределяем сумму равномерно по дням
                }
            }

            // Получаем данные о дополнительных услугах на указанную дату
            var bookingItems = context.BookingItem
                .Where(i => i.ItemDate == date && i.ServiceId != null)
                .ToList();

            // Вычисляем доход от услуг
            decimal serviceRevenue = bookingItems.Sum(i => i.Quantity * i.UnitPrice);

            // Для простоты, считаем налоги 20% от общей выручки
            decimal totalRevenue = roomRevenue + serviceRevenue;
            decimal taxes = totalRevenue * 0.2m;

            // Вычисляем ADR (Average Daily Rate)
            decimal adr = occupiedRooms > 0 ? roomRevenue / occupiedRooms : 0;

            return new DailyIncomeData
            {
                Date = date,
                TotalRevenue = totalRevenue,
                RoomRevenue = roomRevenue,
                ServiceRevenue = serviceRevenue,
                Taxes = taxes,
                ADR = adr,
                OccupiedRooms = occupiedRooms
            };
        }

        private List<DailyIncomeData> GetIncomeDataForPeriod(HotelServiceEntities context, DateTime startDate, DateTime endDate)
        {
            List<DailyIncomeData> result = new List<DailyIncomeData>();

            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                result.Add(GetIncomeData(context, date));
            }

            return result;
        }

        private PaymentSummaryData GetPaymentSummaryData(HotelServiceEntities context, DateTime startDate, DateTime endDate)
        {
            // Get total payments received in the period
            var payments = context.Payment
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .ToList();

            decimal totalPaymentsReceived = payments.Sum(p => p.Amount);

            // Group payments by payment method
            var paymentsByMethod = payments
                .GroupBy(p => p.PaymentMethod.MethodName)
                .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

            // Calculate outstanding balance (total bookings amount - total payments)
            decimal totalBookingAmount = context.Booking
                .Where(b => b.CheckInDate <= endDate && b.BookingStatusId != 5) // Exclude cancelled bookings
                .Sum(b => b.TotalAmount);

            decimal totalPayments = context.Payment.Sum(p => p.Amount);
            decimal outstandingBalance = totalBookingAmount - totalPayments;

            return new PaymentSummaryData
            {
                TotalPaymentsReceived = totalPaymentsReceived,
                PaymentsByMethod = paymentsByMethod,
                OutstandingBalance = outstandingBalance
            };
        }

        #endregion

        #region Financial Reports Export Methods

        private void ExportDailyIncomeReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!DailyIncomeReportDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите дату для отчета.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime reportDate = DailyIncomeReportDatePicker.SelectedDate.Value;

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Отчет_о_доходах_за_день_{reportDate:yyyy-MM-dd}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GenerateDailyIncomeReport(context, reportDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportWeeklyIncomeReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!WeeklyIncomeStartDatePicker.SelectedDate.HasValue || !WeeklyIncomeEndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите даты начала и конца недели.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = WeeklyIncomeStartDatePicker.SelectedDate.Value;
                DateTime endDate = WeeklyIncomeEndDatePicker.SelectedDate.Value;

                // Проверка корректности выбранного периода
                TimeSpan period = endDate - startDate;
                if (period.Days != 6)
                {
                    MessageBox.Show("Период должен составлять ровно 7 дней (неделю).", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Отчет_о_доходах_за_неделю_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GenerateWeeklyIncomeReport(context, startDate, endDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportMonthlyIncomeReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (IncomeMonthComboBox.SelectedItem == null || IncomeYearComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Пожалуйста, выберите месяц и год.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                int month = int.Parse(((ComboBoxItem)IncomeMonthComboBox.SelectedItem).Tag.ToString());
                int year = int.Parse(((ComboBoxItem)IncomeYearComboBox.SelectedItem).Tag.ToString());

                DateTime startDate = new DateTime(year, month, 1);
                DateTime endDate = startDate.AddMonths(1).AddDays(-1);

                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(month);

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Отчет_о_доходах_за_месяц_{monthName}_{year}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GenerateMonthlyIncomeReport(context, startDate, endDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportPaymentSummaryReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!PaymentSummaryStartDatePicker.SelectedDate.HasValue || !PaymentSummaryEndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите даты начала и конца периода.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = PaymentSummaryStartDatePicker.SelectedDate.Value;
                DateTime endDate = PaymentSummaryEndDatePicker.SelectedDate.Value;

                if (startDate > endDate)
                {
                    MessageBox.Show("Дата начала периода должна быть раньше даты окончания.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Сводка_по_платежам_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GeneratePaymentSummaryReport(context, startDate, endDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void IncomeMonthYearChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateIncomeMonthlyDateRange();
        }

        private void UpdateIncomeMonthlyDateRange()
        {
            if (IncomeMonthComboBox.SelectedItem == null || IncomeYearComboBox.SelectedItem == null)
                return;

            int month = int.Parse(((ComboBoxItem)IncomeMonthComboBox.SelectedItem).Tag.ToString());
            int year = int.Parse(((ComboBoxItem)IncomeYearComboBox.SelectedItem).Tag.ToString());

            DateTime startDate = new DateTime(year, month, 1);
            DateTime endDate = startDate.AddMonths(1).AddDays(-1);

            IncomeMonthlyDateRangeText.Text = $"{startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}";
        }

        #endregion

        #region Generate Financial Reports

        private void GenerateDailyIncomeReport(HotelServiceEntities context, DateTime reportDate, string filePath)
        {
            // Получаем данные о доходах на указанную дату
            DailyIncomeData data = GetIncomeData(context, reportDate);

            using (var doc = DocX.Create(filePath))
            {
                // Настройка параметров документа
                SetupDocument(doc);

                // Добавляем заголовок отчета
                var title = doc.InsertParagraph()
                    .Append($"ОТЧЕТ О ДОХОДАХ ГОСТИНИЦЫ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                var subtitle = doc.InsertParagraph()
                    .Append($"за {reportDate:dd.MM.yyyy}")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Создаем таблицу для основных показателей
                var mainTable = doc.AddTable(6, 2);
                mainTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                mainTable.Alignment = Xceed.Document.NET.Alignment.center;
                mainTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Заголовок таблицы
                var mainTableTitle = doc.InsertParagraph()
                    .Append("Финансовые показатели")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                mainTableTitle.SpacingBefore(10).SpacingAfter(5);

                // Настраиваем заголовок таблицы
                SetupTableHeader(mainTable, "Показатель", "Значение");

                // Заполняем строки таблицы данными
                int rowIndex = 1;

                AddRowToTable(mainTable, rowIndex++, "Общий доход", $"{data.TotalRevenue:F2} ₽");
                AddRowToTable(mainTable, rowIndex++, "Доход от номеров", $"{data.RoomRevenue:F2} ₽");
                AddRowToTable(mainTable, rowIndex++, "Доход от услуг", $"{data.ServiceRevenue:F2} ₽");
                AddRowToTable(mainTable, rowIndex++, "Налоги", $"{data.Taxes:F2} ₽");
                AddRowToTable(mainTable, rowIndex++, "Средний дневной тариф (ADR)", $"{data.ADR:F2} ₽");

                // Добавляем таблицу в документ
                doc.InsertParagraph().InsertTableAfterSelf(mainTable);

                // Добавляем информацию о загрузке номеров
                var occupancyTitle = doc.InsertParagraph()
                    .Append("Информация о загрузке номеров")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                occupancyTitle.SpacingBefore(15).SpacingAfter(5);

                var occupancyTable = doc.AddTable(2, 2);
                occupancyTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                occupancyTable.Alignment = Xceed.Document.NET.Alignment.center;
                occupancyTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Настраиваем заголовок таблицы
                SetupTableHeader(occupancyTable, "Показатель", "Значение");

                // Заполняем таблицу данными
                AddRowToTable(occupancyTable, 1, "Занятые номера", data.OccupiedRooms.ToString());

                // Добавляем таблицу в документ
                doc.InsertParagraph().InsertTableAfterSelf(occupancyTable);

                // Добавляем нижний колонтитул с датой формирования отчета
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Добавляем подпись
                AddSignature(doc);

                // Сохраняем документ
                doc.Save();
            }
        }

        private void GenerateWeeklyIncomeReport(HotelServiceEntities context, DateTime startDate, DateTime endDate, string filePath)
        {
            // Получаем данные о доходах за неделю
            List<DailyIncomeData> weeklyData = GetIncomeDataForPeriod(context, startDate, endDate);

            using (var doc = DocX.Create(filePath))
            {
                // Настройка параметров документа
                SetupDocument(doc);

                // Добавляем заголовок отчета
                var title = doc.InsertParagraph()
                    .Append($"ЕЖЕНЕДЕЛЬНЫЙ ОТЧЕТ О ДОХОДАХ ГОСТИНИЦЫ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                var subtitle = doc.InsertParagraph()
                    .Append($"за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Создаем таблицу для данных по дням
                var dailyTable = doc.AddTable(weeklyData.Count + 1, 6);
                dailyTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                dailyTable.Alignment = Xceed.Document.NET.Alignment.center;
                dailyTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Добавляем заголовок
                var tableTitle = doc.InsertParagraph()
                    .Append("Доходы по дням недели")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                tableTitle.SpacingBefore(10).SpacingAfter(5);

                // Настраиваем заголовок таблицы
                dailyTable.Rows[0].Cells[0].Paragraphs[0].Append("Дата")
                    .Font("Times New Roman").FontSize(11).Bold();
                dailyTable.Rows[0].Cells[1].Paragraphs[0].Append("День недели")
                    .Font("Times New Roman").FontSize(11).Bold();
                dailyTable.Rows[0].Cells[2].Paragraphs[0].Append("Общий доход")
                    .Font("Times New Roman").FontSize(11).Bold();
                dailyTable.Rows[0].Cells[3].Paragraphs[0].Append("Доход от номеров")
                    .Font("Times New Roman").FontSize(11).Bold();
                dailyTable.Rows[0].Cells[4].Paragraphs[0].Append("Доход от услуг")
                    .Font("Times New Roman").FontSize(11).Bold();
                dailyTable.Rows[0].Cells[5].Paragraphs[0].Append("ADR")
                    .Font("Times New Roman").FontSize(11).Bold();

                // Заполняем таблицу данными по дням
                for (int i = 0; i < weeklyData.Count; i++)
                {
                    var data = weeklyData[i];
                    int rowIndex = i + 1;

                    dailyTable.Rows[rowIndex].Cells[0].Paragraphs[0].Append(data.Date.ToString("dd.MM.yyyy"))
                        .Font("Times New Roman").FontSize(11);
                    dailyTable.Rows[rowIndex].Cells[1].Paragraphs[0].Append(GetDayOfWeekName(data.Date))
                        .Font("Times New Roman").FontSize(11);
                    dailyTable.Rows[rowIndex].Cells[2].Paragraphs[0].Append($"{data.TotalRevenue:F2} ₽")
                        .Font("Times New Roman").FontSize(11);
                    dailyTable.Rows[rowIndex].Cells[3].Paragraphs[0].Append($"{data.RoomRevenue:F2} ₽")
                        .Font("Times New Roman").FontSize(11);
                    dailyTable.Rows[rowIndex].Cells[4].Paragraphs[0].Append($"{data.ServiceRevenue:F2} ₽")
                        .Font("Times New Roman").FontSize(11);
                    dailyTable.Rows[rowIndex].Cells[5].Paragraphs[0].Append($"{data.ADR:F2} ₽")
                        .Font("Times New Roman").FontSize(11);
                }

                // Добавляем таблицу в документ
                doc.InsertParagraph().InsertTableAfterSelf(dailyTable);

                // Добавляем сводную информацию
                var summaryTitle = doc.InsertParagraph()
                    .Append("Сводная информация за неделю")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                summaryTitle.SpacingBefore(15).SpacingAfter(5);

                var summaryTable = doc.AddTable(5, 2);
                summaryTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                summaryTable.Alignment = Xceed.Document.NET.Alignment.center;
                summaryTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Настраиваем заголовок сводной таблицы
                SetupTableHeader(summaryTable, "Показатель", "Значение");

                // Заполняем сводную таблицу
                decimal totalRevenue = weeklyData.Sum(d => d.TotalRevenue);
                decimal totalRoomRevenue = weeklyData.Sum(d => d.RoomRevenue);
                decimal totalServiceRevenue = weeklyData.Sum(d => d.ServiceRevenue);
                decimal avgADR = weeklyData.Average(d => d.ADR);
                decimal maxDailyRevenue = weeklyData.Max(d => d.TotalRevenue);

                int rowIdx = 1;
                AddRowToTable(summaryTable, rowIdx++, "Общий доход за неделю", $"{totalRevenue:F2} ₽");
                AddRowToTable(summaryTable, rowIdx++, "Доход от номеров за неделю", $"{totalRoomRevenue:F2} ₽");
                AddRowToTable(summaryTable, rowIdx++, "Доход от услуг за неделю", $"{totalServiceRevenue:F2} ₽");
                AddRowToTable(summaryTable, rowIdx++, "Средний ADR за неделю", $"{avgADR:F2} ₽");

                // Добавляем сводную таблицу в документ
                doc.InsertParagraph().InsertTableAfterSelf(summaryTable);

                // Добавляем нижний колонтитул с датой формирования отчета
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Добавляем подпись
                AddSignature(doc);

                // Сохраняем документ
                doc.Save();
            }
        }

        private void GenerateMonthlyIncomeReport(HotelServiceEntities context, DateTime startDate, DateTime endDate, string filePath)
        {
            // Получаем данные о доходах за месяц
            List<DailyIncomeData> monthlyData = GetIncomeDataForPeriod(context, startDate, endDate);

            using (var doc = DocX.Create(filePath))
            {
                // Настройка параметров документа
                SetupDocument(doc);

                // Добавляем заголовок отчета
                var title = doc.InsertParagraph()
                    .Append($"ЕЖЕМЕСЯЧНЫЙ ОТЧЕТ О ДОХОДАХ ГОСТИНИЦЫ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                string monthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(startDate.Month);
                var subtitle = doc.InsertParagraph()
                    .Append($"за {monthName} {startDate.Year} года")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Добавляем сводную информацию
                var summaryTitle = doc.InsertParagraph()
                    .Append("Сводная информация за месяц")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                summaryTitle.SpacingBefore(10).SpacingAfter(5);

                var summaryTable = doc.AddTable(5, 2);
                summaryTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                summaryTable.Alignment = Xceed.Document.NET.Alignment.center;
                summaryTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Настраиваем заголовок сводной таблицы
                SetupTableHeader(summaryTable, "Показатель", "Значение");

                // Заполняем сводную таблицу
                decimal totalRevenue = monthlyData.Sum(d => d.TotalRevenue);
                decimal totalRoomRevenue = monthlyData.Sum(d => d.RoomRevenue);
                decimal totalServiceRevenue = monthlyData.Sum(d => d.ServiceRevenue);
                decimal avgADR = monthlyData.Average(d => d.ADR);
                var maxRevenueDay = monthlyData.OrderByDescending(d => d.TotalRevenue).First();

                int rowIdx = 1;
                AddRowToTable(summaryTable, rowIdx++, "Общий доход за месяц", $"{totalRevenue:F2} ₽");
                AddRowToTable(summaryTable, rowIdx++, "Доход от номеров за месяц", $"{totalRoomRevenue:F2} ₽");
                AddRowToTable(summaryTable, rowIdx++, "Доход от услуг за месяц", $"{totalServiceRevenue:F2} ₽");
                AddRowToTable(summaryTable, rowIdx++, "Средний ADR за месяц", $"{avgADR:F2} ₽");

                // Добавляем сводную таблицу в документ
                doc.InsertParagraph().InsertTableAfterSelf(summaryTable);

                // Группируем данные по неделям
                var weeklyGroupsTitle = doc.InsertParagraph()
                    .Append("Данные о доходах по неделям")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                weeklyGroupsTitle.SpacingBefore(15).SpacingAfter(5);

                // Группируем данные по неделям
                var weeklyGroups = monthlyData.GroupBy(d => GetWeekOfMonth(d.Date));

                foreach (var weekGroup in weeklyGroups)
                {
                    var weekData = weekGroup.ToList();

                    var weekTitle = doc.InsertParagraph()
                        .Append($"Неделя {weekGroup.Key}: {weekData.First().Date:dd.MM.yyyy} - {weekData.Last().Date:dd.MM.yyyy}")
                        .Font("Times New Roman")
                        .FontSize(11);
                    weekTitle.SpacingBefore(5).SpacingAfter(2);

                    var weekTable = doc.AddTable(weekData.Count + 1, 5);
                    weekTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                    weekTable.Alignment = Xceed.Document.NET.Alignment.center;
                    weekTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                    // Настраиваем заголовок таблицы
                    weekTable.Rows[0].Cells[0].Paragraphs[0].Append("Дата")
                        .Font("Times New Roman").FontSize(11).Bold();
                    weekTable.Rows[0].Cells[1].Paragraphs[0].Append("Общий доход")
                        .Font("Times New Roman").FontSize(11).Bold();
                    weekTable.Rows[0].Cells[2].Paragraphs[0].Append("Доход от номеров")
                        .Font("Times New Roman").FontSize(11).Bold();
                    weekTable.Rows[0].Cells[3].Paragraphs[0].Append("Доход от услуг")
                        .Font("Times New Roman").FontSize(11).Bold();
                    weekTable.Rows[0].Cells[4].Paragraphs[0].Append("ADR")
                        .Font("Times New Roman").FontSize(11).Bold();

                    // Заполняем таблицу данными
                    // Заполняем таблицу данными по дням
                    for (int i = 0; i < weekData.Count; i++)
                    {
                        var data = weekData[i];
                        int rowIndex = i + 1;

                        weekTable.Rows[rowIndex].Cells[0].Paragraphs[0].Append($"{data.Date:dd.MM.yyyy} ({GetDayOfWeekNameShort(data.Date)})")
                            .Font("Times New Roman").FontSize(11);
                        weekTable.Rows[rowIndex].Cells[1].Paragraphs[0].Append($"{data.TotalRevenue:F2} ₽")
                            .Font("Times New Roman").FontSize(11);
                        weekTable.Rows[rowIndex].Cells[2].Paragraphs[0].Append($"{data.RoomRevenue:F2} ₽")
                            .Font("Times New Roman").FontSize(11);
                        weekTable.Rows[rowIndex].Cells[3].Paragraphs[0].Append($"{data.ServiceRevenue:F2} ₽")
                            .Font("Times New Roman").FontSize(11);
                        weekTable.Rows[rowIndex].Cells[4].Paragraphs[0].Append($"{data.ADR:F2} ₽")
                            .Font("Times New Roman").FontSize(11);
                    }

                    // Добавляем таблицу в документ
                    doc.InsertParagraph().InsertTableAfterSelf(weekTable);
                }

                // Добавляем нижний колонтитул с датой формирования отчета
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Добавляем подпись
                AddSignature(doc);

                // Сохраняем документ
                doc.Save();
            }
        }

        private void GeneratePaymentSummaryReport(HotelServiceEntities context, DateTime startDate, DateTime endDate, string filePath)
        {
            // Получаем данные о платежах за указанный период
            PaymentSummaryData data = GetPaymentSummaryData(context, startDate, endDate);

            using (var doc = DocX.Create(filePath))
            {
                // Настройка параметров документа
                SetupDocument(doc);

                // Добавляем заголовок отчета
                var title = doc.InsertParagraph()
                    .Append($"СВОДКА ПО ПЛАТЕЖАМ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                var subtitle = doc.InsertParagraph()
                    .Append($"за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Создаем таблицу для основных показателей
                var mainTable = doc.AddTable(3, 2);
                mainTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                mainTable.Alignment = Xceed.Document.NET.Alignment.center;
                mainTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Заголовок таблицы
                var mainTableTitle = doc.InsertParagraph()
                    .Append("Основные показатели")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                mainTableTitle.SpacingBefore(10).SpacingAfter(5);

                // Настраиваем заголовок таблицы
                SetupTableHeader(mainTable, "Показатель", "Значение");

                // Заполняем строки таблицы данными
                int rowIndex = 1;

                AddRowToTable(mainTable, rowIndex++, "Общая сумма полученных платежей", $"{data.TotalPaymentsReceived:F2} ₽");
                AddRowToTable(mainTable, rowIndex++, "Непогашенные остатки", $"{data.OutstandingBalance:F2} ₽");

                // Добавляем таблицу в документ
                doc.InsertParagraph().InsertTableAfterSelf(mainTable);

                // Создаем таблицу для платежей по способам оплаты
                if (data.PaymentsByMethod.Any())
                {
                    var methodsTitle = doc.InsertParagraph()
                        .Append("Платежи по способам оплаты")
                        .Font("Times New Roman")
                        .FontSize(12)
                        .Bold();
                    methodsTitle.SpacingBefore(15).SpacingAfter(5);

                    var methodsTable = doc.AddTable(data.PaymentsByMethod.Count + 1, 3);
                    methodsTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                    methodsTable.Alignment = Xceed.Document.NET.Alignment.center;
                    methodsTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                    // Настраиваем заголовок таблицы
                    methodsTable.Rows[0].Cells[0].Paragraphs[0].Append("Способ оплаты")
                        .Font("Times New Roman").FontSize(11).Bold();
                    methodsTable.Rows[0].Cells[1].Paragraphs[0].Append("Сумма")
                        .Font("Times New Roman").FontSize(11).Bold();
                    methodsTable.Rows[0].Cells[2].Paragraphs[0].Append("Процент от общей суммы")
                        .Font("Times New Roman").FontSize(11).Bold();

                    // Заполняем таблицу данными
                    int methodIndex = 1;
                    foreach (var method in data.PaymentsByMethod)
                    {
                        decimal percentage = data.TotalPaymentsReceived > 0
                            ? method.Value / data.TotalPaymentsReceived * 100
                            : 0;

                        methodsTable.Rows[methodIndex].Cells[0].Paragraphs[0].Append(method.Key)
                            .Font("Times New Roman").FontSize(11);
                        methodsTable.Rows[methodIndex].Cells[1].Paragraphs[0].Append($"{method.Value:F2} ₽")
                            .Font("Times New Roman").FontSize(11);
                        methodsTable.Rows[methodIndex].Cells[2].Paragraphs[0].Append($"{percentage:F2}%")
                            .Font("Times New Roman").FontSize(11);

                        methodIndex++;
                    }

                    // Добавляем таблицу в документ
                    doc.InsertParagraph().InsertTableAfterSelf(methodsTable);
                }
                else
                {
                    var noPaymentsMsg = doc.InsertParagraph()
                        .Append("В указанный период платежи не производились.")
                        .Font("Times New Roman")
                        .FontSize(11)
                        .Italic();
                    noPaymentsMsg.SpacingBefore(15);
                }

                // Добавляем нижний колонтитул с датой формирования отчета
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Добавляем подпись
                AddSignature(doc);

                // Сохраняем документ
                doc.Save();
            }
        }

        #region Guest Data Classes

        private class GuestDemographicsData
        {
            public int TotalGuests { get; set; }
            public double AverageStayDuration { get; set; }
            public int NewGuests { get; set; }
            public int ReturningGuests { get; set; }
            public Dictionary<string, int> GuestsByCountry { get; set; }
        }

        private class BookingSourceData
        {
            public Dictionary<string, int> BookingsBySource { get; set; }
            public Dictionary<string, decimal> RevenueBySource { get; set; }
        }

        private class GuestGroupData
        {
            public Dictionary<string, int> GuestsByGroup { get; set; }
            public Dictionary<string, double> AverageStayByGroup { get; set; }
        }

        #endregion

        #region Room Cleaning Data Classes

        private class RoomCleaningStatusData
        {
            public int TotalRooms { get; set; }
            public int CleanedRooms { get; set; }
            public int NeedCleaningRooms { get; set; }
            public int InspectedRooms { get; set; }
            public Dictionary<string, List<string>> RoomsByStatus { get; set; }
        }

        private class CleaningAssignmentData
        {
            public string StaffName { get; set; }
            public List<string> AssignedRooms { get; set; }
            public DateTime AssignmentTime { get; set; }
            public DateTime? CompletionTime { get; set; }
            public TimeSpan? Duration { get; set; }
        }

        private class CleaningEfficiencyData
        {
            public Dictionary<string, double> AverageCleaningTimeByRoomType { get; set; }
            public Dictionary<string, int> TasksCompletedByStaff { get; set; }
            public double OverallEfficiencyScore { get; set; }
        }

        #endregion

        #region Guest Report Methods

        private void InitializeGuestReportControls()
        {
            // Initialize date pickers for guest reports
            GuestDemographicsStartDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            GuestDemographicsEndDatePicker.SelectedDate = DateTime.Today;

            BookingSourcesStartDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            BookingSourcesEndDatePicker.SelectedDate = DateTime.Today;

            GuestGroupsStartDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, 1, 1);
            GuestGroupsEndDatePicker.SelectedDate = DateTime.Today;
        }

        private GuestDemographicsData GetGuestDemographicsData(HotelServiceEntities context, DateTime startDate, DateTime endDate)
        {
            // Get bookings within the date range
            var bookings = context.Booking
                .Where(b => b.CheckInDate >= startDate && b.CheckInDate <= endDate)
                .Include(b => b.Guest)
                .ToList();

            // Calculate total guests
            int totalGuests = bookings.Select(b => b.GuestId).Distinct().Count();

            // Calculate average stay duration
            double averageStayDuration = 0;
            if (bookings.Any())
            {
                averageStayDuration = bookings.Average(b => (b.CheckOutDate - b.CheckInDate).Days);
            }

            // Get returning guests
            var guestVisitCounts = bookings
                .GroupBy(b => b.GuestId)
                .ToDictionary(g => g.Key, g => g.Count());

            int returningGuests = guestVisitCounts.Count(kv => kv.Value > 1);
            int newGuests = totalGuests - returningGuests;

            // Get guests by country/region (using address information if available)
            var guestsByCountry = new Dictionary<string, int>();
            var guests = bookings.Select(b => b.Guest).Distinct().ToList();

            // This is simplified - in a real application, you would parse addresses to extract country information
            guestsByCountry["Россия"] = guests.Count(g => g.Address != null && g.Address.Contains("Россия"));
            guestsByCountry["Другие страны"] = guests.Count(g => g.Address == null || !g.Address.Contains("Россия"));

            return new GuestDemographicsData
            {
                TotalGuests = totalGuests,
                AverageStayDuration = averageStayDuration,
                NewGuests = newGuests,
                ReturningGuests = returningGuests,
                GuestsByCountry = guestsByCountry
            };
        }

        private BookingSourceData GetBookingSourceData(HotelServiceEntities context, DateTime startDate, DateTime endDate)
        {
            // Get bookings within the date range
            var bookings = context.Booking
                .Where(b => b.CheckInDate >= startDate && b.CheckInDate <= endDate)
                .Include(b => b.BookingSource)
                .ToList();

            // Group bookings by source
            var bookingsBySource = bookings
                .GroupBy(b => b.BookingSource.SourceName)
                .ToDictionary(g => g.Key, g => g.Count());

            // Calculate revenue by source
            var revenueBySource = bookings
                .GroupBy(b => b.BookingSource.SourceName)
                .ToDictionary(g => g.Key, g => g.Sum(b => b.TotalAmount));

            return new BookingSourceData
            {
                BookingsBySource = bookingsBySource,
                RevenueBySource = revenueBySource
            };
        }

        private GuestGroupData GetGuestGroupData(HotelServiceEntities context, DateTime startDate, DateTime endDate)
        {
            // Get bookings within the date range
            var bookings = context.Booking
                .Where(b => b.CheckInDate >= startDate && b.CheckInDate <= endDate)
                .Include(b => b.Guest)
                .Include(b => b.Guest.GuestGroup)
                .ToList();

            // Group guests by guest group
            var guestsByGroup = new Dictionary<string, int>();
            var averageStayByGroup = new Dictionary<string, double>();

            // Add VIP group
            var vipGuests = bookings
                .Where(b => b.Guest.IsVIP)
                .Select(b => b.GuestId)
                .Distinct()
                .Count();
            guestsByGroup["VIP"] = vipGuests;

            // Calculate average stay duration for VIP guests
            var vipBookings = bookings.Where(b => b.Guest.IsVIP).ToList();
            if (vipBookings.Any())
            {
                averageStayByGroup["VIP"] = vipBookings.Average(b => (b.CheckOutDate - b.CheckInDate).Days);
            }
            else
            {
                averageStayByGroup["VIP"] = 0;
            }

            // Add Non-VIP group
            var nonVipGuests = bookings
                .Where(b => !b.Guest.IsVIP)
                .Select(b => b.GuestId)
                .Distinct()
                .Count();
            guestsByGroup["Стандартные"] = nonVipGuests;

            // Calculate average stay duration for non-VIP guests
            var nonVipBookings = bookings.Where(b => !b.Guest.IsVIP).ToList();
            if (nonVipBookings.Any())
            {
                averageStayByGroup["Стандартные"] = nonVipBookings.Average(b => (b.CheckOutDate - b.CheckInDate).Days);
            }
            else
            {
                averageStayByGroup["Стандартные"] = 0;
            }

            // Additional group processing would go here if guest groups are used in the database

            return new GuestGroupData
            {
                GuestsByGroup = guestsByGroup,
                AverageStayByGroup = averageStayByGroup
            };
        }

        private void ExportGuestDemographicsReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!GuestDemographicsStartDatePicker.SelectedDate.HasValue || !GuestDemographicsEndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите даты начала и конца периода.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = GuestDemographicsStartDatePicker.SelectedDate.Value;
                DateTime endDate = GuestDemographicsEndDatePicker.SelectedDate.Value;

                if (startDate > endDate)
                {
                    MessageBox.Show("Дата начала периода должна быть раньше даты окончания.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Демография_гостей_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GenerateGuestDemographicsReport(context, startDate, endDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportBookingSourcesReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!BookingSourcesStartDatePicker.SelectedDate.HasValue || !BookingSourcesEndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите даты начала и конца периода.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = BookingSourcesStartDatePicker.SelectedDate.Value;
                DateTime endDate = BookingSourcesEndDatePicker.SelectedDate.Value;

                if (startDate > endDate)
                {
                    MessageBox.Show("Дата начала периода должна быть раньше даты окончания.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Источники_бронирования_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GenerateBookingSourcesReport(context, startDate, endDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportGuestGroupsReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!GuestGroupsStartDatePicker.SelectedDate.HasValue || !GuestGroupsEndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите даты начала и конца периода.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = GuestGroupsStartDatePicker.SelectedDate.Value;
                DateTime endDate = GuestGroupsEndDatePicker.SelectedDate.Value;

                if (startDate > endDate)
                {
                    MessageBox.Show("Дата начала периода должна быть раньше даты окончания.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Анализ_гостей_по_группам_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GenerateGuestGroupsReport(context, startDate, endDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Room Cleaning Report Methods

        private void InitializeCleaningReportControls()
        {
            // Initialize date pickers for cleaning reports
            DailyCleaningReportDatePicker.SelectedDate = DateTime.Today;

            CleaningAssignmentsStartDatePicker.SelectedDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            CleaningAssignmentsEndDatePicker.SelectedDate = DateTime.Today;


        }

        private RoomCleaningStatusData GetRoomCleaningStatusData(HotelServiceEntities context, DateTime date)
        {
            // Get all rooms
            var rooms = context.Room.Include(r => r.RoomStatus).ToList();

            // Count rooms by status
            int cleanedRooms = rooms.Count(r => r.RoomStatusId == 1); // Assuming status 1 is "Clean"
            int needCleaningRooms = rooms.Count(r => r.RoomStatusId == 2); // Assuming status 2 is "Needs Cleaning"
            int inspectedRooms = rooms.Count(r => r.RoomStatusId == 3); // Assuming status 3 is "Inspected"

            // Group rooms by status
            var roomsByStatus = new Dictionary<string, List<string>>();

            roomsByStatus["Убранные"] = rooms
                .Where(r => r.RoomStatusId == 1)
                .Select(r => r.RoomNumber)
                .ToList();

            roomsByStatus["Требуют уборки"] = rooms
                .Where(r => r.RoomStatusId == 2)
                .Select(r => r.RoomNumber)
                .ToList();

            roomsByStatus["Проверенные"] = rooms
                .Where(r => r.RoomStatusId == 3)
                .Select(r => r.RoomNumber)
                .ToList();

            return new RoomCleaningStatusData
            {
                TotalRooms = rooms.Count,
                CleanedRooms = cleanedRooms,
                NeedCleaningRooms = needCleaningRooms,
                InspectedRooms = inspectedRooms,
                RoomsByStatus = roomsByStatus
            };
        }

        private List<CleaningAssignmentData> GetCleaningAssignmentData(HotelServiceEntities context, DateTime startDate, DateTime endDate)
        {
            // This is a simplified version - in a real application, you would have a proper table for cleaning assignments
            // For now, we'll generate some sample data
            var result = new List<CleaningAssignmentData>();

            var rooms = context.Room.ToList();
            var roomsPerStaff = rooms.Count / 3; // Assume we have 3 staff members

            string[] staffNames = { "Иванова А.П.", "Петрова Е.С.", "Сидорова Т.В." };

            for (int i = 0; i < staffNames.Length; i++)
            {
                var staffRooms = rooms
                    .Skip(i * roomsPerStaff)
                    .Take(roomsPerStaff)
                    .Select(r => r.RoomNumber)
                    .ToList();

                var assignmentTime = new DateTime(startDate.Year, startDate.Month, startDate.Day, 8, 0, 0);
                var completionTime = assignmentTime.AddHours(4); // Assume 4 hours to complete

                result.Add(new CleaningAssignmentData
                {
                    StaffName = staffNames[i],
                    AssignedRooms = staffRooms,
                    AssignmentTime = assignmentTime,
                    CompletionTime = completionTime,
                    Duration = completionTime - assignmentTime
                });
            }

            return result;
        }

        private CleaningEfficiencyData GetCleaningEfficiencyData(HotelServiceEntities context, DateTime startDate, DateTime endDate)
        {
            // This is a simplified version - in a real application, you would have proper tables for tracking cleaning efficiency
            // For now, we'll generate some sample data
            var result = new CleaningEfficiencyData();

            // Sample average cleaning times by room type
            result.AverageCleaningTimeByRoomType = new Dictionary<string, double>
    {
        { "Стандартный", 30 }, // 30 minutes
        { "Люкс", 45 }, // 45 minutes
        { "Апартамент", 60 } // 60 minutes
    };

            // Sample tasks completed by staff
            result.TasksCompletedByStaff = new Dictionary<string, int>
    {
        { "Иванова А.П.", 15 },
        { "Петрова Е.С.", 12 },
        { "Сидорова Т.В.", 18 }
    };

            // Sample overall efficiency score (out of 100)
            result.OverallEfficiencyScore = 85.5;

            return result;
        }

        private void ExportDailyCleaningReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!DailyCleaningReportDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите дату для отчета.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime reportDate = DailyCleaningReportDatePicker.SelectedDate.Value;

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Отчет_по_уборке_{reportDate:yyyy-MM-dd}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GenerateDailyCleaningReport(context, reportDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportCleaningAssignmentsReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!CleaningAssignmentsStartDatePicker.SelectedDate.HasValue || !CleaningAssignmentsEndDatePicker.SelectedDate.HasValue)
                {
                    MessageBox.Show("Пожалуйста, выберите даты начала и конца периода.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                DateTime startDate = CleaningAssignmentsStartDatePicker.SelectedDate.Value;
                DateTime endDate = CleaningAssignmentsEndDatePicker.SelectedDate.Value;

                if (startDate > endDate)
                {
                    MessageBox.Show("Дата начала периода должна быть раньше даты окончания.", "Предупреждение",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Word Document (*.docx)|*.docx",
                    FileName = $"Отчет_по_назначенным_задачам_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.docx",
                    DefaultExt = ".docx"
                };

                if (saveDialog.ShowDialog() == true)
                {
                    string filePath = saveDialog.FileName;

                    using (var context = new HotelServiceEntities())
                    {
                        GenerateCleaningAssignmentsReport(context, startDate, endDate, filePath);
                    }

                    AskToOpenFile(filePath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportCleaningEfficiencyReport_Click(object sender, RoutedEventArgs e)
        {
            try
            {
              

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при экспорте отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Generate Guest Reports

        private void GenerateGuestDemographicsReport(HotelServiceEntities context, DateTime startDate, DateTime endDate, string filePath)
        {
            // Get guest demographics data
            GuestDemographicsData data = GetGuestDemographicsData(context, startDate, endDate);

            using (var doc = DocX.Create(filePath))
            {
                // Setup document properties
                SetupDocument(doc);

                // Add report title
                var title = doc.InsertParagraph()
                    .Append("ОТЧЕТ ПО ДЕМОГРАФИИ ГОСТЕЙ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                var subtitle = doc.InsertParagraph()
                    .Append($"за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Create main statistics table
                var mainTable = doc.AddTable(3, 2);
                mainTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                mainTable.Alignment = Xceed.Document.NET.Alignment.center;
                mainTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Add table title
                var mainTableTitle = doc.InsertParagraph()
                    .Append("Основная статистика")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                mainTableTitle.SpacingBefore(10).SpacingAfter(5);

                // Setup table header
                SetupTableHeader(mainTable, "Показатель", "Значение");

                // Fill table with data
                int rowIndex = 1;
                AddRowToTable(mainTable, rowIndex++, "Общее количество гостей", data.TotalGuests.ToString());
                AddRowToTable(mainTable, rowIndex++, "Средняя продолжительность пребывания", $"{data.AverageStayDuration:F2} дней");

                // Add table to document
                doc.InsertParagraph().InsertTableAfterSelf(mainTable);

                // Add guest breakdown
                var breakdownTitle = doc.InsertParagraph()
                    .Append("Распределение гостей")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                breakdownTitle.SpacingBefore(15).SpacingAfter(5);

                var breakdownTable = doc.AddTable(3, 2);
                breakdownTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                breakdownTable.Alignment = Xceed.Document.NET.Alignment.center;
                breakdownTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Setup table header
                SetupTableHeader(breakdownTable, "Категория", "Количество");

                // Fill table with data
                rowIndex = 1;
                AddRowToTable(breakdownTable, rowIndex++, "Новые гости", data.NewGuests.ToString());
                AddRowToTable(breakdownTable, rowIndex++, "Постоянные гости", data.ReturningGuests.ToString());

                // Add table to document
                doc.InsertParagraph().InsertTableAfterSelf(breakdownTable);

                // Add country breakdown
                if (data.GuestsByCountry.Any())
                {
                    var countryTitle = doc.InsertParagraph()
                        .Append("Гости по странам")
                        .Font("Times New Roman")
                        .FontSize(12)
                        .Bold();
                    countryTitle.SpacingBefore(15).SpacingAfter(5);

                    var countryTable = doc.AddTable(data.GuestsByCountry.Count + 1, 2);
                    countryTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                    countryTable.Alignment = Xceed.Document.NET.Alignment.center;
                    countryTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                    // Setup table header
                    SetupTableHeader(countryTable, "Страна", "Количество гостей");

                    // Fill table with data
                    rowIndex = 1;
                    foreach (var country in data.GuestsByCountry)
                    {
                        AddRowToTable(countryTable, rowIndex++, country.Key, country.Value.ToString());
                    }

                    // Add table to document
                    doc.InsertParagraph().InsertTableAfterSelf(countryTable);
                }

                // Add footer with report generation date
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Add signature
                AddSignature(doc);

                // Save document
                doc.Save();
            }
        }

        private void GenerateBookingSourcesReport(HotelServiceEntities context, DateTime startDate, DateTime endDate, string filePath)
        {
            // Get booking source data
            BookingSourceData data = GetBookingSourceData(context, startDate, endDate);

            using (var doc = DocX.Create(filePath))
            {
                // Setup document properties
                SetupDocument(doc);

                // Add report title
                var title = doc.InsertParagraph()
                    .Append("ОТЧЕТ ПО ИСТОЧНИКАМ БРОНИРОВАНИЯ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                var subtitle = doc.InsertParagraph()
                    .Append($"за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Create bookings by source table
                if (data.BookingsBySource.Any())
                {
                    var bookingsTitle = doc.InsertParagraph()
                        .Append("Количество бронирований по источникам")
                        .Font("Times New Roman")
                        .FontSize(12)
                        .Bold();
                    bookingsTitle.SpacingBefore(10).SpacingAfter(5);

                    var bookingsTable = doc.AddTable(data.BookingsBySource.Count + 1, 3);
                    bookingsTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                    bookingsTable.Alignment = Xceed.Document.NET.Alignment.center;
                    bookingsTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                    // Setup table header
                    bookingsTable.Rows[0].Cells[0].Paragraphs[0].Append("Источник")
                        .Font("Times New Roman").FontSize(11).Bold();
                    bookingsTable.Rows[0].Cells[1].Paragraphs[0].Append("Количество")
                        .Font("Times New Roman").FontSize(11).Bold();
                    bookingsTable.Rows[0].Cells[2].Paragraphs[0].Append("Процент")
                        .Font("Times New Roman").FontSize(11).Bold();

                    // Calculate total bookings
                    int totalBookings = data.BookingsBySource.Values.Sum();

                    // Fill table with data
                    int rowIndex = 1;
                    foreach (var source in data.BookingsBySource)
                    {
                        double percentage = totalBookings > 0 ? (double)source.Value / totalBookings * 100 : 0;

                        bookingsTable.Rows[rowIndex].Cells[0].Paragraphs[0].Append(source.Key)
                            .Font("Times New Roman").FontSize(11);
                        bookingsTable.Rows[rowIndex].Cells[1].Paragraphs[0].Append(source.Value.ToString())
                            .Font("Times New Roman").FontSize(11);
                        bookingsTable.Rows[rowIndex].Cells[2].Paragraphs[0].Append($"{percentage:F2}%")
                            .Font("Times New Roman").FontSize(11);

                        rowIndex++;
                    }

                    // Add table to document
                    doc.InsertParagraph().InsertTableAfterSelf(bookingsTable);
                }
                else
                {
                    var noDataMsg = doc.InsertParagraph()
                        .Append("В указанный период бронирования отсутствуют.")
                        .Font("Times New Roman")
                        .FontSize(11)
                        .Italic();
                    noDataMsg.SpacingBefore(10);
                }

                // Create revenue by source table
                if (data.RevenueBySource.Any())
                {
                    var revenueTitle = doc.InsertParagraph()
                        .Append("Доход по источникам бронирования")
                        .Font("Times New Roman")
                        .FontSize(12)
                        .Bold();
                    revenueTitle.SpacingBefore(15).SpacingAfter(5);

                    var revenueTable = doc.AddTable(data.RevenueBySource.Count + 1, 3);
                    revenueTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                    revenueTable.Alignment = Xceed.Document.NET.Alignment.center;
                    revenueTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                    // Setup table header
                    revenueTable.Rows[0].Cells[0].Paragraphs[0].Append("Источник")
                        .Font("Times New Roman").FontSize(11).Bold();
                    revenueTable.Rows[0].Cells[1].Paragraphs[0].Append("Доход")
                        .Font("Times New Roman").FontSize(11).Bold();
                    revenueTable.Rows[0].Cells[2].Paragraphs[0].Append("Процент")
                        .Font("Times New Roman").FontSize(11).Bold();

                    // Calculate total revenue
                    decimal totalRevenue = data.RevenueBySource.Values.Sum();

                    // Fill table with data
                    int rowIndex = 1;
                    foreach (var source in data.RevenueBySource)
                    {
                        double percentage = totalRevenue > 0 ? (double)(source.Value / totalRevenue * 100) : 0;

                        revenueTable.Rows[rowIndex].Cells[0].Paragraphs[0].Append(source.Key)
                            .Font("Times New Roman").FontSize(11);
                        revenueTable.Rows[rowIndex].Cells[1].Paragraphs[0].Append($"{source.Value:F2} ₽")
                            .Font("Times New Roman").FontSize(11);
                        revenueTable.Rows[rowIndex].Cells[2].Paragraphs[0].Append($"{percentage:F2}%")
                            .Font("Times New Roman").FontSize(11);

                        rowIndex++;
                    }

                    // Add table to document
                    doc.InsertParagraph().InsertTableAfterSelf(revenueTable);
                }

                // Add footer with report generation date
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Add signature
                AddSignature(doc);

                // Save document
                doc.Save();
            }
        }

        private void GenerateGuestGroupsReport(HotelServiceEntities context, DateTime startDate, DateTime endDate, string filePath)
        {
            // Get guest group data
            GuestGroupData data = GetGuestGroupData(context, startDate, endDate);

            using (var doc = DocX.Create(filePath))
            {
                // Setup document properties
                SetupDocument(doc);

                // Add report title
                var title = doc.InsertParagraph()
                    .Append("ОТЧЕТ ПО АНАЛИЗУ ГОСТЕЙ ПО ГРУППАМ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                var subtitle = doc.InsertParagraph()
                    .Append($"за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Create guests by group table
                if (data.GuestsByGroup.Any())
                {
                    var guestsTitle = doc.InsertParagraph()
                        .Append("Количество гостей по группам")
                        .Font("Times New Roman")
                        .FontSize(12)
                        .Bold();
                    guestsTitle.SpacingBefore(10).SpacingAfter(5);

                    var guestsTable = doc.AddTable(data.GuestsByGroup.Count + 1, 3);
                    guestsTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                    guestsTable.Alignment = Xceed.Document.NET.Alignment.center;
                    guestsTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                    // Setup table header
                    guestsTable.Rows[0].Cells[0].Paragraphs[0].Append("Группа")
                        .Font("Times New Roman").FontSize(11).Bold();
                    guestsTable.Rows[0].Cells[1].Paragraphs[0].Append("Количество")
                        .Font("Times New Roman").FontSize(11).Bold();
                    guestsTable.Rows[0].Cells[2].Paragraphs[0].Append("Процент")
                        .Font("Times New Roman").FontSize(11).Bold();

                    // Calculate total guests
                    int totalGuests = data.GuestsByGroup.Values.Sum();

                    // Fill table with data
                    int rowIndex = 1;
                    foreach (var group in data.GuestsByGroup)
                    {
                        double percentage = totalGuests > 0 ? (double)group.Value / totalGuests * 100 : 0;

                        guestsTable.Rows[rowIndex].Cells[0].Paragraphs[0].Append(group.Key)
                            .Font("Times New Roman").FontSize(11);
                        guestsTable.Rows[rowIndex].Cells[1].Paragraphs[0].Append(group.Value.ToString())
                            .Font("Times New Roman").FontSize(11);
                        guestsTable.Rows[rowIndex].Cells[2].Paragraphs[0].Append($"{percentage:F2}%")
                            .Font("Times New Roman").FontSize(11);

                        rowIndex++;
                    }

                    // Add table to document
                    doc.InsertParagraph().InsertTableAfterSelf(guestsTable);
                }
                else
                {
                    var noDataMsg = doc.InsertParagraph()
                        .Append("В указанный период гости отсутствуют.")
                        .Font("Times New Roman")
                        .FontSize(11)
                        .Italic();
                    noDataMsg.SpacingBefore(10);
                }

                // Create average stay by group table
                if (data.AverageStayByGroup.Any())
                {
                    var stayTitle = doc.InsertParagraph()
                        .Append("Средняя продолжительность пребывания по группам")
                        .Font("Times New Roman")
                        .FontSize(12)
                        .Bold();
                    stayTitle.SpacingBefore(15).SpacingAfter(5);

                    var stayTable = doc.AddTable(data.AverageStayByGroup.Count + 1, 2);
                    stayTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                    stayTable.Alignment = Xceed.Document.NET.Alignment.center;
                    stayTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                    // Setup table header
                    SetupTableHeader(stayTable, "Группа", "Средняя продолжительность (дней)");

                    // Fill table with data
                    int rowIndex = 1;
                    foreach (var group in data.AverageStayByGroup)
                    {
                        AddRowToTable(stayTable, rowIndex++, group.Key, $"{group.Value:F2}");
                    }

                    // Add table to document
                    doc.InsertParagraph().InsertTableAfterSelf(stayTable);
                }

                // Add footer with report generation date
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Add signature
                AddSignature(doc);

                // Save document
                doc.Save();
            }
        }

        #endregion

        #region Generate Room Cleaning Reports

        private void GenerateDailyCleaningReport(HotelServiceEntities context, DateTime reportDate, string filePath)
        {
            // Get room cleaning status data
            RoomCleaningStatusData data = GetRoomCleaningStatusData(context, reportDate);

            using (var doc = DocX.Create(filePath))
            {
                // Setup document properties
                SetupDocument(doc);

                // Add report title
                var title = doc.InsertParagraph()
                    .Append("ОТЧЕТ ПО СТАТУСУ УБОРКИ НОМЕРОВ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                var subtitle = doc.InsertParagraph()
                    .Append($"на {reportDate:dd.MM.yyyy}")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Create main statistics table
                var mainTable = doc.AddTable(5, 2);
                mainTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                mainTable.Alignment = Xceed.Document.NET.Alignment.center;
                mainTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Add table title
                var mainTableTitle = doc.InsertParagraph()
                    .Append("Общая статистика")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                mainTableTitle.SpacingBefore(10).SpacingAfter(5);

                // Setup table header
                SetupTableHeader(mainTable, "Показатель", "Значение");

                // Fill table with data
                int rowIndex = 1;
                AddRowToTable(mainTable, rowIndex++, "Общее количество номеров", data.TotalRooms.ToString());
                AddRowToTable(mainTable, rowIndex++, "Убранные номера", data.CleanedRooms.ToString());
                AddRowToTable(mainTable, rowIndex++, "Номера, требующие уборки", data.NeedCleaningRooms.ToString());
                AddRowToTable(mainTable, rowIndex++, "Проверенные номера", data.InspectedRooms.ToString());

                // Add table to document
                doc.InsertParagraph().InsertTableAfterSelf(mainTable);

                // Add detailed room lists by status
                foreach (var status in data.RoomsByStatus)
                {
                    if (status.Value.Any())
                    {
                        var statusTitle = doc.InsertParagraph()
                            .Append($"Список номеров со статусом \"{status.Key}\"")
                            .Font("Times New Roman")
                            .FontSize(12)
                            .Bold();
                        statusTitle.SpacingBefore(15).SpacingAfter(5);

                        // Combine room numbers into comma-separated string
                        var roomsList = string.Join(", ", status.Value);
                        var roomsContent = doc.InsertParagraph()
                            .Append(roomsList)
                            .Font("Times New Roman")
                            .FontSize(11);
                        roomsContent.SpacingAfter(10);
                    }
                }

                // Add footer with report generation date
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Add signature
                AddSignature(doc);

                // Save document
                doc.Save();
            }
        }

        private void GenerateCleaningAssignmentsReport(HotelServiceEntities context, DateTime startDate, DateTime endDate, string filePath)
        {
            // Get cleaning assignment data
            List<CleaningAssignmentData> assignments = GetCleaningAssignmentData(context, startDate, endDate);

            using (var doc = DocX.Create(filePath))
            {
                // Setup document properties
                SetupDocument(doc);

                // Add report title
                var title = doc.InsertParagraph()
                    .Append("ОТЧЕТ ПО НАЗНАЧЕННЫМ ЗАДАЧАМ УБОРКИ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                var subtitle = doc.InsertParagraph()
                    .Append($"за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                if (assignments.Any())
                {
                    // Create assignments table
                    var assignmentsTitle = doc.InsertParagraph()
                        .Append("Назначенные задачи по уборке")
                        .Font("Times New Roman")
                        .FontSize(12)
                        .Bold();
                    assignmentsTitle.SpacingBefore(10).SpacingAfter(5);

                    var assignmentsTable = doc.AddTable(assignments.Count + 1, 4);
                    assignmentsTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                    assignmentsTable.Alignment = Xceed.Document.NET.Alignment.center;
                    assignmentsTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                    // Setup table header
                    assignmentsTable.Rows[0].Cells[0].Paragraphs[0].Append("Сотрудник")
                        .Font("Times New Roman").FontSize(11).Bold();
                    assignmentsTable.Rows[0].Cells[1].Paragraphs[0].Append("Количество номеров")
                        .Font("Times New Roman").FontSize(11).Bold();
                    assignmentsTable.Rows[0].Cells[2].Paragraphs[0].Append("Время начала")
                        .Font("Times New Roman").FontSize(11).Bold();
                    assignmentsTable.Rows[0].Cells[3].Paragraphs[0].Append("Время выполнения")
                        .Font("Times New Roman").FontSize(11).Bold();

                    // Fill table with data
                    for (int i = 0; i < assignments.Count; i++)
                    {
                        var assignment = assignments[i];
                        int rowIndex = i + 1;

                        assignmentsTable.Rows[rowIndex].Cells[0].Paragraphs[0].Append(assignment.StaffName)
                            .Font("Times New Roman").FontSize(11);
                        assignmentsTable.Rows[rowIndex].Cells[1].Paragraphs[0].Append(assignment.AssignedRooms.Count.ToString())
                            .Font("Times New Roman").FontSize(11);
                        assignmentsTable.Rows[rowIndex].Cells[2].Paragraphs[0].Append(assignment.AssignmentTime.ToString("HH:mm"))
                            .Font("Times New Roman").FontSize(11);

                        string completionTime = assignment.CompletionTime.HasValue
                            ? assignment.CompletionTime.Value.ToString("HH:mm")
                            : "Не завершено";
                        assignmentsTable.Rows[rowIndex].Cells[3].Paragraphs[0].Append(completionTime)
                            .Font("Times New Roman").FontSize(11);
                    }

                    // Add table to document
                    doc.InsertParagraph().InsertTableAfterSelf(assignmentsTable);

                    // Add detailed staff assignments
                    foreach (var assignment in assignments)
                    {
                        var staffTitle = doc.InsertParagraph()
                            .Append($"Назначенные номера для {assignment.StaffName}")
                            .Font("Times New Roman")
                            .FontSize(12)
                            .Bold();
                        staffTitle.SpacingBefore(15).SpacingAfter(5);

                        // Combine room numbers into comma-separated string
                        var roomsList = string.Join(", ", assignment.AssignedRooms);
                        var roomsContent = doc.InsertParagraph()
                            .Append(roomsList)
                            .Font("Times New Roman")
                            .FontSize(11);
                        roomsContent.SpacingAfter(10);

                        // Add assignment details
                        var detailsTable = doc.AddTable(3, 2);
                        detailsTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                        detailsTable.Alignment = Xceed.Document.NET.Alignment.left;
                        detailsTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                        int detailRow = 0;
                        AddRowToTable(detailsTable, detailRow++, "Время начала", assignment.AssignmentTime.ToString("HH:mm"));

                        string completionTime = assignment.CompletionTime.HasValue
                            ? assignment.CompletionTime.Value.ToString("HH:mm")
                            : "Не завершено";
                        AddRowToTable(detailsTable, detailRow++, "Время завершения", completionTime);

                        string duration = assignment.Duration.HasValue
                            ? $"{assignment.Duration.Value.TotalMinutes:F0} минут"
                            : "—";
                        AddRowToTable(detailsTable, detailRow++, "Длительность", duration);

                        // Add details table to document
                        doc.InsertParagraph().InsertTableAfterSelf(detailsTable);
                    }
                }
                else
                {
                    var noDataMsg = doc.InsertParagraph()
                        .Append("В указанный период назначенные задачи отсутствуют.")
                        .Font("Times New Roman")
                        .FontSize(11)
                        .Italic();
                    noDataMsg.SpacingBefore(10);
                }

                // Add footer with report generation date
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Add signature
                AddSignature(doc);

                // Save document
                doc.Save();
            }
        }

        private void GenerateCleaningEfficiencyReport(HotelServiceEntities context, DateTime startDate, DateTime endDate, string filePath)
        {
            // Get cleaning efficiency data
            CleaningEfficiencyData data = GetCleaningEfficiencyData(context, startDate, endDate);

            using (var doc = DocX.Create(filePath))
            {
                // Setup document properties
                SetupDocument(doc);

                // Add report title
                var title = doc.InsertParagraph()
                    .Append("ОТЧЕТ ПО ЭФФЕКТИВНОСТИ УБОРКИ")
                    .Font("Times New Roman")
                    .FontSize(14)
                    .Bold();
                title.Alignment = Xceed.Document.NET.Alignment.center;

                var subtitle = doc.InsertParagraph()
                    .Append($"за период с {startDate:dd.MM.yyyy} по {endDate:dd.MM.yyyy}")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                subtitle.Alignment = Xceed.Document.NET.Alignment.center;
                subtitle.SpacingAfter(20);

                // Create main summary
                var summaryTitle = doc.InsertParagraph()
                    .Append("Общая эффективность")
                    .Font("Times New Roman")
                    .FontSize(12)
                    .Bold();
                summaryTitle.SpacingBefore(10).SpacingAfter(5);

                var summaryTable = doc.AddTable(1, 2);
                summaryTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                summaryTable.Alignment = Xceed.Document.NET.Alignment.center;
                summaryTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                // Setup table header
                SetupTableHeader(summaryTable, "Показатель", "Значение");

                // Add efficiency score
                AddRowToTable(summaryTable, 0, "Общий показатель эффективности", $"{data.OverallEfficiencyScore:F2}%");

                // Add table to document
                doc.InsertParagraph().InsertTableAfterSelf(summaryTable);

                // Create average cleaning time table
                if (data.AverageCleaningTimeByRoomType.Any())
                {
                    var timeTitle = doc.InsertParagraph()
                        .Append("Среднее время уборки по типам номеров")
                        .Font("Times New Roman")
                        .FontSize(12)
                        .Bold();
                    timeTitle.SpacingBefore(15).SpacingAfter(5);

                    var timeTable = doc.AddTable(data.AverageCleaningTimeByRoomType.Count + 1, 2);
                    timeTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                    timeTable.Alignment = Xceed.Document.NET.Alignment.center;
                    timeTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                    // Setup table header
                    SetupTableHeader(timeTable, "Тип номера", "Среднее время (минут)");

                    // Fill table with data
                    int rowIndex = 1;
                    foreach (var roomType in data.AverageCleaningTimeByRoomType)
                    {
                        AddRowToTable(timeTable, rowIndex++, roomType.Key, $"{roomType.Value:F0}");
                    }

                    // Add table to document
                    doc.InsertParagraph().InsertTableAfterSelf(timeTable);
                }

                // Create tasks completed table
                if (data.TasksCompletedByStaff.Any())
                {
                    var tasksTitle = doc.InsertParagraph()
                        .Append("Количество выполненных задач по сотрудникам")
                        .Font("Times New Roman")
                        .FontSize(12)
                        .Bold();
                    tasksTitle.SpacingBefore(15).SpacingAfter(5);

                    var tasksTable = doc.AddTable(data.TasksCompletedByStaff.Count + 1, 2);
                    tasksTable.Design = Xceed.Document.NET.TableDesign.TableGrid;
                    tasksTable.Alignment = Xceed.Document.NET.Alignment.center;
                    tasksTable.AutoFit = Xceed.Document.NET.AutoFit.Contents;

                    // Setup table header
                    SetupTableHeader(tasksTable, "Сотрудник", "Количество задач");

                    // Fill table with data
                    int rowIndex = 1;
                    foreach (var staff in data.TasksCompletedByStaff)
                    {
                        AddRowToTable(tasksTable, rowIndex++, staff.Key, staff.Value.ToString());
                    }

                    // Add table to document
                    doc.InsertParagraph().InsertTableAfterSelf(tasksTable);
                }

                // Add footer with report generation date
                var footer = doc.InsertParagraph()
                    .Append($"Отчет сформирован: {DateTime.Now:dd.MM.yyyy HH:mm}")
                    .Font("Times New Roman")
                    .FontSize(10);
                footer.Alignment = Xceed.Document.NET.Alignment.right;
                footer.SpacingBefore(20);

                // Add signature
                AddSignature(doc);

                // Save document
                doc.Save();
            }
        }

        #endregion

        #endregion
        #region Вспомогательные методы

        private void SetupDocument(DocX doc)
        {
            // Устанавливаем поля страницы (в пунктах)
            doc.MarginTop = 72;    // 1 дюйм = 72 пункта
            doc.MarginRight = 72;
            doc.MarginBottom = 72;
            doc.MarginLeft = 72;
        }

        private void SetupTableHeader(Table table, string headerText1, string headerText2)
        {
            table.Rows[0].Cells[0].Paragraphs[0].Append(headerText1)
                .Font("Times New Roman").FontSize(11).Bold();
            table.Rows[0].Cells[1].Paragraphs[0].Append(headerText2)
                .Font("Times New Roman").FontSize(11).Bold();
        }

        private void AddRowToTable(Table table, int rowIndex, string label, string value)
        {
            table.Rows[rowIndex].Cells[0].Paragraphs[0].Append(label)
                .Font("Times New Roman").FontSize(11);
            table.Rows[rowIndex].Cells[1].Paragraphs[0].Append(value)
                .Font("Times New Roman").FontSize(11);
        }

        private void AddSignature(DocX doc)
        {
            // Добавляем подпись внизу документа
            doc.InsertParagraph().SpacingBefore(30);

            var signatureLine = doc.InsertParagraph()
                .Append("_________/__________________/")
                .Font("Times New Roman")
                .FontSize(11);
            signatureLine.Alignment = Xceed.Document.NET.Alignment.right;

            var signatureLabel = doc.InsertParagraph()
                .Append("                                                                                                (подпись, расшифровка)")
                .Font("Times New Roman")
                .FontSize(10);
            signatureLabel.Alignment = Xceed.Document.NET.Alignment.right;
        }

        private void AskToOpenFile(string filePath)
        {
            MessageBoxResult result = MessageBox.Show("Отчет успешно экспортирован. Открыть файл?",
                "Экспорт завершен", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                System.Diagnostics.Process.Start(filePath);
            }
        }

        private string GetDayOfWeekName(DateTime date)
        {
            string[] dayNames = new string[]
            {
                "Воскресенье", "Понедельник", "Вторник", "Среда",
                "Четверг", "Пятница", "Суббота"
            };

            return dayNames[(int)date.DayOfWeek];
        }

        private string GetDayOfWeekNameShort(DateTime date)
        {
            string[] dayNames = new string[]
            {
                "Вск", "Пн", "Вт", "Ср", "Чт", "Пт", "Сб"
            };

            return dayNames[(int)date.DayOfWeek];
        }

        private int GetWeekOfMonth(DateTime date)
        {
            DateTime firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
            DateTime firstSunday = firstDayOfMonth.AddDays((7 - (int)firstDayOfMonth.DayOfWeek) % 7);

            if (date < firstSunday)
                return 0;

            return (date - firstSunday).Days / 7 + 1;
        }

        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            double mean = values.Average();
            double sumOfSquaresOfDifferences = values.Select(val => (val - mean) * (val - mean)).Sum();
            double sd = Math.Sqrt(sumOfSquaresOfDifferences / values.Count());

            return sd;
        }

        #endregion
    }
}