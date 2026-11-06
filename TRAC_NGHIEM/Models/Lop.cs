using System;

namespace TRAC_NGHIEM.Models
{
    /// <summary>
    /// Thực thể Lớp học (Bảng LOP)
    /// </summary>
    public class Lop
    {
        public string MaLop { get; set; }
        public string TenLop { get; set; }
        public int SiSo { get; set; }
        public int SoLichThi { get; set; }

        public Lop()
        {
            MaLop = string.Empty;
            TenLop = string.Empty;
        }

        public Lop(string maLop, string tenLop)
        {
            MaLop = (maLop ?? string.Empty).Trim().ToUpperInvariant();
            TenLop = (tenLop ?? string.Empty).Trim();
        }
    }
}

