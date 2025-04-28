using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace HotelService
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Data.User CurrentUser { get; set; }

        public class User
        {
            public int UserId { get; set; }
            public string Username { get; set; }
            public string Password { get; set; } 
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public int RoleId { get; set; }
            public int? PositionId { get; set; }

            public string FullName
            {
                get
                {
                    if (string.IsNullOrEmpty(MiddleName))
                        return $"{LastName} {FirstName}";
                    else
                        return $"{LastName} {FirstName} {MiddleName}";
                }
            }
        }
    }
}