using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Microsoft.Win32;
using System.Data.Entity;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class DamageReportEditWindow : Window, INotifyPropertyChanged
    {
        private readonly int? _reportId;
        private DamageReport _damageReport;
        private Room _selectedRoom;
        private Booking _selectedBooking;
        private Guest _selectedGuest;
        private readonly ObservableCollection<PhotoItem> _photos = new ObservableCollection<PhotoItem>();
        private readonly string _photoTempDir;
        private List<string> _photosToDelete = new List<string>();

        private readonly string _evidenceRelativePath = "Resources\\Images\\evidence";
        private string _projectRootPath;

        public event PropertyChangedEventHandler PropertyChanged;

        public class PhotoItem : INotifyPropertyChanged
        {
            private string _description;

            public int? EvidenceId { get; set; }
            public string FilePath { get; set; }
            public ImageSource ImageSource { get; set; }
            public bool IsNew { get; set; }

            public string Description
            {
                get => _description;
                set
                {
                    if (_description != value)
                    {
                        _description = value;
                        OnPropertyChanged(nameof(Description));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public DamageReportEditWindow(int? reportId = null)
        {
            InitializeComponent();
            _reportId = reportId;
            _photoTempDir = Path.Combine(Path.GetTempPath(), "HotelServicePhotos", Guid.NewGuid().ToString());

            // Get project root path by going up from the executable directory
            _projectRootPath = GetProjectRootPath();

            if (!Directory.Exists(_photoTempDir))
            {
                Directory.CreateDirectory(_photoTempDir);
            }

            // Ensure evidence directory exists
            string evidenceFullPath = Path.Combine(_projectRootPath, _evidenceRelativePath);
            if (!Directory.Exists(evidenceFullPath))
            {
                Directory.CreateDirectory(evidenceFullPath);
            }

            if (_reportId.HasValue)
            {
                WindowTitleTextBlock.Text = "Редактирование отчета о повреждении";
            }
            else
            {
                WindowTitleTextBlock.Text = "Новый отчет о повреждении";
            }

            PhotosListBox.ItemsSource = _photos;
            LoadReferenceData();

            if (_reportId.HasValue)
            {
                LoadDamageReportData();
            }
            else
            {
                InitializeNewReport();
            }

            UpdatePhotosVisibility();
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
                    var damageTypes = context.DamageType.OrderBy(dt => dt.TypeName).ToList();
                    DamageTypeComboBox.ItemsSource = damageTypes;

                    var statuses = context.TaskStatus.OrderBy(s => s.StatusId).ToList();
                    StatusComboBox.ItemsSource = statuses;
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

        private void LoadDamageReportData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _damageReport = context.DamageReport
                        .Include(dr => dr.DamageType)
                        .Include(dr => dr.Room)
                        .Include(dr => dr.Guest)
                        .Include(dr => dr.Booking)
                        .Include(dr => dr.TaskStatus)
                        .Include(dr => dr.DamageEvidence)
                        .FirstOrDefault(dr => dr.ReportId == _reportId.Value);

                    if (_damageReport == null)
                    {
                        MessageBox.Show("Отчет о повреждении не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    // Заполнение полей формы
                    DamageTypeComboBox.SelectedValue = _damageReport.DamageTypeId;
                    StatusComboBox.SelectedValue = _damageReport.StatusId;
                    ReportDatePicker.SelectedDate = _damageReport.ReportDate;

                    if (_damageReport.Cost.HasValue)
                    {
                        CostTextBox.Text = _damageReport.Cost.Value.ToString("F2");
                    }

                    DescriptionTextBox.Text = _damageReport.Description;

                    // Связанная информация
                    _selectedRoom = _damageReport.Room;
                    if (_selectedRoom != null)
                    {
                        RoomNumberTextBox.Text = _selectedRoom.RoomNumber;
                    }

                    _selectedBooking = _damageReport.Booking;
                    if (_selectedBooking != null)
                    {
                        BookingTextBox.Text = $"#{_selectedBooking.BookingId}";
                    }

                    _selectedGuest = _damageReport.Guest;
                    if (_selectedGuest != null)
                    {
                        string fullName = $"{_selectedGuest.LastName} {_selectedGuest.FirstName} {_selectedGuest.MiddleName}".Trim();
                        GuestTextBox.Text = fullName;
                    }

                    // Загрузка фотографий
                    LoadPhotos();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void LoadPhotos()
        {
            if (_damageReport.DamageEvidence != null && _damageReport.DamageEvidence.Any())
            {
                foreach (var evidence in _damageReport.DamageEvidence)
                {
                    try
                    {
                        // Convert relative path to absolute path
                        string relativePath = evidence.FilePath;
                        string fullPath = Path.Combine(_projectRootPath, relativePath);
                        string tempPath = Path.Combine(_photoTempDir, Path.GetFileName(relativePath));

                        if (File.Exists(fullPath))
                        {
                            File.Copy(fullPath, tempPath, true);

                            var imageSource = new BitmapImage();
                            imageSource.BeginInit();
                            imageSource.CacheOption = BitmapCacheOption.OnLoad;
                            imageSource.UriSource = new Uri(tempPath);
                            imageSource.EndInit();

                            _photos.Add(new PhotoItem
                            {
                                EvidenceId = evidence.EvidenceId,
                                FilePath = tempPath,
                                ImageSource = imageSource,
                                Description = evidence.Description,
                                IsNew = false
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Предупреждение",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
        }

        private void InitializeNewReport()
        {
            ReportDatePicker.SelectedDate = DateTime.Today;

            // Выберем первые элементы из справочников
            if (DamageTypeComboBox.Items.Count > 0)
                DamageTypeComboBox.SelectedIndex = 0;

            if (StatusComboBox.Items.Count > 0)
                StatusComboBox.SelectedIndex = 0;

            CostTextBox.Text = "0.00";
        }

        private void AddPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Изображения|*.jpg;*.jpeg;*.png;*.bmp|Все файлы|*.*",
                Multiselect = true,
                Title = "Выберите изображения"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    try
                    {
                        string fileName = Path.GetFileName(filePath);
                        string destPath = Path.Combine(_photoTempDir, fileName);
                        File.Copy(filePath, destPath, true);

                        var imageSource = new BitmapImage();
                        imageSource.BeginInit();
                        imageSource.CacheOption = BitmapCacheOption.OnLoad;
                        imageSource.UriSource = new Uri(destPath);
                        imageSource.EndInit();

                        _photos.Add(new PhotoItem
                        {
                            FilePath = destPath,
                            ImageSource = imageSource,
                            Description = "",
                            IsNew = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при добавлении изображения {Path.GetFileName(filePath)}: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }

                UpdatePhotosVisibility();
            }
        }

        private void ViewPhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var photoItem = (PhotoItem)button.Tag;

            if (photoItem != null && photoItem.ImageSource != null)
            {
                var imageViewWindow = new ImageViewWindow(photoItem.ImageSource, photoItem.Description);
                imageViewWindow.ShowDialog();
            }
        }

        private void RemovePhotoButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var photoItem = (PhotoItem)button.Tag;

            if (photoItem != null)
            {
                if (photoItem.EvidenceId.HasValue)
                {
                    _photosToDelete.Add(photoItem.FilePath);
                }

                _photos.Remove(photoItem);
                UpdatePhotosVisibility();
            }
        }

        private void UpdatePhotosVisibility()
        {
            NoPhotosTextBlock.Visibility = _photos.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void SelectRoomButton_Click(object sender, RoutedEventArgs e)
        {
            var selectWindow = new RoomSelectWindow(DateTime.Today, DateTime.Today.AddDays(1));
            if (selectWindow.ShowDialog() == true)
            {
                _selectedRoom = selectWindow.SelectedRoom;
                if (_selectedRoom != null)
                {
                    RoomNumberTextBox.Text = _selectedRoom.RoomNumber;
                }
            }
        }

        private void SelectBookingButton_Click(object sender, RoutedEventArgs e)
        {
            // Here you would open a booking selection window
            MessageBox.Show("Функциональность выбора бронирования будет реализована позже.",
                "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SelectGuestButton_Click(object sender, RoutedEventArgs e)
        {
            var selectWindow = new GuestSelectWindow();
            if (selectWindow.ShowDialog() == true)
            {
                _selectedGuest = selectWindow.SelectedGuest;
                if (_selectedGuest != null)
                {
                    string fullName = $"{_selectedGuest.LastName} {_selectedGuest.FirstName} {_selectedGuest.MiddleName}".Trim();
                    GuestTextBox.Text = fullName;
                }
            }
        }

        private bool ValidateReport()
        {
            var errors = new List<string>();

            if (DamageTypeComboBox.SelectedItem == null)
            {
                errors.Add("Не выбран тип повреждения");
            }

            if (StatusComboBox.SelectedItem == null)
            {
                errors.Add("Не выбран статус отчета");
            }

            if (ReportDatePicker.SelectedDate == null)
            {
                errors.Add("Не указана дата обнаружения");
            }

            if (_selectedRoom == null)
            {
                errors.Add("Не выбран номер");
            }

            if (string.IsNullOrWhiteSpace(DescriptionTextBox.Text))
            {
                errors.Add("Не указано описание повреждения");
            }

            decimal cost;
            if (!decimal.TryParse(CostTextBox.Text, out cost) || cost < 0)
            {
                errors.Add("Неверно указана оценка ущерба");
            }

            if (errors.Count > 0)
            {
                ValidationMessageTextBlock.Text = string.Join(", ", errors);
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            ValidationMessageTextBlock.Visibility = Visibility.Collapsed;
            return true;
        }

        private void SaveReport()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    DamageReport reportToSave;

                    if (_reportId.HasValue)
                    {
                        reportToSave = context.DamageReport.Find(_reportId.Value);
                        if (reportToSave == null)
                        {
                            MessageBox.Show("Отчет о повреждении не найден в базе данных.", "Ошибка",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                    else
                    {
                        reportToSave = new DamageReport();
                        context.DamageReport.Add(reportToSave);
                    }

                    // Основные данные
                    reportToSave.DamageTypeId = (int)DamageTypeComboBox.SelectedValue;
                    reportToSave.StatusId = (int)StatusComboBox.SelectedValue;
                    reportToSave.ReportDate = ReportDatePicker.SelectedDate.Value;

                    decimal cost;
                    if (decimal.TryParse(CostTextBox.Text, out cost))
                    {
                        reportToSave.Cost = cost;
                    }

                    reportToSave.Description = DescriptionTextBox.Text;

                    // Связанные данные
                    reportToSave.RoomId = _selectedRoom.RoomId;

                    if (_selectedBooking != null)
                    {
                        reportToSave.BookingId = _selectedBooking.BookingId;
                    }

                    if (_selectedGuest != null)
                    {
                        reportToSave.GuestId = _selectedGuest.GuestId;
                    }

                    // Сохраняем отчет для получения ID
                    context.SaveChanges();

                    // Удаление фотографий, которые были отмечены для удаления
                    if (_reportId.HasValue && _photosToDelete.Count > 0)
                    {
                        var evidenceToDelete = context.DamageEvidence
                            .Where(de => de.ReportId == reportToSave.ReportId)
                            .ToList();

                        foreach (var evidence in evidenceToDelete)
                        {
                            if (_photosToDelete.Contains(evidence.FilePath))
                            {
                                context.DamageEvidence.Remove(evidence);
                            }
                        }
                    }

                    // Create directory structure for this report's photos
                    string reportFolderRelative = Path.Combine(_evidenceRelativePath, reportToSave.ReportId.ToString());
                    string reportFolderAbsolute = Path.Combine(_projectRootPath, reportFolderRelative);

                    if (!Directory.Exists(reportFolderAbsolute))
                    {
                        Directory.CreateDirectory(reportFolderAbsolute);
                    }

                    // Save/update photos
                    foreach (var photo in _photos)
                    {
                        string fileName = Path.GetFileName(photo.FilePath);
                        string relativeFilePath = Path.Combine(reportFolderRelative, fileName);
                        string absoluteFilePath = Path.Combine(_projectRootPath, relativeFilePath);

                        // Copy file to storage location
                        if (File.Exists(photo.FilePath))
                        {
                            File.Copy(photo.FilePath, absoluteFilePath, true);
                        }

                        if (photo.IsNew)
                        {
                            // Add new photo evidence
                            var evidence = new DamageEvidence
                            {
                                ReportId = reportToSave.ReportId,
                                FilePath = relativeFilePath,  // Store relative path in DB
                                Description = photo.Description
                            };

                            context.DamageEvidence.Add(evidence);
                        }
                        else if (photo.EvidenceId.HasValue)
                        {
                            // Update existing photo evidence
                            var evidence = context.DamageEvidence.Find(photo.EvidenceId.Value);
                            if (evidence != null)
                            {
                                evidence.FilePath = relativeFilePath;  // Store relative path in DB
                                evidence.Description = photo.Description;
                            }
                        }
                    }

                    context.SaveChanges();

                    DialogResult = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении отчета: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateReport())
            {
                SaveReport();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void DecimalValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex(@"^[0-9]*(?:\,[0-9]*)?$");
            e.Handled = !regex.IsMatch(((TextBox)sender).Text + e.Text);
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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}