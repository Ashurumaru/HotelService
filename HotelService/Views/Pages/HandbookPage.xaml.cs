using System;
using System.Windows;
using System.Windows.Controls;

namespace HotelService.Views.Pages
{
    public partial class HandbookPage : Page
    {
        public HandbookPage()
        {
            InitializeComponent();
        }

        private void RoomTypeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Типы номеров", "RoomType");
        }

        private void BookingSourceButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Источники бронирования", "BookingSource");
        }

        private void PositionButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Должности", "Position");
        }

        private void AmenityCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Категории удобств", "AmenityCategory");
        }

        private void AmenityButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Удобства", "Amenity");
        }

        private void GuestGroupButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Группы гостей", "GuestGroup");
        }

        private void DamageTypeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Типы повреждений", "DamageType");
        }

        private void ServiceCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Категории услуг", "ServiceCategory");
        }

        private void ServiceButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Услуги", "Service");
        }

        private void PaymentMethodButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Способы оплаты", "PaymentMethod");
        }

        private void DocumentTypeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Типы документов", "DocumentType");
        }

        private void TransactionTypeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Типы транзакций", "TransactionType");
        }

        private void TaskTypeButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Типы задач", "TaskType");
        }

        private void LoyaltyTransactionButton_Click(object sender, RoutedEventArgs e)
        {
            OpenHandbookWindow("Транзакции лояльности", "LoyaltyTransaction");
        }

        private void OpenHandbookWindow(string title, string entityType)
        {
            try
            {
                var handbookWindow = new Views.Windows.GenericHandbookWindow(title, entityType);
                handbookWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии справочника: {ex.Message}",
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}