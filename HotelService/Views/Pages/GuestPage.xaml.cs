using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;
using System.ComponentModel;
using System.Windows.Data;

namespace HotelService.Views.Pages
{
    public partial class GuestPage : Page
    {
        private HotelServiceEntities _context;
        private List<Guest> _allGuests;
        private ICollectionView _guestsView;

        private string _searchText = "";
        private bool? _isVipFilter = null;
        private int? _selectedGroupId = null;

        public GuestPage()
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

                // Load guest groups for filter
                var groups = _context.GuestGroup.OrderBy(g => g.GroupName).ToList();
                GroupFilterComboBox.Items.Clear();
                GroupFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все группы", IsSelected = true });
                foreach (var group in groups)
                {
                    GroupFilterComboBox.Items.Add(group);
                }
                GroupFilterComboBox.DisplayMemberPath = "GroupName";
                GroupFilterComboBox.SelectedValuePath = "GroupId";
                GroupFilterComboBox.SelectedIndex = 0;

                // Load guests
                var guestsQuery = _context.Guest
                    .Include(g => g.GuestGroup)
                    .AsNoTracking();

                _allGuests = guestsQuery.ToList();

                _guestsView = CollectionViewSource.GetDefaultView(_allGuests);
                _guestsView.Filter = ApplyFilters;

                GuestsDataGrid.ItemsSource = _guestsView;

                UpdateStatusBar();
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

            // VIP Status filter
            bool matchesVipStatus = !_isVipFilter.HasValue || guest.IsVIP == _isVipFilter.Value;

            // Group filter
            bool matchesGroup = !_selectedGroupId.HasValue || guest.GroupId == _selectedGroupId.Value;

            // Search text filter
            bool matchesSearch = string.IsNullOrEmpty(_searchText) ||
                                 guest.LastName.ToLower().Contains(_searchText.ToLower()) ||
                                 guest.FirstName.ToLower().Contains(_searchText.ToLower()) ||
                                 (guest.MiddleName != null && guest.MiddleName.ToLower().Contains(_searchText.ToLower())) ||
                                 (guest.Phone != null && guest.Phone.Contains(_searchText)) ||
                                 (guest.Email != null && guest.Email.ToLower().Contains(_searchText.ToLower()));

            return matchesVipStatus && matchesGroup && matchesSearch;
        }

        private void UpdateStatusBar()
        {
           
        }

        private void StatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                string selectedStatus = (comboBox.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (selectedStatus == "VIP гости")
                    _isVipFilter = true;
                else if (selectedStatus == "Обычные гости")
                    _isVipFilter = false;
                else
                    _isVipFilter = null;

                if (_guestsView != null)
                {
                    _guestsView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void GroupFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                if (comboBox.SelectedIndex == 0)
                {
                    _selectedGroupId = null;
                }
                else if (comboBox.SelectedItem is GuestGroup group)
                {
                    _selectedGroupId = group.GroupId;
                }

                if (_guestsView != null)
                {
                    _guestsView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void SearchGuestTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchGuestTextBox.Text?.Trim();

            if (searchText != null && (searchText.Length >= 3 || searchText.Length == 0))
            {
                _searchText = searchText;

                if (_guestsView != null)
                {
                    _guestsView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void AddGuestButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для добавления гостей.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Windows.GuestEditWindow();
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditGuestButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для редактирования гостей.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var guest = (sender as Button)?.DataContext as Guest;
            if (guest == null) return;

            var dialog = new Windows.GuestEditWindow(guest.GuestId);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void ViewGuestButton_Click(object sender, RoutedEventArgs e)
        {
            var guest = (sender as Button)?.DataContext as Guest;
            if (guest == null) return;

            var dialog = new Windows.GuestViewWindow(guest.GuestId);
            dialog.ShowDialog();
        }

        private void DeleteGuestButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для удаления гостей.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var guest = (sender as Button)?.DataContext as Guest;
            if (guest == null) return;

            // Check if the guest has any bookings
            using (var context = new HotelServiceEntities())
            {
                var hasBookings = context.Booking.Any(b => b.GuestId == guest.GuestId);

                if (hasBookings)
                {
                    MessageBox.Show("Невозможно удалить гостя, так как у него есть бронирования.",
                        "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            MessageBoxResult result = MessageBox.Show(
                $"Вы действительно хотите удалить гостя {guest.LastName} {guest.FirstName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        var guestToDelete = context.Guest.Find(guest.GuestId);
                        if (guestToDelete != null)
                        {
                            // First delete any related documents
                            var relatedDocuments = context.GuestDocument.Where(d => d.GuestId == guest.GuestId);
                            context.GuestDocument.RemoveRange(relatedDocuments);

                            // Then delete the guest
                            context.Guest.Remove(guestToDelete);
                            context.SaveChanges();

                            MessageBox.Show("Гость успешно удален.", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении гостя: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void GuestsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (GuestsDataGrid.SelectedItem != null)
            {
                var guest = GuestsDataGrid.SelectedItem as Guest;
                if (guest != null)
                {
                    var dialog = new Windows.GuestViewWindow(guest.GuestId);
                    dialog.ShowDialog();
                }
            }
        }

        private void GuestsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Additional logic if needed when a row is selected
        }
    }
}