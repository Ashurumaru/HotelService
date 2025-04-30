using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class GuestSelectWindow : Window
    {
        private HotelServiceEntities _context;
        private Guest _selectedGuest;

        public Guest SelectedGuest
        {
            get { return _selectedGuest; }
        }

        public GuestSelectWindow()
        {
            InitializeComponent();
            LoadGuests();
            SearchTextBox.Focus();
        }

        private void LoadGuests(string searchText = "")
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                _context = new HotelServiceEntities();

                var guestsQuery = _context.Guest.AsQueryable();

                if (!string.IsNullOrEmpty(searchText))
                {
                    searchText = searchText.ToLower();
                    guestsQuery = guestsQuery.Where(g =>
                        g.FirstName.ToLower().Contains(searchText) ||
                        g.LastName.ToLower().Contains(searchText) ||
                        (g.MiddleName != null && g.MiddleName.ToLower().Contains(searchText)) ||
                        (g.Phone != null && g.Phone.Contains(searchText)) ||
                        (g.Email != null && g.Email.ToLower().Contains(searchText)));
                }

                var guests = guestsQuery.OrderBy(g => g.LastName)
                                    .ThenBy(g => g.FirstName)
                                    .AsNoTracking()
                                    .ToList();

                GuestsDataGrid.ItemsSource = guests;
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

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchTextBox.Text.Trim();

            if (searchText.Length >= 3 || searchText.Length == 0)
            {
                LoadGuests(searchText);
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoadGuests(SearchTextBox.Text.Trim());
            }
        }

        private void GuestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedGuest = GuestsDataGrid.SelectedItem as Guest;
            SelectButton.IsEnabled = (_selectedGuest != null);
        }

        private void GuestsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GuestsDataGrid.SelectedItem != null)
            {
                _selectedGuest = GuestsDataGrid.SelectedItem as Guest;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void AddGuestButton_Click(object sender, RoutedEventArgs e)
        {
            var addGuestWindow = new GuestEditWindow();
            if (addGuestWindow.ShowDialog() == true)
            {
                LoadGuests(SearchTextBox.Text.Trim());

                if (addGuestWindow.CreatedGuest != null)
                {
                    _selectedGuest = _context.Guest.Find(addGuestWindow.CreatedGuest.GuestId);
                    this.DialogResult = true;
                    this.Close();
                }
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedGuest != null)
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
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
            this.DialogResult = false;
            this.Close();
        }
    }
}