using System;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.Helpers
{
    /// <summary>
    /// Quản lý phiên đăng nhập (Session) và kiểm tra phân quyền trên toàn ứng dụng
    /// (Kiểm tra ở cả tầng Giao diện và tầng Nghiệp vụ BLL).
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

        public static void ClearSession()
        {
            CurrentUser = null;
        }

        /// <summary>
        /// Kiểm tra bắt buộc quyền Giáo viên ở tầng BLL trước khi thực thi các chức năng Quản trị.
        /// Ngăn chặn việc sửa giao diện để vượt quyền.
        /// </summary>
        public static void EnsureGiaoVienPermission(string tenChucNang)
        {
            if (!IsLoggedIn)
            {
                throw new UnauthorizedAccessException("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại!");
            }

            if (!IsGiaoVien())
            {
                throw new UnauthorizedAccessException(
                    string.Format("Từ chối truy cập: Tài khoản [{0}] thuộc nhóm Sinh viên, không có quyền thực hiện chức năng [{1}]!",
                        CurrentUser.TenDangNhap, tenChucNang));
            }
        }
    }
}

