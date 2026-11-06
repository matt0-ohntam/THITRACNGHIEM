using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.BLL
{
    /// <summary>
    /// Lớp xử lý nghiệp vụ Quản lý Sinh viên (Subform trong mô hình Master-Detail)
    /// </summary>
    public class SinhVienBLL
    {
        private readonly SinhVienDAL _dal = new SinhVienDAL();

        public DataTable LaySinhVienTheoLop(string maLop)
        {
            if (string.IsNullOrWhiteSpace(maLop))
            {
                return _dal.GetAllSinhVien();
            }
            return _dal.GetSinhVienByLop(maLop);
        }

        public DataTable LaySinhVienDaXoa(string maLop = "")
        {
            SessionManager.EnsureGiaoVienPermission("Xem danh sách sinh viên chờ phục hồi");
            return _dal.GetDeletedSinhVien(maLop);
        }

        public DataTable TimKiemSinhVien(string maLop, string tuKhoa)
        {
            if (string.IsNullOrWhiteSpace(tuKhoa))
            {
                return LaySinhVienTheoLop(maLop);
            }
            return _dal.SearchSinhVien(maLop, tuKhoa);
        }

        public bool ValidateSinhVienInput(SinhVien sv, out string thongBao)
        {
            if (sv == null)
            {
                thongBao = "Dữ liệu sinh viên không hợp lệ!";
                return false;
            }

            string errCode;
            if (!ValidationHelper.IsValidCode(sv.MaSV, 8, out errCode, "Mã sinh viên (MASV)"))
            {
                thongBao = errCode;
                return false;
            }

            sv.MaSV = ValidationHelper.NormalizeCode(sv.MaSV);
            sv.Ho = ValidationHelper.NormalizeText(sv.Ho);
            sv.Ten = ValidationHelper.NormalizeText(sv.Ten);
            sv.DiaChi = ValidationHelper.NormalizeText(sv.DiaChi);
            sv.MaLop = ValidationHelper.NormalizeCode(sv.MaLop);

            if (string.IsNullOrEmpty(sv.Ho))
            {
                thongBao = "Họ và tên đệm của sinh viên (HO) không được để trống!";
                return false;
            }
            if (sv.Ho.Length > 40)
            {
                thongBao = "Họ và tên đệm không được vượt quá 40 ký tự!";
                return false;
            }

            if (string.IsNullOrEmpty(sv.Ten))
            {
                thongBao = "Tên sinh viên (TEN) không được để trống!";
                return false;
            }
            if (sv.Ten.Length > 10)
            {
                thongBao = "Tên sinh viên không được vượt quá 10 ký tự!";
                return false;
            }

            if (sv.NgaySinh.HasValue)
            {
                string errDate;
                if (!ValidationHelper.IsValidBirthDate(sv.NgaySinh.Value, out errDate))
                {
                    thongBao = errDate;
                    return false;
                }
            }

            if (sv.DiaChi.Length > 40)
            {
                thongBao = "Địa chỉ sinh viên không được vượt quá 40 ký tự!";
                return false;
            }

            if (string.IsNullOrEmpty(sv.MaLop))
            {
                thongBao = "Phải chọn Lớp học (MALOP) cho sinh viên!";
                return false;
            }

            thongBao = string.Empty;
            return true;
        }

        public bool ThemSinhVien(SinhVien sv, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Thêm sinh viên");

                if (!ValidateSinhVienInput(sv, out thongBao))
                {
                    return false;
                }

                _dal.InsertSinhVien(sv);
                thongBao = string.Format(
                    "Thêm sinh viên [{0} - {1}] vào lớp [{2}] thành công!\n(Đã tự động kích hoạt tài khoản đăng nhập SV: {0} / Mật khẩu mặc định: {3})",
                    sv.MaSV, sv.HoTen, sv.MaLop, SecurityHelper.DefaultStudentPasswordPlain);
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

        public bool CapNhatSinhVien(SinhVien sv, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Hiệu chỉnh sinh viên");

                if (!ValidateSinhVienInput(sv, out thongBao))
                {
                    return false;
                }

                _dal.UpdateSinhVien(sv);
                thongBao = string.Format("Cập nhật thông tin sinh viên [{0} - {1}] thành công!", sv.MaSV, sv.HoTen);
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

        public bool XoaSinhVien(string maSV, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Xóa sinh viên");

                string cleanMa = ValidationHelper.NormalizeCode(maSV);
                if (string.IsNullOrEmpty(cleanMa))
                {
                    thongBao = "Vui lòng chọn sinh viên cần xóa!";
                    return false;
                }

                _dal.SoftDeleteSinhVien(cleanMa);
                thongBao = string.Format("Đã xóa sinh viên [{0}] (tài khoản đăng nhập tương ứng đã tạm khóa). Bạn có thể nhấn 'Phục hồi' để khôi phục!", cleanMa);
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

        public bool PhucHoiSinhVien(string maSV, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Phục hồi sinh viên");

                string cleanMa = ValidationHelper.NormalizeCode(maSV);
                if (string.IsNullOrEmpty(cleanMa))
                {
                    thongBao = "Không xác định được mã sinh viên cần phục hồi!";
                    return false;
                }

                _dal.RestoreSinhVien(cleanMa);
                thongBao = string.Format("Phục hồi sinh viên [{0}] và kích hoạt lại tài khoản thành công!", cleanMa);
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

        public bool HoanTacThemSinhVien(string maSV, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Hoàn tác thêm sinh viên");
                string cleanMa = ValidationHelper.NormalizeCode(maSV);
                _dal.HardDeleteSinhVienIfUnused(cleanMa);
                thongBao = string.Format("Đã hoàn tác thao tác thêm sinh viên [{0}].", cleanMa);
                return true;
            }
            catch (Exception ex)
            {
                thongBao = ex.Message;
                return false;
            }
        }
    }
}

