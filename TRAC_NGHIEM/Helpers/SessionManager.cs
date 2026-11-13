using System;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.Helpers
{
    /// <summary>
    /// Quản lý phiên đăng nhập SQL Server (Session) và kiểm tra phân quyền trên toàn ứng dụng
    /// (Kiểm tra ở cả tầng Giao diện, tầng Nghiệp vụ BLL và tầng Database Role của SQL Server).
    /// </summary>
    public static class SessionManager
    {
        public static TaiKhoan CurrentUser { get; private set; }

        public static bool IsLoggedIn
        {
            get { return CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.TenDangNhap); }
        }

        public static bool IsGiaoVien()
        {
            return IsLoggedIn && CurrentUser.LaGiaoVien;
        }

        public static bool IsSinhVien()
        {
            return IsLoggedIn && CurrentUser.LaSinhVien;
        }

        public static void StartSession(TaiKhoan user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }
            CurrentUser = user;
        }

        /// <summary>
        /// Kết thúc phiên làm việc, xóa thông tin người dùng và giải phóng toàn bộ kết nối SQL Server
        /// </summary>
        public static void ClearSession()
        {
            CurrentUser = null;
            DatabaseConnection.ClearAuthenticatedConnection();
        }

        /// <summary>
        /// Kiểm tra bắt buộc quyền Giáo viên (role_GiaoVien) ở tầng BLL trước khi thực thi chức năng Quản trị.
        /// </summary>
        public static void EnsureGiaoVienPermission(string tenChucNang)
        {
            if (!IsLoggedIn)
            {
                throw new UnauthorizedAccessException("Phiên đăng nhập SQL Server đã kết thúc. Vui lòng đăng nhập lại!");
            }

            if (!IsGiaoVien())
            {
                throw new UnauthorizedAccessException(
                    string.Format("Từ chối truy cập: Tài khoản SQL Login [{0}] thuộc role_SinhVien, không có quyền thực hiện chức năng [{1}]!",
                        CurrentUser.TenDangNhap, tenChucNang));
            }
        }
    }
}
