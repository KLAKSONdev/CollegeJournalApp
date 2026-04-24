using Xunit;
using CollegeJournalApp.Helpers;

namespace CollegeJournalApp.Tests
{
    /// <summary>
    /// Тесты SessionHelper — чистая бизнес-логика, база данных не нужна.
    /// </summary>
    public class SessionHelperTests
    {
        public SessionHelperTests()
        {
            // Очищаем сессию перед каждым тестом
            SessionHelper.Clear();
        }

        // ──────────────────────────────────────────────────────
        //  Флаги ролей
        // ──────────────────────────────────────────────────────

        [Fact]
        public void IsAdmin_WhenRoleIsAdmin_ReturnsTrue()
        {
            SessionHelper.RoleName = "Admin";
            Assert.True(SessionHelper.IsAdmin);
        }

        [Fact]
        public void IsAdmin_WhenRoleIsNotAdmin_ReturnsFalse()
        {
            SessionHelper.RoleName = "Curator";
            Assert.False(SessionHelper.IsAdmin);
        }

        [Fact]
        public void IsCurator_WhenRoleIsCurator_ReturnsTrue()
        {
            SessionHelper.RoleName = "Curator";
            Assert.True(SessionHelper.IsCurator);
        }

        [Fact]
        public void IsHeadman_WhenRoleIsHeadman_ReturnsTrue()
        {
            SessionHelper.RoleName = "Headman";
            Assert.True(SessionHelper.IsHeadman);
        }

        [Fact]
        public void IsStudent_WhenRoleIsStudent_ReturnsTrue()
        {
            SessionHelper.RoleName = "Student";
            Assert.True(SessionHelper.IsStudent);
        }

        [Fact]
        public void IsTeacher_WhenRoleIsTeacher_ReturnsTrue()
        {
            SessionHelper.RoleName = "Teacher";
            Assert.True(SessionHelper.IsTeacher);
        }

        [Fact]
        public void AllRoleFlags_WhenRoleIsNull_ReturnFalse()
        {
            SessionHelper.RoleName = null;
            Assert.False(SessionHelper.IsAdmin);
            Assert.False(SessionHelper.IsCurator);
            Assert.False(SessionHelper.IsHeadman);
            Assert.False(SessionHelper.IsStudent);
            Assert.False(SessionHelper.IsTeacher);
        }

        // ──────────────────────────────────────────────────────
        //  Clear()
        // ──────────────────────────────────────────────────────

        [Fact]
        public void Clear_ResetsAllPropertiesToDefault()
        {
            SessionHelper.UserId    = 42;
            SessionHelper.Login     = "ivanov";
            SessionHelper.FullName  = "Иванов Иван";
            SessionHelper.RoleName  = "Admin";
            SessionHelper.LastName  = "Иванов";
            SessionHelper.FirstName = "Иван";

            SessionHelper.Clear();

            Assert.Equal(0,    SessionHelper.UserId);
            Assert.Null(SessionHelper.Login);
            Assert.Null(SessionHelper.FullName);
            Assert.Null(SessionHelper.RoleName);
            Assert.Null(SessionHelper.LastName);
            Assert.Null(SessionHelper.FirstName);
        }
    }
}
