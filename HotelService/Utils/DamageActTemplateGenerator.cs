using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Globalization;
using Xceed.Words.NET;
using Xceed.Document.NET;

namespace HotelService.Utils
{
    public class DamageActTemplateGenerator
    {
        public static void GenerateDamageActDocx(
            string filePath,
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
                using (var doc = DocX.Create(filePath))
                {
                    SetupDocument(doc);

                    var headerTitle = doc.InsertParagraph()
                        .Append("АКТ О ПОРЧЕ ИМУЩЕСТВА ГОСТИНИЦЫ (Форма № 9-Г)")
                        .Font("Times New Roman")
                        .FontSize(14)
                        .Bold();
                    headerTitle.Alignment = Alignment.center;
                    headerTitle.SpacingAfter(20);

                    var approvalInfo = doc.InsertParagraph()
                        .Append("Утверждена")
                        .Font("Times New Roman")
                        .FontSize(10);
                    approvalInfo.Alignment = Alignment.right;

                    var approvalDetails1 = doc.InsertParagraph()
                        .Append("Приказом Министерства финансов")
                        .Font("Times New Roman")
                        .FontSize(10);
                    approvalDetails1.Alignment = Alignment.right;

                    var approvalDetails2 = doc.InsertParagraph()
                        .Append("Российской Федерации")
                        .Font("Times New Roman")
                        .FontSize(10);
                    approvalDetails2.Alignment = Alignment.right;

                    var approvalDetails3 = doc.InsertParagraph()
                        .Append("от 13 декабря 1993 г. N 121")
                        .Font("Times New Roman")
                        .FontSize(10);
                    approvalDetails3.Alignment = Alignment.right;
                    approvalDetails3.SpacingAfter(5);

                    var formNumber = doc.InsertParagraph()
                        .Append("Форма N 9-Г")
                        .Font("Times New Roman")
                        .FontSize(10);
                    formNumber.Alignment = Alignment.right;
                    formNumber.SpacingAfter(20);

                    var hotelLinePara = doc.InsertParagraph()
                        .Font("Times New Roman")
                        .FontSize(12);
                    hotelLinePara.Append("Гостиница ");
                    hotelLinePara.Append(hotelName).UnderlineStyle(UnderlineStyle.singleLine);
                    hotelLinePara.SpacingAfter(20);

                    var actNumberLine = doc.InsertParagraph()
                        .Append("000000")
                        .Font("Times New Roman")
                        .FontSize(12);
                    actNumberLine.Alignment = Alignment.center;
                    actNumberLine.SpacingAfter(10);

                    var actHeaderPara = doc.InsertParagraph()
                        .Font("Times New Roman")
                        .FontSize(12);
                    actHeaderPara.Append("АКТ N ");
                    actHeaderPara.Append($" {actNumber} ").UnderlineStyle(UnderlineStyle.singleLine);
                    actHeaderPara.Alignment = Alignment.center;

                    var actSubheader = doc.InsertParagraph()
                        .Append("о порче имущества гостиницы")
                        .Font("Times New Roman")
                        .FontSize(12);
                    actSubheader.Alignment = Alignment.center;

                    var actDatePara = doc.InsertParagraph()
                        .Font("Times New Roman")
                        .FontSize(12);
                    actDatePara.Append("от \"");
                    actDatePara.Append($"{actDate.Day}").UnderlineStyle(UnderlineStyle.singleLine);
                    actDatePara.Append("\" ");
                    actDatePara.Append(GetMonthName(actDate.Month)).UnderlineStyle(UnderlineStyle.singleLine);
                    actDatePara.Append(" ");
                    actDatePara.Append($"{actDate.Year}").UnderlineStyle(UnderlineStyle.singleLine);
                    actDatePara.Append(" г.");
                    actDatePara.Alignment = Alignment.center;
                    actDatePara.SpacingAfter(20);

                    var discoveredPara = doc.InsertParagraph()
                        .Font("Times New Roman")
                        .FontSize(12);
                    discoveredPara.Append("Обнаружено следующее: Гр. ");
                    discoveredPara.Append($" {guiltyPersonFIO} ").UnderlineStyle(UnderlineStyle.singleLine);
                    discoveredPara.SpacingAfter(5);

                    var fioLabel = doc.InsertParagraph()
                        .Append("(Фамилия, имя, отчество)")
                        .Font("Times New Roman")
                        .FontSize(10);
                    fioLabel.Alignment = Alignment.center;
                    fioLabel.SpacingAfter(15);

                    var descriptionLine1 = doc.InsertParagraph()
                        .Append("_______________________________________________________________")
                        .Font("Times New Roman")
                        .FontSize(12);

                    var descriptionPara = doc.InsertParagraph()
                        .Font("Times New Roman")
                        .FontSize(12);
                    if (!string.IsNullOrEmpty(damageDescription))
                    {
                        descriptionPara.Append(damageDescription).UnderlineStyle(UnderlineStyle.singleLine);
                    }
                    else
                    {
                        descriptionPara.Append("_______________________________________________________________");
                    }

                    var descriptionLine2 = doc.InsertParagraph()
                        .Append("_______________________________________________________________")
                        .Font("Times New Roman")
                        .FontSize(12);

                    var descriptionLine3 = doc.InsertParagraph()
                        .Append("_______________________________________________________________")
                        .Font("Times New Roman")
                        .FontSize(12);
                    descriptionLine3.SpacingAfter(15);

                    var totalAmountPara = doc.InsertParagraph()
                        .Font("Times New Roman")
                        .FontSize(12);
                    totalAmountPara.Append("Всего на сумму: ");
                    totalAmountPara.Append($" {totalAmount:N2} руб. ").UnderlineStyle(UnderlineStyle.singleLine);
                    totalAmountPara.SpacingAfter(20);

                    var signaturesTitle = doc.InsertParagraph()
                        .Append("Подписи работников гостиницы:")
                        .Font("Times New Roman")
                        .FontSize(12);
                    signaturesTitle.SpacingAfter(10);

                    for (int i = 0; i < 3; i++)
                    {
                        doc.InsertParagraph()
                            .Append("_____________________________________________")
                            .Font("Times New Roman")
                            .FontSize(12);
                    }

                    var guestSignatureLabel = doc.InsertParagraph()
                        .Append("(подпись лица, причинившего ущерб)")
                        .Font("Times New Roman")
                        .FontSize(10);
                    guestSignatureLabel.Alignment = Alignment.center;
                    guestSignatureLabel.SpacingAfter(15);

                    var receivedFromPara = doc.InsertParagraph()
                        .Font("Times New Roman")
                        .FontSize(12);
                    receivedFromPara.Append("С гр. ");
                    receivedFromPara.Append($" {guiltyPersonFIO} ").UnderlineStyle(UnderlineStyle.singleLine);
                    receivedFromPara.Append(" Получено: ");
                    receivedFromPara.Append($" {totalAmount:N2} руб. ").UnderlineStyle(UnderlineStyle.singleLine);
                    receivedFromPara.SpacingAfter(5);

                    var receivedLabels = doc.InsertParagraph()
                        .Append("(фамилия, имя, отчество)                    (сумма прописью)")
                        .Font("Times New Roman")
                        .FontSize(10);
                    receivedLabels.SpacingAfter(15);

                    var acceptedByPara = doc.InsertParagraph()
                        .Font("Times New Roman")
                        .FontSize(12);
                    acceptedByPara.Append("Принял: ");
                    acceptedByPara.Append($" {receiverPosition} ").UnderlineStyle(UnderlineStyle.singleLine);
                    acceptedByPara.Append(" ");
                    acceptedByPara.Append($" {receiverFIO} ").UnderlineStyle(UnderlineStyle.singleLine);
                    acceptedByPara.Append(" ");
                    acceptedByPara.Append("______________________").UnderlineStyle(UnderlineStyle.singleLine);
                    acceptedByPara.SpacingAfter(5);

                    var acceptedLabels = doc.InsertParagraph()
                        .Append("(должность, фамилия, имя, отчество)              (подпись)")
                        .Font("Times New Roman")
                        .FontSize(10);
                    acceptedLabels.SpacingAfter(20);

                    var damagedItemsPara = doc.InsertParagraph()
                        .Font("Times New Roman")
                        .FontSize(12);
                    damagedItemsPara.Append("Испорченные вещи ");
                    if (!string.IsNullOrEmpty(damagedItems))
                    {
                        damagedItemsPara.Append($" {damagedItems} ").UnderlineStyle(UnderlineStyle.singleLine);
                    }
                    else
                    {
                        damagedItemsPara.Append("_________________________________").UnderlineStyle(UnderlineStyle.singleLine);
                    }
                    damagedItemsPara.SpacingAfter(5);

                    var itemsReceivedPara = doc.InsertParagraph()
                        .Font("Times New Roman")
                        .FontSize(12);
                    itemsReceivedPara.Append("получены ");
                    itemsReceivedPara.Append("_______________________________________________").UnderlineStyle(UnderlineStyle.singleLine);
                    itemsReceivedPara.SpacingAfter(5);

                    var itemsLabels = doc.InsertParagraph()
                        .Append("(фамилия, И.О.)                           (подпись)")
                        .Font("Times New Roman")
                        .FontSize(10);

                    doc.Save();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при генерации акта о повреждении: {ex.Message}", ex);
            }
        }

