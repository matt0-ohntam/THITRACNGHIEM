using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.BLL
{
    /// <summary>
    /// Lớp xử lý nghiệp vụ Tài khoản (Đăng nhập, Phân quyền, Tạo tài khoản Giảng viên, Đổi mật khẩu)
    /// </summary>
    public class TaiKhoanBLL
    {
        private readonly TaiKhoanDAL _dal = new TaiKhoanDAL();

        /// <summary>
        /// Xác thực đăng nhập người dùng (Giảng viên hoặc Sinh viên).
        /// Kiểm tra đầy đủ: tồn tại tài khoản, mật khẩu băm SHA-256, trạng thái khóa, trạng thái xóa mềm của SV.
        /// </summary>
        public bool DangNhap(string tenDangNhap, string matKhauPlain, out TaiKhoan taiKhoan, out string thongBao)
        {
            taiKhoan = null;
            thongBao = string.Empty;

            string userClean = (tenDangNhap ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(userClean))
            {
                thongBao = "Vui lòng nhập Tên đăng nhập (hoặc Mã sinh viên)!";
                return false;
            }

            if (string.IsNullOrEmpty(matKhauPlain))
            {
                thongBao = "Vui lòng nhập Mật khẩu!";
                return false;
            }

            try
            {
                string pwdHash = SecurityHelper.HashPassword(matKhauPlain);
                DataTable dt = _dal.CheckLoginRaw(userClean, pwdHash);

                if (dt == null || dt.Rows.Count == 0)
                {
                    thongBao = "Tài khoản không tồn tại trong hệ thống!";
                    return false;
                }

                DataRow row = dt.Rows[0];
                bool khopMatKhau = Convert.ToInt32(row["KHOP_MATKHAU"]) == 1;
                if (!khopMatKhau)
                {
                    thongBao = "Mật khẩu không chính xác. Vui lòng kiểm tra lại!";
                    return false;
                }

                bool trangThai = Convert.ToBoolean(row["TRANGTHAI"]);
                bool svDaXoa = Convert.ToInt32(row["SV_DAXOA"]) == 1;
                if (!trangThai || svDaXoa)
                {
                    thongBao = "Tài khoản này hiện đang bị khóa hoặc đã ngừng hoạt động. Không thể đăng nhập!";
                    return false;
                }

                taiKhoan = new TaiKhoan
                {
                    TenDangNhap = row["TENDANGNHAP"].ToString().Trim(),
                    Quyen = row["QUYEN"].ToString().Trim(),
                    TrangThai = trangThai,
                    MaGV = row["MAGV"].ToString().Trim(),
                    MaSV = row["MASV"].ToString().Trim(),
                    HoTen = row["HOTEN"].ToString().Trim(),
                    MaLop = row["MALOP"].ToString().Trim(),
                    TenLop = row["TENLOP"].ToString().Trim()
                };

                SessionManager.StartSession(taiKhoan);
                thongBao = string.Format("Đăng nhập thành công! Xin chào {0} ({1}).",
                    taiKhoan.HoTen, taiKhoan.LaGiaoVien ? "Giảng viên" : "Sinh viên");
                return true;
            }
            catch (SqlException sqlEx)
            {
                thongBao = DatabaseConnection.FormatSqlExceptionMessage(sqlEx);
                return false;
            }
            catch (Exception ex)
            {
                thongBao = "Lỗi khi đăng nhập: " + ex.Message;
                return false;
            }
        }

        public DataTable LayDanhSachTaiKhoanGiaoVien()
        {
            SessionManager.EnsureGiaoVienPermission("Xem danh sách tài khoản giảng viên");
            return _dal.GetDanhSachTaiKhoanGiaoVien();
        }

        public DataTable LayGiaoVienChuaCoTaiKhoan()
        {
            SessionManager.EnsureGiaoVienPermission("Lấy danh sách giảng viên chưa có tài khoản");
            return _dal.GetGiaoVienChuaCoTaiKhoan();
        }

        public bool TaoTaiKhoanGiaoVien(string maGV, string tenDangNhap, string matKhauPlain, string xacNhanMatKhauPlain, bool trangThai, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Tạo tài khoản giảng viên");

                string maGVClean = ValidationHelper.NormalizeCode(maGV);
                if (string.IsNullOrEmpty(maGVClean))
                {
                    thongBao = "Vui lòng chọn một Giảng viên cần cấp tài khoản!";
                    return false;
                }

                string errUser;
                if (!ValidationHelper.IsValidUsername(tenDangNhap, out errUser))
                {
                    thongBao = errUser;
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

                string matKhauHash = SecurityHelper.HashPassword(matKhauPlain);
                _dal.TaoTaiKhoanGiaoVien(
                    SessionManager.CurrentUser.TenDangNhap,
                    maGVClean,
                    tenDangNhap.Trim(),
                    matKhauHash,
                    trangThai);

                thongBao = string.Format("Tạo tài khoản [{0}] cho giảng viên [{1}] thành công!", tenDangNhap.Trim(), maGVClean);
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

        public bool CapNhatTrangThaiTaiKhoan(string tenDangNhap, bool trangThaiMoi, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Khóa/Mở tài khoản");

                string targetUser = (tenDangNhap ?? string.Empty).Trim();
                if (string.Equals(targetUser, SessionManager.CurrentUser.TenDangNhap, StringComparison.OrdinalIgnoreCase) && !trangThaiMoi)
                {
                    thongBao = "Không thể tự khóa tài khoản đang đăng nhập của chính bạn!";
                    return false;
                }

                _dal.CapNhatTrangThaiTaiKhoan(targetUser, trangThaiMoi);
                thongBao = string.Format("Đã chuyển trạng thái tài khoản [{0}] sang [{1}].",
                    targetUser, trangThaiMoi ? "Hoạt động" : "Đã khóa");
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

        public bool DoiMatKhau(string matKhauHienTaiPlain, string matKhauMoiPlain, string xacNhanMatKhauMoiPlain, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Đổi mật khẩu giảng viên");

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

                string oldHash = SecurityHelper.HashPassword(matKhauHienTaiPlain);
                string newHash = SecurityHelper.HashPassword(matKhauMoiPlain);

                _dal.DoiMatKhau(SessionManager.CurrentUser.TenDangNhap, oldHash, newHash);
                thongBao = "Đổi mật khẩu thành công!";
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

