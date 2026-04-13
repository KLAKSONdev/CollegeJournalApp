using System;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Data.SqlClient;

namespace CollegeJournalApp.Database
{
    public static class DatabaseHelper
    {
        private static readonly string _connectionString =
            ConfigurationManager.ConnectionStrings["CollegeJournal"].ConnectionString;

        public static SqlConnection GetConnection()
        {
            var conn = new SqlConnection(_connectionString);
            conn.Open();
            return conn;
        }

        public static bool TestConnection()
        {
            try
            {
                using (var conn = GetConnection())
                    return conn.State == ConnectionState.Open;
            }
            catch { return false; }
        }

        public static DataTable ExecuteProcedure(string procedure, SqlParameter[] parameters = null)
        {
            var dt = new DataTable();
            try
            {
                using (var conn = GetConnection())
                using (var cmd = new SqlCommand(procedure, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    new SqlDataAdapter(cmd).Fill(dt);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(TranslateSqlError(ex), "Ошибка базы данных",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Неожиданная ошибка:\n" + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return dt;
        }

        public static int ExecuteNonQuery(string procedure, SqlParameter[] parameters = null)
        {
            try
            {
                using (var conn = GetConnection())
                using (var cmd = new SqlCommand(procedure, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    return cmd.ExecuteNonQuery();
                }
            }
            catch (SqlException ex)
            {
                throw new Exception(TranslateSqlError(ex), ex);
            }
        }

        public static DataRow ExecuteSingleRow(string procedure, SqlParameter[] parameters = null)
        {
            var dt = new DataTable();
            try
            {
                using (var conn = GetConnection())
                using (var cmd = new SqlCommand(procedure, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 30;
                    if (parameters != null) cmd.Parameters.AddRange(parameters);
                    new SqlDataAdapter(cmd).Fill(dt);
                }
            }
            catch (SqlException ex)
            {
                throw new Exception(TranslateSqlError(ex), ex);
            }
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }

        /// <summary>
        /// Переводит SQL-ошибки в понятные русские сообщения
        /// </summary>
        public static string TranslateSqlError(SqlException ex)
        {
            var msg = ex.Message ?? "";

            // Нарушение уникальности
            if (ex.Number == 2627 || ex.Number == 2601)
            {
                if (msg.Contains("UQ_Users_Login") || msg.Contains("Login"))
                    return "Пользователь с таким логином уже существует. Выберите другой логин.";
                if (msg.Contains("UQ_Students_Code") || msg.Contains("StudentCode"))
                    return "Студент с таким номером зачётной книжки уже существует.";
                if (msg.Contains("UQ_Groups_NameYear") || msg.Contains("GroupName"))
                    return "Группа с таким названием уже существует в этом учебном году.";
                if (msg.Contains("UQ_AcademicYears") || msg.Contains("AcademicYears"))
                    return "Учебный год с таким названием уже существует.";
                if (msg.Contains("UQ_Students_OneHeadman") || msg.Contains("Headman"))
                    return "В группе уже есть староста. Сначала снимите полномочия текущего старосты.";
                return "Запись с такими данными уже существует в базе данных.";
            }

            // Нарушение CHECK-ограничения
            if (ex.Number == 547 && msg.Contains("CHECK"))
            {
                if (msg.Contains("CK_Groups_SemesterCourse"))
                    return "Номер семестра не соответствует курсу.\nКурс 1 → семестры 1-2, Курс 2 → семестры 3-4, Курс 3 → семестры 5-6 и т.д.";
                if (msg.Contains("CK_Users_PasswordHash"))
                    return "Пароль слишком короткий. Минимальная длина — 32 символа.";
                if (msg.Contains("CK_Users_Login"))
                    return "Логин должен быть не менее 4 символов и не содержать пробелов.";
                if (msg.Contains("CK_Students_BirthDate") || msg.Contains("BirthDate"))
                    return "Некорректная дата рождения. Допустимый возраст: от 5 до 100 лет.";
                if (msg.Contains("CK_Grades_Value") || msg.Contains("GradeValue"))
                    return "Оценка должна быть от 1 до 5.";
                if (msg.Contains("CK_Groups_Course"))
                    return "Курс должен быть от 1 до 5.";
                if (msg.Contains("CK_Groups_Semester"))
                    return "Семестр должен быть от 1 до 10.";
                if (msg.Contains("fn_IsValidPhone") || msg.Contains("Phone"))
                    return "Неверный формат телефона. Используйте формат: +79001234567 или 89001234567.";
                if (msg.Contains("fn_IsValidEmail") || msg.Contains("Email"))
                    return "Неверный формат email. Пример: user@example.com";
                if (msg.Contains("fn_IsValidSNILS") || msg.Contains("SNILS"))
                    return "Неверный формат СНИЛС. Используйте формат: 123-456-789 00";
                if (msg.Contains("fn_IsValidPassport"))
                    return "Неверный формат паспортных данных. Серия: 4 цифры, номер: 6 цифр.";
                return "Введены некорректные данные. Проверьте правильность заполнения полей.";
            }

            // Нарушение внешнего ключа
            if (ex.Number == 547)
            {
                if (msg.Contains("FK_Students_Group") || msg.Contains("Groups"))
                    return "Указанная группа не найдена. Выберите группу из списка.";
                if (msg.Contains("FK_Students_User") || msg.Contains("Users"))
                    return "Пользователь не найден.";
                if (msg.Contains("FK_Grades_Student") || msg.Contains("Students"))
                    return "Студент не найден в базе данных.";
                return "Ошибка связей данных. Проверьте правильность выбранных значений.";
            }

            // NULL в обязательном поле
            if (ex.Number == 515)
            {
                if (msg.Contains("LastName"))  return "Фамилия обязательна для заполнения.";
                if (msg.Contains("FirstName")) return "Имя обязательно для заполнения.";
                if (msg.Contains("GroupId"))   return "Необходимо выбрать группу.";
                if (msg.Contains("BirthDate")) return "Необходимо указать дату рождения.";
                if (msg.Contains("Login"))     return "Необходимо указать логин.";
                return "Не заполнено обязательное поле. Проверьте все поля формы.";
            }

            // Дедлок
            if (ex.Number == 1205)
                return "Конфликт операций в базе данных. Попробуйте ещё раз.";

            // Таймаут
            if (ex.Number == -2 || ex.Number == 11)
                return "Превышено время ожидания ответа от базы данных. Проверьте подключение.";

            // Нет подключения
            if (ex.Number == 53 || ex.Number == 17)
                return "Не удалось подключиться к базе данных. Проверьте, запущен ли SQL Server.";

            // Ошибка из триггера (RAISERROR)
            if (ex.Number == 50000 || ex.Class == 16)
            {
                if (msg.Contains("выпустившейся группы"))
                    return "Нельзя изменять состав выпустившейся группы.";
                if (msg.Contains("Куратором может быть"))
                    return "Выбранный пользователь не является куратором. Назначьте пользователя с ролью «Куратор».";
                if (msg.Contains("Нельзя создать протокол"))
                    return "Нельзя создать протокол для отменённого мероприятия.";
                if (msg.Contains("Нельзя редактировать подписанный"))
                    return "Протокол уже подписан и не может быть изменён.";
                if (msg.Contains("Студент в поручении"))
                    return "Студент не принадлежит выбранной группе.";
                // Возвращаем оригинальное сообщение из триггера — оно уже на русском
                return msg;
            }

            // По умолчанию — общее сообщение
            return $"Ошибка базы данных (код {ex.Number}).\nОбратитесь к администратору.";
        }
        public static string TableRu(string tableName)
        {
            switch (tableName)
            {
                case "Students":      return "Студенты";
                case "Users":         return "Пользователи";
                case "Groups":        return "Группы";
                case "Grades":        return "Оценки";
                case "Attendance":    return "Посещаемость";
                case "Events":        return "События";
                case "Announcements": return "Объявления";
                case "Assignments":   return "Поручения";
                case "Documents":     return "Документы";
                case "Achievements":  return "Достижения";
                case "Parents":       return "Родители";
                case "AcademicYears": return "Учебные годы";
                case "Dormitories":   return "Общежития";
                case "Teachers":      return "Преподаватели";
                case "Subjects":      return "Дисциплины";
                case "Schedule":      return "Расписание";
                default:              return tableName ?? "—";
            }
        }

    }
}
