using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;

namespace HotelService.Utils
{
    public class DamageActTemplateGenerator
    {
        public static string GenerateTemplate()
        {
            // Форматирование с правильными отступами, выравниванием и линиями подчеркивания
            string template = @"                                         
                           Гостиница __HotelName__
                           
                           АКТ № __ActNumber__                       
                        о порче имущества гостиницы                    
                    от ""DateDay"" DateMonth DateYear г.

Обнаружено следующее: гр. __GuiltyPersonFIO__                                       
                     (Фамилия, Имя, Отчество)

__DamageDescription__________________________________________________________________________________________________________________

Всего на сумму: __TotalAmountSum__ руб.

Подписи работников гостиницы: ________________________________________                      
                            ________________________________________                              
                            ________________________________________                                  

                                  (подпись лица, причинившего ущерб)
С гр. __GuiltyPersonFIO__ Получено: __TotalAmountSum__ руб.       
     (Фамилия, Имя, Отчество)                  (сумма)

Принял: __ReceiverPositionName__ __ReceiverFIO__ _____________________           
        (Должность, Фамилия, Имя, Отчество)          (подпись)

Испорченные вещи: __DamagedItems__________________________________

получены _____________________________________________________________              
          (Фамилия, Имя, Отчество)                   (подпись)";

            return template;
        }

        /// <summary>
        /// Генерирует содержимое акта о причинении ущерба с заполненными данными
        /// </summary>
        /// <param name="hotelName">Название гостиницы</param>
        /// <param name="actNumber">Номер акта</param>
        /// <param name="actDate">Дата акта</param>
        /// <param name="guiltyPersonFIO">ФИО виновного лица</param>
        /// <param name="damageDescription">Описание повреждения</param>
        /// <param name="totalAmount">Общая сумма ущерба</param>
        /// <param name="receiverPosition">Должность принимающего</param>
        /// <param name="receiverFIO">ФИО принимающего</param>
        /// <param name="damagedItems">Испорченные вещи</param>
        /// <returns>Содержимое документа Word</returns>
        public static string GenerateDamageAct(
            string hotelName,
            string actNumber,
            DateTime actDate,
            string guiltyPersonFIO,
            string damageDescription,
            decimal totalAmount,
            string receiverPosition,
            string receiverFIO,
            string damagedItems)
        {
            try
            {
                string result = GenerateTemplate();

                string[] months = { "января", "февраля", "марта", "апреля", "мая", "июня",
                                 "июля", "августа", "сентября", "октября", "ноября", "декабря" };

                result = result.Replace("__HotelName__", hotelName)
                    .Replace("__ActNumber__", actNumber)
                    .Replace("DateDay", actDate.Day.ToString())
                    .Replace("DateMonth", months[actDate.Month - 1])
                    .Replace("DateYear", actDate.Year.ToString())
                    .Replace("__GuiltyPersonFIO__", guiltyPersonFIO)
                    .Replace("__DamageDescription__", damageDescription)
                    .Replace("__TotalAmountSum__", totalAmount.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")))
                    .Replace("__ReceiverPositionName__", receiverPosition)
                    .Replace("__ReceiverFIO__", receiverFIO)
                    .Replace("__DamagedItems__", damagedItems);

                return result;
            }
            catch (Exception ex)
            {
                // Логирование ошибки можно добавить здесь
                throw new Exception($"Ошибка при генерации акта о повреждении: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Создает и сохраняет документ акта о повреждении
        /// </summary>
        /// <param name="outputPath">Путь для сохранения</param>
        /// <param name="content">Содержимое документа</param>
        public static void SaveActDocument(string outputPath, string content)
        {
            try
            {
                // Используем Encoding.GetEncoding(1251) для корректного отображения кириллицы
                File.WriteAllText(outputPath, content, Encoding.GetEncoding(1251));
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении акта: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Возвращает название месяца в родительном падеже
        /// </summary>
        public static string GetMonthName(int month)
        {
            string[] months = { "января", "февраля", "марта", "апреля", "мая", "июня",
                             "июля", "августа", "сентября", "октября", "ноября", "декабря" };

            if (month >= 1 && month <= 12)
                return months[month - 1];

            return string.Empty;
        }

        /// <summary>
        /// Создает временный файл акта о повреждении
        /// </summary>
        public static string CreateTemporaryActFile(
            string hotelName,
            string actNumber,
            DateTime actDate,
            string guiltyPersonFIO,
            string damageDescription,
            decimal totalAmount,
            string receiverPosition,
            string receiverFIO,
            string damagedItems)
        {
            try
            {
                string content = GenerateDamageAct(
                    hotelName,
                    actNumber,
                    actDate,
                    guiltyPersonFIO,
                    damageDescription,
                    totalAmount,
                    receiverPosition,
                    receiverFIO,
                    damagedItems);

                // Формируем имя файла для сохранения
                string fileName = $"Акт_повреждения_{actNumber}_{actDate:dd.MM.yyyy}.doc";
                string savePath = Path.Combine(Path.GetTempPath(), fileName);

                // Записываем сгенерированный документ во временный файл
                SaveActDocument(savePath, content);

                return savePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при создании временного файла акта: {ex.Message}", ex);
            }
        }
    }
}