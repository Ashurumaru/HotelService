using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Data.Entity;
using System.IO;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class DamageReportViewWindow : Window
    {
        private readonly int _reportId;
        private DamageReport _damageReport;
        private string _projectRootPath;

        public class PhotoItem
        {
            public int EvidenceId { get; set; }
            public string FilePath { get; set; }
            public ImageSource ImageSource { get; set; }
            public string Description { get; set; }
        }

        public DamageReportViewWindow(int reportId)
        {
            InitializeComponent();
            _reportId = reportId;
            _projectRootPath = GetProjectRootPath();
            LoadDamageReportData();
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
                        .FirstOrDefault(dr => dr.ReportId == _reportId);

                    if (_damageReport == null)
                    {
                        MessageBox.Show("Отчет о повреждении не найден.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        Close();
                        return;
                    }

                    DisplayDamageReportData();
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

        private void DisplayDamageReportData()
        {
            // Основная информация
            ReportIdTextBlock.Text = _damageReport.ReportId.ToString();
            ReportDateTextBlock.Text = _damageReport.ReportDate.ToString("dd.MM.yyyy HH:mm");
            DamageTypeTextBlock.Text = _damageReport.DamageType?.TypeName ?? "Не указан";
            StatusTextBlock.Text = _damageReport.TaskStatus?.StatusName ?? "Не указан";

            if (_damageReport.Cost.HasValue)
            {
                CostTextBlock.Text = string.Format("{0:N2} ₽", _damageReport.Cost.Value);
            }
            else
            {
                CostTextBlock.Text = "Не определена";
            }

            // Связанная информация
            RoomNumberTextBlock.Text = _damageReport.Room?.RoomNumber ?? "Не указан";

            if (_damageReport.BookingId.HasValue)
            {
                BookingIdTextBlock.Text = $"#{_damageReport.BookingId.Value}";
            }
            else
            {
                BookingIdTextBlock.Text = "Не связано";
            }

            if (_damageReport.Guest != null)
            {
                string fullName = $"{_damageReport.Guest.LastName} {_damageReport.Guest.FirstName} {_damageReport.Guest.MiddleName}".Trim();
                GuestNameTextBlock.Text = fullName;
            }
            else
            {
                GuestNameTextBlock.Text = "Не указан";
            }


            DescriptionTextBlock.Text = _damageReport.Description ?? "Описание отсутствует";
        }

        private void LoadPhotos()
        {
            var photos = new List<PhotoItem>();

            if (_damageReport.DamageEvidence != null && _damageReport.DamageEvidence.Any())
            {
                foreach (var evidence in _damageReport.DamageEvidence)
                {
                    try
                    {
                        // Convert relative path to absolute path
                        string relativePath = evidence.FilePath;
                        string fullPath = Path.Combine(_projectRootPath, relativePath);
                        BitmapImage imageSource = null;

                        if (File.Exists(fullPath))
                        {
                            imageSource = new BitmapImage();
                            imageSource.BeginInit();
                            imageSource.CacheOption = BitmapCacheOption.OnLoad;
                            imageSource.UriSource = new Uri(fullPath);
                            imageSource.EndInit();
                        }

                        photos.Add(new PhotoItem
                        {
                            EvidenceId = evidence.EvidenceId,
                            FilePath = fullPath,
                            ImageSource = imageSource,
                            Description = evidence.Description
                        });
                    }
                    catch (Exception ex)
                    {
                        // Handle errors loading images
                        MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Предупреждение",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }

                PhotosListBox.ItemsSource = photos;
                NoPhotosTextBlock.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoPhotosTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void ViewFullImage_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var photoItem = (PhotoItem)button.Tag;

            if (photoItem != null && photoItem.ImageSource != null)
            {
                var imageViewWindow = new ImageViewWindow(photoItem.ImageSource, photoItem.Description);
                imageViewWindow.ShowDialog();
            }
        }

        private void EditReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для редактирования отчетов о повреждениях.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editWindow = new DamageReportEditWindow(_reportId);
            if (editWindow.ShowDialog() == true)
            {
                LoadDamageReportData();
            }
        }

        private void GenerateActButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1 && App.CurrentUser.RoleId != 2)
            {
                MessageBox.Show("У вас нет прав для формирования акта.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var actWindow = new DamageActGeneratorWindow(_reportId);
            actWindow.ShowDialog();
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