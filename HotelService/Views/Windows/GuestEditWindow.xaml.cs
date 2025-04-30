using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class GuestEditWindow : Window
    {
        private HotelServiceEntities _context;
        private Guest _guest;
        private bool _isNewGuest;

        public Guest CreatedGuest { get; private set; }

        public GuestEditWindow(int? guestId = null)
        {
            InitializeComponent();

            _isNewGuest = guestId == null;

            if (_isNewGuest)
            {
                WindowTitleTextBlock.Text = "Создание нового гостя";
                _guest = new Guest();
            }
            else
            {
                WindowTitleTextBlock.Text = "Редактирование информации о госте";
                LoadGuest(guestId.Value);
            }

            LoadComboBoxData();
            InitializeFields();
        }

        private void LoadGuest(int guestId)
        {
            try
            {
                _context = new HotelServiceEntities();
                _guest = _context.Guest.Find(guestId);

                if (_guest == null)
                {
                    MessageBox.Show("Гость не найден.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных гостя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
            }
        }

        private void LoadComboBoxData()
        {
            try
            {
                _context = new HotelServiceEntities();

                var groups = _context.GuestGroup.ToList();
                GroupComboBox.ItemsSource = groups;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке справочных данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeFields()
        {
            if (_guest != null)
            {
                LastNameTextBox.Text = _guest.LastName;
                FirstNameTextBox.Text = _guest.FirstName;
                MiddleNameTextBox.Text = _guest.MiddleName;
                PhoneTextBox.Text = _guest.Phone;
                EmailTextBox.Text = _guest.Email;
                AddressTextBox.Text = _guest.Address;
                NotesTextBox.Text = _guest.Notes;
                VipCheckBox.IsChecked = _guest.IsVIP;

                if (_guest.DateOfBirth.HasValue)
                {
                    BirthDatePicker.SelectedDate = _guest.DateOfBirth.Value;
                }

                if (_guest.GroupId.HasValue)
                {
                    GroupComboBox.SelectedValue = _guest.GroupId.Value;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                SaveGuest();
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                ShowError("Необходимо указать фамилию гостя.");
                LastNameTextBox.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                ShowError("Необходимо указать имя гостя.");
                FirstNameTextBox.Focus();
                return false;
            }

            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text) && !IsValidEmail(EmailTextBox.Text))
            {
                ShowError("Указан некорректный формат email-адреса.");
                EmailTextBox.Focus();
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private void SaveGuest()
        {
            try
            {
                _guest.LastName = LastNameTextBox.Text.Trim();
                _guest.FirstName = FirstNameTextBox.Text.Trim();
                _guest.MiddleName = string.IsNullOrWhiteSpace(MiddleNameTextBox.Text) ? null : MiddleNameTextBox.Text.Trim();
                _guest.Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim();
                _guest.Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim();
                _guest.Address = string.IsNullOrWhiteSpace(AddressTextBox.Text) ? null : AddressTextBox.Text.Trim();
                _guest.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();
                _guest.IsVIP = VipCheckBox.IsChecked ?? false;
                _guest.DateOfBirth = BirthDatePicker.SelectedDate;
                _guest.GroupId = GroupComboBox.SelectedValue as int?;

                using (var context = new HotelServiceEntities())
                {
                    if (_isNewGuest)
                    {
                        _guest.CurrentPoints = 0;
                        context.Guest.Add(_guest);
                    }
                    else
                    {
                        var guestToUpdate = context.Guest.Find(_guest.GuestId);
                        if (guestToUpdate != null)
                        {
                            context.Entry(guestToUpdate).CurrentValues.SetValues(_guest);
                        }
                    }

                    context.SaveChanges();

                    if (_isNewGuest)
                    {
                        CreatedGuest = _guest;
                    }
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении данных гостя: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowError(string message)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}