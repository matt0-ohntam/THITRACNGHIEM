using System;
using System.Data;
using System.Data.SqlClient;

namespace TRAC_NGHIEM.DAL
{
    /// <summary>
    /// Lớp truy cập dữ liệu Tài khoản sử dụng cơ chế SQL Server Login, Database User và Database Role.
    /// Hoàn toàn không sử dụng bảng TAIKHOAN.
    /// </summary>
    public class TaiKhoanDAL
    {
        /// <summary>
        /// Mở kết nối trực tiếp tới SQL Server bằng SQL Server Authentication với thông tin người dùng nhập vào,
        /// sau đó kiểm tra ánh xạ Database User, Role (role_GiaoVien / role_SinhVien) và đối chiếu MAGV / MASV.
        /// </summary>
        public DataTable AuthenticateAndGetSessionInfo(string loginName, string password)
        {
            string userConnStr = DatabaseConnection.BuildUserConnectionString(loginName, password, "TRAC_NGHIEM", true);
            DataTable dt = new DataTable();

            using (SqlConnection conn = new SqlConnection(userConnStr))
            {
                using (SqlCommand cmd = new SqlCommand("dbo.sp_LayThongTinDangNhapHienTai", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 15;

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
        /// Lấy danh sách tất cả giáo viên kết hợp trạng thái SQL Server Login, Database User và role_GiaoVien
        /// </summary>
        public DataTable GetDanhSachTaiKhoanGiaoVien()
        {
            return DatabaseConnection.ExecuteQuery("dbo.sp_LayDanhSachTaiKhoanGiaoVien", CommandType.StoredProcedure);
        }

        /// <summary>
        /// Lấy danh sách giáo viên chưa được cấp SQL Server Login
        /// </summary>
        public DataTable GetGiaoVienChuaCoTaiKhoan()
        {
            return DatabaseConnection.ExecuteQuery("dbo.sp_LayGiaoVienChuaCoTaiKhoan", CommandType.StoredProcedure);
        }

        /// <summary>
        /// Tạo SQL Server Login, Database User và gán vào role_GiaoVien cho giáo viên (qua Stored Procedure ký Certificate)
        /// </summary>
        public void TaoTaiKhoanGiaoVien(string maGV, string tenDangNhap, string matKhauPlain)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@MaGV", SqlDbType.NChar, 8) { Value = maGV.Trim().ToUpperInvariant() },
                new SqlParameter("@TenDangNhap", SqlDbType.NVarChar, 50) { Value = tenDangNhap.Trim().ToUpperInvariant() },
                new SqlParameter("@MatKhauPlain", SqlDbType.NVarChar, 128) { Value = matKhauPlain }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_TaoTaiKhoanGiaoVien", CommandType.StoredProcedure, parameters);
        }

        /// <summary>
        /// Thu hồi (vô hiệu hóa) SQL Server Login của giáo viên (ALTER LOGIN [...] DISABLE)
        /// </summary>
        public void ThuHoiTaiKhoanGiaoVien(string maGV)
        {
            SqlParameter param = new SqlParameter("@MaGV", SqlDbType.NChar, 8)
            {
                Value = maGV.Trim().ToUpperInvariant()
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_ThuHoiTaiKhoanGiaoVien", CommandType.StoredProcedure, param);
        }

        /// <summary>
        /// Kích hoạt lại SQL Server Login đã bị thu hồi của giáo viên (ALTER LOGIN [...] ENABLE)
        /// </summary>
        public void KichHoatLaiTaiKhoanGiaoVien(string maGV)
        {
            SqlParameter param = new SqlParameter("@MaGV", SqlDbType.NChar, 8)
            {
                Value = maGV.Trim().ToUpperInvariant()
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_KichHoatLaiTaiKhoanGiaoVien", CommandType.StoredProcedure, param);
        }

        /// <summary>
        /// Đổi mật khẩu của chính SQL Server Login đang đăng nhập (ALTER LOGIN ... WITH PASSWORD ... OLD_PASSWORD ...)
        /// </summary>
        public void DoiMatKhau(string matKhauHienTaiPlain, string matKhauMoiPlain)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@MatKhauHienTai", SqlDbType.NVarChar, 128) { Value = matKhauHienTaiPlain },
                new SqlParameter("@MatKhauMoi", SqlDbType.NVarChar, 128) { Value = matKhauMoiPlain }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_DoiMatKhau", CommandType.StoredProcedure, parameters);
        }
    }
}
