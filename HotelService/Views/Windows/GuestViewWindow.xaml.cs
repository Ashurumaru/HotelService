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
using System.Windows.Media;

namespace HotelService.Views.Windows
{
    public partial class GuestViewWindow : Window
    {
        private readonly int _guestId;
        private Guest _guest;
        private string _documentsDirectory;

        public GuestViewWindow(int guestId)
        {
            InitializeComponent();
            _guestId = guestId;

            InitializeDocumentsDirectory();
            LoadGuestData();
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
                        .Include(g => g.GuestDocument.Select(d => d.DocumentType))
                        .Include(g => g.Booking)
                        .Include(g => g.Booking.Select(b => b.Room))
                        .Include(g => g.Booking.Select(b => b.Room.RoomType))
                        .Include(g => g.Booking.Select(b => b.BookingStatus))
                        .Include(g => g.LoyaltyTransaction)
                        .Include(g => g.LoyaltyTransaction.Select(t => t.TransactionType))
                        .FirstOrDefault(g => g.GuestId == _guestId);

                    if (_guest == null)
                    {
                        MessageBox.Show("Гость не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    DisplayGuestData();
                    LoadDocuments();
                    LoadBookingHistory();
                    LoadLoyaltyTransactions();
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

        private void DisplayGuestData()
        {
            string fullName = _guest.LastName + " " + _guest.FirstName + " " + _guest.MiddleName;
            GuestNameTextBlock.Text = fullName.Trim();

            LastNameTextBlock.Text = _guest.LastName ?? "Не указана";
            FirstNameTextBlock.Text = _guest.FirstName ?? "Не указано";
            MiddleNameTextBlock.Text = _guest.MiddleName ?? "Не указано";

            if (!string.IsNullOrEmpty(_guest.Gender))
            {
                string genderDisplay = _guest.Gender == "М" ? "Мужской" :
                                      _guest.Gender == "Ж" ? "Женский" : _guest.Gender;
                GenderDisplayTextBlock.Text = genderDisplay;
            }
            else
            {
                GenderDisplayTextBlock.Text = "Не указан";
            }

            if (_guest.DateOfBirth.HasValue)
            {
                DateOfBirthTextBlock.Text = _guest.DateOfBirth.Value.ToString("dd.MM.yyyy");
            }
            else
            {
                DateOfBirthTextBlock.Text = "Не указана";
            }

            BirthPlaceTextBlock.Text = _guest.BirthPlace ?? "Не указано";

            VIPStatusTextBlock.Text = _guest.IsVIP ? "Да" : "Нет";
            VIPStatusTextBlock.Foreground = _guest.IsVIP
                ? FindResource("AccentColor") as SolidColorBrush
                : FindResource("TextSecondaryColor") as SolidColorBrush;

            VIPBadge.Visibility = _guest.IsVIP ? Visibility.Visible : Visibility.Collapsed;

            GuestGroupTextBlock.Text = _guest.GuestGroup?.GroupName ?? "Не указана";

            PhoneTextBlock.Text = _guest.Phone ?? "Не указан";
            EmailTextBlock.Text = _guest.Email ?? "Не указан";
            AddressTextBlock.Text = _guest.Address ?? "Не указан";

            if (!string.IsNullOrEmpty(_guest.Notes))
            {
                NotesTextBlock.Text = _guest.Notes;
                NoNotesTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                NotesTextBlock.Visibility = Visibility.Collapsed;
                NoNotesTextBlock.Visibility = Visibility.Visible;
            }

            PointsTextBlock.Text = _guest.CurrentPoints.ToString();
            LoyaltyPointsTextBlock.Text = _guest.CurrentPoints.ToString();

            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                EditGuestButton.Visibility = Visibility.Collapsed;
                AddDocumentButton.Visibility = Visibility.Collapsed;
                AddBookingButton.Visibility = Visibility.Collapsed;
                CreateBookingButton.Visibility = Visibility.Collapsed;
                AddLoyaltyPointsButton.Visibility = Visibility.Collapsed;
            }
        }

        private void LoadDocuments()
        {
            try
            {
                var documents = _guest.GuestDocument.OrderByDescending(d => d.UploadedAt).ToList();
                DocumentsDataGrid.ItemsSource = documents;
                NoDocumentsTextBlock.Visibility = documents.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                DocumentsDataGrid.ItemsSource = null;
                NoDocumentsTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void LoadBookingHistory()
        {
            try
            {
                var bookings = _guest.Booking.OrderByDescending(b => b.CheckInDate).ToList();
                BookingsDataGrid.ItemsSource = bookings;
                NoBookingsTextBlock.Visibility = bookings.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                BookingsDataGrid.ItemsSource = null;
                NoBookingsTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void LoadLoyaltyTransactions()
        {
            try
            {
                var transactions = _guest.LoyaltyTransaction.OrderByDescending(t => t.TransactionDate).ToList();
                TransactionsDataGrid.ItemsSource = transactions;
                NoTransactionsTextBlock.Visibility = transactions.Any() ? Visibility.Collapsed : Visibility.Visible;
            }
            catch
            {
                TransactionsDataGrid.ItemsSource = null;
                NoTransactionsTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void AddDocumentButton_Click(object sender, RoutedEventArgs e)
        {
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
                            GuestId = _guestId,
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

                        LoadGuestData();
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

                            LoadGuestData();
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

        private void AddBookingButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewBooking();
        }

        private void CreateBookingButton_Click(object sender, RoutedEventArgs e)
        {
            CreateNewBooking();
        }

        private void CreateNewBooking()
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для создания бронирований.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var bookingWindow = new BookingEditWindow();
            if (bookingWindow.ShowDialog() == true)
            {
                LoadGuestData();
            }
        }

        private void AddLoyaltyPointsButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Функция добавления баллов лояльности будет реализована позже.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditGuestButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new GuestEditWindow(_guestId);
            if (editWindow.ShowDialog() == true)
            {
                LoadGuestData();
            }
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
            Close();
        }

        private void CloseButton2_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}