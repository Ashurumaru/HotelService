using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class MaintenanceTaskWindow : Window
    {
        private readonly int? _maintenanceId;
        private readonly int? _roomId;
        private readonly bool _isViewMode;
        private RoomMaintenance _maintenance;

        public MaintenanceTaskWindow(int? maintenanceId = null, int? roomId = null, bool isViewMode = false)
        {
            InitializeComponent();
            _maintenanceId = maintenanceId;
            _roomId = roomId;
            _isViewMode = isViewMode;

            if (_maintenanceId.HasValue)
            {
                WindowTitleTextBlock.Text = _isViewMode ? "Просмотр задачи обслуживания" : "Редактирование задачи обслуживания";
            }
            else
            {
                WindowTitleTextBlock.Text = "Новая задача обслуживания";
            }

            LoadReferenceData();

            if (_maintenanceId.HasValue)
            {
                LoadMaintenanceData();
            }
            else
            {
                InitializeNewTask();
            }

            if (_isViewMode)
            {
                SetViewMode();
            }
        }

        private void LoadReferenceData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    // Загрузка номеров
                    var rooms = context.Room
                        .OrderBy(r => r.RoomNumber)
                        .ToList();

                    RoomComboBox.ItemsSource = rooms;
                    RoomComboBox.DisplayMemberPath = "RoomNumber";
                    RoomComboBox.SelectedValuePath = "RoomId";

                    // Загрузка типов задач
                    var taskTypes = context.TaskType
                        .OrderBy(tt => tt.TypeName)
                        .ToList();

                    TaskTypeComboBox.ItemsSource = taskTypes;
                    TaskTypeComboBox.DisplayMemberPath = "TypeName";
                    TaskTypeComboBox.SelectedValuePath = "TaskTypeId";

                    // Загрузка статусов задач
                    var taskStatuses = context.TaskStatus
                        .OrderBy(ts => ts.StatusId)
                        .ToList();

                    StatusComboBox.ItemsSource = taskStatuses;
                    StatusComboBox.DisplayMemberPath = "StatusName";
                    StatusComboBox.SelectedValuePath = "StatusId";
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

        private void LoadMaintenanceData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _maintenance = context.RoomMaintenance
                        .Include(rm => rm.Room)
                        .Include(rm => rm.TaskType)
                        .Include(rm => rm.TaskStatus)
                        .FirstOrDefault(rm => rm.MaintenanceId == _maintenanceId);

                    if (_maintenance == null)
                    {
                        MessageBox.Show("Задача обслуживания не найдена.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    // Заполнение полей формы
                    RoomComboBox.SelectedValue = _maintenance.RoomId;
                    TaskTypeComboBox.SelectedValue = _maintenance.TaskTypeId;
                    StatusComboBox.SelectedValue = _maintenance.StatusId;

                    // Установка приоритета
                    foreach (ComboBoxItem item in PriorityComboBox.Items)
                    {
                        if (item.Tag != null && int.Parse(item.Tag.ToString()) == _maintenance.Priority)
                        {
                            PriorityComboBox.SelectedItem = item;
                            break;
                        }
                    }

                    StartDatePicker.SelectedDate = _maintenance.StartDate;
                    CompletionDatePicker.SelectedDate = _maintenance.CompletionDate;

                    IssueDescriptionTextBox.Text = _maintenance.IssueDescription;
                    NotesTextBox.Text = _maintenance.Notes;

                    // Включение/отключение поля даты выполнения в зависимости от статуса
                    UpdateCompletionDateVisibility();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных задачи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void InitializeNewTask()
        {
            // Установка начальных значений для новой задачи
            StartDatePicker.SelectedDate = DateTime.Today;

            StatusComboBox.SelectedIndex = 0; // Обычно первый статус - "Новая" или "В ожидании"

            if (TaskTypeComboBox.Items.Count > 0)
            {
                TaskTypeComboBox.SelectedIndex = 0;
            }

            // Если номер уже задан, выбираем его
            if (_roomId.HasValue)
            {
                RoomComboBox.SelectedValue = _roomId;
                RoomComboBox.IsEnabled = false;
            }
            else
            {
                RoomComboBox.IsEnabled = true;
            }

            // По умолчанию средний приоритет
            foreach (ComboBoxItem item in PriorityComboBox.Items)
            {
                if (item.Tag != null && int.Parse(item.Tag.ToString()) == 2)
                {
                    PriorityComboBox.SelectedItem = item;
                    break;
                }
            }

            // Отключаем поле даты выполнения для новой задачи
            CompletionDatePicker.IsEnabled = false;
        }

        private void SetViewMode()
        {
            // Отключаем редактирование в режиме просмотра
            RoomComboBox.IsEnabled = false;
            TaskTypeComboBox.IsEnabled = false;
            StatusComboBox.IsEnabled = false;
            PriorityComboBox.IsEnabled = false;
            StartDatePicker.IsEnabled = false;
            CompletionDatePicker.IsEnabled = false;
            IssueDescriptionTextBox.IsReadOnly = true;
            NotesTextBox.IsReadOnly = true;

            // Скрываем кнопки редактирования и показываем кнопку закрытия
            EditButtonsPanel.Visibility = Visibility.Collapsed;
            CloseViewButton.Visibility = Visibility.Visible;
        }

        private void StatusComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateCompletionDateVisibility();
        }

        private void UpdateCompletionDateVisibility()
        {
            if (StatusComboBox.SelectedValue == null) return;

            // Определяем, является ли выбранный статус "завершенным" (предполагаем, что статус с ID 3 - "Выполнено")
            var statusId = (int)StatusComboBox.SelectedValue;
            bool isCompleted = statusId == 3;

            // Если статус "Выполнено", разрешаем указать дату выполнения
            CompletionDatePicker.IsEnabled = isCompleted && !_isViewMode;

            // Если статус изменился на "Выполнено" и дата не установлена, устанавливаем текущую дату
            if (isCompleted && (!CompletionDatePicker.SelectedDate.HasValue || CompletionDatePicker.SelectedDate == DateTime.MinValue))
            {
                CompletionDatePicker.SelectedDate = DateTime.Today;
            }
        }

        private void TaskTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Логика при изменении типа задачи, если необходимо
        }

        private bool ValidateTask()
        {
            List<string> errors = new List<string>();

            if (RoomComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать номер.");
            }

            if (TaskTypeComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать тип задачи.");
            }

            if (StatusComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать статус задачи.");
            }

            if (string.IsNullOrWhiteSpace(IssueDescriptionTextBox.Text))
            {
                errors.Add("Необходимо указать описание проблемы.");
            }

            if (!StartDatePicker.SelectedDate.HasValue)
            {
                errors.Add("Необходимо указать дату создания задачи.");
            }

            // Если статус "Выполнено", проверяем наличие даты выполнения
            if (StatusComboBox.SelectedValue != null && (int)StatusComboBox.SelectedValue == 3 && !CompletionDatePicker.SelectedDate.HasValue)
            {
                errors.Add("Для выполненной задачи необходимо указать дату выполнения.");
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

        private void SaveTask()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    RoomMaintenance taskToSave;

                    if (_maintenanceId.HasValue)
                    {
                        // Редактирование существующей задачи
                        taskToSave = context.RoomMaintenance.Find(_maintenanceId.Value);
                        if (taskToSave == null)
                        {
                            MessageBox.Show("Задача не найдена в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        // Создание новой задачи
                        taskToSave = new RoomMaintenance();
                        context.RoomMaintenance.Add(taskToSave);
                    }

                    // Сохранение данных задачи
                    taskToSave.RoomId = (int)RoomComboBox.SelectedValue;
                    taskToSave.TaskTypeId = (int)TaskTypeComboBox.SelectedValue;
                    taskToSave.StatusId = (int)StatusComboBox.SelectedValue;

                    // Получение приоритета из выбранного элемента
                    var selectedPriorityItem = PriorityComboBox.SelectedItem as ComboBoxItem;
                    if (selectedPriorityItem != null && selectedPriorityItem.Tag != null)
                    {
                        taskToSave.Priority = int.Parse(selectedPriorityItem.Tag.ToString());
                    }
                    else
                    {
                        taskToSave.Priority = 2; // Средний приоритет по умолчанию
                    }

                    taskToSave.StartDate = StartDatePicker.SelectedDate;
                    taskToSave.CompletionDate = CompletionDatePicker.SelectedDate;
                    taskToSave.IssueDescription = IssueDescriptionTextBox.Text;
                    taskToSave.Notes = NotesTextBox.Text;

                    // Если это новая задача, добавляем информацию о создателе
                    if (!_maintenanceId.HasValue && App.CurrentUser != null)
                    {
                        taskToSave.CreatedBy = App.CurrentUser.UserId;
                    }

                    context.SaveChanges();

                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении задачи: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateTask())
            {
                SaveTask();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseViewButton_Click(object sender, RoutedEventArgs e)
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
            DialogResult = false;
            Close();
        }
    }
}