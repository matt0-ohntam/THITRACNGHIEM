using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.Helpers;

namespace TRAC_NGHIEM.DAL
{
    /// <summary>
    /// Tầng truy cập dữ liệu (DAL) cho:
    /// - Mục 7b: Xem kết quả (in lại các câu đã thi của sinh viên).
    /// - Mục 8: Bảng điểm môn học (report in theo mẫu trường PTIT).
    /// </summary>
    public class BaoCaoDAL
    {
        public DataTable LayDanhSachBaiThiDaCo(string maLop = null, string maSV = null, string maMH = null)
        {
            SqlParameter pLop = new SqlParameter("@MaLop", SqlDbType.NChar, 8)
            {
                Value = string.IsNullOrWhiteSpace(maLop) ? (object)DBNull.Value : maLop.Trim().ToUpperInvariant()
            };
            SqlParameter pSV = new SqlParameter("@MaSV", SqlDbType.NChar, 8)
            {
                Value = string.IsNullOrWhiteSpace(maSV) ? (object)DBNull.Value : maSV.Trim().ToUpperInvariant()
            };
            SqlParameter pMH = new SqlParameter("@MaMH", SqlDbType.NChar, 5)
            {
                Value = string.IsNullOrWhiteSpace(maMH) ? (object)DBNull.Value : maMH.Trim().ToUpperInvariant()
            };

            return DatabaseConnection.ExecuteQuery(
                "dbo.sp_LayDanhSachBaiThiDaCo",
                CommandType.StoredProcedure,
                pLop, pSV, pMH);
        }

        public DataTable LayThongTinHeaderKetQua(string maSV, string maMH, short lan)
        {
            SqlParameter pSV = new SqlParameter("@MaSV", SqlDbType.NChar, 8) { Value = (maSV ?? string.Empty).Trim().ToUpperInvariant() };
            SqlParameter pMH = new SqlParameter("@MaMH", SqlDbType.NChar, 5) { Value = (maMH ?? string.Empty).Trim().ToUpperInvariant() };
            SqlParameter pLan = new SqlParameter("@Lan", SqlDbType.SmallInt) { Value = lan };

            return DatabaseConnection.ExecuteQuery(
                "dbo.sp_LayThongTinHeaderKetQua",
                CommandType.StoredProcedure,
                pSV, pMH, pLan);
        }

        public DataTable LayChiTietCauHoiDaThi(string maSV, string maMH, short lan)
        {
            SqlParameter pSV = new SqlParameter("@MaSV", SqlDbType.NChar, 8) { Value = (maSV ?? string.Empty).Trim().ToUpperInvariant() };
            SqlParameter pMH = new SqlParameter("@MaMH", SqlDbType.NChar, 5) { Value = (maMH ?? string.Empty).Trim().ToUpperInvariant() };
            SqlParameter pLan = new SqlParameter("@Lan", SqlDbType.SmallInt) { Value = lan };

            return DatabaseConnection.ExecuteQuery(
                "dbo.sp_XemKetQuaBaiThi",
                CommandType.StoredProcedure,
                pSV, pMH, pLan);
        }

        public DataTable LayBangDiemMonHoc(string maLop, string maMH, short lan)
        {
            SqlParameter pLop = new SqlParameter("@MaLop", SqlDbType.NChar, 8) { Value = (maLop ?? string.Empty).Trim().ToUpperInvariant() };
            SqlParameter pMH = new SqlParameter("@MaMH", SqlDbType.NChar, 5) { Value = (maMH ?? string.Empty).Trim().ToUpperInvariant() };
            SqlParameter pLan = new SqlParameter("@Lan", SqlDbType.SmallInt) { Value = lan };

            return DatabaseConnection.ExecuteQuery(
                "dbo.sp_BaoCaoBangDiemMonHoc",
                CommandType.StoredProcedure,
                pLop, pMH, pLan);
        }
    }
}

