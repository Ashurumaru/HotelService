using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HotelService.Data;
using System.Reflection;
using System.Linq;
using System.Data.Entity;
using System.Globalization;
using System.Windows.Data;

namespace HotelService.Views.Windows
{
    public partial class HandbookItemEditWindow : Window
    {
        private string _entityType;
        private Type _entityDbType;
        private object _entityToEdit;
        private bool _isNewEntity;
        private Dictionary<string, Control> _formControls = new Dictionary<string, Control>();
        private PropertyInfo _primaryKeyProperty;
        private Dictionary<string, PropertyInfo> _editableProperties = new Dictionary<string, PropertyInfo>();
        private Dictionary<string, List<object>> _referenceData = new Dictionary<string, List<object>>();

        public HandbookItemEditWindow(string entityType, object entity = null)
        {
            InitializeComponent();
            _entityType = entityType;
            _entityToEdit = entity;
            _isNewEntity = entity == null;

            if (_isNewEntity)
            {
                WindowTitleTextBlock.Text = "Добавление новой записи";
            }
            else
            {
                WindowTitleTextBlock.Text = "Редактирование записи";
            }

            SetupEntityConfig();
            LoadReferenceData();
            CreateFormFields();
            LoadEntityData();
        }

        private void SetupEntityConfig()
        {
            switch (_entityType)
            {
                case "RoomType":
                    _entityDbType = typeof(RoomType);
                    _primaryKeyProperty = _entityDbType.GetProperty("RoomTypeId");
                    _editableProperties["TypeName"] = _entityDbType.GetProperty("TypeName");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    break;
                case "BookingSource":
                    _entityDbType = typeof(BookingSource);
                    _primaryKeyProperty = _entityDbType.GetProperty("SourceId");
                    _editableProperties["SourceName"] = _entityDbType.GetProperty("SourceName");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    break;
                case "Position":
                    _entityDbType = typeof(Position);
                    _primaryKeyProperty = _entityDbType.GetProperty("PositionId");
                    _editableProperties["PositionName"] = _entityDbType.GetProperty("PositionName");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    break;
                case "AmenityCategory":
                    _entityDbType = typeof(AmenityCategory);
                    _primaryKeyProperty = _entityDbType.GetProperty("CategoryId");
                    _editableProperties["CategoryName"] = _entityDbType.GetProperty("CategoryName");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    break;
                case "Amenity":
                    _entityDbType = typeof(Amenity);
                    _primaryKeyProperty = _entityDbType.GetProperty("AmenityId");
                    _editableProperties["AmenityName"] = _entityDbType.GetProperty("AmenityName");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    _editableProperties["CategoryId"] = _entityDbType.GetProperty("CategoryId");
                    break;
                case "GuestGroup":
                    _entityDbType = typeof(GuestGroup);
                    _primaryKeyProperty = _entityDbType.GetProperty("GroupId");
                    _editableProperties["GroupName"] = _entityDbType.GetProperty("GroupName");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    break;
                case "DamageType":
                    _entityDbType = typeof(DamageType);
                    _primaryKeyProperty = _entityDbType.GetProperty("DamageTypeId");
                    _editableProperties["TypeName"] = _entityDbType.GetProperty("TypeName");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    break;
                case "ServiceCategory":
                    _entityDbType = typeof(ServiceCategory);
                    _primaryKeyProperty = _entityDbType.GetProperty("CategoryId");
                    _editableProperties["CategoryName"] = _entityDbType.GetProperty("CategoryName");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    break;
                case "Service":
                    _entityDbType = typeof(Service);
                    _primaryKeyProperty = _entityDbType.GetProperty("ServiceId");
                    _editableProperties["ServiceName"] = _entityDbType.GetProperty("ServiceName");
                    _editableProperties["CategoryId"] = _entityDbType.GetProperty("CategoryId");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    _editableProperties["Price"] = _entityDbType.GetProperty("Price");
                    break;
                case "PaymentMethod":
                    _entityDbType = typeof(PaymentMethod);
                    _primaryKeyProperty = _entityDbType.GetProperty("PaymentMethodId");
                    _editableProperties["MethodName"] = _entityDbType.GetProperty("MethodName");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    break;
                case "DocumentType":
                    _entityDbType = typeof(DocumentType);
                    _primaryKeyProperty = _entityDbType.GetProperty("DocumentTypeId");
                    _editableProperties["TypeName"] = _entityDbType.GetProperty("TypeName");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    break;
                case "TransactionType":
                    _entityDbType = typeof(TransactionType);
                    _primaryKeyProperty = _entityDbType.GetProperty("TypeId");
                    _editableProperties["TypeName"] = _entityDbType.GetProperty("TypeName");
                    _editableProperties["TypeDescription"] = _entityDbType.GetProperty("TypeDescription");
                    break;
                case "TaskType":
                    _entityDbType = typeof(TaskType);
                    _primaryKeyProperty = _entityDbType.GetProperty("TaskTypeId");
                    _editableProperties["TypeName"] = _entityDbType.GetProperty("TypeName");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    break;
                case "LoyaltyTransaction":
                    _entityDbType = typeof(LoyaltyTransaction);
                    _primaryKeyProperty = _entityDbType.GetProperty("TransactionId");
                    _editableProperties["GuestId"] = _entityDbType.GetProperty("GuestId");
                    _editableProperties["TypeId"] = _entityDbType.GetProperty("TypeId");
                    _editableProperties["Points"] = _entityDbType.GetProperty("Points");
                    _editableProperties["BookingId"] = _entityDbType.GetProperty("BookingId");
                    _editableProperties["Description"] = _entityDbType.GetProperty("Description");
                    _editableProperties["TransactionDate"] = _entityDbType.GetProperty("TransactionDate");
                    break;
                default:
                    throw new ArgumentException("Неизвестный тип справочника");
            }

            if (_isNewEntity)
            {
                _entityToEdit = Activator.CreateInstance(_entityDbType);
            }
        }

