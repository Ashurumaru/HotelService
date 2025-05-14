using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Text.RegularExpressions;
using HotelService.Data;
using System.ComponentModel;

namespace HotelService.Views.Windows
{
    public partial class AmenitySelectWindow : Window
    {
        public Amenity SelectedAmenity { get; private set; }
        public int Quantity { get; private set; }
        public string Notes { get; private set; }

        private List<Amenity> _allAmenities;
        private ICollectionView _amenitiesView;
        private string _searchText = "";
        private int? _selectedCategoryId;

        public AmenitySelectWindow()
        {
            InitializeComponent();
            LoadData();

            Loaded += (s, e) => SearchTextBox.Focus();
        }

        private void LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    // Load categories for filter
                    var categories = context.AmenityCategory.OrderBy(c => c.CategoryName).ToList();

                    var allCategoriesItem = new ComboBoxItem { Content = "Все категории" };
                    CategoryFilterComboBox.Items.Add(allCategoriesItem);

                    foreach (var category in categories)
                    {
                        CategoryFilterComboBox.Items.Add(category);
                    }

                    CategoryFilterComboBox.DisplayMemberPath = "CategoryName";
                    CategoryFilterComboBox.SelectedValuePath = "CategoryId";
                    CategoryFilterComboBox.SelectedItem = allCategoriesItem;

                    // Load all amenities
                    _allAmenities = context.Amenity
                        .Include("AmenityCategory")
                        .OrderBy(a => a.AmenityName)
                        .ToList();

                    _amenitiesView = CollectionViewSource.GetDefaultView(_allAmenities);
                    _amenitiesView.Filter = ApplyFilters;

                    AmenitiesListBox.ItemsSource = _amenitiesView;

                    NoAmenitiesTextBlock.Visibility = _allAmenities.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                if (_allAmenities == null)
                    _allAmenities = new List<Amenity>();

                if (_amenitiesView == null)
                {
                    _amenitiesView = CollectionViewSource.GetDefaultView(_allAmenities);
                    AmenitiesListBox.ItemsSource = _amenitiesView;
                }

                NoAmenitiesTextBlock.Visibility = Visibility.Visible;
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private bool ApplyFilters(object item)
        {
            if (!(item is Amenity amenity))
                return false;

            bool matchesCategory = !_selectedCategoryId.HasValue || amenity.CategoryId == _selectedCategoryId.Value;

            bool matchesSearch = string.IsNullOrEmpty(_searchText) ||
                                 amenity.AmenityName.ToLower().Contains(_searchText.ToLower()) ||
                                 (amenity.Description != null && amenity.Description.ToLower().Contains(_searchText.ToLower()));

            return matchesCategory && matchesSearch;
        }

        private void CategoryFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CategoryFilterComboBox.SelectedItem is ComboBoxItem)
            {
                _selectedCategoryId = null;
            }
            else if (CategoryFilterComboBox.SelectedItem is AmenityCategory category)
            {
                _selectedCategoryId = category.CategoryId;
            }

            if (_amenitiesView != null)
            {
                _amenitiesView.Refresh();
                CheckNoAmenitiesVisibility();
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchTextBox.Text.Trim();

            if (_amenitiesView != null)
            {
                _amenitiesView.Refresh();
                CheckNoAmenitiesVisibility();
            }
        }

        private void CheckNoAmenitiesVisibility()
        {
            bool hasItems = false;
            foreach (var item in _amenitiesView)
            {
                hasItems = true;
                break;
            }

            NoAmenitiesTextBlock.Visibility = hasItems ? Visibility.Collapsed : Visibility.Visible;
        }

        private void AmenitiesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectButton.IsEnabled = AmenitiesListBox.SelectedItem != null;
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);

            // Also verify the resulting number would be >0
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                string newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

                if (string.IsNullOrEmpty(newText) || !int.TryParse(newText, out int result) || result <= 0)
                {
                    e.Handled = true;
                }
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedAmenity = AmenitiesListBox.SelectedItem as Amenity;

            if (SelectedAmenity == null)
            {
                MessageBox.Show("Пожалуйста, выберите удобство из списка.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Пожалуйста, укажите корректное количество.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Quantity = quantity;
            Notes = NotesTextBox.Text;

            DialogResult = true;
            Close();
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