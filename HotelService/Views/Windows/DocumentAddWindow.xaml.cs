using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.IO;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class DocumentAddWindow : Window
    {
        public int SelectedDocumentTypeId { get; private set; }
        public DateTime? IssueDate { get; private set; }
        public DateTime? ExpiryDate { get; private set; }
        public string SelectedFilePath { get; private set; }

        public DocumentAddWindow(List<DocumentType> documentTypes)
        {
            InitializeComponent();

            // Initialize document types
            DocumentTypeComboBox.ItemsSource = documentTypes;
            DocumentTypeComboBox.DisplayMemberPath = "TypeName";
            DocumentTypeComboBox.SelectedValuePath = "DocumentTypeId";

            if (documentTypes.Count > 0)
            {
                DocumentTypeComboBox.SelectedIndex = 0;
            }

            // Set default dates
            IssueDatePicker.SelectedDate = DateTime.Today;
            ExpiryDatePicker.SelectedDate = DateTime.Today.AddYears(10); // Default 10-year validity
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Выберите файл документа",
                Filter = "Изображения (*.jpg, *.jpeg, *.png, *.bmp)|*.jpg;*.jpeg;*.png;*.bmp|PDF файлы (*.pdf)|*.pdf|Все файлы (*.*)|*.*",
                FilterIndex = 1,
                Multiselect = false
            };

            if (openFileDialog.ShowDialog() == true)
            {
                SelectedFilePath = openFileDialog.FileName;
                FilePathTextBox.Text = SelectedFilePath;

                // Show preview for images
                if (IsImageFile(SelectedFilePath))
                {
                    try
                    {
                        ShowImagePreview(SelectedFilePath);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при загрузке изображения: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    FilePreviewLabel.Visibility = Visibility.Collapsed;
                    FilePreviewBorder.Visibility = Visibility.Collapsed;
                }
            }
        }

        private bool IsImageFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            return extension == ".jpg" || extension == ".jpeg" || extension == ".png" || extension == ".bmp";
        }

        private void ShowImagePreview(string imagePath)
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(imagePath);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                FilePreviewImage.Source = bitmap;
                FilePreviewLabel.Visibility = Visibility.Visible;
                FilePreviewBorder.Visibility = Visibility.Visible;
            }
            catch
            {
                FilePreviewLabel.Visibility = Visibility.Collapsed;
                FilePreviewBorder.Visibility = Visibility.Collapsed;
            }
        }

        private bool ValidateInput()
        {
            ValidationMessageTextBlock.Text = "";

            if (DocumentTypeComboBox.SelectedItem == null)
            {
                ValidationMessageTextBlock.Text = "Выберите тип документа.";
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                ValidationMessageTextBlock.Text = "Выберите файл документа.";
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            if (!File.Exists(SelectedFilePath))
            {
                ValidationMessageTextBlock.Text = "Выбранный файл не существует.";
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
                return false;
            }

            ValidationMessageTextBlock.Visibility = Visibility.Collapsed;
            return true;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                SelectedDocumentTypeId = (int)DocumentTypeComboBox.SelectedValue;
                IssueDate = IssueDatePicker.SelectedDate;
                ExpiryDate = ExpiryDatePicker.SelectedDate;

                DialogResult = true;
                Close();
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