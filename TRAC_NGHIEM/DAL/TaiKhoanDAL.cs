using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.DAL
{
    /// <summary>
    /// Lớp truy cập dữ liệu Tài khoản (Đăng nhập, Tạo tài khoản Giảng viên, Đổi mật khẩu)
    /// </summary>
    public class TaiKhoanDAL
    {
        public DataTable CheckLoginRaw(string tenDangNhap, string matKhauHash)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@TenDangNhap", SqlDbType.NVarChar, 50) { Value = tenDangNhap.Trim() },
                new SqlParameter("@MatKhauHash", SqlDbType.NVarChar, 100) { Value = matKhauHash }
            };

            return DatabaseConnection.ExecuteQuery("dbo.sp_DangNhap", CommandType.StoredProcedure, parameters);
        }

        public DataTable GetDanhSachTaiKhoanGiaoVien()
        {
            const string sql = @"
                SELECT
                    tk.TENDANGNHAP,
                    RTRIM(tk.MAGV) AS MAGV,
                    (gv.HO + N' ' + gv.TEN) AS HOTEN_GV,
                    tk.QUYEN,
                    CASE WHEN tk.TRANGTHAI = 1 THEN N'Hoạt động' ELSE N'Đã khóa' END AS TRANGTHAI_TEXT,
                    tk.TRANGTHAI,
                    tk.NGAYTAO
                FROM dbo.TAIKHOAN tk
                INNER JOIN dbo.GIAOVIEN gv ON tk.MAGV = gv.MAGV
                WHERE tk.QUYEN = N'GIANGVIEN'
                ORDER BY tk.NGAYTAO DESC, tk.TENDANGNHAP ASC;";

            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text);
        }

        public DataTable GetGiaoVienChuaCoTaiKhoan()
        {
            const string sql = @"
                SELECT
                    RTRIM(gv.MAGV) AS MAGV,
                    gv.HO,
                    gv.TEN,
                    (RTRIM(gv.MAGV) + N' - ' + gv.HO + N' ' + gv.TEN) AS HIENTHI
                FROM dbo.GIAOVIEN gv
                LEFT JOIN dbo.TAIKHOAN tk ON tk.MAGV = gv.MAGV
                WHERE tk.TENDANGNHAP IS NULL
                ORDER BY gv.MAGV ASC;";

            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text);
        }

        public void TaoTaiKhoanGiaoVien(string nguoiThucHien, string maGV, string tenDangNhap, string matKhauHash, bool trangThai)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@NguoiThucHien", SqlDbType.NVarChar, 50) { Value = nguoiThucHien },
                new SqlParameter("@MaGV", SqlDbType.NChar, 8) { Value = maGV.Trim().ToUpperInvariant() },
                new SqlParameter("@TenDangNhap", SqlDbType.NVarChar, 50) { Value = tenDangNhap.Trim() },
                new SqlParameter("@MatKhauHash", SqlDbType.NVarChar, 100) { Value = matKhauHash },
                new SqlParameter("@TrangThai", SqlDbType.Bit) { Value = trangThai }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_TaoTaiKhoanGiaoVien", CommandType.StoredProcedure, parameters);
        }

        public void CapNhatTrangThaiTaiKhoan(string tenDangNhap, bool trangThaiMoi)
        {
            const string sql = @"
                UPDATE dbo.TAIKHOAN
                SET TRANGTHAI = @TrangThai
                WHERE TENDANGNHAP = @TenDangNhap;";

            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@TrangThai", SqlDbType.Bit) { Value = trangThaiMoi },
                new SqlParameter("@TenDangNhap", SqlDbType.NVarChar, 50) { Value = tenDangNhap.Trim() }
            };

            DatabaseConnection.ExecuteNonQuery(sql, CommandType.Text, parameters);
        }

        public void DoiMatKhau(string tenDangNhap, string matKhauHienTaiHash, string matKhauMoiHash)
        {
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@TenDangNhap", SqlDbType.NVarChar, 50) { Value = tenDangNhap.Trim() },
                new SqlParameter("@MatKhauHienTaiHash", SqlDbType.NVarChar, 100) { Value = matKhauHienTaiHash },
                new SqlParameter("@MatKhauMoiHash", SqlDbType.NVarChar, 100) { Value = matKhauMoiHash }
            };

            DatabaseConnection.ExecuteNonQuery("dbo.sp_DoiMatKhau", CommandType.StoredProcedure, parameters);
        }
    }
}

