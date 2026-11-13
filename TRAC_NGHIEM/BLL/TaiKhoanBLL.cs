using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.BLL
{
    /// <summary>
    /// Lớp xử lý nghiệp vụ Tài khoản bằng cơ chế gốc của SQL Server:
    /// SQL Server Login, Database User, Database Role (role_GiaoVien, role_SinhVien).
    /// Không sử dụng bảng TAIKHOAN.
    /// </summary>
    public class TaiKhoanBLL
    {
        private readonly TaiKhoanDAL _dal = new TaiKhoanDAL();

        /// <summary>
        /// Xác thực đăng nhập bằng SQL Server Authentication:
        /// 1. Kiểm tra định dạng đầu vào.
        /// 2. Mở kết nối trực tiếp tới SQL Server với Login & Password người dùng vừa nhập.
        /// 3. Kiểm tra ánh xạ Database User và quyền qua SUSER_SNAME(), USER_NAME(), IS_MEMBER().
        /// 4. Đối chiếu Login với MAGV (GIAOVIEN) hoặc MASV (SINHVIEN).
        /// </summary>
        public bool DangNhap(string tenDangNhap, string matKhauPlain, out TaiKhoan taiKhoan, out string thongBao)
        {
            taiKhoan = null;
            thongBao = string.Empty;

            string userClean = (tenDangNhap ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(userClean))
            {
                thongBao = "Vui lòng nhập Tên đăng nhập (Mã giáo viên MAGV hoặc Mã sinh viên MASV)!";
                return false;
            }

            if (string.IsNullOrEmpty(matKhauPlain))
            {
                thongBao = "Vui lòng nhập Mật khẩu!";
                return false;
            }

            string errUser;
            if (!ValidationHelper.IsValidUsername(userClean, out errUser))
            {
                thongBao = "Đăng nhập không thành công: " + errUser;
                return false;
            }

            try
            {
                // Mở kết nối SQL Server bằng đúng SQL Login & Password vừa nhập và kiểm tra role/ánh xạ
                DataTable dt = _dal.AuthenticateAndGetSessionInfo(userClean, matKhauPlain);

                if (dt == null || dt.Rows.Count == 0)
                {
                    DatabaseConnection.ClearAuthenticatedConnection();
                    thongBao = "Đăng nhập không thành công: Không lấy được thông tin phân quyền từ SQL Server!";
                    return false;
                }

                DataRow row = dt.Rows[0];
                bool trangThai = Convert.ToBoolean(row["TRANGTHAI"]);
                bool svDaXoa = Convert.ToBoolean(row["SV_DAXOA"]);
                if (!trangThai || svDaXoa)
                {
                    DatabaseConnection.ClearAuthenticatedConnection();
                    thongBao = "Tài khoản này hiện đã bị thu hồi hoặc ngừng hoạt động. Không thể đăng nhập!";
                    return false;
                }

                taiKhoan = new TaiKhoan
                {
                    TenDangNhap = Convert.ToString(row["TENDANGNHAP"]).Trim(),
                    DbUserName = row.Table.Columns.Contains("DB_USERNAME") ? Convert.ToString(row["DB_USERNAME"]).Trim() : userClean,
                    DatabaseRole = row.Table.Columns.Contains("ROLE_NAME") ? Convert.ToString(row["ROLE_NAME"]).Trim() : string.Empty,
                    Quyen = Convert.ToString(row["QUYEN"]).Trim(),
                    TrangThai = trangThai,
                    MaGV = Convert.ToString(row["MAGV"]).Trim(),
                    MaSV = Convert.ToString(row["MASV"]).Trim(),
                    HoTen = Convert.ToString(row["HOTEN"]).Trim(),
                    MaLop = Convert.ToString(row["MALOP"]).Trim(),
                    TenLop = Convert.ToString(row["TENLOP"]).Trim()
                };

                // Thiết lập kết nối cho phiên làm việc của người dùng hiện tại
                DatabaseConnection.SetAuthenticatedUserConnection(taiKhoan.TenDangNhap, matKhauPlain);
                SessionManager.StartSession(taiKhoan);

                thongBao = string.Format("Đăng nhập SQL Server thành công! Xin chào {0} ({1} - Role: {2}).",
                    taiKhoan.HoTen,
                    taiKhoan.LaGiaoVien ? "Giáo viên" : "Sinh viên",
                    taiKhoan.DatabaseRole);
                return true;
            }
            catch (SqlException sqlEx)
            {
                DatabaseConnection.ClearAuthenticatedConnection();
                thongBao = DatabaseConnection.FormatSqlExceptionMessage(sqlEx);
                return false;
            }
            catch (Exception ex)
            {
                DatabaseConnection.ClearAuthenticatedConnection();
                thongBao = "Đăng nhập không thành công: " + ex.Message;
                return false;
            }
        }

        public DataTable LayDanhSachTaiKhoanGiaoVien()
        {
            SessionManager.EnsureGiaoVienPermission("Xem danh sách tài khoản giáo viên");
            return _dal.GetDanhSachTaiKhoanGiaoVien();
        }

        public DataTable LayGiaoVienChuaCoTaiKhoan()
        {
            SessionManager.EnsureGiaoVienPermission("Lấy danh sách giáo viên chưa có SQL Login");
            return _dal.GetGiaoVienChuaCoTaiKhoan();
        }

        /// <summary>
        /// Tạo tài khoản SQL Server Login, Database User và gán role_GiaoVien cho giáo viên
        /// </summary>
        public bool TaoTaiKhoanGiaoVien(string maGV, string tenDangNhap, string matKhauPlain, string xacNhanMatKhauPlain, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Tạo tài khoản SQL Login cho giáo viên");

                string maGVClean = ValidationHelper.NormalizeCode(maGV);
                if (string.IsNullOrEmpty(maGVClean))
                {
                    thongBao = "Vui lòng chọn một Giáo viên cần cấp tài khoản SQL Login!";
                    return false;
                }

                string errCode;
                if (!ValidationHelper.IsValidCode(maGVClean, 8, out errCode, "Mã giáo viên (MAGV)"))
                {
                    thongBao = errCode;
                    return false;
                }

                string loginClean = ValidationHelper.NormalizeCode(tenDangNhap);
                string errUser;
                if (!ValidationHelper.IsValidUsername(loginClean, out errUser))
                {
                    thongBao = errUser;
                    return false;
                }

                if (!string.Equals(loginClean, maGVClean, StringComparison.OrdinalIgnoreCase))
                {
                    thongBao = string.Format("Theo quy định, Tên đăng nhập (SQL Login) bắt buộc phải trùng khớp với Mã giáo viên [{0}]!", maGVClean);
                    return false;
                }

                if (string.IsNullOrEmpty(matKhauPlain) || matKhauPlain.Length < 6)
                {
                    thongBao = "Mật khẩu phải có ít nhất 6 ký tự!";
                    return false;
                }

                if (!string.Equals(matKhauPlain, xacNhanMatKhauPlain, StringComparison.Ordinal))
                {
                    thongBao = "Xác nhận mật khẩu không khớp với mật khẩu đã nhập!";
                    return false;
                }

                _dal.TaoTaiKhoanGiaoVien(maGVClean, loginClean, matKhauPlain);

                thongBao = string.Format("Đã tạo SQL Server Login [{0}], Database User [{0}] và gán vào [role_GiaoVien] thành công!", maGVClean);
                return true;
            }
            catch (SqlException sqlEx)
            {
                thongBao = DatabaseConnection.FormatSqlExceptionMessage(sqlEx);
                return false;
            }
            catch (Exception ex)
            {
                thongBao = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Overload tương thích ngược khi truyền thêm cờ trạng thái
        /// </summary>
        public bool TaoTaiKhoanGiaoVien(string maGV, string tenDangNhap, string matKhauPlain, string xacNhanMatKhauPlain, bool trangThai, out string thongBao)
        {
            if (!TaoTaiKhoanGiaoVien(maGV, tenDangNhap, matKhauPlain, xacNhanMatKhauPlain, out thongBao))
            {
                return false;
            }

            if (!trangThai)
            {
                string msgDisable;
                ThuHoiTaiKhoanGiaoVien(maGV, out msgDisable);
            }
            return true;
        }

        /// <summary>
        /// Thu hồi (vô hiệu hóa) SQL Server Login của giáo viên (ALTER LOGIN [...] DISABLE)
        /// </summary>
        public bool ThuHoiTaiKhoanGiaoVien(string maGV, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Thu hồi tài khoản SQL Login của giáo viên");

                string maGVClean = ValidationHelper.NormalizeCode(maGV);
                string errCode;
                if (!ValidationHelper.IsValidCode(maGVClean, 8, out errCode, "Mã giáo viên (MAGV)"))
                {
                    thongBao = errCode;
                    return false;
                }

                if (string.Equals(maGVClean, SessionManager.CurrentUser.TenDangNhap, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(maGVClean, SessionManager.CurrentUser.MaGV, StringComparison.OrdinalIgnoreCase))
                {
                    thongBao = "Không thể tự thu hồi (vô hiệu hóa) tài khoản SQL Login đang đăng nhập của chính bạn!";
                    return false;
                }

                _dal.ThuHoiTaiKhoanGiaoVien(maGVClean);
                thongBao = string.Format("Đã thu hồi (vô hiệu hóa) tài khoản SQL Server Login [{0}] thành công! Giáo viên này không thể đăng nhập mới.", maGVClean);
                return true;
            }
            catch (SqlException sqlEx)
            {
                thongBao = DatabaseConnection.FormatSqlExceptionMessage(sqlEx);
                return false;
            }
            catch (Exception ex)
            {
                thongBao = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Kích hoạt lại SQL Server Login đã bị vô hiệu hóa của giáo viên (ALTER LOGIN [...] ENABLE)
        /// </summary>
        public bool KichHoatLaiTaiKhoanGiaoVien(string maGV, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Kích hoạt lại tài khoản SQL Login của giáo viên");

                string maGVClean = ValidationHelper.NormalizeCode(maGV);
                string errCode;
                if (!ValidationHelper.IsValidCode(maGVClean, 8, out errCode, "Mã giáo viên (MAGV)"))
                {
                    thongBao = errCode;
                    return false;
                }

                _dal.KichHoatLaiTaiKhoanGiaoVien(maGVClean);
                thongBao = string.Format("Đã kích hoạt lại tài khoản SQL Server Login [{0}] thành công!", maGVClean);
                return true;
            }
            catch (SqlException sqlEx)
            {
                thongBao = DatabaseConnection.FormatSqlExceptionMessage(sqlEx);
                return false;
            }
            catch (Exception ex)
            {
                thongBao = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Đổi mật khẩu của chính SQL Server Login giáo viên đang đăng nhập
        /// </summary>
        public bool DoiMatKhau(string matKhauHienTaiPlain, string matKhauMoiPlain, string xacNhanMatKhauMoiPlain, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Đổi mật khẩu tài khoản SQL Login");

                if (string.IsNullOrEmpty(matKhauHienTaiPlain))
                {
                    thongBao = "Vui lòng nhập Mật khẩu hiện tại!";
                    return false;
                }

                if (string.IsNullOrEmpty(matKhauMoiPlain))
                {
                    thongBao = "Mật khẩu mới không được để trống!";
                    return false;
                }

                if (matKhauMoiPlain.Length < 6)
                {
                    thongBao = "Mật khẩu mới phải có độ dài từ 6 ký tự trở lên!";
                    return false;
                }

                if (!string.Equals(matKhauMoiPlain, xacNhanMatKhauMoiPlain, StringComparison.Ordinal))
                {
                    thongBao = "Xác nhận mật khẩu mới không khớp!";
                    return false;
                }

                if (string.Equals(matKhauHienTaiPlain, matKhauMoiPlain, StringComparison.Ordinal))
                {
                    thongBao = "Mật khẩu mới không được trùng với mật khẩu hiện tại!";
                    return false;
                }

                _dal.DoiMatKhau(matKhauHienTaiPlain, matKhauMoiPlain);

                // Cập nhật lại chuỗi kết nối trong phiên hiện tại với mật khẩu mới
                SqlConnection.ClearAllPools();
                DatabaseConnection.SetAuthenticatedUserConnection(SessionManager.CurrentUser.TenDangNhap, matKhauMoiPlain);

                thongBao = "Đổi mật khẩu SQL Server Login thành công!";
                return true;
            }
            catch (SqlException sqlEx)
            {
                thongBao = DatabaseConnection.FormatSqlExceptionMessage(sqlEx);
                return false;
            }
            catch (Exception ex)
            {
                thongBao = ex.Message;
                return false;
            }
        }
    }
}
