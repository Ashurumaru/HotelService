using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;
using System.ComponentModel;
using System.Windows.Data;

namespace HotelService.Views.Pages
{
    public partial class RoomPage : Page
    {
        private HotelServiceEntities _context;
        private List<Room> _allRooms;
        private ICollectionView _roomsView;

        private string _searchText = "";
        private int? _selectedRoomTypeId = null;
        private int? _selectedRoomStatusId = null;
        private int? _selectedFloor = null;

        public RoomPage()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                _context = new HotelServiceEntities();

                // Load room types for filter
                var roomTypes = _context.RoomType.OrderBy(rt => rt.TypeName).ToList();
                RoomTypeFilterComboBox.Items.Clear();
                RoomTypeFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все типы", IsSelected = true });
                foreach (var roomType in roomTypes)
                {
                    RoomTypeFilterComboBox.Items.Add(roomType);
                }
                RoomTypeFilterComboBox.DisplayMemberPath = "TypeName";
                RoomTypeFilterComboBox.SelectedValuePath = "RoomTypeId";
                RoomTypeFilterComboBox.SelectedIndex = 0;

                // Load room statuses for filter
                var roomStatuses = _context.RoomStatus.OrderBy(rs => rs.StatusName).ToList();
                RoomStatusFilterComboBox.Items.Clear();
                RoomStatusFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все статусы", IsSelected = true });
                foreach (var roomStatus in roomStatuses)
                {
                    RoomStatusFilterComboBox.Items.Add(roomStatus);
                }
                RoomStatusFilterComboBox.DisplayMemberPath = "StatusName";
                RoomStatusFilterComboBox.SelectedValuePath = "RoomStatusId";
                RoomStatusFilterComboBox.SelectedIndex = 0;

                // Load floors for filter
                var floors = _context.Room
                    .Select(r => r.FloorNumber)
                    .Distinct()
                    .OrderBy(f => f)
                    .ToList();

                FloorFilterComboBox.Items.Clear();
                FloorFilterComboBox.Items.Add(new ComboBoxItem { Content = "Все этажи", IsSelected = true });
                foreach (var floor in floors)
                {
                    FloorFilterComboBox.Items.Add(new ComboBoxItem { Content = floor.ToString() });
                }
                FloorFilterComboBox.SelectedIndex = 0;

                // Load rooms
                var roomsQuery = _context.Room
                    .Include(r => r.RoomType)
                    .Include(r => r.RoomStatus)
                    .AsNoTracking();

                _allRooms = roomsQuery.ToList();

                _roomsView = CollectionViewSource.GetDefaultView(_allRooms);
                _roomsView.Filter = ApplyFilters;

                RoomsDataGrid.ItemsSource = _roomsView;

                UpdateStatusBar();
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

        private bool ApplyFilters(object item)
        {
            if (!(item is Room room))
                return false;

            // Room Type filter
            bool matchesRoomType = !_selectedRoomTypeId.HasValue || room.RoomTypeId == _selectedRoomTypeId.Value;

            // Room Status filter
            bool matchesRoomStatus = !_selectedRoomStatusId.HasValue || room.RoomStatusId == _selectedRoomStatusId.Value;

            // Floor filter
            bool matchesFloor = !_selectedFloor.HasValue || room.FloorNumber == _selectedFloor.Value;

            // Search text filter
            bool matchesSearch = string.IsNullOrEmpty(_searchText) ||
                                 room.RoomNumber.ToLower().Contains(_searchText.ToLower()) ||
                                 (room.Comments != null && room.Comments.ToLower().Contains(_searchText.ToLower()));

            return matchesRoomType && matchesRoomStatus && matchesFloor && matchesSearch;
        }

        private void UpdateStatusBar()
        {
           
        }

        private void RoomTypeFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                if (comboBox.SelectedIndex == 0)
                {
                    _selectedRoomTypeId = null;
                }
                else if (comboBox.SelectedItem is RoomType roomType)
                {
                    _selectedRoomTypeId = roomType.RoomTypeId;
                }

