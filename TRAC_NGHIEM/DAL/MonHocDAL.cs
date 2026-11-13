using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.DAL
{
    /// <summary>
    /// Lớp truy cập dữ liệu Môn học (Bảng MONHOC & View vw_DanhSachMonHoc)
    /// </summary>
    public class MonHocDAL
    {
        public DataTable GetAllMonHoc()
        {
            const string sql = @"
                SELECT MAMH, TENMH, SO_CAU_HOI, SO_LICH_THI, SO_BAI_THI
                FROM dbo.vw_DanhSachMonHoc
                ORDER BY MAMH ASC;";
            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text);
        }

        public DataTable GetDeletedMonHoc()
        {
            const string sql = @"
                SELECT RTRIM(MAMH) AS MAMH, TENMH
                FROM dbo.MONHOC
                WHERE DAXOA = 1
                ORDER BY MAMH ASC;";
            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text);
        }

        public DataTable SearchMonHoc(string keyword)
        {
            const string sql = @"
                SELECT MAMH, TENMH, SO_CAU_HOI, SO_LICH_THI, SO_BAI_THI
                FROM dbo.vw_DanhSachMonHoc
                WHERE MAMH LIKE @Keyword OR TENMH LIKE @Keyword
                ORDER BY MAMH ASC;";

            SqlParameter param = new SqlParameter("@Keyword", SqlDbType.NVarChar, 50)
            {
                Value = "%" + (keyword ?? string.Empty).Trim() + "%"
            };

            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text, param);
        }

        public void InsertMonHoc(MonHoc mh)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@MaMH", SqlDbType.NChar, 5) { Value = mh.MaMH.Trim().ToUpperInvariant() },
                new SqlParameter("@TenMH", SqlDbType.NVarChar, 40) { Value = mh.TenMH.Trim() }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_ThemMonHoc", CommandType.StoredProcedure, parameters);
        }

        public void UpdateMonHoc(MonHoc mh)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@MaMH", SqlDbType.NChar, 5) { Value = mh.MaMH.Trim().ToUpperInvariant() },
                new SqlParameter("@TenMH", SqlDbType.NVarChar, 40) { Value = mh.TenMH.Trim() }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_CapNhatMonHoc", CommandType.StoredProcedure, parameters);
        }

        public void SoftDeleteMonHoc(string maMH)
        {
            SqlParameter param = new SqlParameter("@MaMH", SqlDbType.NChar, 5)
            {
                Value = maMH.Trim().ToUpperInvariant()
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_XoaMonHoc", CommandType.StoredProcedure, param);
        }

        public void RestoreMonHoc(string maMH)
        {
            SqlParameter param = new SqlParameter("@MaMH", SqlDbType.NChar, 5)
            {
                Value = maMH.Trim().ToUpperInvariant()
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_PhucHoiMonHoc", CommandType.StoredProcedure, param);
        }

        public void HardDeleteMonHocIfUnused(string maMH)
        {
            const string sql = @"
                DELETE FROM dbo.MONHOC
                WHERE RTRIM(MAMH) = RTRIM(@MaMH)
                  AND NOT EXISTS (SELECT 1 FROM dbo.BODE WHERE RTRIM(MAMH) = RTRIM(@MaMH))
                  AND NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN_DANGKY WHERE RTRIM(MAMH) = RTRIM(@MaMH))
                  AND NOT EXISTS (SELECT 1 FROM dbo.BANGDIEM WHERE RTRIM(MAMH) = RTRIM(@MaMH));";

            SqlParameter param = new SqlParameter("@MaMH", SqlDbType.NChar, 5)
            {
                Value = maMH.Trim().ToUpperInvariant()
            };
            DatabaseConnection.ExecuteNonQuery(sql, CommandType.Text, param);
        }
    }
}
