using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace TRAC_NGHIEM.DAL
{
    /// <summary>
    /// Lớp quản lý kết nối cơ sở dữ liệu SQL Server bằng ADO.NET.
    /// Sử dụng SQL Server Authentication theo từng tài khoản người dùng (Giáo viên / Sinh viên).
    /// Tuyệt đối không lưu cố định username/password hay tài khoản sa trong App.config.
    /// </summary>
    public static class DatabaseConnection
    {
        private const string ConnectionName = "TRAC_NGHIEM_DB";
        private static string _activeUserConnectionString = null;

        /// <summary>
        /// Lấy cấu hình máy chủ và tên database nền từ App.config (không chứa thông tin đăng nhập cố định)
        /// </summary>
        private static string GetBaseServerConnectionString()
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
        /// Tạo chuỗi kết nối SQL Server Authentication động từ thông tin đăng nhập của người dùng
        /// </summary>
        public static string BuildUserConnectionString(string loginName, string password, string initialCatalog = "TRAC_NGHIEM", bool pooling = true)
        {
            if (string.IsNullOrWhiteSpace(loginName))
            {
                throw new ArgumentException("Tên đăng nhập SQL Server không được để trống!", "loginName");
            }

            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(GetBaseServerConnectionString())
            {
                InitialCatalog = string.IsNullOrWhiteSpace(initialCatalog) ? "TRAC_NGHIEM" : initialCatalog,
                IntegratedSecurity = false,
                UserID = loginName.Trim(),
                Password = password ?? string.Empty,
                TrustServerCertificate = true,
                ConnectTimeout = 10,
                Pooling = pooling,
                ApplicationName = "PTIT_TRAC_NGHIEM_WINFORMS"
            };

            return builder.ConnectionString;
        }

        /// <summary>
        /// Thiết lập chuỗi kết nối của phiên người dùng sau khi SQL Server xác thực thành công
        /// </summary>
        public static void SetAuthenticatedUserConnection(string loginName, string password)
        {
            _activeUserConnectionString = BuildUserConnectionString(loginName, password, "TRAC_NGHIEM", true);
        }

        /// <summary>
        /// Giải phóng kết nối và xóa thông tin xác thực khi người dùng đăng xuất
        /// </summary>
        public static void ClearAuthenticatedConnection()
        {
            _activeUserConnectionString = null;
            SqlConnection.ClearAllPools();
        }

        /// <summary>
        /// Lấy chuỗi kết nối của người dùng đang đăng nhập hiện tại
        /// </summary>
        public static string GetConnectionString()
        {
            if (string.IsNullOrWhiteSpace(_activeUserConnectionString))
            {
                throw new InvalidOperationException(
                    "Chưa có phiên kết nối SQL Server hợp lệ. Vui lòng đăng nhập bằng tài khoản SQL Server Login!");
            }
            return _activeUserConnectionString;
        }

        /// <summary>
        /// Lấy chuỗi kết nối tới database master theo tài khoản hiện tại (phục vụ kiểm tra / Restore khi có quyền)
        /// </summary>
        public static string GetMasterConnectionString()
        {
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(GetConnectionString())
            {
                InitialCatalog = "master",
                Pooling = false
            };
            return builder.ConnectionString;
        }

        /// <summary>
        /// Khởi tạo đối tượng SqlConnection mới tới database TRAC_NGHIEM bằng quyền của người dùng hiện tại
        /// </summary>
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(GetConnectionString());
        }

        /// <summary>
        /// Kiểm tra kết nối của phiên hiện tại tới SQL Server và database TRAC_NGHIEM
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
                    return "Tài khoản SQL Login này chưa được ánh xạ (cấp quyền Database User) vào cơ sở dữ liệu 'TRAC_NGHIEM'!";
                case 18456:
                    return "Đăng nhập không thành công: Tên đăng nhập (SQL Login), mật khẩu không chính xác hoặc tài khoản đã bị vô hiệu hóa!";
                case 18470:
                    return "Tài khoản SQL Server Login này đã bị thu hồi (vô hiệu hóa - DISABLED)! Không thể đăng nhập.";
                case 229:
                case 230:
                    return "Từ chối truy cập trên SQL Server: Tài khoản hiện tại không được phân quyền thực hiện thao tác này!";
                case 15025:
                    return "SQL Server Login này đã tồn tại trên máy chủ!";
                case 15023:
                    return "Database User này đã tồn tại trong cơ sở dữ liệu TRAC_NGHIEM!";
                case 15118:
                    return "Mật khẩu không đáp ứng chính sách độ phức tạp của hệ thống (CHECK_POLICY)!";
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