                if (_roomsView != null)
                {
                    _roomsView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void RoomStatusFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                if (comboBox.SelectedIndex == 0)
                {
                    _selectedRoomStatusId = null;
                }
                else if (comboBox.SelectedItem is RoomStatus roomStatus)
                {
                    _selectedRoomStatusId = roomStatus.RoomStatusId;
                }

                if (_roomsView != null)
                {
                    _roomsView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void FloorFilterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var comboBox = sender as ComboBox;
            if (comboBox?.SelectedItem != null)
            {
                if (comboBox.SelectedIndex == 0)
                {
                    _selectedFloor = null;
                }
                else if (comboBox.SelectedItem is ComboBoxItem item)
                {
                    string content = item.Content?.ToString();
                    if (int.TryParse(content, out int floor))
                    {
                        _selectedFloor = floor;
                    }
                }

                if (_roomsView != null)
                {
                    _roomsView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void SearchRoomTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchRoomTextBox.Text?.Trim();

            if (searchText != null && (searchText.Length >= 2 || searchText.Length == 0))
            {
                _searchText = searchText;

                if (_roomsView != null)
                {
                    _roomsView.Refresh();
                    UpdateStatusBar();
                }
            }
        }

        private void AddRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для добавления новых номеров.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Windows.RoomEditWindow();
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void EditRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для редактирования номеров.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var room = (sender as Button)?.DataContext as Room;
            if (room == null) return;

            var dialog = new Windows.RoomEditWindow(room.RoomId);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void ViewRoomButton_Click(object sender, RoutedEventArgs e)
        {
            var room = (sender as Button)?.DataContext as Room;
            if (room == null) return;

            var dialog = new Windows.RoomViewWindow(room.RoomId);
            dialog.ShowDialog();
        }

        private void DeleteRoomButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для удаления номеров.", "Доступ запрещен",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var room = (sender as Button)?.DataContext as Room;
            if (room == null) return;

            // Check if the room is currently booked
            using (var context = new HotelServiceEntities())
            {
                var hasActiveBookings = context.Booking
                    .Any(b => b.RoomId == room.RoomId &&
                             (b.BookingStatusId == 1 || b.BookingStatusId == 2 || b.BookingStatusId == 3));

                if (hasActiveBookings)
                {
                    MessageBox.Show("Невозможно удалить номер, так как он используется в активных бронированиях.",
                        "Ошибка удаления", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            MessageBoxResult result = MessageBox.Show(
                $"Вы действительно хотите удалить номер {room.RoomNumber}?",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        var roomToDelete = context.Room.Find(room.RoomId);
                        if (roomToDelete != null)
                        {
                            // First delete any related data
                            var roomAmenities = context.RoomAmenity.Where(ra => ra.RoomId == room.RoomId);
                            context.RoomAmenity.RemoveRange(roomAmenities);

                            var roomImages = context.RoomImage.Where(ri => ri.RoomId == room.RoomId);
                            context.RoomImage.RemoveRange(roomImages);

                            var roomMaintenance = context.RoomMaintenance.Where(rm => rm.RoomId == room.RoomId);
                            context.RoomMaintenance.RemoveRange(roomMaintenance);

                            // Then delete the room
                            context.Room.Remove(roomToDelete);
                            context.SaveChanges();

                            MessageBox.Show("Номер успешно удален.", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadData();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении номера: {ex.Message}", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void AddMaintenanceButton_Click(object sender, RoutedEventArgs e)
        {
            var room = (sender as Button)?.DataContext as Room;
            if (room == null) return;

            var dialog = new Windows.MaintenanceTaskWindow(roomId: room.RoomId);
            if (dialog.ShowDialog() == true)
            {
                LoadData();
            }
        }

        private void MaintenanceTasksButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Windows.MaintenanceTasksWindow();
            dialog.ShowDialog();
        }

        private void RoomsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (RoomsDataGrid.SelectedItem != null)
            {
                var room = RoomsDataGrid.SelectedItem as Room;
                if (room != null)
                {
                    var dialog = new Windows.RoomViewWindow(room.RoomId);
                    dialog.ShowDialog();
                }
            }
        }

        private void RoomsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Additional logic if needed when a row is selected
        }
    }
}