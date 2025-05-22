using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;
using Microsoft.Win32;
using System.IO;
using System.Text.RegularExpressions;

namespace HotelService.Views.Windows
{
    public partial class GuestEditWindow : Window
    {
        private readonly int? _guestId;
        private Guest _guest;
        private bool _isInitializing = true;
        private string _documentsDirectory;

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
                WindowTitleTextBlock.Text = "Добавление нового гостя";
            }

            InitializeDocumentsDirectory();
            LoadReferenceData();

            if (_guestId.HasValue)
            {
                LoadGuestData();
            }
            else
            {
                InitializeNewGuest();
            }

            _isInitializing = false;
        }

        private void InitializeDocumentsDirectory()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _documentsDirectory = Path.Combine(appDataPath, "HotelService", "GuestDocuments");

            if (!Directory.Exists(_documentsDirectory))
            {
                Directory.CreateDirectory(_documentsDirectory);
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

                    if (groups.Count == 0)
                    {
                        var defaultGroup = new GuestGroup
                        {
                            GroupId = 1,
                            GroupName = "Стандартная группа",
                            Description = "Группа по умолчанию для всех гостей"
                        };
                        context.GuestGroup.Add(defaultGroup);
                        context.SaveChanges();

                        groups = context.GuestGroup.OrderBy(g => g.GroupName).ToList();
                        GroupComboBox.ItemsSource = groups;
                    }

                    GroupComboBox.SelectedIndex = 0;
                }

                GenderComboBox.SelectedIndex = 0;
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
                    _guest = context.Guest
                        .Include(g => g.GuestGroup)
                        .Include(g => g.GuestDocument)
                        .FirstOrDefault(g => g.GuestId == _guestId);

                    if (_guest == null)
                    {
                        MessageBox.Show("Гость не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    LastNameTextBox.Text = _guest.LastName;
                    FirstNameTextBox.Text = _guest.FirstName;
                    MiddleNameTextBox.Text = _guest.MiddleName;

                    if (!string.IsNullOrEmpty(_guest.Gender))
                    {
                        foreach (ComboBoxItem item in GenderComboBox.Items)
                        {
                            if (item.Tag?.ToString() == _guest.Gender)
                            {
                                GenderComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    if (_guest.DateOfBirth.HasValue)
                    {
                        DateOfBirthPicker.SelectedDate = _guest.DateOfBirth.Value;
                    }

                    BirthPlaceTextBox.Text = _guest.BirthPlace;
                    IsVIPCheckBox.IsChecked = _guest.IsVIP;

                    if (_guest.GroupId.HasValue)
                    {
                        foreach (var item in GroupComboBox.Items)
                        {
                            var group = item as GuestGroup;
                            if (group != null && group.GroupId == _guest.GroupId.Value)
                            {
                                GroupComboBox.SelectedItem = item;
                                break;
                            }
                        }
                    }

                    PhoneTextBox.Text = _guest.Phone;
                    EmailTextBox.Text = _guest.Email;
                    AddressTextBox.Text = _guest.Address;
                    NotesTextBox.Text = _guest.Notes;

                    PointsTextBlock.Text = _guest.CurrentPoints.ToString();

                    LoadDocuments(context);
                    LoadBookingHistory(context);
                    LoadLoyaltyTransactions(context);
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

        private void LoadDocuments(HotelServiceEntities context)
        {
            try
            {
                var documents = context.GuestDocument
                    .Include(d => d.DocumentType)
                    .Include(d => d.User)
                    .Where(d => d.GuestId == _guestId)
                    .OrderByDescending(d => d.UploadedAt)
                    .ToList();

                DocumentsDataGrid.ItemsSource = documents;
                NoDocumentsTextBlock.Visibility = documents.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                DocumentsDataGrid.ItemsSource = null;
                NoDocumentsTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void LoadBookingHistory(HotelServiceEntities context)
        {
            try
            {
                var bookings = context.Booking
                    .Include(b => b.Room)
                    .Include(b => b.Room.RoomType)
                    .Include(b => b.BookingStatus)
                    .Where(b => b.GuestId == _guestId)
                    .OrderByDescending(b => b.CheckInDate)
                    .ToList();

                BookingsDataGrid.ItemsSource = bookings;
                NoBookingsTextBlock.Visibility = bookings.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                BookingsDataGrid.ItemsSource = null;
                NoBookingsTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void LoadLoyaltyTransactions(HotelServiceEntities context)
        {
            try
            {
                var transactions = context.LoyaltyTransaction
                    .Include(t => t.TransactionType)
                    .Where(t => t.GuestId == _guestId)
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList();

                TransactionsDataGrid.ItemsSource = transactions;
                NoTransactionsTextBlock.Visibility = transactions.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                TransactionsDataGrid.ItemsSource = null;
                NoTransactionsTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void InitializeNewGuest()
        {
            DateOfBirthPicker.SelectedDate = null;
            IsVIPCheckBox.IsChecked = false;
            GenderComboBox.SelectedIndex = 0;

            TabControl tabControl = this.FindName("TabControl") as TabControl;
            if (tabControl != null && tabControl.Items.Count > 2)
            {
                var bookingsTab = tabControl.Items[2] as TabItem;
                var loyaltyTab = tabControl.Items[3] as TabItem;

                if (bookingsTab != null)
                    bookingsTab.Visibility = Visibility.Collapsed;

                if (loyaltyTab != null)
                    loyaltyTab.Visibility = Visibility.Collapsed;
            }

            NoBookingsTextBlock.Visibility = Visibility.Visible;
            NoTransactionsTextBlock.Visibility = Visibility.Visible;
            NoDocumentsTextBlock.Visibility = Visibility.Visible;

            PointsTextBlock.Text = "0";
        }

        private bool ValidateGuest()
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(LastNameTextBox.Text))
            {
                errors.Add("Фамилия обязательна для заполнения.");
            }

            if (string.IsNullOrWhiteSpace(FirstNameTextBox.Text))
            {
                errors.Add("Имя обязательно для заполнения.");
            }

            if (!string.IsNullOrWhiteSpace(EmailTextBox.Text))
            {
                string emailPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                if (!Regex.IsMatch(EmailTextBox.Text, emailPattern))
                {
                    errors.Add("Некорректный формат email.");
                }
            }

            if (!string.IsNullOrWhiteSpace(PhoneTextBox.Text))
            {
                string phonePattern = @"^[\d\+\-\(\) ]{7,20}$";
                if (!Regex.IsMatch(PhoneTextBox.Text, phonePattern))
                {
                    errors.Add("Некорректный формат телефона.");
                }
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
                        guestToSave.CurrentPoints = 0;
                    }

                    guestToSave.LastName = LastNameTextBox.Text.Trim();
                    guestToSave.FirstName = FirstNameTextBox.Text.Trim();
                    guestToSave.MiddleName = string.IsNullOrWhiteSpace(MiddleNameTextBox.Text) ? null : MiddleNameTextBox.Text.Trim();

                    if (GenderComboBox.SelectedItem is ComboBoxItem selectedGender)
                    {
                        guestToSave.Gender = selectedGender.Tag?.ToString();
                    }

                    guestToSave.DateOfBirth = DateOfBirthPicker.SelectedDate;
                    guestToSave.BirthPlace = string.IsNullOrWhiteSpace(BirthPlaceTextBox.Text) ? null : BirthPlaceTextBox.Text.Trim();
                    guestToSave.IsVIP = IsVIPCheckBox.IsChecked ?? false;

                    if (GroupComboBox.SelectedItem is GuestGroup selectedGroup)
                    {
                        guestToSave.GroupId = selectedGroup.GroupId;
                    }

                    guestToSave.Phone = string.IsNullOrWhiteSpace(PhoneTextBox.Text) ? null : PhoneTextBox.Text.Trim();
                    guestToSave.Email = string.IsNullOrWhiteSpace(EmailTextBox.Text) ? null : EmailTextBox.Text.Trim();
                    guestToSave.Address = string.IsNullOrWhiteSpace(AddressTextBox.Text) ? null : AddressTextBox.Text.Trim();
                    guestToSave.Notes = string.IsNullOrWhiteSpace(NotesTextBox.Text) ? null : NotesTextBox.Text.Trim();

                    context.SaveChanges();

                    if (!_guestId.HasValue)
                    {
                        _guest = guestToSave;
                    }

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

        private void AddDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_guestId.HasValue && _guest == null)
            {
                MessageBox.Show("Пожалуйста, сначала сохраните гостя, чтобы добавить документы.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using (var context = new HotelServiceEntities())
                {
                    var documentTypes = context.DocumentType.OrderBy(dt => dt.TypeName).ToList();

                    if (documentTypes.Count == 0)
                    {
                        var defaultTypes = new List<DocumentType>
                        {
                            new DocumentType { DocumentTypeId = 1, TypeName = "Паспорт РФ", Description = "Паспорт гражданина Российской Федерации" },
                            new DocumentType { DocumentTypeId = 2, TypeName = "Загранпаспорт", Description = "Заграничный паспорт" },
                            new DocumentType { DocumentTypeId = 3, TypeName = "Водительские права", Description = "Водительское удостоверение" },
                            new DocumentType { DocumentTypeId = 4, TypeName = "Свидетельство о рождении", Description = "Свидетельство о рождении для детей" }
                        };

                        context.DocumentType.AddRange(defaultTypes);
                        context.SaveChanges();

                        documentTypes = context.DocumentType.OrderBy(dt => dt.TypeName).ToList();
                    }

                    var documentAddWindow = new DocumentAddWindow(documentTypes);
                    if (documentAddWindow.ShowDialog() == true)
                    {
                        var newDocument = new GuestDocument
                        {
                            GuestId = _guestId ?? _guest.GuestId,
                            DocumentTypeId = documentAddWindow.SelectedDocumentTypeId,
                            DocumentSeries = documentAddWindow.DocumentSeries,
                            DocumentNumber = documentAddWindow.DocumentNumber,
                            IssuedBy = documentAddWindow.IssuedBy,
                            IssueDate = documentAddWindow.IssueDate,
                            ExpiryDate = documentAddWindow.ExpiryDate,
                            UploadedAt = DateTime.Now,
                            UploadedBy = App.CurrentUser?.UserId
                        };

                        if (!string.IsNullOrEmpty(documentAddWindow.SelectedFilePath))
                        {
                            string fileExt = Path.GetExtension(documentAddWindow.SelectedFilePath);
                            string newFileName = $"doc_{Guid.NewGuid()}{fileExt}";
                            string destPath = Path.Combine(_documentsDirectory, newFileName);

                            File.Copy(documentAddWindow.SelectedFilePath, destPath, true);
                            newDocument.DocumentPath = destPath;
                        }

                        context.GuestDocument.Add(newDocument);
                        context.SaveChanges();

                        LoadDocuments(context);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении документа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ViewDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            var document = (sender as Button)?.DataContext as GuestDocument;
            if (document == null || string.IsNullOrEmpty(document.DocumentPath))
            {
                MessageBox.Show("Файл документа не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                if (File.Exists(document.DocumentPath))
                {
                    System.Diagnostics.Process.Start(document.DocumentPath);
                }
                else
                {
                    MessageBox.Show("Файл документа не найден по указанному пути.", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии документа: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            var document = (sender as Button)?.DataContext as GuestDocument;
            if (document == null) return;

            MessageBoxResult result = MessageBox.Show(
                "Вы действительно хотите удалить этот документ?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        var docToDelete = context.GuestDocument.Find(document.DocumentId);
                        if (docToDelete != null)
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(docToDelete.DocumentPath) && File.Exists(docToDelete.DocumentPath))
                                {
                                    File.Delete(docToDelete.DocumentPath);
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Не удалось удалить файл документа: {ex.Message}",
                                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }

                            context.GuestDocument.Remove(docToDelete);
                            context.SaveChanges();

                            LoadDocuments(context);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении документа: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewBookingButton_Click(object sender, RoutedEventArgs e)
        {
            var booking = (sender as Button)?.DataContext as Booking;
            if (booking == null) return;

            var bookingViewWindow = new BookingViewWindow(booking.BookingId);
            bookingViewWindow.ShowDialog();
        }

        private void AddPointsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_guestId.HasValue)
            {
                MessageBox.Show("Пожалуйста, сначала сохраните гостя, чтобы добавить баллы лояльности.",
                    "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            MessageBox.Show("Функция добавления баллов лояльности будет реализована позже.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
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