using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.IO;
using System.Data.Entity;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class RoomEditWindow : Window
    {
        private readonly int? _roomId;
        private Room _room;
        private ObservableCollection<RoomAmenityViewModel> _amenities;
        private ObservableCollection<RoomImageViewModel> _images;
        private bool _isInitializing = true;

        private readonly string _roomImagesRelativePath = "Resources\\Images\\rooms";
        private string _projectRootPath;

        public class RoomAmenityViewModel
        {
            public RoomAmenity RoomAmenity { get; set; }
            public int AmenityId { get; set; }
            public int Quantity { get; set; }
            public string Notes { get; set; }
        }

        public class RoomImageViewModel
        {
            public int? RoomImageId { get; set; }
            public string ImagePath { get; set; }
            public bool IsDefault { get; set; }
            public ImageSource ImageSource { get; set; }
            public bool IsNew { get; set; }
        }

        public RoomEditWindow(int? roomId = null)
        {
            InitializeComponent();
            _roomId = roomId;

            // Get project root path
            _projectRootPath = GetProjectRootPath();

            if (_roomId.HasValue)
            {
                WindowTitleTextBlock.Text = "Редактирование номера";
            }
            else
            {
                WindowTitleTextBlock.Text = "Создание нового номера";
            }

            LoadReferenceData();

            if (_roomId.HasValue)
            {
                LoadRoomData();
            }
            else
            {
                InitializeNewRoom();
            }

            _isInitializing = false;
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

        private void LoadReferenceData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    // Загрузка типов номеров
                    var roomTypes = context.RoomType.OrderBy(rt => rt.TypeName).ToList();
                    RoomTypeComboBox.ItemsSource = roomTypes;
                    RoomTypeComboBox.DisplayMemberPath = "TypeName";
                    RoomTypeComboBox.SelectedValuePath = "RoomTypeId";

                    // Загрузка статусов номеров
                    var roomStatuses = context.RoomStatus.OrderBy(rs => rs.StatusName).ToList();
                    RoomStatusComboBox.ItemsSource = roomStatuses;
                    RoomStatusComboBox.DisplayMemberPath = "StatusName";
                    RoomStatusComboBox.SelectedValuePath = "RoomStatusId";
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

        private void LoadRoomData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _room = context.Room
                        .Include(r => r.RoomType)
                        .Include(r => r.RoomStatus)
                        .FirstOrDefault(r => r.RoomId == _roomId);

                    if (_room == null)
                    {
                        MessageBox.Show("Номер не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    // Заполнение полей формы
                    RoomNumberTextBox.Text = _room.RoomNumber;
                    RoomTypeComboBox.SelectedValue = _room.RoomTypeId;
                    RoomStatusComboBox.SelectedValue = _room.RoomStatusId;
                    FloorNumberTextBox.Text = _room.FloorNumber.ToString();
                    MaxOccupancyTextBox.Text = _room.MaxOccupancy.ToString();
                    BasePriceTextBox.Text = _room.BasePrice.ToString("F2");

                    if (_room.SquareMeters.HasValue)
                    {
                        SquareMetersTextBox.Text = _room.SquareMeters.Value.ToString();
                    }

                    CommentsTextBox.Text = _room.Comments;

                    // Загрузка удобств
                    LoadAmenities(context);

                    // Загрузка изображений
                    LoadImages(context);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных номера: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void LoadAmenities(HotelServiceEntities context)
        {
            // Загрузка удобств для номера
            var roomAmenities = context.RoomAmenity
                .Include(ra => ra.Amenity)
                .Include(ra => ra.Amenity.AmenityCategory)
                .Where(ra => ra.RoomId == _roomId)
                .ToList();

            _amenities = new ObservableCollection<RoomAmenityViewModel>(
                roomAmenities.Select(ra => new RoomAmenityViewModel
                {
                    RoomAmenity = ra,
                    AmenityId = ra.AmenityId,
                    Quantity = ra.Quantity ?? 1,
                    Notes = ra.Notes
                })
            );

            AmenitiesDataGrid.ItemsSource = _amenities;
            NoAmenitiesTextBlock.Visibility = _amenities.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void LoadImages(HotelServiceEntities context)
        {
            // Загрузка изображений для номера
            var roomImages = context.RoomImage
                .Where(ri => ri.RoomId == _roomId)
                .ToList();

            _images = new ObservableCollection<RoomImageViewModel>();

            foreach (var image in roomImages)
            {
                try
                {
                    // Convert relative path to full path
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

                    _images.Add(new RoomImageViewModel
                    {
                        RoomImageId = image.RoomImageId,
                        ImagePath = relativePath,
                        IsDefault = image.IsDefault,
                        ImageSource = imageSource,
                        IsNew = false
                    });
                }
                catch (Exception ex)
                {
                    // Log the error
                    Console.WriteLine($"Ошибка загрузки изображения: {ex.Message}");
                }
            }

            ImagesListBox.ItemsSource = _images;
            NoImagesTextBlock.Visibility = _images.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void InitializeNewRoom()
        {
            RoomStatusComboBox.SelectedIndex = 0; // Первый статус (обычно "Свободен")
            RoomTypeComboBox.SelectedIndex = 0;   // Первый тип номера
            FloorNumberTextBox.Text = "1";
            MaxOccupancyTextBox.Text = "2";
            BasePriceTextBox.Text = "0.00";
            SquareMetersTextBox.Text = "0";

            // Инициализация пустых коллекций
            _amenities = new ObservableCollection<RoomAmenityViewModel>();
            AmenitiesDataGrid.ItemsSource = _amenities;
            NoAmenitiesTextBlock.Visibility = Visibility.Visible;

            _images = new ObservableCollection<RoomImageViewModel>();
            ImagesListBox.ItemsSource = _images;
            NoImagesTextBlock.Visibility = Visibility.Visible;
        }

        private void AddAmenityButton_Click(object sender, RoutedEventArgs e)
        {
            var amenitySelectWindow = new AmenitySelectWindow();
            if (amenitySelectWindow.ShowDialog() == true)
            {
                var amenity = amenitySelectWindow.SelectedAmenity;
                var quantity = amenitySelectWindow.Quantity;
                var notes = amenitySelectWindow.Notes;

                // Проверяем, не добавлено ли уже это удобство
                var existingAmenity = _amenities.FirstOrDefault(a => a.AmenityId == amenity.AmenityId);
                if (existingAmenity != null)
                {
                    MessageBox.Show("Это удобство уже добавлено в номер.", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var newRoomAmenity = new RoomAmenity
                {
                    AmenityId = amenity.AmenityId,
                    Quantity = quantity,
                    Notes = notes,
                    Amenity = amenity
                };

                var viewModel = new RoomAmenityViewModel
                {
                    RoomAmenity = newRoomAmenity,
                    AmenityId = amenity.AmenityId,
                    Quantity = quantity,
                    Notes = notes
                };

                _amenities.Add(viewModel);
                NoAmenitiesTextBlock.Visibility = Visibility.Collapsed;
            }
        }

        private void EditAmenityButton_Click(object sender, RoutedEventArgs e)
        {
            var amenityViewModel = (sender as Button)?.DataContext as RoomAmenityViewModel;
            if (amenityViewModel == null) return;

            var amenityEditWindow = new AmenityEditWindow(
                amenityViewModel.RoomAmenity.AmenityId,
                amenityViewModel.Quantity,
                amenityViewModel.Notes);

            if (amenityEditWindow.ShowDialog() == true)
            {
                amenityViewModel.Quantity = amenityEditWindow.Quantity;
                amenityViewModel.Notes = amenityEditWindow.Notes;
                amenityViewModel.RoomAmenity.Quantity = amenityEditWindow.Quantity;
                amenityViewModel.RoomAmenity.Notes = amenityEditWindow.Notes;

                // Обновляем отображение
                AmenitiesDataGrid.Items.Refresh();
            }
        }

        private void DeleteAmenityButton_Click(object sender, RoutedEventArgs e)
        {
            var amenityViewModel = (sender as Button)?.DataContext as RoomAmenityViewModel;
            if (amenityViewModel == null) return;

            MessageBoxResult result = MessageBox.Show(
                $"Вы действительно хотите удалить удобство из номера?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _amenities.Remove(amenityViewModel);
                NoAmenitiesTextBlock.Visibility = _amenities.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void AddImageButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*",
                Multiselect = true
            };

            if (openFileDialog.ShowDialog() == true)
            {
                bool firstImage = _images.Count == 0;

                foreach (var fileName in openFileDialog.FileNames)
                {
                    try
                    {
                        // Generate a unique temp filename to avoid collisions
                        string tempFileName = $"{DateTime.Now.ToString("yyyyMMddHHmmssfff")}_{Path.GetFileName(fileName)}";
                        string tempFilePath = Path.Combine(Path.GetTempPath(), tempFileName);

                        // Copy to temp for now - will be moved to the correct location when saved
                        File.Copy(fileName, tempFilePath, true);

                        // Load the image for display
                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.UriSource = new Uri(tempFilePath);
                        bitmap.EndInit();

                        _images.Add(new RoomImageViewModel
                        {
                            RoomImageId = null,
                            ImagePath = tempFilePath,  // Temporary path - will be updated on save
                            IsDefault = firstImage,    // First added image is set as default
                            ImageSource = bitmap,
                            IsNew = true
                        });

                        firstImage = false;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при добавлении изображения {fileName}: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                NoImagesTextBlock.Visibility = _images.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void DefaultImageCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            var checkBox = sender as CheckBox;
            var imageViewModel = checkBox?.DataContext as RoomImageViewModel;

            if (imageViewModel != null)
            {
                // Remove default flag from all other images
                foreach (var image in _images)
                {
                    if (image != imageViewModel)
                    {
                        image.IsDefault = false;
                    }
                }

                ImagesListBox.Items.Refresh();
            }
        }

        private void DeleteImageButton_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var imageId = button?.Tag;
            var imageViewModel = button?.DataContext as RoomImageViewModel;

            if (imageViewModel != null)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Вы действительно хотите удалить это изображение?",
                    "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    // If deleting the default image, we need to select a new default
                    bool wasDefault = imageViewModel.IsDefault;

                    _images.Remove(imageViewModel);

                    if (wasDefault && _images.Count > 0)
                    {
                        _images[0].IsDefault = true;
                    }

                    NoImagesTextBlock.Visibility = _images.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                }
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"[^0-9\,\.]+");

            // Проверяем, что вводятся только числа или десятичный разделитель
            bool isMatch = regex.IsMatch(e.Text);

            // Проверяем, не пытается ли пользователь добавить второй десятичный разделитель
            if (!isMatch)
            {
                TextBox textBox = sender as TextBox;
                if (textBox != null)
                {
                    string futureText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
                    isMatch = futureText.Count(c => c == ',' || c == '.') > 1;
                }
            }

            e.Handled = isMatch;
        }

        private bool ValidateRoom()
        {
            List<string> errors = new List<string>();

            if (string.IsNullOrWhiteSpace(RoomNumberTextBox.Text))
            {
                errors.Add("Необходимо указать номер комнаты.");
            }

            if (RoomTypeComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать тип номера.");
            }

            if (RoomStatusComboBox.SelectedItem == null)
            {
                errors.Add("Необходимо выбрать статус номера.");
            }

            int floorNumber;
            if (!int.TryParse(FloorNumberTextBox.Text, out floorNumber) || floorNumber <= 0)
            {
                errors.Add("Необходимо указать корректный номер этажа.");
            }

            int maxOccupancy;
            if (!int.TryParse(MaxOccupancyTextBox.Text, out maxOccupancy) || maxOccupancy <= 0)
            {
                errors.Add("Необходимо указать корректную максимальную вместимость.");
            }

            decimal basePrice;
            if (!decimal.TryParse(BasePriceTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out basePrice) || basePrice < 0)
            {
                errors.Add("Необходимо указать корректную базовую цену.");
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

        private void SaveRoom()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    Room roomToSave;

                    if (_roomId.HasValue)
                    {
                        // Редактирование существующего номера
                        roomToSave = context.Room.Find(_roomId.Value);
                        if (roomToSave == null)
                        {
                            MessageBox.Show("Номер не найден в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        // Создание нового номера
                        roomToSave = new Room();
                        context.Room.Add(roomToSave);
                    }

                    // Сохранение данных номера
                    roomToSave.RoomNumber = RoomNumberTextBox.Text;
                    roomToSave.RoomTypeId = (int)RoomTypeComboBox.SelectedValue;
                    roomToSave.RoomStatusId = (int)RoomStatusComboBox.SelectedValue;
                    roomToSave.FloorNumber = int.Parse(FloorNumberTextBox.Text);
                    roomToSave.MaxOccupancy = int.Parse(MaxOccupancyTextBox.Text);
                    roomToSave.BasePrice = decimal.Parse(BasePriceTextBox.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture);

                    if (!string.IsNullOrEmpty(SquareMetersTextBox.Text))
                    {
                        roomToSave.SquareMeters = int.Parse(SquareMetersTextBox.Text);
                    }

                    roomToSave.Comments = CommentsTextBox.Text;

                    // Save the room to get an ID for new records
                    context.SaveChanges();

                    // Save amenities
                    SaveAmenities(context, roomToSave);

                    // Save images
                    SaveImages(context, roomToSave);

                    context.SaveChanges();

                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении номера: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SaveAmenities(HotelServiceEntities context, Room room)
        {
            // Delete existing amenities
            var existingAmenities = context.RoomAmenity.Where(ra => ra.RoomId == room.RoomId).ToList();
            context.RoomAmenity.RemoveRange(existingAmenities);

            // Add new amenities
            foreach (var amenityViewModel in _amenities)
            {
                var roomAmenity = new RoomAmenity
                {
                    RoomId = room.RoomId,
                    AmenityId = amenityViewModel.AmenityId,
                    Quantity = amenityViewModel.Quantity,
                    Notes = amenityViewModel.Notes
                };

                context.RoomAmenity.Add(roomAmenity);
            }
        }

        private void SaveImages(HotelServiceEntities context, Room room)
        {
            // Get existing images from database
            var existingImages = context.RoomImage.Where(ri => ri.RoomId == room.RoomId).ToList();
            var imagesToDelete = new List<RoomImage>();

            // Identify images to delete
            foreach (var existingImage in existingImages)
            {
                bool keep = false;
                foreach (var imageViewModel in _images)
                {
                    if (imageViewModel.RoomImageId == existingImage.RoomImageId)
                    {
                        keep = true;
                        // Update IsDefault flag
                        existingImage.IsDefault = imageViewModel.IsDefault;
                        break;
                    }
                }

                if (!keep)
                {
                    imagesToDelete.Add(existingImage);
                }
            }

            // Delete unwanted images
            foreach (var imageToDelete in imagesToDelete)
            {
                context.RoomImage.Remove(imageToDelete);
            }

            // Create room images directory if it doesn't exist
            string roomFolderRelative = Path.Combine(_roomImagesRelativePath, room.RoomId.ToString());
            string roomFolderAbsolute = Path.Combine(_projectRootPath, roomFolderRelative);

            if (!Directory.Exists(roomFolderAbsolute))
            {
                Directory.CreateDirectory(roomFolderAbsolute);
            }

            // Add new images
            foreach (var imageViewModel in _images)
            {
                if (imageViewModel.IsNew)
                {
                    string fileName = Path.GetFileName(imageViewModel.ImagePath);
                    string relativeFilePath = Path.Combine(roomFolderRelative, fileName);
                    string absoluteFilePath = Path.Combine(_projectRootPath, relativeFilePath);

                    // Copy file to storage location
                    if (File.Exists(imageViewModel.ImagePath))
                    {
                        File.Copy(imageViewModel.ImagePath, absoluteFilePath, true);
                    }

                    var roomImage = new RoomImage
                    {
                        RoomId = room.RoomId,
                        ImagePath = relativeFilePath,  // Store relative path in the database
                        IsDefault = imageViewModel.IsDefault
                    };

                    context.RoomImage.Add(roomImage);
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateRoom())
            {
                SaveRoom();
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