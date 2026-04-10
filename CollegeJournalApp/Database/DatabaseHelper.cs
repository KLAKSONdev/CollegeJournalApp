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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка базы данных",
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка базы данных",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return -1;
            }
        }

        public static DataRow ExecuteSingleRow(string procedure, SqlParameter[] parameters = null)
        {
            var dt = ExecuteProcedure(procedure, parameters);
            return dt.Rows.Count > 0 ? dt.Rows[0] : null;
        }
    }
}