        private static void SetupDocument(DocX doc)
        {
            doc.MarginLeft = 72f;
            doc.MarginRight = 72f;
            doc.MarginTop = 72f;
            doc.MarginBottom = 72f;
        }

        public static string CreateDamageActDocx(
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
                string fileName = $"Акт_повреждения_{actNumber}_{actDate:dd.MM.yyyy}.docx";
                string savePath = Path.Combine(Path.GetTempPath(), fileName);

                GenerateDamageActDocx(
                    savePath,
                    hotelName,
                    actNumber,
                    actDate,
                    guiltyPersonFIO,
                    damageDescription,
                    totalAmount,
                    receiverPosition,
                    receiverFIO,
                    damagedItems);

                return savePath;
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при создании временного файла акта: {ex.Message}", ex);
            }
        }

        public static string GetMonthName(int month)
        {
            string[] months = { "января", "февраля", "марта", "апреля", "мая", "июня",
                             "июля", "августа", "сентября", "октября", "ноября", "декабря" };

            if (month >= 1 && month <= 12)
                return months[month - 1];

            return string.Empty;
        }

        [Obsolete("Используйте CreateDamageActDocx для создания документов Word")]
        public static string GenerateTemplate()
        {
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

        [Obsolete("Используйте CreateDamageActDocx для создания документов Word")]
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
                throw new Exception($"Ошибка при генерации акта о повреждении: {ex.Message}", ex);
            }
        }

        [Obsolete("Используйте CreateDamageActDocx для создания документов Word")]
        public static void SaveActDocument(string outputPath, string content)
        {
            try
            {
                File.WriteAllText(outputPath, content, Encoding.GetEncoding(1251));
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка при сохранении акта: {ex.Message}", ex);
            }
        }

        [Obsolete("Используйте CreateDamageActDocx для создания документов Word")]
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

                string fileName = $"Акт_повреждения_{actNumber}_{actDate:dd.MM.yyyy}.doc";
                string savePath = Path.Combine(Path.GetTempPath(), fileName);

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