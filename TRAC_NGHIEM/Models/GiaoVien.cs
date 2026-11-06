using System;

namespace TRAC_NGHIEM.Models
{
    /// <summary>
    /// Thực thể Giáo viên / Giảng viên (Bảng GIAOVIEN)
    /// </summary>
    public class GiaoVien
    {
        public string MaGV { get; set; }
        public string Ho { get; set; }
        public string Ten { get; set; }
        public string SoDTLL { get; set; }
        public string DiaChi { get; set; }
        public string TenDangNhap { get; set; }
        public bool CoTaiKhoan { get; set; }
        public int SoCauDaSoan { get; set; }
        public int SoLichDangKy { get; set; }

        public string HoTen
        {
            get { return string.Format("{0} {1}", (Ho ?? string.Empty).Trim(), (Ten ?? string.Empty).Trim()).Trim(); }
        }

        public string HienThiComboBox
        {
            get { return string.Format("{0} - {1}", (MaGV ?? string.Empty).Trim(), HoTen); }
        }

        public GiaoVien()
        {
            MaGV = string.Empty;
            Ho = string.Empty;
            Ten = string.Empty;
            SoDTLL = string.Empty;
            DiaChi = string.Empty;
            TenDangNhap = string.Empty;
        }
    }
}

