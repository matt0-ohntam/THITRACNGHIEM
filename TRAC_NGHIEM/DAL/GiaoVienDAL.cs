using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.DAL
{
    /// <summary>
    /// Lớp truy cập dữ liệu Giáo viên (Bảng GIAOVIEN & View vw_DanhSachGiaoVien)
    /// </summary>
    public class GiaoVienDAL
    {
        public DataTable GetAllGiaoVien()
        {
            const string sql = @"
                SELECT MAGV, HO, TEN, HOTEN, SODTLL, DIACHI,
                       ISNULL(TENDANGNHAP, N'(Chưa cấp TK)') AS TENDANGNHAP,
                       CO_TAI_KHOAN, SO_CAU_DA_SOAN, SO_LICH_DANG_KY
                FROM dbo.vw_DanhSachGiaoVien
                ORDER BY MAGV ASC;";
            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text);
        }

        public DataTable SearchGiaoVien(string keyword)
        {
            const string sql = @"
                SELECT MAGV, HO, TEN, HOTEN, SODTLL, DIACHI,
                       ISNULL(TENDANGNHAP, N'(Chưa cấp TK)') AS TENDANGNHAP,
                       CO_TAI_KHOAN, SO_CAU_DA_SOAN, SO_LICH_DANG_KY
                FROM dbo.vw_DanhSachGiaoVien
                WHERE MAGV LIKE @Keyword
                   OR HO LIKE @Keyword
                   OR TEN LIKE @Keyword
                   OR HOTEN LIKE @Keyword
                   OR SODTLL LIKE @Keyword
                ORDER BY MAGV ASC;";

            SqlParameter param = new SqlParameter("@Keyword", SqlDbType.NVarChar, 60)
            {
                Value = "%" + (keyword ?? string.Empty).Trim() + "%"
            };

            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text, param);
        }

        public void InsertGiaoVien(GiaoVien gv)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@MaGV", SqlDbType.NChar, 8) { Value = gv.MaGV.Trim().ToUpperInvariant() },
                new SqlParameter("@Ho", SqlDbType.NVarChar, 40) { Value = gv.Ho.Trim() },
                new SqlParameter("@Ten", SqlDbType.NVarChar, 10) { Value = gv.Ten.Trim() },
                new SqlParameter("@SoDTLL", SqlDbType.NChar, 12) { Value = string.IsNullOrWhiteSpace(gv.SoDTLL) ? (object)DBNull.Value : gv.SoDTLL.Trim() },
                new SqlParameter("@DiaChi", SqlDbType.NVarChar, 50) { Value = string.IsNullOrWhiteSpace(gv.DiaChi) ? (object)DBNull.Value : gv.DiaChi.Trim() }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_ThemGiaoVien", CommandType.StoredProcedure, parameters);
        }

        public void UpdateGiaoVien(GiaoVien gv)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@MaGV", SqlDbType.NChar, 8) { Value = gv.MaGV.Trim().ToUpperInvariant() },
                new SqlParameter("@Ho", SqlDbType.NVarChar, 40) { Value = gv.Ho.Trim() },
                new SqlParameter("@Ten", SqlDbType.NVarChar, 10) { Value = gv.Ten.Trim() },
                new SqlParameter("@SoDTLL", SqlDbType.NChar, 12) { Value = string.IsNullOrWhiteSpace(gv.SoDTLL) ? (object)DBNull.Value : gv.SoDTLL.Trim() },
                new SqlParameter("@DiaChi", SqlDbType.NVarChar, 50) { Value = string.IsNullOrWhiteSpace(gv.DiaChi) ? (object)DBNull.Value : gv.DiaChi.Trim() }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_CapNhatGiaoVien", CommandType.StoredProcedure, parameters);
        }

        public void DeleteGiaoVien(string maGV)
        {
            SqlParameter param = new SqlParameter("@MaGV", SqlDbType.NChar, 8)
            {
                Value = maGV.Trim().ToUpperInvariant()
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_XoaGiaoVien", CommandType.StoredProcedure, param);
        }
    }
}