        private void LoadReferenceData()
        {
            try
            {
                using (var context = new HotelServiceEntities())
                {
                    if (_editableProperties.ContainsKey("CategoryId"))
                    {
                        if (_entityType == "Amenity")
                        {
                            _referenceData["CategoryId"] = context.AmenityCategory.ToList<object>();
                        }
                        else if (_entityType == "Service")
                        {
                            _referenceData["CategoryId"] = context.ServiceCategory.ToList<object>();
                        }
                    }

                    if (_editableProperties.ContainsKey("GuestId"))
                    {
                        _referenceData["GuestId"] = context.Guest.ToList<object>();
                    }

                    if (_editableProperties.ContainsKey("TypeId") && _entityType == "LoyaltyTransaction")
                    {
                        _referenceData["TypeId"] = context.TransactionType.ToList<object>();
                    }

                    if (_editableProperties.ContainsKey("BookingId"))
                    {
                        _referenceData["BookingId"] = context.Booking.ToList<object>();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке справочных данных: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateFormFields()
        {
            FormPanel.Children.Clear();
            _formControls.Clear();

            foreach (var propEntry in _editableProperties)
            {
                var propName = propEntry.Key;
                var propInfo = propEntry.Value;
                var displayName = GetDisplayHeaderName(propName);

                var label = new TextBlock
                {
                    Text = displayName,
                    Style = (Style)FindResource("LabelTextStyle"),
                    Margin = new Thickness(0, 10, 0, 5)
                };
                FormPanel.Children.Add(label);

                Control control;

                if (propName.EndsWith("Id") && _referenceData.ContainsKey(propName))
                {
                    var comboBox = new ComboBox
                    {
                        Style = (Style)FindResource("DefaultComboBoxStyle"),
                        Margin = new Thickness(0, 0, 0, 10),
                        Tag = propName
                    };

                    var items = _referenceData[propName];
                    comboBox.ItemsSource = items;

                    switch (propName)
                    {
                        case "CategoryId" when _entityType == "Amenity":
                            comboBox.DisplayMemberPath = "CategoryName";
                            comboBox.SelectedValuePath = "CategoryId";
                            break;
                        case "CategoryId" when _entityType == "Service":
                            comboBox.DisplayMemberPath = "CategoryName";
                            comboBox.SelectedValuePath = "CategoryId";
                            break;
                        case "GuestId":
                            // For guests, use an ItemTemplate to display full name
                            comboBox.SelectedValuePath = "GuestId";

                            // Create a custom display template
                            var dataTemplate = new DataTemplate();
                            var textBlockFactory = new FrameworkElementFactory(typeof(TextBlock));

                            // Create a multi-binding to concatenate LastName, FirstName, and MiddleName
                            var multibinding = new MultiBinding();
                            multibinding.StringFormat = "{0} {1} {2}";

                            multibinding.Bindings.Add(new Binding("LastName"));
                            multibinding.Bindings.Add(new Binding("FirstName"));
                            multibinding.Bindings.Add(new Binding("MiddleName"));

                            textBlockFactory.SetBinding(TextBlock.TextProperty, multibinding);
                            dataTemplate.VisualTree = textBlockFactory;

                            comboBox.ItemTemplate = dataTemplate;
                            break;
                        case "TypeId":
                            comboBox.DisplayMemberPath = "TypeName";
                            comboBox.SelectedValuePath = "TypeId";
                            break;
                        case "BookingId":
                            comboBox.DisplayMemberPath = "BookingId";
                            comboBox.SelectedValuePath = "BookingId";
                            break;
                    }

                    control = comboBox;
                }
                else if (propInfo.PropertyType == typeof(DateTime) || propInfo.PropertyType == typeof(DateTime?))
                {
                    var datePicker = new DatePicker
                    {
                        Style = (Style)TryFindResource("DefaultDatePickerStyle"),
                        Margin = new Thickness(0, 0, 0, 10),
                        Tag = propName
                    };
                    control = datePicker;
                }
                else if (propInfo.PropertyType == typeof(bool) || propInfo.PropertyType == typeof(bool?))
                {
                    var checkBox = new CheckBox
                    {
                        Content = displayName,
                        Margin = new Thickness(0, 0, 0, 10),
                        Tag = propName
                    };
                    control = checkBox;
                }
                else if (propInfo.PropertyType == typeof(decimal) || propInfo.PropertyType == typeof(decimal?))
                {
                    var textBox = new TextBox
                    {
                        Style = (Style)FindResource("DefaultTextBoxStyle"),
                        Margin = new Thickness(0, 0, 0, 10),
                        Tag = propName
                    };
                    textBox.PreviewTextInput += (s, e) =>
                    {
                        e.Handled = !IsValidDecimalInput(e.Text, textBox.Text);
                    };
                    control = textBox;
                }
                else if (propInfo.PropertyType == typeof(int) || propInfo.PropertyType == typeof(int?))
                {
                    var textBox = new TextBox
                    {
                        Style = (Style)FindResource("DefaultTextBoxStyle"),
                        Margin = new Thickness(0, 0, 0, 10),
                        Tag = propName
                    };
                    textBox.PreviewTextInput += (s, e) =>
                    {
                        e.Handled = !IsValidIntegerInput(e.Text);
                    };
                    control = textBox;
                }
                else if (propName == "Description" || propName == "TypeDescription")
                {
                    var textBox = new TextBox
                    {
                        Style = (Style)FindResource("DefaultTextBoxStyle"),
                        TextWrapping = TextWrapping.Wrap,
                        AcceptsReturn = true,
                        Height = 80,
                        VerticalContentAlignment = VerticalAlignment.Top,
                        Margin = new Thickness(0, 0, 0, 10),
                        Tag = propName
                    };
                    control = textBox;
                }
                else
                {
                    var textBox = new TextBox
                    {
                        Style = (Style)FindResource("DefaultTextBoxStyle"),
                        Margin = new Thickness(0, 0, 0, 10),
                        Tag = propName
                    };
                    control = textBox;
                }

                FormPanel.Children.Add(control);
                _formControls[propName] = control;
            }
        }

        private bool IsValidDecimalInput(string text, string currentText)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            string newText = currentText.Insert(((TextBox)_formControls["Price"]).CaretIndex, text);

            if (newText.Count(c => c == ',') > 1 || newText.Count(c => c == '.') > 1)
                return false;

            if (newText.Contains(".") && newText.Contains(","))
                return false;

            if (text == "," || text == ".")
            {
                if (string.IsNullOrEmpty(currentText) || currentText.Contains(",") || currentText.Contains("."))
                    return false;
                return true;
            }

            return int.TryParse(text, out _);
        }

        private bool IsValidIntegerInput(string text)
        {
            return int.TryParse(text, out _);
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
                case "BookingId": return "Бронирование";
                case "TransactionDate": return "Дата";
                default: return propertyName;
            }
        }

        private void LoadEntityData()
        {
            if (_entityToEdit == null)
                return;

            foreach (var propEntry in _editableProperties)
            {
                var propName = propEntry.Key;
                var propInfo = propEntry.Value;
                var value = propInfo.GetValue(_entityToEdit);

                if (!_formControls.TryGetValue(propName, out var control))
                    continue;

                if (control is TextBox textBox)
                {
                    textBox.Text = value?.ToString() ?? "";
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.IsChecked = value != null && (bool)value;
                }
                else if (control is DatePicker datePicker)
                {
                    if (value != null && value is DateTime date && date != DateTime.MinValue)
                    {
                        datePicker.SelectedDate = date;
                    }
                    else
                    {
                        datePicker.SelectedDate = DateTime.Today;
                    }
                }
                else if (control is ComboBox comboBox)
                {
                    if (value != null)
                    {
                        comboBox.SelectedValue = value;
                    }
                    else if (comboBox.Items.Count > 0)
                    {
                        // Select first item as default for new records
                        comboBox.SelectedIndex = 0;
                    }
                }
            }
        }

        private bool ValidateForm()
        {
            var errors = new List<string>();

            foreach (var propEntry in _editableProperties)
            {
                var propName = propEntry.Key;
                var propInfo = propEntry.Value;
                var displayName = GetDisplayHeaderName(propName);

                if (!_formControls.TryGetValue(propName, out var control))
                    continue;

                if (control is TextBox textBox &&
                    propName != "Description" &&
                    propName != "TypeDescription" &&
                    string.IsNullOrWhiteSpace(textBox.Text))
                {
                    errors.Add($"Поле \"{displayName}\" должно быть заполнено.");
                }
                else if (control is ComboBox comboBox && comboBox.SelectedItem == null)
                {
                    errors.Add($"Необходимо выбрать значение для поля \"{displayName}\".");
                }
                else if (control is DatePicker datePicker && !datePicker.SelectedDate.HasValue)
                {
                    errors.Add($"Необходимо выбрать дату для поля \"{displayName}\".");
                }
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

        private void SaveEntity()
        {
            try
            {
                foreach (var propEntry in _editableProperties)
                {
                    var propName = propEntry.Key;
                    var propInfo = propEntry.Value;

                    if (!_formControls.TryGetValue(propName, out var control))
                        continue;

                    object value = null;

                    if (control is TextBox textBox)
                    {
                        if (propInfo.PropertyType == typeof(int) || propInfo.PropertyType == typeof(int?))
                        {
                            if (int.TryParse(textBox.Text, out int intValue))
                                value = intValue;
                        }
                        else if (propInfo.PropertyType == typeof(decimal) || propInfo.PropertyType == typeof(decimal?))
                        {
                            string numText = textBox.Text.Replace(',', '.');
                            if (decimal.TryParse(numText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal decValue))
                                value = decValue;
                        }
                        else
                        {
                            value = textBox.Text;
                        }
                    }
                    else if (control is CheckBox checkBox)
                    {
                        value = checkBox.IsChecked ?? false;
                    }
                    else if (control is DatePicker datePicker)
                    {
                        value = datePicker.SelectedDate ?? DateTime.Today;
                    }
                    else if (control is ComboBox comboBox)
                    {
                        value = comboBox.SelectedValue;
                    }

                    propInfo.SetValue(_entityToEdit, value);
                }

                using (var context = new HotelServiceEntities())
                {
                    if (_isNewEntity)
                    {
                        // For new entities, use the standard context.Set<T>().Add() method
                        var setMethod = typeof(DbContext).GetMethod("Set").MakeGenericMethod(_entityDbType);
                        var dbSet = setMethod.Invoke(context, null);

                        // Get the appropriate Add method
                        var addMethod = dbSet.GetType().GetMethod("Add", new[] { _entityDbType });
                        if (addMethod != null)
                        {
                            addMethod.Invoke(dbSet, new[] { _entityToEdit });
                        }
                        else
                        {
                            // Fallback - attach and set state directly
                            context.Entry(_entityToEdit).State = EntityState.Added;
                        }
                    }
                    else
                    {
                        // For existing entities, just attach and mark as modified
                        context.Entry(_entityToEdit).State = EntityState.Modified;
                    }

                    context.SaveChanges();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                ValidationMessageTextBlock.Text = $"Ошибка при сохранении: {ex.Message}";
                ValidationMessageTextBlock.Visibility = Visibility.Visible;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateForm())
            {
                SaveEntity();
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