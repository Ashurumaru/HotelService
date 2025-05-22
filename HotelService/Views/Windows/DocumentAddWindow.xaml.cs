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
        public string DocumentSeries { get; private set; }
        public string DocumentNumber { get; private set; }
        public string IssuedBy { get; private set; }
        public DateTime? IssueDate { get; private set; }
        public DateTime? ExpiryDate { get; private set; }
        public string SelectedFilePath { get; private set; }

        public DocumentAddWindow(List<DocumentType> documentTypes)
        {
            InitializeComponent();

            DocumentTypeComboBox.ItemsSource = documentTypes;
            DocumentTypeComboBox.DisplayMemberPath = "TypeName";
            DocumentTypeComboBox.SelectedValuePath = "DocumentTypeId";

            if (documentTypes.Count > 0)
            {
                DocumentTypeComboBox.SelectedIndex = 0;
            }

            IssueDatePicker.SelectedDate = DateTime.Today;
            ExpiryDatePicker.SelectedDate = DateTime.Today.AddYears(10);
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
            List<string> errors = new List<string>();

            if (DocumentTypeComboBox.SelectedItem == null)
            {
                errors.Add("Выберите тип документа.");
            }

            if (string.IsNullOrWhiteSpace(DocumentSeriesTextBox.Text))
            {
                errors.Add("Введите серию документа.");
            }

            if (string.IsNullOrWhiteSpace(DocumentNumberTextBox.Text))
            {
                errors.Add("Введите номер документа.");
            }

            if (string.IsNullOrWhiteSpace(IssuedByTextBox.Text))
            {
                errors.Add("Введите информацию о том, кем выдан документ.");
            }

            if (!IssueDatePicker.SelectedDate.HasValue)
            {
                errors.Add("Выберите дату выдачи документа.");
            }

            if (string.IsNullOrEmpty(SelectedFilePath))
            {
                errors.Add("Выберите файл документа.");
            }
            else if (!File.Exists(SelectedFilePath))
            {
                errors.Add("Выбранный файл не существует.");
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

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                SelectedDocumentTypeId = (int)DocumentTypeComboBox.SelectedValue;
                DocumentSeries = DocumentSeriesTextBox.Text.Trim();
                DocumentNumber = DocumentNumberTextBox.Text.Trim();
                IssuedBy = IssuedByTextBox.Text.Trim();
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