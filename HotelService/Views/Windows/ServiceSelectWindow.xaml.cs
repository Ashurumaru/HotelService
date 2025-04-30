using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;
using System.ComponentModel;

namespace HotelService.Views.Windows
{
    public partial class ServiceSelectWindow : Window
    {
        public Service SelectedService { get; private set; }

        private List<Service> _allServices;
        private ICollectionView _servicesView;
        private string _searchText = "";
        private int? _selectedCategoryId = null;

        public ServiceSelectWindow()
        {
            InitializeComponent();

            LoadData();

            Loaded += (s, e) =>
            {
                SearchTextBox.Focus();
                if (ServicesDataGrid.Items.Count > 0)
                    ServicesDataGrid.SelectedIndex = 0;
            };
        }

        private void LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _allServices = context.Service
                        .Include(s => s.ServiceCategory)
                        .OrderBy(s => s.ServiceName)
                        .ToList();

                    _servicesView = CollectionViewSource.GetDefaultView(_allServices);
                    _servicesView.Filter = ApplyFilters;

                    ServicesDataGrid.ItemsSource = _servicesView;

                    // Load categories for filter
                    var categories = context.ServiceCategory.OrderBy(c => c.CategoryName).ToList();

                    CategoryFilterComboBox.Items.Clear();
                    CategoryFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все категории", Tag = null });

                    foreach (var category in categories)
                    {
                        CategoryFilterComboBox.Items.Add(new ComboBoxItem { Content = category.CategoryName, Tag = category.CategoryId });
                    }

                    CategoryFilterComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                if (_allServices == null)
                    _allServices = new List<Service>();

                if (_servicesView == null)
                {
                    _servicesView = CollectionViewSource.GetDefaultView(_allServices);
                    ServicesDataGrid.ItemsSource = _servicesView;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private bool ApplyFilters(object item)
        {
            if (!(item is Service service))
                return false;


            bool matchesSearch = string.IsNullOrWhiteSpace(_searchText) ||
                                (service.ServiceName?.ToLower().Contains(_searchText.ToLower()) ?? false) ||
                                (service.Description?.ToLower().Contains(_searchText.ToLower()) ?? false);

            return matchesSearch;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchTextBox.Text.Trim();

            if (_servicesView != null)
            {
                _servicesView.Refresh();
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && ServicesDataGrid.Items.Count > 0)
            {
                ServicesDataGrid.Focus();
                ServicesDataGrid.SelectedIndex = 0;

                DataGridRow row = (DataGridRow)ServicesDataGrid.ItemContainerGenerator.ContainerFromIndex(0);
                if (row != null)
                {
                    row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                }
            }
            else if (e.Key == Key.Enter && ServicesDataGrid.Items.Count > 0)
            {
                ServicesDataGrid.SelectedIndex = 0;
                SelectService();
            }
        }

        private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = CategoryFilterComboBox.SelectedItem as ComboBoxItem;
            if (selectedItem != null)
            {
                _selectedCategoryId = selectedItem.Tag as int?;

                if (_servicesView != null)
                {
                    _servicesView.Refresh();
                }
            }
        }

        private void ServicesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectButton.IsEnabled = ServicesDataGrid.SelectedItem != null;
        }

        private void ServicesDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ServicesDataGrid.SelectedItem != null)
            {
                SelectService();
            }
        }

        private void SelectService()
        {
            SelectedService = ServicesDataGrid.SelectedItem as Service;
            DialogResult = true;
            Close();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectService();
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