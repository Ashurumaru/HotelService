using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using System.Windows.Media;
using HotelService.Data;
using System.IO;

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
                        .FirstOrDefault(g => g.GuestId == _guestId);

                    if (_guest == null)
                    {
                        MessageBox.Show("Гость не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    // Display guest data
                    DisplayGuestInfo();

                    // Load related data
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

        private void DisplayGuestInfo()
        {

            GuestNameTextBlock.Text = $"{_guest.LastName} {_guest.FirstName} {(_guest.MiddleName ?? "")}".Trim();

            // Set initials
            string initials = "";
            if (_guest.FirstName.Length > 0)
                initials += _guest.FirstName[0];
            if (_guest.LastName.Length > 0)
                initials += _guest.LastName[0];

            // Points
            PointsTextBlock.Text = _guest.CurrentPoints.ToString();
            LoyaltyPointsTextBlock.Text = _guest.CurrentPoints.ToString();

            // Personal data
            LastNameTextBlock.Text = _guest.LastName;
            FirstNameTextBlock.Text = _guest.FirstName;
            MiddleNameTextBlock.Text = _guest.MiddleName ?? "Не указано";
            DateOfBirthTextBlock.Text = _guest.DateOfBirth.HasValue ? _guest.DateOfBirth.Value.ToString("dd.MM.yyyy") : "Не указано";
            VIPStatusTextBlock.Text = _guest.IsVIP ? "Да" : "Нет";
            VIPStatusTextBlock.Foreground = _guest.IsVIP
                ? FindResource("AccentColor") as SolidColorBrush
                : FindResource("TextSecondaryColor") as SolidColorBrush;
            GuestGroupTextBlock.Text = _guest.GuestGroup?.GroupName ?? "Не назначена";

            // Contact info
            PhoneTextBlock.Text = _guest.Phone ?? "Не указан";
            EmailTextBlock.Text = _guest.Email ?? "Не указан";
            AddressTextBlock.Text = _guest.Address ?? "Не указан";

            // Notes
            if (string.IsNullOrWhiteSpace(_guest.Notes))
            {
                NotesTextBlock.Visibility = Visibility.Collapsed;
                NoNotesTextBlock.Visibility = Visibility.Visible;
            }
            else
            {
                NotesTextBlock.Text = _guest.Notes;
                NotesTextBlock.Visibility = Visibility.Visible;
                NoNotesTextBlock.Visibility = Visibility.Collapsed;
            }

            // Check permissions
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                EditGuestButton.Visibility = Visibility.Collapsed;
                AddDocumentButton.Visibility = Visibility.Collapsed;
                AddBookingButton.Visibility = Visibility.Collapsed;
                CreateBookingButton.Visibility = Visibility.Collapsed;
                AddLoyaltyPointsButton.Visibility = Visibility.Collapsed;
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

        private void AddDocumentButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new HotelServiceEntities())
                {
                    var documentTypes = context.DocumentType.OrderBy(dt => dt.TypeName).ToList();

                    if (documentTypes.Count == 0)
                    {
                        // Add default document types if none exist
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
                            IssueDate = documentAddWindow.IssueDate,
                            ExpiryDate = documentAddWindow.ExpiryDate,
                            UploadedAt = DateTime.Now,
                            UploadedBy = App.CurrentUser?.UserId
                        };

                        // Handle document file
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

                        // Refresh documents list
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
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для удаления документов.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

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
                            // Delete the file if it exists
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

                            // Delete the database record
                            context.GuestDocument.Remove(docToDelete);
                            context.SaveChanges();

                            // Refresh the documents list
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

            try
            {
                using (var context = new HotelServiceEntities())
                {
                    // Pre-select the current guest
                    var guest = context.Guest.Find(_guestId);
                    if (guest != null)
                    {
                        // The booking window should have a method to pre-select a guest
                        // For now, we'll just open the window and let the user select the guest
                        if (bookingWindow.ShowDialog() == true)
                        {
                            LoadBookingHistory(context);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при подготовке к созданию бронирования: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddLoyaltyPointsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для управления баллами лояльности.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Show context menu with loyalty operations
            var contextMenu = new ContextMenu();

            var earnItem = new MenuItem { Header = "Начисление баллов" };
            earnItem.Click += (s, args) => OpenLoyaltyTransaction(1); // Type 1 = EARNING
            contextMenu.Items.Add(earnItem);

            var redeemItem = new MenuItem { Header = "Списание баллов" };
            redeemItem.Click += (s, args) => OpenLoyaltyTransaction(2); // Type 2 = REDEEMING
            contextMenu.Items.Add(redeemItem);

            var adjustItem = new MenuItem { Header = "Корректировка баллов" };
            adjustItem.Click += (s, args) => OpenLoyaltyTransaction(3); // Type 3 = ADJUSTMENT
            contextMenu.Items.Add(adjustItem);

            contextMenu.IsOpen = true;
        }

        private void OpenLoyaltyTransaction(int transactionTypeId)
        {
            var loyaltyWindow = new LoyaltyTransactionWindow(_guestId, null, transactionTypeId);
            if (loyaltyWindow.ShowDialog() == true)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        var guest = context.Guest.Find(_guestId);
                        if (guest == null)
                        {
                            MessageBox.Show("Гость не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        PointsTextBlock.Text = guest.CurrentPoints.ToString();
                        LoyaltyPointsTextBlock.Text = guest.CurrentPoints.ToString();

                        LoadLoyaltyTransactions(context);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при обновлении данных: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
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