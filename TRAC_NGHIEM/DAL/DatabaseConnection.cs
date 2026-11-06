using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TRAC_NGHIEM.DAL
{
    /// <summary>
    /// Lớp quản lý kết nối cơ sở dữ liệu SQL Server tập trung bằng ADO.NET.
    /// Đọc ConnectionString từ App.config, sử dụng Parameterized Query và giải phóng tài nguyên bằng khối using.
    /// </summary>
    public static class DatabaseConnection
    {
        private const string ConnectionName = "TRAC_NGHIEM_DB";

        /// <summary>
        /// Lấy chuỗi kết nối tới database TRAC_NGHIEM từ App.config
        /// </summary>
        public static string GetConnectionString()
        {
            ConnectionStringSettings settings = ConfigurationManager.ConnectionStrings[ConnectionName];
            if (settings == null || string.IsNullOrWhiteSpace(settings.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Không tìm thấy cấu hình chuỗi kết nối 'TRAC_NGHIEM_DB' trong file App.config!");
            }
            return settings.ConnectionString;
        }

        /// <summary>
        /// Lấy chuỗi kết nối tới database master (phục vụ chức năng Backup / Restore an toàn)
        /// </summary>
        public static string GetMasterConnectionString()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(GetConnectionString());
            builder.InitialCatalog = "master";
            builder.Pooling = false; // Không giữ connection pool khi thao tác Restore
            return builder.ConnectionString;
        }

        /// <summary>
        /// Khởi tạo đối tượng SqlConnection mới tới database TRAC_NGHIEM
        /// </summary>
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(GetConnectionString());
        }

        /// <summary>
        /// Kiểm tra kết nối tới SQL Server và database TRAC_NGHIEM
        /// </summary>
        public static bool TestConnection(out string errorMessage)
        {
            try
            {
                using (SqlConnection conn = GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT DB_NAME();", conn))
                    {
                        object result = cmd.ExecuteScalar();
                        errorMessage = string.Empty;
                        return result != null;
                    }
                }
            }
            catch (SqlException sqlEx)
            {
                errorMessage = FormatSqlExceptionMessage(sqlEx);
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = "Lỗi kết nối cơ sở dữ liệu: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Thực thi truy vấn SELECT trả về DataTable (Hỗ trợ cả Text và StoredProcedure)
        /// </summary>
        public static DataTable ExecuteQuery(string queryOrSpName, CommandType commandType = CommandType.Text, params SqlParameter[] parameters)
        {
            DataTable dt = new DataTable();
            using (SqlConnection conn = GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(queryOrSpName, conn))
                {
                    cmd.CommandType = commandType;
                    cmd.CommandTimeout = 30;
                    if (parameters != null && parameters.Length > 0)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        conn.Open();
                        adapter.Fill(dt);
                    }
                }
            }
            return dt;
        }

        /// <summary>
        /// Thực thi lệnh INSERT, UPDATE, DELETE hoặc Stored Procedure không trả về bảng
        /// </summary>
        public static int ExecuteNonQuery(string queryOrSpName, CommandType commandType = CommandType.Text, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(queryOrSpName, conn))
                {
                    cmd.CommandType = commandType;
                    cmd.CommandTimeout = 30;
                    if (parameters != null && parameters.Length > 0)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    conn.Open();
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Thực thi truy vấn trả về 1 giá trị đơn (ô đầu tiên của dòng đầu tiên)
        /// </summary>
        public static object ExecuteScalar(string queryOrSpName, CommandType commandType = CommandType.Text, params SqlParameter[] parameters)
        {
            using (SqlConnection conn = GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(queryOrSpName, conn))
                {
                    cmd.CommandType = commandType;
                    cmd.CommandTimeout = 30;
                    if (parameters != null && parameters.Length > 0)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }

                    conn.Open();
                    return cmd.ExecuteScalar();
                }
            }
        }

        /// <summary>
        /// Chuyển đổi thông báo lỗi SqlException sang tiếng Việt rõ ràng, thân thiện với người dùng
        /// </summary>
        public static string FormatSqlExceptionMessage(SqlException ex)
        {
            if (ex == null) return "Lỗi SQL Server không xác định.";

            switch (ex.Number)
            {
                case -1:
                case 2:
                case 53:
                    return "Không thể kết nối đến máy chủ SQL Server. Vui lòng kiểm tra dịch vụ SQL Server (MSSQLSERVER) đã chạy chưa!";
                case 4060:
                    return "Không thể mở cơ sở dữ liệu 'TRAC_NGHIEM'. Vui lòng kiểm tra database đã được khởi tạo chưa!";
                case 18456:
                    return "Đăng nhập SQL Server thất bại (Không đủ quyền xác thực)!";
                case 2627:
                case 2601:
                    return "Dữ liệu bị trùng lặp (Vi phạm ràng buộc khóa chính hoặc UNIQUE): " + ex.Message;
                case 547:
                    return "Dữ liệu vi phạm ràng buộc khóa ngoại (FOREIGN KEY) hoặc điều kiện kiểm tra (CHECK): " + ex.Message;
                default:
                    return ex.Message;
            }
        }
    }
}

