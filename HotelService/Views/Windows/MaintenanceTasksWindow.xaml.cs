using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Data.Entity;
using HotelService.Data;
using System.ComponentModel;

namespace HotelService.Views.Windows
{
    public partial class MaintenanceTasksWindow : Window
    {
        private HotelServiceEntities _context;
        private List<RoomMaintenance> _allTasks;
        private ICollectionView _tasksView;

        private string _searchText = "";
        private int? _selectedStatusId = null;
        private int? _selectedTaskTypeId = null;
        private int? _selectedPriority = null;

        public class TaskViewModel : RoomMaintenance
        {
            public string PriorityText
            {
                get
                {
                    switch (Priority)
                    {
                        case 1: return "Низкий";
                        case 2: return "Средний";
                        case 3: return "Высокий";
                        case 4: return "Критический";
                        default: return "Не указан";
                    }
                }
            }
        }

        public MaintenanceTasksWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                _context = new HotelServiceEntities();

                // Load task statuses for filter
                var taskStatuses = _context.TaskStatus.OrderBy(ts => ts.StatusId).ToList();
                StatusFilterComboBox.Items.Clear();
                StatusFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все статусы", IsSelected = true });
                foreach (var status in taskStatuses)
                {
                    StatusFilterComboBox.Items.Add(status);
                }
                StatusFilterComboBox.DisplayMemberPath = "StatusName";
                StatusFilterComboBox.SelectedValuePath = "StatusId";
                StatusFilterComboBox.SelectedIndex = 0;

                // Load task types for filter
                var taskTypes = _context.TaskType.OrderBy(tt => tt.TypeName).ToList();
                TaskTypeFilterComboBox.Items.Clear();
                TaskTypeFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все типы", IsSelected = true });
                foreach (var type in taskTypes)
                {
                    TaskTypeFilterComboBox.Items.Add(type);
                }
                TaskTypeFilterComboBox.DisplayMemberPath = "TypeName";
                TaskTypeFilterComboBox.SelectedValuePath = "TaskTypeId";
                TaskTypeFilterComboBox.SelectedIndex = 0;

                // Load tasks
                var tasksQuery = _context.RoomMaintenance
                    .Include(rm => rm.Room)
                    .Include(rm => rm.TaskType)
                    .Include(rm => rm.TaskStatus)
                    .AsNoTracking()
                    .OrderByDescending(rm => rm.StartDate)
                    .ToList();

                // Convert to view models
                _allTasks = tasksQuery.Select(t => new TaskViewModel
                {
                    MaintenanceId = t.MaintenanceId,
                    RoomId = t.RoomId,
                    Room = t.Room,
                    TaskTypeId = t.TaskTypeId,
                    TaskType = t.TaskType,
                    IssueDescription = t.IssueDescription,
                    Priority = t.Priority,
                    StatusId = t.StatusId,
                    TaskStatus = t.TaskStatus,
                    StartDate = t.StartDate,
                    CompletionDate = t.CompletionDate,
                    Notes = t.Notes,
                    DamageReportId = t.DamageReportId,
                    Cost = t.Cost,
                    CreatedBy = t.CreatedBy
                }).Cast<RoomMaintenance>().ToList();

                _tasksView = CollectionViewSource.GetDefaultView(_allTasks);
                _tasksView.Filter = ApplyFilters;

                MaintenanceTasksDataGrid.ItemsSource = _tasksView;

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                if (_allTasks == null)
                    _allTasks = new List<RoomMaintenance>();

                if (_tasksView == null)
                {
                    _tasksView = CollectionViewSource.GetDefaultView(_allTasks);
                    MaintenanceTasksDataGrid.ItemsSource = _tasksView;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private bool ApplyFilters(object item)
        {
            if (!(item is RoomMaintenance task))
                return false;

            // Status filter
            bool matchesStatus = !_selectedStatusId.HasValue || task.StatusId == _selectedStatusId.Value;

            // Task type filter
            bool matchesTaskType = !_selectedTaskTypeId.HasValue || task.TaskTypeId == _selectedTaskTypeId.Value;

            // Priority filter
            bool matchesPriority = !_selectedPriority.HasValue || task.Priority == _selectedPriority.Value;

            // Search text filter
            bool matchesSearch = string.IsNullOrEmpty(_searchText) ||
                                 task.IssueDescription.ToLower().Contains(_searchText.ToLower()) ||
                                 (task.Notes != null && task.Notes.ToLower().Contains(_searchText.ToLower())) ||
                                 (task.Room != null && task.Room.RoomNumber.ToLower().Contains(_searchText.ToLower()));

            return matchesStatus && matchesTaskType && matchesPriority && matchesSearch;
        }

        private void UpdateStatusBar()
        {
           
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                if (comboBox.SelectedIndex == 0)
                {
                    _selectedStatusId = null;
                }
                else if (comboBox.SelectedItem is TaskStatus status)
                {
                    _selectedStatusId = status.StatusId;
                }

                if (_tasksView != null)
                {
                    _tasksView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void TaskTypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                if (comboBox.SelectedIndex == 0)
                {
                    _selectedTaskTypeId = null;
                }
                else if (comboBox.SelectedItem is TaskType taskType)
                {
                    _selectedTaskTypeId = taskType.TaskTypeId;
                }

                if (_tasksView != null)
                {
                    _tasksView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void PriorityFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                var selectedItem = comboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null && selectedItem.Tag != null)
                {
                    int priority = int.Parse(selectedItem.Tag.ToString());
                    _selectedPriority = priority == 0 ? null : (int?)priority;

                    if (_tasksView != null)
                    {
                        _tasksView.Refresh();
                        UpdateStatusBar();
                    }
                }
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text?.Trim();

            if (searchText != null && (searchText.Length >= 2 || searchText.Length == 0))
            {
                _searchText = searchText;

                if (_tasksView != null)
                {
                    _tasksView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void AddTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MaintenanceTaskWindow();
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void ViewTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.DataContext as RoomMaintenance;
            if (task == null) return;

            var dialog = new MaintenanceTaskWindow(task.MaintenanceId, isViewMode: true);
            dialog.ShowDialog();
        }

        private void EditTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.DataContext as RoomMaintenance;
            if (task == null) return;

            var dialog = new MaintenanceTaskWindow(task.MaintenanceId);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void DeleteTaskButton_Click(object sender, RoutedEventArgs e)
        {
            var task = (sender as Button)?.DataContext as RoomMaintenance;
            if (task == null) return;

            MessageBoxResult result = MessageBox.Show(
                $"Вы действительно хотите удалить задачу #{task.MaintenanceId}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        var taskToDelete = context.RoomMaintenance.Find(task.MaintenanceId);
                        if (taskToDelete != null)
                        {
                            context.RoomMaintenance.Remove(taskToDelete);
                            context.SaveChanges();

                            MessageBox.Show("Задача успешно удалена.", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении задачи: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void MaintenanceTasksDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (MaintenanceTasksDataGrid.SelectedItem != null)
            {
                var task = MaintenanceTasksDataGrid.SelectedItem as RoomMaintenance;
                if (task != null)
                {
                    var dialog = new MaintenanceTaskWindow(task.MaintenanceId, isViewMode: true);
                    dialog.ShowDialog();
                }
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
    }
}