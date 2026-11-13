using System;

namespace TRAC_NGHIEM.Models
{
    /// <summary>
    /// Thông tin phiên đăng nhập SQL Server Login & Database Role của người dùng hiện tại.
    /// (Không sử dụng bảng TAIKHOAN; thông tin lấy từ SUSER_SNAME(), USER_NAME(), IS_MEMBER() và GIAOVIEN/SINHVIEN).
    /// </summary>
    public class TaiKhoan
    {
        public string TenDangNhap { get; set; }
        public string DbUserName { get; set; }
        public string DatabaseRole { get; set; }
        public string MaGV { get; set; }
        public string MaSV { get; set; }
        public string Quyen { get; set; }
        public bool TrangThai { get; set; }
        public string HoTen { get; set; }
        public string MaLop { get; set; }
        public string TenLop { get; set; }

        public bool LaGiaoVien
        {
            get
            {
                return string.Equals(Quyen, "GIANGVIEN", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(DatabaseRole, "role_GiaoVien", StringComparison.OrdinalIgnoreCase);
            }
        }

        public bool LaSinhVien
        {
            get
            {
                return string.Equals(Quyen, "SINHVIEN", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(DatabaseRole, "role_SinhVien", StringComparison.OrdinalIgnoreCase);
            }
        }

        public string MaNguoiDung
        {
            get { return LaGiaoVien ? (MaGV ?? string.Empty).Trim() : (MaSV ?? string.Empty).Trim(); }
        }

        public TaiKhoan()
        {
            TenDangNhap = string.Empty;
            DbUserName = string.Empty;
            DatabaseRole = string.Empty;
            MaGV = string.Empty;
            MaSV = string.Empty;
            Quyen = string.Empty;
            HoTen = string.Empty;
            MaLop = string.Empty;
            TenLop = string.Empty;
            TrangThai = true;
        }
    }
}
