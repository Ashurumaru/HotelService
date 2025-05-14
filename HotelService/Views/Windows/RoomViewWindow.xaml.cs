using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Data.Entity;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class RoomViewWindow : Window
    {
        private readonly int _roomId;
        private Room _room;
        private string _projectRootPath;

        public class RoomImageViewModel
        {
            public int RoomImageId { get; set; }
            public string ImagePath { get; set; }
            public bool IsDefault { get; set; }
            public ImageSource ImageSource { get; set; }
        }

        public class RoomAmenityViewModel
        {
            public RoomAmenity Amenity { get; set; }
            public string QuantityText { get; set; }
        }

        public class BookingViewModel
        {
            public int BookingId { get; set; }
            public string GuestName { get; set; }
            public DateTime CheckInDate { get; set; }
            public DateTime CheckOutDate { get; set; }
            public string StatusName { get; set; }
            public decimal TotalAmount { get; set; }
        }

        public class MaintenanceViewModel
        {
            public int MaintenanceId { get; set; }
            public DateTime StartDate { get; set; }
            public TaskType TaskType { get; set; }
            public string IssueDescription { get; set; }
            public int Priority { get; set; }
            public string PriorityText
            {
                get
                {
                    switch (Priority)
                    {
                        case 1: return "Низкий";
                        case 2: return "Средний";
                        case 3: return "Высокий";
                        case 4: return "Критический";
                        default: return "Не указан";
                    }
                }
            }
            public TaskStatus TaskStatus { get; set; }
            public DateTime? CompletionDate { get; set; }
        }

        public RoomViewWindow(int roomId)
        {
            InitializeComponent();
            _roomId = roomId;
            _projectRootPath = GetProjectRootPath();
            LoadRoomData();
        }

        private string GetProjectRootPath()
        {
            // Start with the directory where the executable is located
            string directory = AppDomain.CurrentDomain.BaseDirectory;

            // Typically, the executable is in bin/Debug or bin/Release
            // So we need to go up two directories to reach the project root
            for (int i = 0; i < 2; i++)
            {
                directory = Directory.GetParent(directory)?.FullName;
                if (directory == null)
                {
                    // If we can't go up, just use the current directory
                    return AppDomain.CurrentDomain.BaseDirectory;
                }
            }

            return directory;
        }

        private void LoadRoomData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    // Загрузка основной информации о номере
                    _room = context.Room
                        .Include(r => r.RoomType)
                        .Include(r => r.RoomStatus)
                        .FirstOrDefault(r => r.RoomId == _roomId);

                    if (_room == null)
                    {
                        MessageBox.Show("Номер не найден.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    // Загрузка связанных данных
                    LoadRoomImages(context);
                    LoadRoomAmenities(context);
                    LoadRoomMaintenance(context);
                    LoadRoomBookings(context);

                    // Отображение данных
                    DisplayRoomData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void LoadRoomImages(HotelServiceEntities context)
        {
            var roomImages = context.RoomImage
                .Where(ri => ri.RoomId == _roomId)
                .ToList();

            var imageViewModels = new ObservableCollection<RoomImageViewModel>();

            foreach (var image in roomImages)
            {
                try
                {
                    // Convert relative path to absolute path
                    string relativePath = image.ImagePath;
                    string fullPath = Path.Combine(_projectRootPath, relativePath);

                    ImageSource imageSource = null;

                    if (File.Exists(fullPath))
                    {
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(fullPath);
                        bitmap.EndInit();
                        imageSource = bitmap;
                    }
                    else
                    {
                        // Fallback for missing images
                        imageSource = new BitmapImage(new Uri("/Resources/Images/no-image.png", UriKind.Relative));
                    }

                    imageViewModels.Add(new RoomImageViewModel
                    {
                        RoomImageId = image.RoomImageId,
                        ImagePath = fullPath,
                        IsDefault = image.IsDefault,
                        ImageSource = imageSource
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                }
            }

            ImagesItemsControl.ItemsSource = imageViewModels;
            NoImagesTextBlock.Visibility = imageViewModels.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadRoomAmenities(HotelServiceEntities context)
        {
            var roomAmenities = context.RoomAmenity
                .Include(ra => ra.Amenity)
                .Include(ra => ra.Amenity.AmenityCategory)
                .Where(ra => ra.RoomId == _roomId)
                .ToList();

            var amenityViewModels = new ObservableCollection<RoomAmenityViewModel>();

            foreach (var amenity in roomAmenities)
            {
                string quantityText = amenity.Quantity.HasValue ? $"{amenity.Quantity}" : "";

                amenityViewModels.Add(new RoomAmenityViewModel
                {
                    Amenity = amenity,
                    QuantityText = quantityText
                });
            }

            AmenitiesDataGrid.ItemsSource = amenityViewModels;
            NoAmenitiesTextBlock.Visibility = amenityViewModels.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadRoomMaintenance(HotelServiceEntities context)
        {
            var maintenanceTasks = context.RoomMaintenance
                .Include(rm => rm.TaskType)
                .Include(rm => rm.TaskStatus)
                .Where(rm => rm.RoomId == _roomId)
                .OrderByDescending(rm => rm.StartDate)
                .ToList();

            var maintenanceViewModels = new ObservableCollection<MaintenanceViewModel>();

            foreach (var task in maintenanceTasks)
            {
                maintenanceViewModels.Add(new MaintenanceViewModel
                {
                    MaintenanceId = task.MaintenanceId,
                    StartDate = task.StartDate ?? DateTime.MinValue,
                    TaskType = task.TaskType,
                    IssueDescription = task.IssueDescription,
                    Priority = task.Priority,
                    TaskStatus = task.TaskStatus,
                    CompletionDate = task.CompletionDate
                });
            }

            MaintenanceDataGrid.ItemsSource = maintenanceViewModels;
            NoMaintenanceTextBlock.Visibility = maintenanceViewModels.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadRoomBookings(HotelServiceEntities context)
        {
            var bookings = context.Booking
                .Include(b => b.Guest)
                .Include(b => b.BookingStatus)
                .Where(b => b.RoomId == _roomId)
                .OrderByDescending(b => b.CheckInDate)
                .ToList();

            var bookingViewModels = new ObservableCollection<BookingViewModel>();

            foreach (var booking in bookings)
            {
                string guestName = booking.Guest != null
                    ? $"{booking.Guest.LastName} {booking.Guest.FirstName} {booking.Guest.MiddleName}".Trim()
                    : "Гость не указан";

                bookingViewModels.Add(new BookingViewModel
                {
                    BookingId = booking.BookingId,
                    GuestName = guestName,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    StatusName = booking.BookingStatus?.StatusName ?? "Неизвестно",
                    TotalAmount = booking.TotalAmount
                });
            }

            BookingsDataGrid.ItemsSource = bookingViewModels;
            NoBookingsTextBlock.Visibility = bookingViewModels.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void DisplayRoomData()
        {
            // Оснвная информация
            RoomTitleTextBlock.Text = $"Номер {_room.RoomNumber} - {_room.RoomType?.TypeName ?? "Не указан"}";

            StatusTextBlock.Text = _room.RoomStatus?.StatusName ?? "Не указан";
            FloorTextBlock.Text = $"{_room.FloorNumber} этаж";

            string occupancyText = GetGuestsText(_room.MaxOccupancy);
            OccupancyTextBlock.Text = occupancyText;

            PriceTextBlock.Text = $"Базовая цена: {_room.BasePrice:N2} ₽ за ночь";

            SquareMetersTextBlock.Text = _room.SquareMeters.HasValue
                ? $"{_room.SquareMeters.Value} м²"
                : "Не указана";

            // Описание
            if (!string.IsNullOrEmpty(_room.Comments))
            {
                CommentsTextBlock.Text = _room.Comments;
                NoCommentsTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                CommentsTextBlock.Visibility = Visibility.Collapsed;
                NoCommentsTextBlock.Visibility = Visibility.Visible;
            }

            // Проверка прав доступа для кнопок
            if (App.CurrentUser.RoleId != 1)
            {
                EditRoomButton.Visibility = Visibility.Collapsed;
            }
        }

        private string GetGuestsText(int guests)
        {
            if (guests % 10 == 1 && guests % 100 != 11)
                return $"{guests} гость";
            else if ((guests % 10 == 2 || guests % 10 == 3 || guests % 10 == 4) &&
                    (guests % 100 < 10 || guests % 100 > 20))
                return $"{guests} гостя";
            else
                return $"{guests} гостей";
        }

        private void AddMaintenanceButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new MaintenanceTaskWindow(roomId: _roomId);
            if (dialog.ShowDialog() == true)
            {
                using (var context = new HotelServiceEntities())
                {
                    LoadRoomMaintenance(context);
                }
            }
        }

        private void ViewMaintenanceButton_Click(object sender, RoutedEventArgs e)
        {
            var maintenance = (sender as Button)?.DataContext as MaintenanceViewModel;
            if (maintenance == null) return;

            var dialog = new MaintenanceTaskWindow(maintenanceId: maintenance.MaintenanceId, isViewMode: true);
            dialog.ShowDialog();
        }

        private void ViewBookingButton_Click(object sender, RoutedEventArgs e)
        {
            var booking = (sender as Button)?.DataContext as BookingViewModel;
            if (booking == null) return;

            var dialog = new BookingViewWindow(booking.BookingId);
            dialog.ShowDialog();
        }

        private void EditRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для редактирования номеров.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new RoomEditWindow(_roomId);
            if (dialog.ShowDialog() == true)
            {
                LoadRoomData();
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
    }
}