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
    public partial class GuestSelectWindow : Window
    {
        public Guest SelectedGuest { get; private set; }

        private List<Guest> _allGuests;
        private ICollectionView _guestsView;
        private string _searchText = "";

        public GuestSelectWindow()
        {
            InitializeComponent();

            LoadData();

            Loaded += (s, e) =>
            {
                SearchTextBox.Focus();
                if (GuestsDataGrid.Items.Count > 0)
                    GuestsDataGrid.SelectedIndex = 0;
            };
        }

        private void LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _allGuests = context.Guest
                        .OrderBy(g => g.LastName)
                        .ThenBy(g => g.FirstName)
                        .ToList();

                    _guestsView = CollectionViewSource.GetDefaultView(_allGuests);
                    _guestsView.Filter = ApplyFilters;

                    GuestsDataGrid.ItemsSource = _guestsView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                if (_allGuests == null)
                    _allGuests = new List<Guest>();

                if (_guestsView == null)
                {
                    _guestsView = CollectionViewSource.GetDefaultView(_allGuests);
                    GuestsDataGrid.ItemsSource = _guestsView;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private bool ApplyFilters(object item)
        {
            if (!(item is Guest guest))
                return false;

            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            string searchLower = _searchText.ToLower();

            bool matchesLastName = guest.LastName != null && guest.LastName.ToLower().Contains(searchLower);
            bool matchesFirstName = guest.FirstName != null && guest.FirstName.ToLower().Contains(searchLower);
            bool matchesMiddleName = guest.MiddleName != null && guest.MiddleName.ToLower().Contains(searchLower);
            bool matchesPhone = guest.Phone != null && guest.Phone.Contains(searchLower);
            bool matchesEmail = guest.Email != null && guest.Email.ToLower().Contains(searchLower);

            return matchesLastName || matchesFirstName || matchesMiddleName || matchesPhone || matchesEmail;
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchTextBox.Text.Trim();

            if (_guestsView != null)
            {
                _guestsView.Refresh();
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down && GuestsDataGrid.Items.Count > 0)
            {
                GuestsDataGrid.Focus();
                GuestsDataGrid.SelectedIndex = 0;

                DataGridRow row = (DataGridRow)GuestsDataGrid.ItemContainerGenerator.ContainerFromIndex(0);
                if (row != null)
                {
                    row.MoveFocus(new TraversalRequest(FocusNavigationDirection.Down));
                }
            }
            else if (e.Key == Key.Enter && GuestsDataGrid.Items.Count > 0)
            {
                GuestsDataGrid.SelectedIndex = 0;
                SelectGuest();
            }
        }

        private void GuestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectButton.IsEnabled = GuestsDataGrid.SelectedItem != null;
        }

        private void GuestsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GuestsDataGrid.SelectedItem != null)
            {
                SelectGuest();
            }
        }

        private void SelectGuest()
        {
            SelectedGuest = GuestsDataGrid.SelectedItem as Guest;
            DialogResult = true;
            Close();
        }

        private void AddGuestButton_Click(object sender, RoutedEventArgs e)
        {
            var guestEditWindow = new GuestEditWindow();
            if (guestEditWindow.ShowDialog() == true)
            {
                SelectedGuest = guestEditWindow.CreatedGuest;

                LoadData();

                if (SelectedGuest != null)
                {
                    foreach (var item in GuestsDataGrid.Items)
                    {
                        if (item is Guest guest && guest.GuestId == SelectedGuest.GuestId)
                        {
                            GuestsDataGrid.SelectedItem = item;
                            GuestsDataGrid.ScrollIntoView(item);
                            break;
                        }
                    }
                }

                DialogResult = true;
                Close();
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectGuest();
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