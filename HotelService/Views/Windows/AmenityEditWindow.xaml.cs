using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;
using System.Linq;
using System.Windows.Controls;

namespace HotelService.Views.Windows
{
    public partial class AmenityEditWindow : Window
    {
        private readonly int _amenityId;
        private Amenity _amenity;

        public int Quantity { get; private set; }
        public string Notes { get; private set; }

        public AmenityEditWindow(int amenityId, int quantity, string notes)
        {
            InitializeComponent();
            _amenityId = amenityId;
            Quantity = quantity;
            Notes = notes;

            LoadAmenityData();

            QuantityTextBox.Text = quantity.ToString();
            NotesTextBox.Text = notes;
        }

        private void LoadAmenityData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    _amenity = context.Amenity
                        .Include(a => a.AmenityCategory)
                        .FirstOrDefault(a => a.AmenityId == _amenityId);

                    if (_amenity == null)
                    {
                        MessageBox.Show("Удобство не найдено.", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        DialogResult = false;
                        Close();
                        return;
                    }

                    // Display amenity name
                    AmenityNameTextBlock.Text = _amenity.AmenityName;
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

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);

            // Also verify the resulting number would be >0
            TextBox textBox = sender as TextBox;
            if (textBox != null)
            {
                string newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);

                if (string.IsNullOrEmpty(newText) || !int.TryParse(newText, out int result) || result <= 0)
                {
                    e.Handled = true;
                }
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(QuantityTextBox.Text, out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Пожалуйста, укажите корректное количество.", "Предупреждение",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Quantity = quantity;
            Notes = NotesTextBox.Text;

            DialogResult = true;
            Close();
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