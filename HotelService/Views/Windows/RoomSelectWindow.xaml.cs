using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;
using System.ComponentModel;

namespace HotelService.Views.Windows
{
    public partial class RoomSelectWindow : Window
    {
        public Room SelectedRoom { get; private set; }

        private List<Room> _allRooms;
        private ICollectionView _roomsView;

        private DateTime _checkInDate;
        private DateTime _checkOutDate;
        private int? _excludeBookingId;

        private int? _selectedRoomTypeId;
        private int? _selectedFloor;
        private int? _selectedOccupancy;

        public RoomSelectWindow(DateTime checkInDate, DateTime checkOutDate, int? excludeBookingId = null)
        {
            InitializeComponent();

            _checkInDate = checkInDate;
            _checkOutDate = checkOutDate;
            _excludeBookingId = excludeBookingId;

            LoadData();

            Loaded += (s, e) =>
            {
                if (RoomsDataGrid.Items.Count > 0)
                    RoomsDataGrid.Focus();
            };
        }

        private void LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                using (var context = new HotelServiceEntities())
                {
                    // Загрузка фильтров
                    LoadFilters(context);

                    // Получаем номера, которые не заняты в указанный период
                    var bookedRoomIds = context.Booking
                        .Where(b =>
                            ((b.CheckInDate <= _checkOutDate && b.CheckOutDate >= _checkInDate) ||
                             (b.CheckInDate <= _checkOutDate && b.CheckOutDate >= _checkOutDate) ||
                             (b.CheckInDate >= _checkInDate && b.CheckOutDate <= _checkOutDate)) &&
                            b.BookingStatusId != 5 && // Исключаем отмененные бронирования
                            (_excludeBookingId == null || b.BookingId != _excludeBookingId.Value)) // Исключаем текущее бронирование при редактировании
                        .Select(b => b.RoomId)
                        .ToList();

                    _allRooms = context.Room
                        .Include(r => r.RoomType)
                        .Include(r => r.RoomStatus)
                        .Where(r => !bookedRoomIds.Contains(r.RoomId) && r.RoomStatusId == 1) // Статус 1 - свободен
                        .OrderBy(r => r.RoomNumber)
                        .ToList();

                    _roomsView = CollectionViewSource.GetDefaultView(_allRooms);
                    _roomsView.Filter = ApplyFilters;

                    RoomsDataGrid.ItemsSource = _roomsView;

                    UpdateStatusBar();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);

                if (_allRooms == null)
                    _allRooms = new List<Room>();

                if (_roomsView == null)
                {
                    _roomsView = CollectionViewSource.GetDefaultView(_allRooms);
                    RoomsDataGrid.ItemsSource = _roomsView;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private void LoadFilters(HotelServiceEntities context)
        {
            // Загрузка типов номеров для фильтра
            var roomTypes = context.RoomType.OrderBy(t => t.TypeName).ToList();
            var allRoomTypesItem = new ComboBoxItem { Content = "Все типы" };

            RoomTypeFilterComboBox.Items.Clear();
            RoomTypeFilterComboBox.Items.Add(allRoomTypesItem);

            foreach (var roomType in roomTypes)
            {
                RoomTypeFilterComboBox.Items.Add(roomType);
            }

            RoomTypeFilterComboBox.SelectedItem = allRoomTypesItem;

            // Загрузка этажей для фильтра
            var floors = context.Room
                .Select(r => r.FloorNumber)
                .Distinct()
                .OrderBy(f => f)
                .ToList();

            FloorFilterComboBox.Items.Clear();
            FloorFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все этажи" });

            foreach (var floor in floors)
            {
                FloorFilterComboBox.Items.Add(new ComboBoxItem { Content = floor.ToString() });
            }

            FloorFilterComboBox.SelectedIndex = 0;

            // Загрузка вместимости для фильтра
            var occupancies = context.Room
                .Select(r => r.MaxOccupancy)
                .Distinct()
                .OrderBy(o => o)
                .ToList();

            OccupancyFilterComboBox.Items.Clear();
            OccupancyFilterComboBox.Items.Add(new ComboBoxItem { Content = "Любая" });

            foreach (var occupancy in occupancies)
            {
                OccupancyFilterComboBox.Items.Add(new ComboBoxItem { Content = occupancy.ToString() });
            }

            OccupancyFilterComboBox.SelectedIndex = 0;
        }

        private bool ApplyFilters(object item)
        {
            if (!(item is Room room))
                return false;

            bool matchesRoomType = !_selectedRoomTypeId.HasValue || room.RoomTypeId == _selectedRoomTypeId.Value;
            bool matchesFloor = !_selectedFloor.HasValue || room.FloorNumber == _selectedFloor.Value;
            bool matchesOccupancy = !_selectedOccupancy.HasValue || room.MaxOccupancy == _selectedOccupancy.Value;

            return matchesRoomType && matchesFloor && matchesOccupancy;
        }

        private void UpdateStatusBar()
        {
            int count = 0;
            foreach (var item in _roomsView)
            {
                count++;
            }

            StatusTextBlock.Text = $"Доступно номеров: {count}";
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == RoomTypeFilterComboBox)
            {
                if (RoomTypeFilterComboBox.SelectedIndex == 0)
                {
                    _selectedRoomTypeId = null;
                }
                else if (RoomTypeFilterComboBox.SelectedItem is RoomType roomType)
                {
                    _selectedRoomTypeId = roomType.RoomTypeId;
                }
            }
            else if (sender == FloorFilterComboBox)
            {
                if (FloorFilterComboBox.SelectedIndex == 0)
                {
                    _selectedFloor = null;
                }
                else if (FloorFilterComboBox.SelectedItem is ComboBoxItem item)
                {
                    string content = item.Content?.ToString();
                    if (int.TryParse(content, out int floor))
                    {
                        _selectedFloor = floor;
                    }
                }
            }
            else if (sender == OccupancyFilterComboBox)
            {
                if (OccupancyFilterComboBox.SelectedIndex == 0)
                {
                    _selectedOccupancy = null;
                }
                else if (OccupancyFilterComboBox.SelectedItem is ComboBoxItem item)
                {
                    string content = item.Content?.ToString();
                    if (int.TryParse(content, out int occupancy))
                    {
                        _selectedOccupancy = occupancy;
                    }
                }
            }

            if (_roomsView != null)
            {
                _roomsView.Refresh();
                UpdateStatusBar();
            }
        }

        private void RoomsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectButton.IsEnabled = RoomsDataGrid.SelectedItem != null;
        }

        private void RoomsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RoomsDataGrid.SelectedItem != null)
            {
                SelectRoom();
            }
        }

        private void SelectRoom()
        {
            SelectedRoom = RoomsDataGrid.SelectedItem as Room;
            DialogResult = true;
            Close();
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            SelectRoom();
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