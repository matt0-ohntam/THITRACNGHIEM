using System;

namespace TRAC_NGHIEM.Models
{
    /// <summary>
    /// Thực thể Môn học (Bảng MONHOC)
    /// </summary>
    public class MonHoc
    {
        public string MaMH { get; set; }
        public string TenMH { get; set; }
        public bool DaXoa { get; set; }
        public int SoCauHoi { get; set; }
        public int SoLichThi { get; set; }
        public int SoBaiThi { get; set; }

        public MonHoc()
        {
            MaMH = string.Empty;
            TenMH = string.Empty;
            DaXoa = false;
        }

        public MonHoc(string maMH, string tenMH, bool daXoa = false)
        {
            MaMH = (maMH ?? string.Empty).Trim().ToUpperInvariant();
            TenMH = (tenMH ?? string.Empty).Trim();
            DaXoa = daXoa;
        }
    }
}

