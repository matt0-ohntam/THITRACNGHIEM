using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.DAL
{
    /// <summary>
    /// Lớp truy cập dữ liệu Lớp học (Bảng LOP & View vw_DanhSachLop)
    /// </summary>
    public class LopDAL
    {
        public DataTable GetAllLop()
        {
            const string sql = @"
                SELECT MALOP, TENLOP, SI_SO, SO_LICH_THI
                FROM dbo.vw_DanhSachLop
                ORDER BY MALOP ASC;";
            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text);
        }

        public DataTable SearchLop(string keyword)
        {
            const string sql = @"
                SELECT MALOP, TENLOP, SI_SO, SO_LICH_THI
                FROM dbo.vw_DanhSachLop
                WHERE MALOP LIKE @Keyword OR TENLOP LIKE @Keyword
                ORDER BY MALOP ASC;";

            SqlParameter param = new SqlParameter("@Keyword", SqlDbType.NVarChar, 50)
            {
                Value = "%" + (keyword ?? string.Empty).Trim() + "%"
            };

            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text, param);
        }

        public void InsertLop(Lop lop)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@MaLop", SqlDbType.NChar, 8) { Value = lop.MaLop.Trim().ToUpperInvariant() },
                new SqlParameter("@TenLop", SqlDbType.NVarChar, 40) { Value = lop.TenLop.Trim() }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_ThemLop", CommandType.StoredProcedure, parameters);
        }

        public void UpdateLop(Lop lop)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@MaLop", SqlDbType.NChar, 8) { Value = lop.MaLop.Trim().ToUpperInvariant() },
                new SqlParameter("@TenLop", SqlDbType.NVarChar, 40) { Value = lop.TenLop.Trim() }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_CapNhatLop", CommandType.StoredProcedure, parameters);
        }

        public void DeleteLop(string maLop)
        {
            SqlParameter param = new SqlParameter("@MaLop", SqlDbType.NChar, 8)
            {
                Value = maLop.Trim().ToUpperInvariant()
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_XoaLop", CommandType.StoredProcedure, param);
        }
    }
}

