using Xunit;
using CollegeJournalApp.Database;

namespace CollegeJournalApp.Tests
{
    /// <summary>
    /// Чистые юнит-тесты DatabaseHelper — проверяют трансляцию ошибок и маппинг таблиц.
    /// База данных НЕ нужна.
    /// </summary>
    public class DatabaseHelperUnitTests
    {
        // ──────────────────────────────────────────────────────
        //  TranslateSqlErrorCore — нарушение уникальности (2627)
        // ──────────────────────────────────────────────────────

        [Fact]
        public void TranslateSqlError_UniqueViolation_LoginConstraint_ReturnsLoginMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(2627, "Violation of UNIQUE KEY constraint 'UQ_Users_Login'.");
            Assert.Contains("логином", msg);
        }

        [Fact]
        public void TranslateSqlError_UniqueViolation_StudentCodeConstraint_ReturnsStudentCodeMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(2627, "Violation of UNIQUE KEY constraint 'UQ_Students_Code'.");
            Assert.Contains("зачётной книжки", msg);
        }

        [Fact]
        public void TranslateSqlError_UniqueViolation_GroupNameConstraint_ReturnsGroupMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(2627, "Violation of UNIQUE KEY constraint 'UQ_Groups_NameYear'.");
            Assert.Contains("Группа", msg);
        }

        [Fact]
        public void TranslateSqlError_UniqueViolation_AcademicYearConstraint_ReturnsYearMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(2627, "Violation of UNIQUE KEY constraint 'UQ_AcademicYears'.");
            Assert.Contains("Учебный год", msg);
        }

        [Fact]
        public void TranslateSqlError_UniqueViolation_HeadmanConstraint_ReturnsHeadmanMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(2627, "Violation of UNIQUE KEY constraint 'UQ_Students_OneHeadman'.");
            Assert.Contains("староста", msg);
        }

        [Fact]
        public void TranslateSqlError_UniqueViolation_UnknownConstraint_ReturnsFallbackMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(2601, "Duplicate key row in object 'dbo.SomeTable'.");
            Assert.Contains("уже существует", msg);
        }

        // ──────────────────────────────────────────────────────
        //  TranslateSqlErrorCore — CHECK-ограничения (547 + "CHECK")
        // ──────────────────────────────────────────────────────

        [Fact]
        public void TranslateSqlError_Check_SemesterCourse_ReturnsSemesterMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(547, "The INSERT statement conflicted with the CHECK constraint 'CK_Groups_SemesterCourse'.");
            Assert.Contains("семестра", msg);
        }

        [Fact]
        public void TranslateSqlError_Check_BirthDate_ReturnsBirthDateMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(547, "The INSERT statement conflicted with the CHECK constraint 'CK_Students_BirthDate'.");
            Assert.Contains("дата рождения", msg);
        }

        [Fact]
        public void TranslateSqlError_Check_GradeValue_ReturnsGradeMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(547, "Conflicted with the CHECK constraint 'CK_Grades_Value'.");
            Assert.Contains("Оценка", msg);
        }

        [Fact]
        public void TranslateSqlError_Check_Course_ReturnsCourseMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(547, "Conflicted with the CHECK constraint 'CK_Groups_Course'.");
            Assert.Contains("Курс", msg);
        }

        [Fact]
        public void TranslateSqlError_Check_Semester_ReturnsSemesterRangeMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(547, "Conflicted with the CHECK constraint 'CK_Groups_Semester'.");
            Assert.Contains("Семестр", msg);
        }

        // ──────────────────────────────────────────────────────
        //  TranslateSqlErrorCore — нарушение внешнего ключа (547, без "CHECK")
        // ──────────────────────────────────────────────────────

        [Fact]
        public void TranslateSqlError_ForeignKey_GroupNotFound_ReturnsGroupMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(547, "Conflicted with the FOREIGN KEY constraint 'FK_Students_Group'.");
            Assert.Contains("группа", msg);
        }

        [Fact]
        public void TranslateSqlError_ForeignKey_Unknown_ReturnsFallback()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(547, "Conflicted with the FOREIGN KEY constraint 'FK_SomeTable_Other'.");
            Assert.Contains("связей", msg);
        }

        // ──────────────────────────────────────────────────────
        //  TranslateSqlErrorCore — NULL в обязательном поле (515)
        // ──────────────────────────────────────────────────────

        [Fact]
        public void TranslateSqlError_NullField_LastName_ReturnsLastNameMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(515, "Cannot insert the value NULL into column 'LastName'.");
            Assert.Contains("Фамилия", msg);
        }

        [Fact]
        public void TranslateSqlError_NullField_GroupId_ReturnsGroupMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(515, "Cannot insert the value NULL into column 'GroupId'.");
            Assert.Contains("группу", msg);
        }

        [Fact]
        public void TranslateSqlError_NullField_Unknown_ReturnsFallback()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(515, "Cannot insert the value NULL into column 'SomeOtherColumn'.");
            Assert.Contains("обязательное поле", msg);
        }

        // ──────────────────────────────────────────────────────
        //  TranslateSqlErrorCore — прочие коды ошибок
        // ──────────────────────────────────────────────────────

        [Fact]
        public void TranslateSqlError_Deadlock_ReturnsDeadlockMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(1205, "Transaction was deadlocked.");
            Assert.Contains("Конфликт", msg);
        }

        [Fact]
        public void TranslateSqlError_Timeout_ReturnsTimeoutMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(-2, "Timeout expired.");
            Assert.Contains("ожидания", msg);
        }

        [Fact]
        public void TranslateSqlError_ConnectionFailed_ReturnsConnectionMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(53, "A network-related or instance-specific error.");
            Assert.Contains("подключиться", msg);
        }

        [Fact]
        public void TranslateSqlError_Raiserror50000_GraduatedGroup_ReturnsGraduatedMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(50000, "Нельзя изменять состав выпустившейся группы.");
            Assert.Contains("выпустившейся группы", msg);
        }

        [Fact]
        public void TranslateSqlError_Raiserror50000_CuratorCheck_ReturnsCuratorMessage()
        {
            var msg = DatabaseHelper.TranslateSqlErrorCore(50000, "Куратором может быть только пользователь с ролью Куратор.");
            Assert.Contains("куратором", msg);
        }

        [Fact]
        public void TranslateSqlError_UnknownErrorCode_ReturnsCodeInMessage()
        {
            // Передаём errorClass=0 чтобы не попасть в ветку RAISERROR (errorClass==16)
            var msg = DatabaseHelper.TranslateSqlErrorCore(99999, "Some unknown error.", errorClass: 0);
            Assert.Contains("99999", msg);
        }

        // ──────────────────────────────────────────────────────
        //  TableRu — маппинг имён таблиц
        // ──────────────────────────────────────────────────────

        [Theory]
        [InlineData("Students",      "Студенты")]
        [InlineData("Users",         "Пользователи")]
        [InlineData("Groups",        "Группы")]
        [InlineData("Grades",        "Оценки")]
        [InlineData("Attendance",    "Посещаемость")]
        [InlineData("Announcements", "Объявления")]
        [InlineData("Documents",     "Документы")]
        [InlineData("Teachers",      "Преподаватели")]
        [InlineData("Subjects",      "Дисциплины")]
        [InlineData("Schedule",      "Расписание")]
        public void TableRu_KnownTable_ReturnsRussianName(string tableName, string expected)
        {
            Assert.Equal(expected, DatabaseHelper.TableRu(tableName));
        }

        [Fact]
        public void TableRu_UnknownTable_ReturnsTableNameAsIs()
        {
            Assert.Equal("SomeTable", DatabaseHelper.TableRu("SomeTable"));
        }

        [Fact]
        public void TableRu_NullInput_ReturnsDash()
        {
            Assert.Equal("—", DatabaseHelper.TableRu(null));
        }
    }
}
