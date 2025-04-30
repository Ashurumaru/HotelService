using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;

namespace HotelService.Views.Windows
{
    public partial class RoomSelectWindow : Window
    {
        private HotelServiceEntities _context;
        private Room _selectedRoom;
        private DateTime _checkInDate;
        private DateTime _checkOutDate;
        private int _adultsCount;
        private int? _currentRoomId;

        public Room SelectedRoom
        {
            get { return _selectedRoom; }
        }

        public RoomSelectWindow(DateTime checkInDate, DateTime checkOutDate, int adultsCount, int? currentRoomId = null)
        {
            InitializeComponent();
            _checkInDate = checkInDate.Date;
            _checkOutDate = checkOutDate.Date;
            _adultsCount = adultsCount;
            _currentRoomId = currentRoomId;

            LoadFilterData();
            LoadRooms();
        }

        private void LoadFilterData()
        {
            try
            {
                _context = new HotelServiceEntities();

                // Загрузка типов номеров
                var roomTypes = _context.RoomType.ToList();
                RoomTypeFilterComboBox.ItemsSource = roomTypes;

                // Добавление пустого элемента "Все типы"
                RoomTypeFilterComboBox.SelectedIndex = -1;

                // Загрузка этажей
                var floors = _context.Room
                    .Select(r => r.FloorNumber)
                    .Distinct()
                    .OrderBy(f => f)
                    .ToList();

                var floorItems = new List<ComboBoxItem>
                {
                    new ComboBoxItem { Content = "Все этажи", Tag = null }
                };

                foreach (var floor in floors)
                {
                    floorItems.Add(new ComboBoxItem { Content = floor.ToString(), Tag = floor });
                }

                FloorFilterComboBox.ItemsSource = floorItems;
                FloorFilterComboBox.SelectedIndex = 0;

                // Загрузка вариантов вместимости
                var occupancyItems = new List<ComboBoxItem>
                {
                    new ComboBoxItem { Content = "Любая", Tag = 0 },
                    new ComboBoxItem { Content = "1 человек", Tag = 1 },
                    new ComboBoxItem { Content = "2 человека", Tag = 2 },
                    new ComboBoxItem { Content = "3 человека", Tag = 3 },
                    new ComboBoxItem { Content = "4 и более", Tag = 4 }
                };

                OccupancyFilterComboBox.ItemsSource = occupancyItems;
                OccupancyFilterComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных для фильтров: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadRooms()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                _context = new HotelServiceEntities();

                // Находим номера, которые не заняты в выбранные даты
                var occupiedRoomIds = _context.Booking
                    .Where(b => b.BookingId != (_currentRoomId.HasValue ? 0 : -1)) // Исключаем текущее бронирование при редактировании
                    .Where(b => b.BookingStatusId != 5) // Исключаем отмененные бронирования
                    .Where(b => (_checkInDate < b.CheckOutDate && _checkOutDate > b.CheckInDate)) // Проверка на перекрытие дат
                    .Select(b => b.RoomId)
                    .ToList();

                var roomsQuery = _context.Room
                    .Include(r => r.RoomType)
                    .Include(r => r.RoomStatus)
                    .Where(r => r.RoomStatusId == 1) // Только свободные номера
                    .Where(r => r.MaxOccupancy >= _adultsCount) // С достаточной вместимостью
                    .Where(r => !occupiedRoomIds.Contains(r.RoomId)) // Не занятые в выбранные даты
                    .AsQueryable();

                // Применяем фильтры
                ApplyFilters(ref roomsQuery);

                var rooms = roomsQuery.OrderBy(r => r.RoomNumber).ToList();

                // Добавляем текущий номер в список, если редактируем бронирование
                if (_currentRoomId.HasValue)
                {
                    var currentRoom = _context.Room
                        .Include(r => r.RoomType)
                        .Include(r => r.RoomStatus)
                        .FirstOrDefault(r => r.RoomId == _currentRoomId.Value);

                    if (currentRoom != null && !rooms.Any(r => r.RoomId == currentRoom.RoomId))
                    {
                        rooms.Insert(0, currentRoom);
                    }
                }

                RoomsDataGrid.ItemsSource = rooms;
                StatusTextBlock.Text = $"Доступно номеров: {rooms.Count}";
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

        private void ApplyFilters(ref IQueryable<Room> query)
        {
            // Фильтр по типу номера
            if (RoomTypeFilterComboBox.SelectedValue != null)
            {
                int roomTypeId = (int)RoomTypeFilterComboBox.SelectedValue;
                query = query.Where(r => r.RoomTypeId == roomTypeId);
            }

            // Фильтр по этажу
            if (FloorFilterComboBox.SelectedIndex > 0)
            {
                var selectedItem = FloorFilterComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem?.Tag != null)
                {
                    int floor = (int)selectedItem.Tag;
                    query = query.Where(r => r.FloorNumber == floor);
                }
            }

            // Фильтр по вместимости
            if (OccupancyFilterComboBox.SelectedIndex > 0)
            {
                var selectedItem = OccupancyFilterComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem?.Tag != null)
                {
                    int occupancy = (int)selectedItem.Tag;
                    if (occupancy == 4)
                    {
                        query = query.Where(r => r.MaxOccupancy >= 4);
                    }
                    else
                    {
                        query = query.Where(r => r.MaxOccupancy == occupancy);
                    }
                }
            }
        }

        private void FilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadRooms();
        }

        private void RoomsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedRoom = RoomsDataGrid.SelectedItem as Room;
            SelectButton.IsEnabled = (_selectedRoom != null);
        }

        private void RoomsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RoomsDataGrid.SelectedItem != null)
            {
                _selectedRoom = RoomsDataGrid.SelectedItem as Room;
                this.DialogResult = true;
                this.Close();
            }
        }

        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedRoom != null)
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
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
            this.DialogResult = false;
            this.Close();
        }
    }
}