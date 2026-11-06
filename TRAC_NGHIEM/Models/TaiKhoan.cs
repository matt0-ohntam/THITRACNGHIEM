using System;

namespace TRAC_NGHIEM.Models
{
    /// <summary>
    /// Thực thể Tài khoản người dùng (Bảng TAIKHOAN)
    /// </summary>
    public class TaiKhoan
    {
        public string TenDangNhap { get; set; }
        public string MatKhauHash { get; set; }
        public string MaGV { get; set; }
        public string MaSV { get; set; }
        public string Quyen { get; set; }
        public bool TrangThai { get; set; }
        public string HoTen { get; set; }
        public string MaLop { get; set; }
        public string TenLop { get; set; }

        public bool LaGiaoVien
        {
            get { return string.Equals(Quyen, "GIANGVIEN", StringComparison.OrdinalIgnoreCase); }
        }

        public bool LaSinhVien
        {
            get { return string.Equals(Quyen, "SINHVIEN", StringComparison.OrdinalIgnoreCase); }
        }

        public string MaNguoiDung
        {
            get { return LaGiaoVien ? (MaGV ?? string.Empty).Trim() : (MaSV ?? string.Empty).Trim(); }
        }

        public TaiKhoan()
        {
            TenDangNhap = string.Empty;
            MatKhauHash = string.Empty;
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

