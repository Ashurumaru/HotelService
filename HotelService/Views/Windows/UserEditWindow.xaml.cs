using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Text.RegularExpressions;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class UserEditWindow : Window
    {
        private readonly int? _userId;
        private User _user;
        private bool _isInitializing = true;
        private bool _isPasswordChanged = false;

        public UserEditWindow(int? userId = null)
        {
            InitializeComponent();
            _userId = userId;

            if (_userId.HasValue)
            {
                WindowTitleTextBlock.Text = "Редактирование пользователя";
            }
            else
            {
                WindowTitleTextBlock.Text = "Добавление нового пользователя";
            }

            LoadReferenceData();

            if (_userId.HasValue)
            {
                LoadUserData();
            }
            else
            {
                InitializeNewUser();
            }

            _isInitializing = false;
        }

        private void LoadReferenceData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    // Load roles
                    var roles = context.Role.OrderBy(r => r.RoleId).ToList();
                    RoleComboBox.ItemsSource = roles;

                    // Load positions
                    var positions = context.Position.OrderBy(p => p.PositionName).ToList();
                    PositionComboBox.ItemsSource = positions;

                    // If there are no default roles, create them
                    if (roles.Count == 0)
                    {
                        var defaultRoles = new List<Role>
                        {
                            new Role { RoleId = 1, RoleName = "Администратор системы", Description = "Полный доступ к системе" },
                            new Role { RoleId = 2, RoleName = "Администратор стойки регистрации", Description = "Доступ к функциям обслуживания гостей" },
                            new Role { RoleId = 3, RoleName = "Сотрудник хозяйственной службы", Description = "Доступ к управлению номерным фондом" }
                        };

                        context.Role.AddRange(defaultRoles);
                        context.SaveChanges();

                        // Refresh roles
                        roles = context.Role.OrderBy(r => r.RoleId).ToList();
                        RoleComboBox.ItemsSource = roles;
                    }

                    // If there are no default positions, create them
                    if (positions.Count == 0)
                    {
                        var defaultPositions = new List<Position>
                        {
                            new Position { PositionId = 1, PositionName = "Директор", Description = "Руководитель гостиницы" },
                            new Position { PositionId = 2, PositionName = "Администратор", Description = "Администратор гостиницы" },
                            new Position { PositionId = 3, PositionName = "Горничная", Description = "Сотрудник хозяйственной службы" },
                            new Position { PositionId = 4, PositionName = "Техник", Description = "Технический специалист" }
                        };

                        context.Position.AddRange(defaultPositions);
                        context.SaveChanges();

                        // Refresh positions
                        positions = context.Position.OrderBy(p => p.PositionName).ToList();
                        PositionComboBox.ItemsSource = positions;
                    }
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

        private void LoadUserData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _user = context.User.Find(_userId);

                    if (_user == null)
                    {
                        MessageBox.Show("Пользователь не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    // Fill personal data
                    UsernameTextBox.Text = _user.Username;

                    // Password boxes are left empty for editing
                    // When saving, we'll check if they've been filled

                    LastNameTextBox.Text = _user.LastName;
                    FirstNameTextBox.Text = _user.FirstName;
                    MiddleNameTextBox.Text = _user.MiddleName;
                    EmailTextBox.Text = _user.Email;
                    PhoneTextBox.Text = _user.Phone;

                    // Set role
                    foreach (var role in RoleComboBox.Items)
                    {
                        if (role is Role r && r.RoleId == _user.RoleId)
                        {
                            RoleComboBox.SelectedItem = role;
                            break;
                        }
                    }

                    // Set position
                    if (_user.PositionId.HasValue)
                    {
                        foreach (var position in PositionComboBox.Items)
                        {
                            if (position is Position p && p.PositionId == _user.PositionId.Value)
                            {
                                PositionComboBox.SelectedItem = position;
                                break;
                            }
                        }
                    }

                    // If editing the current user, restrict role changes
                    if (_user.UserId == App.CurrentUser.UserId)
                    {
                        RoleComboBox.IsEnabled = false;
                        RoleComboBox.ToolTip = "Нельзя изменить свою роль";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных пользователя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void InitializeNewUser()
        {
            // Set default role to admin if this is the first user
            using (var context = new HotelServiceEntities())
            {
                if (context.User.Count() == 0)
                {
                    // First user should be admin
                    var adminRole = RoleComboBox.Items.Cast<Role>().FirstOrDefault(r => r.RoleId == 1);
                    if (adminRole != null)
                    {
                        RoleComboBox.SelectedItem = adminRole;
                    }
                }
                else
                {
                    // Default to first role in the list
                    if (RoleComboBox.Items.Count > 0)
                    {
                        RoleComboBox.SelectedIndex = 0;
                    }
                }
            }

            // Set default position if available
            if (PositionComboBox.Items.Count > 0)
            {
                PositionComboBox.SelectedIndex = 0;
            }
        }

        private bool ValidateUser()
        {
            List<string> errors = new List<string>();

            // Validate username
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                errors.Add("Логин обязателен для заполнения");
            }
            else
            {
                // Check if username already exists (for new users)
                if (!_userId.HasValue)
                {
                    using (var context = new HotelServiceEntities())
                    {
                        if (context.User.Any(u => u.Username == UsernameTextBox.Text.Trim()))
                        {
                            errors.Add("Пользователь с таким логином уже существует");
                        }
                    }
                }
                else
                {
                    // For existing users, check if username exists with different ID
                    using (var context = new HotelServiceEntities())
                    {
                        if (context.User.Any(u => u.Username == UsernameTextBox.Text.Trim() && u.UserId != _userId.Value))
                        {
                            errors.Add("Пользователь с таким логином уже существует");
                        }
                    }
                }
            }

            // Validate password
            if (!_userId.HasValue || (!string.IsNullOrEmpty(PasswordBox.Password) || !string.IsNullOrEmpty(ConfirmPasswordBox.Password)))
            {
                if (string.IsNullOrEmpty(PasswordBox.Password))
                {
                    errors.Add("Пароль обязателен для заполнения");
                }
                else if (PasswordBox.Password.Length < 6)
                {
                    errors.Add("Пароль должен содержать не менее 6 символов");
                }
                else if (PasswordBox.Password != ConfirmPasswordBox.Password)
                {
                    errors.Add("Пароли не совпадают");
                }
                else
                {
                    _isPasswordChanged = true;
                }
            }

            // Validate name fields
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                errors.Add("Фамилия обязательна для заполнения");
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                errors.Add("Имя обязательно для заполнения");
            }

            // Validate email
            if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                errors.Add("Email обязателен для заполнения");
            }
            else
            {
                string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                if (!Regex.IsMatch(EmailTextBox.Text, emailPattern))
                {
                    errors.Add("Некорректный формат email");
                }
            }

            // Validate role
            if (RoleComboBox.SelectedItem == null)
            {
                errors.Add("Роль обязательна для выбора");
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

        private void SaveUser()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    User userToSave;

                    if (_userId.HasValue)
                    {
                        // Editing existing user
                        userToSave = context.User.Find(_userId.Value);
                        if (userToSave == null)
                        {
                            MessageBox.Show("Пользователь не найден в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        // Creating new user
                        userToSave = new User();
                        context.User.Add(userToSave);
                    }

                    // Update fields
                    userToSave.Username = UsernameTextBox.Text.Trim();

                    // Only update password if it was changed
                    if (_isPasswordChanged)
                    {
                        userToSave.Password = PasswordBox.Password;
                    }

                    userToSave.LastName = LastNameTextBox.Text.Trim();
                    userToSave.FirstName = FirstNameTextBox.Text.Trim();
                    userToSave.MiddleName = string.IsNullOrWhiteSpace(MiddleNameTextBox.Text) ? null : MiddleNameTextBox.Text.Trim();
                    userToSave.Email = EmailTextBox.Text.Trim();
                    userToSave.Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim();

                    // Set role - don't change role if editing current user
                    if (!(_userId.HasValue && _user.UserId == App.CurrentUser.UserId))
                    {
                        userToSave.RoleId = (int)RoleComboBox.SelectedValue;
                    }

                    // Set position
                    if (PositionComboBox.SelectedItem != null)
                    {
                        userToSave.PositionId = (int)PositionComboBox.SelectedValue;
                    }
                    else
                    {
                        userToSave.PositionId = null;
                    }

                    context.SaveChanges();

                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении пользователя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateUser())
            {
                SaveUser();
            }
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