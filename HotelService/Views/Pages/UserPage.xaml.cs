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
    public partial class UserPage : Page
    {
        private HotelServiceEntities _context;
        private List<User> _allUsers;
        private ICollectionView _usersView;

        private string _searchText = "";
        private int? _selectedRoleId = null;
        private int? _selectedPositionId = null;

        public UserPage()
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

                // Load roles for filter
                var roles = _context.Role.OrderBy(r => r.RoleName).ToList();
                RoleFilterComboBox.Items.Clear();
                RoleFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все роли", IsSelected = true });
                foreach (var role in roles)
                {
                    RoleFilterComboBox.Items.Add(role);
                }
                RoleFilterComboBox.SelectedIndex = 0;

                // Load positions for filter
                var positions = _context.Position.OrderBy(p => p.PositionName).ToList();
                PositionFilterComboBox.Items.Clear();
                PositionFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все должности", IsSelected = true });
                foreach (var position in positions)
                {
                    PositionFilterComboBox.Items.Add(position);
                }
                PositionFilterComboBox.SelectedIndex = 0;

                // Load users
                var usersQuery = _context.User
                    .Include(u => u.Role)
                    .Include(u => u.Position)
                    .AsNoTracking();

                _allUsers = usersQuery.ToList();

                _usersView = CollectionViewSource.GetDefaultView(_allUsers);
                _usersView.Filter = ApplyFilters;

                UsersDataGrid.ItemsSource = _usersView;

                UpdateStatusBar();

                // Disable user management if not an admin
                if (App.CurrentUser.RoleId != 1)
                {
                    AddUserButton.IsEnabled = false;
                    AddUserButton.ToolTip = "Только администратор системы может добавлять пользователей";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                if (_allUsers == null)
                    _allUsers = new List<User>();

                if (_usersView == null)
                {
                    _usersView = CollectionViewSource.GetDefaultView(_allUsers);
                    UsersDataGrid.ItemsSource = _usersView;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private bool ApplyFilters(object item)
        {
            if (!(item is User user))
                return false;

            // Role filter
            bool matchesRole = !_selectedRoleId.HasValue || user.RoleId == _selectedRoleId.Value;

            // Position filter
            bool matchesPosition = !_selectedPositionId.HasValue || (user.PositionId.HasValue && user.PositionId.Value == _selectedPositionId.Value);

            // Search text filter
            bool matchesSearch = string.IsNullOrEmpty(_searchText) ||
                                 user.LastName.ToLower().Contains(_searchText.ToLower()) ||
                                 user.FirstName.ToLower().Contains(_searchText.ToLower()) ||
                                 (user.MiddleName != null && user.MiddleName.ToLower().Contains(_searchText.ToLower())) ||
                                 user.Username.ToLower().Contains(_searchText.ToLower()) ||
                                 user.Email.ToLower().Contains(_searchText.ToLower());

            return matchesRole && matchesPosition && matchesSearch;
        }

        private void UpdateStatusBar()
        {
           
        }

        private void RoleFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                if (comboBox.SelectedIndex == 0)
                {
                    _selectedRoleId = null;
                }
                else if (comboBox.SelectedItem is Role role)
                {
                    _selectedRoleId = role.RoleId;
                }

                if (_usersView != null)
                {
                    _usersView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void PositionFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                if (comboBox.SelectedIndex == 0)
                {
                    _selectedPositionId = null;
                }
                else if (comboBox.SelectedItem is Position position)
                {
                    _selectedPositionId = position.PositionId;
                }

                if (_usersView != null)
                {
                    _usersView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void SearchUserTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchUserTextBox.Text?.Trim();

            if (searchText != null && (searchText.Length >= 3 || searchText.Length == 0))
            {
                _searchText = searchText;

                if (_usersView != null)
                {
                    _usersView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для добавления пользователей системы.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Windows.UserEditWindow();
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для редактирования пользователей системы.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = (sender as Button)?.DataContext as User;
            if (user == null) return;

            var dialog = new Windows.UserEditWindow(user.UserId);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void DeleteUserButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для удаления пользователей системы.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var user = (sender as Button)?.DataContext as User;
            if (user == null) return;

            // Prevent deleting yourself
            if (user.UserId == App.CurrentUser.UserId)
            {
                MessageBox.Show("Вы не можете удалить самого себя.",
                    "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if this is the last admin user
            using (var context = new HotelServiceEntities())
            {
                var adminCount = context.User.Count(u => u.RoleId == 1);

                if (adminCount <= 1 && user.RoleId == 1)
                {
                    MessageBox.Show("Невозможно удалить единственного администратора системы.",
                        "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            MessageBoxResult result = MessageBox.Show(
                $"Вы действительно хотите удалить пользователя {user.LastName} {user.FirstName}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        var userToDelete = context.User.Find(user.UserId);
                        if (userToDelete != null)
                        {
                            context.User.Remove(userToDelete);
                            context.SaveChanges();

                            MessageBox.Show("Пользователь успешно удален.", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении пользователя: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UsersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Additional logic if needed when a row is selected
        }
    }
}