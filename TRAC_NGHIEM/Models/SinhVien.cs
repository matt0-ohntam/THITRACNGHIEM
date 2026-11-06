using System;

namespace TRAC_NGHIEM.Models
{
    /// <summary>
    /// Thực thể Sinh viên (Bảng SINHVIEN)
    /// </summary>
    public class SinhVien
    {
        public string MaSV { get; set; }
        public string Ho { get; set; }
        public string Ten { get; set; }
        public DateTime? NgaySinh { get; set; }
        public string DiaChi { get; set; }
        public string MaLop { get; set; }
        public string TenLop { get; set; }
        public bool DaXoa { get; set; }
        public int SoLanThi { get; set; }

        public string HoTen
        {
            get { return string.Format("{0} {1}", (Ho ?? string.Empty).Trim(), (Ten ?? string.Empty).Trim()).Trim(); }
        }

        public SinhVien()
        {
            MaSV = string.Empty;
            Ho = string.Empty;
            Ten = string.Empty;
            DiaChi = string.Empty;
            MaLop = string.Empty;
            TenLop = string.Empty;
        }
    }
}

