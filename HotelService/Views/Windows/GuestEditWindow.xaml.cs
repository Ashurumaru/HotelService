using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class GuestEditWindow : Window
    {
        private readonly int? _guestId;
        private Guest _guest;

        public Guest CreatedGuest { get; private set; }

        public GuestEditWindow(int? guestId = null)
        {
            InitializeComponent();
            _guestId = guestId;

            if (_guestId.HasValue)
            {
                WindowTitleTextBlock.Text = "Редактирование гостя";
            }
            else
            {
                WindowTitleTextBlock.Text = "Создание нового гостя";
            }

            LoadReferenceData();

            if (_guestId.HasValue)
            {
                LoadGuestData();
            }
            else
            {
                InitializeNewGuest();
            }
        }

        private void LoadReferenceData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    var groups = context.GuestGroup.OrderBy(g => g.GroupName).ToList();

                    GroupComboBox.ItemsSource = groups;
                    GroupComboBox.DisplayMemberPath = "GroupName";
                    GroupComboBox.SelectedValuePath = "GroupId";

                    if (groups.Count > 0)
                    {
                        var noGroupItem = new GuestGroup { GroupId = 0, GroupName = "Без группы" };
                        var groupsList = new List<GuestGroup> { noGroupItem };
                        groupsList.AddRange(groups);
                        GroupComboBox.ItemsSource = groupsList;
                        GroupComboBox.SelectedIndex = 0;
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

        private void LoadGuestData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _guest = context.Guest.FirstOrDefault(g => g.GuestId == _guestId);

                    if (_guest == null)
                    {
                        MessageBox.Show("Гость не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    LastNameTextBox.Text = _guest.LastName;
                    FirstNameTextBox.Text = _guest.FirstName;
                    MiddleNameTextBox.Text = _guest.MiddleName;

                    if (_guest.DateOfBirth.HasValue)
                    {
                        DateOfBirthPicker.SelectedDate = _guest.DateOfBirth.Value;
                    }

                    PhoneTextBox.Text = _guest.Phone;
                    EmailTextBox.Text = _guest.Email;
                    AddressTextBox.Text = _guest.Address;
                    NotesTextBox.Text = _guest.Notes;
                    IsVIPCheckBox.IsChecked = _guest.IsVIP;
                    LoyaltyPointsTextBox.Text = _guest.CurrentPoints.ToString();

                    if (_guest.GroupId.HasValue)
                    {
                        GroupComboBox.SelectedValue = _guest.GroupId.Value;
                    }
                    else
                    {
                        GroupComboBox.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных гостя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void InitializeNewGuest()
        {
            DateOfBirthPicker.SelectedDate = null;
            IsVIPCheckBox.IsChecked = false;
            LoyaltyPointsTextBox.Text = "0";
        }

        private bool ValidateGuest()
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                errors.Add("Необходимо указать фамилию.");
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                errors.Add("Необходимо указать имя.");
            }

            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                if (!Regex.IsMatch(EmailTextBox.Text, emailPattern))
                {
                    errors.Add("Указан некорректный email.");
                }
            }

            if (!string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                string phonePattern = @"^[+]?[\d\s\-\(\)]{7,20}$";
                if (!Regex.IsMatch(PhoneTextBox.Text, phonePattern))
                {
                    errors.Add("Указан некорректный номер телефона.");
                }
            }

            int loyaltyPoints;
            if (!int.TryParse(LoyaltyPointsTextBox.Text, out loyaltyPoints) || loyaltyPoints < 0)
            {
                errors.Add("Количество баллов лояльности должно быть неотрицательным числом.");
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

        private void SaveGuest()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    Guest guestToSave;

                    if (_guestId.HasValue)
                    {
                        guestToSave = context.Guest.Find(_guestId.Value);
                        if (guestToSave == null)
                        {
                            MessageBox.Show("Гость не найден в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        guestToSave = new Guest();
                        context.Guest.Add(guestToSave);
                    }

                    guestToSave.LastName = LastNameTextBox.Text.Trim();
                    guestToSave.FirstName = FirstNameTextBox.Text.Trim();
                    guestToSave.MiddleName = MiddleNameTextBox.Text.Trim();
                    guestToSave.DateOfBirth = DateOfBirthPicker.SelectedDate;
                    guestToSave.Phone = PhoneTextBox.Text.Trim();
                    guestToSave.Email = EmailTextBox.Text.Trim();
                    guestToSave.Address = AddressTextBox.Text.Trim();
                    guestToSave.Notes = NotesTextBox.Text.Trim();
                    guestToSave.IsVIP = IsVIPCheckBox.IsChecked ?? false;

                    int loyaltyPoints;
                    if (int.TryParse(LoyaltyPointsTextBox.Text, out loyaltyPoints))
                    {
                        guestToSave.CurrentPoints = loyaltyPoints;
                    }
                    else
                    {
                        guestToSave.CurrentPoints = 0;
                    }

                    if (GroupComboBox.SelectedIndex > 0)
                    {
                        guestToSave.GroupId = (int)GroupComboBox.SelectedValue;
                    }
                    else
                    {
                        guestToSave.GroupId = null;
                    }

                    context.SaveChanges();
                    CreatedGuest = guestToSave;
                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении гостя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateGuest())
            {
                SaveGuest();
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