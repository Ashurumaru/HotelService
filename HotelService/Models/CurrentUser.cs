namespace SchoolApp.Models
{
    /// <summary>
    /// Static class to store current user information
    /// </summary>
    public static class CurrentUser
    {
        public static int UserId { get; set; }
        public static string FirstName { get; set; }
        public static string LastName { get; set; }
        public static string MiddleName { get; set; }
        public static string Login { get; set; }
        public static string Email { get; set; }
        public static string PhoneNumber { get; set; }
        public static int RoleId { get; set; }

        /// <summary>
        /// Returns the full name of the user in the format "LastName FirstName MiddleName"
        /// </summary>
        public static string FullName
        {
            get
            {
                if (string.IsNullOrEmpty(MiddleName))
                    return $"{LastName} {FirstName}";
                else
                    return $"{LastName} {FirstName} {MiddleName}";
            }
        }

        /// <summary>
        /// Returns the short name of the user in the format "LastName F.M."
        /// </summary>
        public static string ShortName
        {
            get
            {
                if (string.IsNullOrEmpty(FirstName))
                    return LastName;
                else if (string.IsNullOrEmpty(MiddleName))
                    return $"{LastName} {FirstName[0]}.";
                else
                    return $"{LastName} {FirstName[0]}.{MiddleName[0]}.";
            }
        }

        /// <summary>
        /// Check if user has the specified role
        /// </summary>
        /// <param name="roleId">Role ID to check</param>
        /// <returns>True if user has the role, otherwise false</returns>
        public static bool HasRole(int roleId)
        {
            return RoleId == roleId;
        }

        /// <summary>
        /// Check if the current user is an Administrator
        /// </summary>
        public static bool IsAdmin => RoleId == 1;

        /// <summary>
        /// Check if the current user is a Technician
        /// </summary>
        public static bool IsTechnician => RoleId == 2;

        /// <summary>
        /// Check if the current user is a Teacher
        /// </summary>
        public static bool IsTeacher => RoleId == 3;

        /// <summary>
        /// Check if the current user is a Facilities Manager
        /// </summary>
        public static bool IsFacilitiesManager => RoleId == 4;
    }
}