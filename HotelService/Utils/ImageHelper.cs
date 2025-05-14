using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace HotelService.Utils
{
    public static class ImageHelper
    {
        public static readonly string DocumentsBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents");
        public static readonly string ImagesBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images");
        public static readonly string DamageReportsPath = Path.Combine(ImagesBasePath, "DamageReports");
        public static readonly string TemplatesPath = Path.Combine(DocumentsBasePath, "Templates");

        static ImageHelper()
        {
            EnsureDirectoriesExist();
        }

        public static void EnsureDirectoriesExist()
        {
            try
            {
                if (!Directory.Exists(DocumentsBasePath))
                    Directory.CreateDirectory(DocumentsBasePath);

                if (!Directory.Exists(ImagesBasePath))
                    Directory.CreateDirectory(ImagesBasePath);

                if (!Directory.Exists(DamageReportsPath))
                    Directory.CreateDirectory(DamageReportsPath);

                if (!Directory.Exists(TemplatesPath))
                    Directory.CreateDirectory(TemplatesPath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при создании директорий: {ex.Message}",
                    "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public static BitmapImage LoadImage(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return null;

            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(filePath);
                image.EndInit();
                return image;
            }
            catch
            {
                return null;
            }
        }

        public static string SaveImageToStorage(string sourceFilePath, string targetDirectory, string fileName = null)
        {
            try
            {
                if (!Directory.Exists(targetDirectory))
                    Directory.CreateDirectory(targetDirectory);

                string targetFileName = fileName ?? Path.GetFileName(sourceFilePath);
                string targetFilePath = Path.Combine(targetDirectory, targetFileName);

                File.Copy(sourceFilePath, targetFilePath, true);
                return targetFilePath;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Ошибка при сохранении изображения: {ex.Message}",
                    "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return null;
            }
        }

        public static string GetDamageReportImagePath(int reportId)
        {
            string reportDirectory = Path.Combine(DamageReportsPath, reportId.ToString());

            if (!Directory.Exists(reportDirectory))
                Directory.CreateDirectory(reportDirectory);

            return reportDirectory;
        }
    }
}