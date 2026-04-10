namespace CollegeJournalApp.Helpers
{
    public static class SessionHelper
    {
        public static int    UserId    { get; set; }
        public static string Login     { get; set; }
        public static string FullName  { get; set; }
        public static string RoleName  { get; set; }
        public static string LastName  { get; set; }
        public static string FirstName { get; set; }

        public static bool IsAdmin   => RoleName == "Admin";
        public static bool IsCurator => RoleName == "Curator";
        public static bool IsHeadman => RoleName == "Headman";
        public static bool IsStudent => RoleName == "Student";

        public static void Clear()
        {
            UserId = 0; Login = null; FullName = null;
            RoleName = null; LastName = null; FirstName = null;
        }
    }
}
