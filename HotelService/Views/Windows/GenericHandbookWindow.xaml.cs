using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Data.Entity;
using HotelService.Data;
using System.Reflection;
using System.ComponentModel;
using System.Collections;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HotelService.Views.Windows
{
    public partial class GenericHandbookWindow : Window
    {
        private string _entityType;
        private Type _entityDbType;
        private string _searchText = "";
        private ICollectionView _itemsView;
        private List<object> _allItems;
        private DbContext _context;
        private PropertyInfo _primaryKeyProperty;
        private string _displayNameProperty;
        private List<string> _displayProperties;

        public GenericHandbookWindow(string title, string entityType)
        {
            InitializeComponent();
            WindowTitleTextBlock.Text = title;
            Title = title;
            _entityType = entityType;

            SetupEntityConfig();
            LoadData();
        }

        private void SetupEntityConfig()
        {
            _context = new HotelServiceEntities();

            switch (_entityType)
            {
                case "RoomType":
                    _entityDbType = typeof(RoomType);
                    _primaryKeyProperty = _entityDbType.GetProperty("RoomTypeId");
                    _displayNameProperty = "TypeName";
                    _displayProperties = new List<string> { "TypeName", "Description" };
                    break;
                case "BookingSource":
                    _entityDbType = typeof(BookingSource);
                    _primaryKeyProperty = _entityDbType.GetProperty("SourceId");
                    _displayNameProperty = "SourceName";
                    _displayProperties = new List<string> { "SourceName", "Description" };
                    break;
                case "Position":
                    _entityDbType = typeof(Position);
                    _primaryKeyProperty = _entityDbType.GetProperty("PositionId");
                    _displayNameProperty = "PositionName";
                    _displayProperties = new List<string> { "PositionName", "Description" };
                    break;
                case "AmenityCategory":
                    _entityDbType = typeof(AmenityCategory);
                    _primaryKeyProperty = _entityDbType.GetProperty("CategoryId");
                    _displayNameProperty = "CategoryName";
                    _displayProperties = new List<string> { "CategoryName", "Description" };
                    break;
                case "Amenity":
                    _entityDbType = typeof(Amenity);
                    _primaryKeyProperty = _entityDbType.GetProperty("AmenityId");
                    _displayNameProperty = "AmenityName";
                    _displayProperties = new List<string> { "AmenityName", "Description", "CategoryId" };
                    break;
                case "GuestGroup":
                    _entityDbType = typeof(GuestGroup);
                    _primaryKeyProperty = _entityDbType.GetProperty("GroupId");
                    _displayNameProperty = "GroupName";
                    _displayProperties = new List<string> { "GroupName", "Description" };
                    break;
                case "DamageType":
                    _entityDbType = typeof(DamageType);
                    _primaryKeyProperty = _entityDbType.GetProperty("DamageTypeId");
                    _displayNameProperty = "TypeName";
                    _displayProperties = new List<string> { "TypeName", "Description" };
                    break;
                case "ServiceCategory":
                    _entityDbType = typeof(ServiceCategory);
                    _primaryKeyProperty = _entityDbType.GetProperty("CategoryId");
                    _displayNameProperty = "CategoryName";
                    _displayProperties = new List<string> { "CategoryName", "Description" };
                    break;
                case "Service":
                    _entityDbType = typeof(Service);
                    _primaryKeyProperty = _entityDbType.GetProperty("ServiceId");
                    _displayNameProperty = "ServiceName";
                    _displayProperties = new List<string> { "ServiceName", "CategoryId", "Description", "Price" };
                    break;
                case "PaymentMethod":
                    _entityDbType = typeof(PaymentMethod);
                    _primaryKeyProperty = _entityDbType.GetProperty("PaymentMethodId");
                    _displayNameProperty = "MethodName";
                    _displayProperties = new List<string> { "MethodName", "Description" };
                    break;
                case "DocumentType":
                    _entityDbType = typeof(DocumentType);
                    _primaryKeyProperty = _entityDbType.GetProperty("DocumentTypeId");
                    _displayNameProperty = "TypeName";
                    _displayProperties = new List<string> { "TypeName", "Description" };
                    break;
                case "TransactionType":
                    _entityDbType = typeof(TransactionType);
                    _primaryKeyProperty = _entityDbType.GetProperty("TypeId");
                    _displayNameProperty = "TypeName";
                    _displayProperties = new List<string> { "TypeName", "TypeDescription" };
                    break;
                case "TaskType":
                    _entityDbType = typeof(TaskType);
                    _primaryKeyProperty = _entityDbType.GetProperty("TaskTypeId");
                    _displayNameProperty = "TypeName";
                    _displayProperties = new List<string> { "TypeName", "Description" };
                    break;
                case "LoyaltyTransaction":
                    _entityDbType = typeof(LoyaltyTransaction);
                    _primaryKeyProperty = _entityDbType.GetProperty("TransactionId");
                    _displayNameProperty = "Description";
                    _displayProperties = new List<string> { "GuestId", "TypeId", "Points", "Description", "TransactionDate" };
                    break;
                default:
                    throw new ArgumentException("Неизвестный тип справочника");
            }

            SetupDataGridColumns();
        }

        private void SetupDataGridColumns()
        {
            ItemsDataGrid.Columns.Clear();

            // Don't show the ID column in the DataGrid - remove this section
            // We'll only show readable properties

            foreach (var propName in _displayProperties)
            {
                var prop = _entityDbType.GetProperty(propName);
                if (prop != null)
                {
                    var header = GetDisplayHeaderName(propName);
                    var width = propName == _displayNameProperty ? new DataGridLength(1, DataGridLengthUnitType.Star) : new DataGridLength(150);

                    if (prop.PropertyType == typeof(DateTime) || prop.PropertyType == typeof(DateTime?))
                    {
                        ItemsDataGrid.Columns.Add(new DataGridTextColumn
                        {
                            Header = header,
                            Binding = new Binding(propName) { StringFormat = "dd.MM.yyyy" },
                            Width = width
                        });
                    }
                    else if (prop.PropertyType == typeof(decimal) || prop.PropertyType == typeof(decimal?))
                    {
                        ItemsDataGrid.Columns.Add(new DataGridTextColumn
                        {
                            Header = header,
                            Binding = new Binding(propName) { StringFormat = "{0:N2} ₽" },
                            Width = width
                        });
                    }
                    else if (propName.EndsWith("Id") && propName != _primaryKeyProperty.Name)
                    {
                        // For foreign keys, we'll create a custom binding to the related entity
                        string relatedEntityName = GetRelatedEntityName(propName);
                        if (!string.IsNullOrEmpty(relatedEntityName))
                        {
                            ItemsDataGrid.Columns.Add(new DataGridTextColumn
                            {
                                Header = header,
                                Binding = new Binding(relatedEntityName),
                                Width = width
                            });
                        }
                        else
                        {
                            // Fallback if we can't determine the related entity
                            ItemsDataGrid.Columns.Add(new DataGridTextColumn
                            {
                                Header = header,
                                Binding = new Binding(propName),
                                Width = width
                            });
                        }
                    }
                    else
                    {
                        ItemsDataGrid.Columns.Add(new DataGridTextColumn
                        {
                            Header = header,
                            Binding = new Binding(propName),
                            Width = width
                        });
                    }
                }
            }

            // Add action column for edit and delete buttons
            var actionColumn = new DataGridTemplateColumn
            {
                Header = "Действия",
                Width = new DataGridLength(100)
            };

            var cellTemplate = new DataTemplate();
            var stackPanelFactory = new FrameworkElementFactory(typeof(StackPanel));
            stackPanelFactory.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);
            stackPanelFactory.SetValue(FrameworkElement.HorizontalAlignmentProperty, HorizontalAlignment.Center);

            // Edit button
            var editButtonFactory = new FrameworkElementFactory(typeof(Button));
            editButtonFactory.SetValue(Button.WidthProperty, 32.0);
            editButtonFactory.SetValue(Button.HeightProperty, 32.0);
            editButtonFactory.SetValue(Button.StyleProperty, FindResource("ActionButtonIconStyle"));
            editButtonFactory.SetValue(Button.ToolTipProperty, "Редактировать");
            editButtonFactory.SetValue(Button.MarginProperty, new Thickness(0, 0, 5, 0));
            editButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(EditButton_RowClick));

            var editIconFactory = new FrameworkElementFactory(typeof(Path));
            editIconFactory.SetValue(Path.DataProperty, Geometry.Parse("M12,2 L2,12 L1,15 L4,14 L14,4 L12,2 M12,2 L14,4"));
            editIconFactory.SetValue(Path.StrokeProperty, Brushes.White);
            editIconFactory.SetValue(Path.StrokeThicknessProperty, 1.5);

            editButtonFactory.AppendChild(editIconFactory);
            stackPanelFactory.AppendChild(editButtonFactory);

            // Delete button
            var deleteButtonFactory = new FrameworkElementFactory(typeof(Button));
            deleteButtonFactory.SetValue(Button.WidthProperty, 32.0);
            deleteButtonFactory.SetValue(Button.HeightProperty, 32.0);
            deleteButtonFactory.SetValue(Button.StyleProperty, FindResource("DeleteButtonIconStyle"));
            deleteButtonFactory.SetValue(Button.ToolTipProperty, "Удалить");
            deleteButtonFactory.AddHandler(Button.ClickEvent, new RoutedEventHandler(DeleteButton_RowClick));

            var deleteIconFactory = new FrameworkElementFactory(typeof(Path));
            deleteIconFactory.SetValue(Path.DataProperty, Geometry.Parse("M2,4 H14 M12,4 V14 C12,15 11,16 10,16 H6 C5,16 4,15 4,14 V4 M6,2 H10"));
            deleteIconFactory.SetValue(Path.StrokeProperty, Brushes.White);
            deleteIconFactory.SetValue(Path.StrokeThicknessProperty, 1.5);

            deleteButtonFactory.AppendChild(deleteIconFactory);
            stackPanelFactory.AppendChild(deleteButtonFactory);

            cellTemplate.VisualTree = stackPanelFactory;
            actionColumn.CellTemplate = cellTemplate;

            ItemsDataGrid.Columns.Add(actionColumn);
        }
        private void DeleteItem(object selectedItem)
        {
            try
            {
                using (var context = new HotelServiceEntities())
                {
                    // Use the Set<T> method to get the right DbSet
                    var setMethod = typeof(DbContext).GetMethod("Set").MakeGenericMethod(_entityDbType);
                    var dbSet = setMethod.Invoke(context, null);

                    // Get the ID of the selected item
                    var idValue = _primaryKeyProperty.GetValue(selectedItem);

                    // Find the entity to delete
                    var findMethod = dbSet.GetType().GetMethod("Find", new[] { _primaryKeyProperty.PropertyType });
                    if (findMethod != null)
                    {
                        var entityToDelete = findMethod.Invoke(dbSet, new[] { idValue });

                        if (entityToDelete != null)
                        {
                            // Mark the entity as deleted
                            context.Entry(entityToDelete).State = EntityState.Deleted;
                            context.SaveChanges();

                            MessageBox.Show("Запись успешно удалена.", "Успех",
                                           MessageBoxButton.OK, MessageBoxImage.Information);
                            LoadData();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении записи: {ex.Message}",
                               "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private string GetRelatedEntityName(string foreignKeyProperty)
        {
            // Map of foreign key properties to navigation properties or display properties
            switch (_entityType)
            {
                case "Amenity" when foreignKeyProperty == "CategoryId":
                    return "AmenityCategory.CategoryName";

                case "Service" when foreignKeyProperty == "CategoryId":
                    return "ServiceCategory.CategoryName";

                case "LoyaltyTransaction" when foreignKeyProperty == "GuestId":
                    return "Guest.LastName";

                case "LoyaltyTransaction" when foreignKeyProperty == "TypeId":
                    return "TransactionType.TypeName";

                case "LoyaltyTransaction" when foreignKeyProperty == "BookingId":
                    return "BookingId";  // Just show the ID for now

                default:
                    return ""; // Return empty string if no mapping exists
            }
        }

        private void EditButton_RowClick(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для редактирования записей в справочнике.",
                                "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            var dataContext = button.DataContext;

            if (dataContext != null)
            {
                OpenItemEditWindow(dataContext);
            }
        }

        private void DeleteButton_RowClick(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для удаления записей из справочника.",
                                "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var button = sender as Button;
            var selectedItem = button.DataContext;

            if (selectedItem == null)
                return;

            var displayNameProp = _entityDbType.GetProperty(_displayNameProperty);
            var displayValue = displayNameProp?.GetValue(selectedItem)?.ToString() ?? "выбранную запись";
            var idValue = _primaryKeyProperty.GetValue(selectedItem)?.ToString() ?? "";

            var result = MessageBox.Show($"Вы действительно хотите удалить \"{displayValue}\" (ID: {idValue})?",
                                        "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteItem(selectedItem);
            }
        }
        private string GetDisplayHeaderName(string propertyName)
        {
            switch (propertyName)
            {
                case "TypeName": return "Название";
                case "SourceName": return "Название";
                case "PositionName": return "Название";
                case "CategoryName": return "Название";
                case "AmenityName": return "Название";
                case "GroupName": return "Название";
                case "ServiceName": return "Название";
                case "MethodName": return "Название";
                case "Description": return "Описание";
                case "TypeDescription": return "Описание";
                case "CategoryId": return "Категория";
                case "Price": return "Цена";
                case "GuestId": return "Гость";
                case "TypeId": return "Тип";
                case "Points": return "Баллы";
                case "TransactionDate": return "Дата";
                default: return propertyName;
            }
        }

        private void LoadData()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                // Try pluralized form first, then singular form of the entity name
                var dbSetProperty = _context.GetType().GetProperty(_entityType + "s") ??
                                   _context.GetType().GetProperty(_entityType);

                if (dbSetProperty == null)
                {
                    MessageBox.Show($"Не удалось найти DbSet для типа {_entityType}",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dbSet = dbSetProperty.GetValue(_context);

                // Cast the dbSet to IEnumerable and convert to list
                if (dbSet is IEnumerable<object> enumerable)
                {
                    _allItems = enumerable.ToList();
                }
                else if (dbSet is IEnumerable dbSetEnumerable)
                {
                    // If it's a non-generic IEnumerable, convert each item to object
                    _allItems = dbSetEnumerable.Cast<object>().ToList();
                }

                _itemsView = CollectionViewSource.GetDefaultView(_allItems);
                _itemsView.Filter = ApplyFilters;

                ItemsDataGrid.ItemsSource = _itemsView;
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                if (_allItems == null)
                    _allItems = new List<object>();

                if (_itemsView == null)
                {
                    _itemsView = CollectionViewSource.GetDefaultView(_allItems);
                    ItemsDataGrid.ItemsSource = _itemsView;
                }
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private bool ApplyFilters(object item)
        {
            if (string.IsNullOrWhiteSpace(_searchText))
                return true;

            var searchLower = _searchText.ToLower();
            var displayNameProp = _entityDbType.GetProperty(_displayNameProperty);

            if (displayNameProp != null)
            {
                var displayValue = displayNameProp.GetValue(item)?.ToString();
                if (displayValue != null && displayValue.ToLower().Contains(searchLower))
                    return true;
            }

            foreach (var propName in _displayProperties)
            {
                if (propName == _displayNameProperty)
                    continue;

                var prop = _entityDbType.GetProperty(propName);
                if (prop != null)
                {
                    var value = prop.GetValue(item)?.ToString();
                    if (value != null && value.ToLower().Contains(searchLower))
                        return true;
                }
            }

            return false;
        }

        private void UpdateStatusBar()
        {
            int count = 0;
            foreach (var item in _itemsView)
            {
                count++;
            }

            StatusTextBlock.Text = $"Всего записей: {count}";
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchText = SearchTextBox.Text.Trim().ToLower();
            if (_itemsView != null)
            {
                _itemsView.Refresh();
                UpdateStatusBar();
            }
        }

        private void SearchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && ItemsDataGrid.Items.Count > 0)
            {
                ItemsDataGrid.SelectedIndex = 0;
                ItemsDataGrid.Focus();
            }
        }

        private void ItemsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var isItemSelected = ItemsDataGrid.SelectedItem != null;
            EditButton.IsEnabled = isItemSelected;
            DeleteButton.IsEnabled = isItemSelected;
        }

        private void ItemsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ItemsDataGrid.SelectedItem != null)
            {
                EditSelectedItem();
            }
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для добавления записей в справочник.",
                                "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OpenItemEditWindow(null);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для редактирования записей в справочнике.",
                                "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            EditSelectedItem();
        }

        private void EditSelectedItem()
        {
            var selectedItem = ItemsDataGrid.SelectedItem;
            if (selectedItem != null)
            {
                OpenItemEditWindow(selectedItem);
            }
        }

        private void OpenItemEditWindow(object item)
        {
            try
            {
                var editWindow = new HandbookItemEditWindow(_entityType, item);
                if (editWindow.ShowDialog() == true)
                {
                    LoadData();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии окна редактирования: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.CurrentUser.RoleId != 1)
            {
                MessageBox.Show("У вас нет прав для удаления записей из справочника.",
                                "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedItem = ItemsDataGrid.SelectedItem;
            if (selectedItem == null)
                return;

            var displayNameProp = _entityDbType.GetProperty(_displayNameProperty);
            var displayValue = displayNameProp?.GetValue(selectedItem)?.ToString() ?? "выбранную запись";
            var idValue = _primaryKeyProperty.GetValue(selectedItem)?.ToString() ?? "";

            var result = MessageBox.Show($"Вы действительно хотите удалить \"{displayValue}\" (ID: {idValue})?",
                                        "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var context = new HotelServiceEntities())
                    {
                        var dbSetProperty = context.GetType().GetProperty(_entityType + "s") ??
                                           context.GetType().GetProperty(_entityType);
                        var dbSet = dbSetProperty.GetValue(context);

                        // Get the DbSet directly by type to access its methods properly
                        var dbSetType = typeof(DbSet<>).MakeGenericType(_entityDbType);
                        var findMethod = dbSetType.GetMethod("Find", new[] { _primaryKeyProperty.PropertyType });

                        if (findMethod != null)
                        {
                            var entityToDelete = findMethod.Invoke(dbSet, new[] { _primaryKeyProperty.GetValue(selectedItem) });

                            if (entityToDelete != null)
                            {
                                // Use the standard Entity Framework method to remove
                                context.Entry(entityToDelete).State = EntityState.Deleted;
                                context.SaveChanges();

                                MessageBox.Show("Запись успешно удалена.", "Успех",
                                                MessageBoxButton.OK, MessageBoxImage.Information);
                                LoadData();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении записи: {ex.Message}",
                                   "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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