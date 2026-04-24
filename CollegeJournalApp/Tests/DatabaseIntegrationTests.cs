using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;
using CollegeJournalApp.Database;

namespace CollegeJournalApp.Tests
{
    /// <summary>
    /// Интеграционные тесты против реальной (localdb)\MSSQLLocalDB.
    /// Все тесты только читают данные — БД не изменяется.
    /// </summary>
    [Collection("Database")]
    public class DatabaseIntegrationTests
    {
        private const string ConnStr =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=CollegeJournal;" +
            "Integrated Security=True;Connect Timeout=30;TrustServerCertificate=True;";

        // ──────────────────────────────────────────────────────
        //  Подключение
        // ──────────────────────────────────────────────────────

        [Fact]
        public void TestConnection_Returns_True()
        {
            Assert.True(DatabaseHelper.TestConnection(),
                "TestConnection() вернул false — база CollegeJournal недоступна.");
        }

        [Fact]
        public void GetConnection_Opens_Successfully()
        {
            using (var conn = DatabaseHelper.GetConnection())
            {
                Assert.Equal(ConnectionState.Open, conn.State);
            }
        }

        // ──────────────────────────────────────────────────────
        //  sp_GetAcademicYears
        // ──────────────────────────────────────────────────────

        [Fact]
        public void sp_GetAcademicYears_ReturnsDataTable()
        {
            var dt = ExecSp("sp_GetAcademicYears");
            Assert.NotNull(dt);
        }

        [Fact]
        public void sp_GetAcademicYears_HasExpectedColumns()
        {
            var dt = ExecSp("sp_GetAcademicYears");
            Assert.True(dt.Columns.Contains("YearId"),  "Нет колонки YearId");
            Assert.True(dt.Columns.Contains("Title"),   "Нет колонки Title");
        }

        // ──────────────────────────────────────────────────────
        //  sp_GetAllGroups
        // ──────────────────────────────────────────────────────

        [Fact]
        public void sp_GetAllGroups_ReturnsDataTable()
        {
            var dt = ExecSp("sp_GetAllGroups");
            Assert.NotNull(dt);
        }

        [Fact]
        public void sp_GetAllGroups_HasGroupNameColumn()
        {
            var dt = ExecSp("sp_GetAllGroups");
            Assert.True(dt.Columns.Contains("GroupName"), "Нет колонки GroupName");
        }

        // ──────────────────────────────────────────────────────
        //  sp_GetAdminAttendanceToday
        // ──────────────────────────────────────────────────────

        [Fact]
        public void sp_GetAdminAttendanceToday_ReturnsDataTable()
        {
            var dt = ExecSp("sp_GetAdminAttendanceToday");
            Assert.NotNull(dt);
        }

        [Fact]
        public void sp_GetAdminAttendanceToday_HasAllCountColumns()
        {
            var dt = ExecSp("sp_GetAdminAttendanceToday");
            Assert.True(dt.Columns.Contains("GroupName"),     "Нет колонки GroupName");
            Assert.True(dt.Columns.Contains("TotalStudents"), "Нет колонки TotalStudents");
            Assert.True(dt.Columns.Contains("PresentCount"),  "Нет колонки PresentCount");
            Assert.True(dt.Columns.Contains("AbsentCount"),   "Нет колонки AbsentCount");
            Assert.True(dt.Columns.Contains("LateCount"),     "Нет колонки LateCount");
        }

        [Fact]
        public void sp_GetAdminAttendanceToday_CountsAreNonNegative()
        {
            var dt = ExecSp("sp_GetAdminAttendanceToday");
            foreach (DataRow row in dt.Rows)
            {
                Assert.True((int)row["TotalStudents"] >= 0);
                Assert.True((int)row["PresentCount"]  >= 0);
                Assert.True((int)row["AbsentCount"]   >= 0);
                Assert.True((int)row["LateCount"]     >= 0);
            }
        }

        // ──────────────────────────────────────────────────────
        //  sp_GetDormitories
        // ──────────────────────────────────────────────────────

        [Fact]
        public void sp_GetDormitories_ReturnsDataTable()
        {
            var dt = ExecSp("sp_GetDormitories");
            Assert.NotNull(dt);
        }

        // ──────────────────────────────────────────────────────
        //  sp_GetSubjectsAll
        // ──────────────────────────────────────────────────────

        [Fact]
        public void sp_GetSubjectsAll_ReturnsDataTable()
        {
            var dt = ExecSp("sp_GetSubjectsAll");
            Assert.NotNull(dt);
        }

        // ──────────────────────────────────────────────────────
        //  sp_GetAllTeachers
        // ──────────────────────────────────────────────────────

        [Fact]
        public void sp_GetAllTeachers_ReturnsDataTable()
        {
            var dt = ExecSp("sp_GetAllTeachers");
            Assert.NotNull(dt);
        }

        // ──────────────────────────────────────────────────────
        //  sp_GetAdminAlerts
        // ──────────────────────────────────────────────────────

        [Fact]
        public void sp_GetAdminAlerts_ReturnsDataTable()
        {
            var dt = ExecSp("sp_GetAdminAlerts");
            Assert.NotNull(dt);
        }

        // ──────────────────────────────────────────────────────
        //  Вспомогательный метод — ADO.NET напрямую (без MessageBox)
        // ──────────────────────────────────────────────────────

        private static DataTable ExecSp(string spName, SqlParameter[] parameters = null)
        {
            var dt = new DataTable();
            using (var conn = new SqlConnection(ConnStr))
            {
                conn.Open();
                using (var cmd = new SqlCommand(spName, conn))
                {
                    cmd.CommandType    = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    new SqlDataAdapter(cmd).Fill(dt);
                }
            }
            return dt;
        }
    }
}
