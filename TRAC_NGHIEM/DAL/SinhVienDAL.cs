using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.DAL
{
    /// <summary>
    /// Lớp truy cập dữ liệu Sinh viên (Bảng SINHVIEN & View vw_DanhSachSinhVienTheoLop).
    /// Sử dụng Parameterized Query đúng kiểu dữ liệu cột và không truy vấn bảng TAIKHOAN.
    /// </summary>
    public class SinhVienDAL
    {
        public DataTable GetSinhVienByLop(string maLop)
        {
            const string sql = @"
                SELECT MASV, HO, TEN, HOTEN, NGAYSINH, DIACHI, MALOP, TENLOP, SO_LAN_THI
                FROM dbo.vw_DanhSachSinhVienTheoLop
                WHERE RTRIM(MALOP) = RTRIM(@MaLop)
                ORDER BY TEN ASC, HO ASC, MASV ASC;";

            SqlParameter param = new SqlParameter("@MaLop", SqlDbType.NChar, 8)
            {
                Value = (maLop ?? string.Empty).Trim().ToUpperInvariant()
            };

            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text, param);
        }

        public DataTable GetAllSinhVien()
        {
            const string sql = @"
                SELECT MASV, HO, TEN, HOTEN, NGAYSINH, DIACHI, MALOP, TENLOP, SO_LAN_THI
                FROM dbo.vw_DanhSachSinhVienTheoLop
                ORDER BY MALOP ASC, TEN ASC, HO ASC;";
            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text);
        }

        public DataTable SearchSinhVien(string maLop, string keyword)
        {
            const string sql = @"
                SELECT MASV, HO, TEN, HOTEN, NGAYSINH, DIACHI, MALOP, TENLOP, SO_LAN_THI
                FROM dbo.vw_DanhSachSinhVienTheoLop
                WHERE (RTRIM(@MaLop) = N'' OR RTRIM(MALOP) = RTRIM(@MaLop))
                  AND (MASV LIKE @Keyword OR HO LIKE @Keyword OR TEN LIKE @Keyword OR HOTEN LIKE @Keyword)
                ORDER BY MALOP ASC, TEN ASC, HO ASC;";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@MaLop", SqlDbType.NChar, 8) { Value = (maLop ?? string.Empty).Trim().ToUpperInvariant() },
                new SqlParameter("@Keyword", SqlDbType.NVarChar, 60) { Value = "%" + (keyword ?? string.Empty).Trim() + "%" }
            };

            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text, parameters);
        }

        public DataTable GetDeletedSinhVien(string maLop)
        {
            const string sql = @"
                SELECT RTRIM(MASV) AS MASV, HO, TEN, (HO + N' ' + TEN) AS HOTEN, NGAYSINH, DIACHI, RTRIM(MALOP) AS MALOP
                FROM dbo.SINHVIEN
                WHERE DAXOA = 1 AND (RTRIM(@MaLop) = N'' OR RTRIM(MALOP) = RTRIM(@MaLop))
                ORDER BY MASV ASC;";

            SqlParameter param = new SqlParameter("@MaLop", SqlDbType.NChar, 8)
            {
                Value = (maLop ?? string.Empty).Trim().ToUpperInvariant()
            };

            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text, param);
        }

        public void InsertSinhVien(SinhVien sv)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@MaSV", SqlDbType.NChar, 8) { Value = sv.MaSV.Trim().ToUpperInvariant() },
                new SqlParameter("@Ho", SqlDbType.NVarChar, 40) { Value = sv.Ho.Trim() },
                new SqlParameter("@Ten", SqlDbType.NVarChar, 10) { Value = sv.Ten.Trim() },
                new SqlParameter("@NgaySinh", SqlDbType.DateTime) { Value = sv.NgaySinh.HasValue ? (object)sv.NgaySinh.Value : DBNull.Value },
                new SqlParameter("@DiaChi", SqlDbType.NVarChar, 40) { Value = string.IsNullOrWhiteSpace(sv.DiaChi) ? (object)DBNull.Value : sv.DiaChi.Trim() },
                new SqlParameter("@MaLop", SqlDbType.NChar, 8) { Value = sv.MaLop.Trim().ToUpperInvariant() },
                new SqlParameter("@MatKhauMacDinh", SqlDbType.NVarChar, 100) { Value = SecurityHelper.DefaultStudentPasswordPlain }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_ThemSinhVien", CommandType.StoredProcedure, parameters);
        }

        public void UpdateSinhVien(SinhVien sv)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@MaSV", SqlDbType.NChar, 8) { Value = sv.MaSV.Trim().ToUpperInvariant() },
                new SqlParameter("@Ho", SqlDbType.NVarChar, 40) { Value = sv.Ho.Trim() },
                new SqlParameter("@Ten", SqlDbType.NVarChar, 10) { Value = sv.Ten.Trim() },
                new SqlParameter("@NgaySinh", SqlDbType.DateTime) { Value = sv.NgaySinh.HasValue ? (object)sv.NgaySinh.Value : DBNull.Value },
                new SqlParameter("@DiaChi", SqlDbType.NVarChar, 40) { Value = string.IsNullOrWhiteSpace(sv.DiaChi) ? (object)DBNull.Value : sv.DiaChi.Trim() },
                new SqlParameter("@MaLop", SqlDbType.NChar, 8) { Value = sv.MaLop.Trim().ToUpperInvariant() }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_CapNhatSinhVien", CommandType.StoredProcedure, parameters);
        }

        public void SoftDeleteSinhVien(string maSV)
        {
            SqlParameter param = new SqlParameter("@MaSV", SqlDbType.NChar, 8)
            {
                Value = maSV.Trim().ToUpperInvariant()
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_XoaSinhVien", CommandType.StoredProcedure, param);
        }

        public void RestoreSinhVien(string maSV)
        {
            SqlParameter param = new SqlParameter("@MaSV", SqlDbType.NChar, 8)
            {
                Value = maSV.Trim().ToUpperInvariant()
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_PhucHoiSinhVien", CommandType.StoredProcedure, param);
        }

        public void HardDeleteSinhVienIfUnused(string maSV)
        {
            SqlParameter param = new SqlParameter("@MaSV", SqlDbType.NChar, 8)
            {
                Value = maSV.Trim().ToUpperInvariant()
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_HoanTacThemSinhVien", CommandType.StoredProcedure, param);
        }
    }
}
