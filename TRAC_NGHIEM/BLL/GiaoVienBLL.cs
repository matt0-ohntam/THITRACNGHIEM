using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.BLL
{
    /// <summary>
    /// Lớp xử lý nghiệp vụ Quản lý Giáo viên (Thêm, Xóa, Hiệu chỉnh, Tìm kiếm)
    /// </summary>
    public class GiaoVienBLL
    {
        private readonly GiaoVienDAL _dal = new GiaoVienDAL();

        public DataTable LayDanhSachGiaoVien()
        {
            return _dal.GetAllGiaoVien();
        }

        public DataTable TimKiemGiaoVien(string tuKhoa)
        {
            if (string.IsNullOrWhiteSpace(tuKhoa))
            {
                return _dal.GetAllGiaoVien();
            }
            return _dal.SearchGiaoVien(tuKhoa);
        }

        private bool ValidateGiaoVienInput(GiaoVien gv, out string thongBao)
        {
            if (gv == null)
            {
                thongBao = "Thông tin giáo viên không hợp lệ!";
                return false;
            }

            string errCode;
            if (!ValidationHelper.IsValidCode(gv.MaGV, 8, out errCode, "Mã giáo viên (MAGV)"))
            {
                thongBao = errCode;
                return false;
            }

            gv.MaGV = ValidationHelper.NormalizeCode(gv.MaGV);
            gv.Ho = ValidationHelper.NormalizeText(gv.Ho);
            gv.Ten = ValidationHelper.NormalizeText(gv.Ten);
            gv.SoDTLL = (gv.SoDTLL ?? string.Empty).Trim();
            gv.DiaChi = ValidationHelper.NormalizeText(gv.DiaChi);

            if (string.IsNullOrEmpty(gv.Ho))
            {
                thongBao = "Họ và tên đệm giáo viên (HO) không được để trống!";
                return false;
            }
            if (gv.Ho.Length > 40)
            {
                thongBao = "Họ và tên đệm không được vượt quá 40 ký tự!";
                return false;
            }

            if (string.IsNullOrEmpty(gv.Ten))
            {
                thongBao = "Tên giáo viên (TEN) không được để trống!";
                return false;
            }
            if (gv.Ten.Length > 10)
            {
                thongBao = "Tên giáo viên không được vượt quá 10 ký tự!";
                return false;
            }

            string errPhone;
            if (!ValidationHelper.IsValidPhone(gv.SoDTLL, out errPhone))
            {
                thongBao = errPhone;
                return false;
            }

            if (gv.DiaChi.Length > 50)
            {
                thongBao = "Địa chỉ liên hệ không được vượt quá 50 ký tự!";
                return false;
            }

            thongBao = string.Empty;
            return true;
        }

        public bool ThemGiaoVien(GiaoVien gv, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Thêm giáo viên");

                if (!ValidateGiaoVienInput(gv, out thongBao))
                {
                    return false;
                }

                _dal.InsertGiaoVien(gv);
                thongBao = string.Format("Thêm giáo viên [{0} - {1}] thành công!", gv.MaGV, gv.HoTen);
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

        public bool CapNhatGiaoVien(GiaoVien gv, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Hiệu chỉnh giáo viên");

                if (!ValidateGiaoVienInput(gv, out thongBao))
                {
                    return false;
                }

                _dal.UpdateGiaoVien(gv);
                thongBao = string.Format("Cập nhật thông tin giáo viên [{0} - {1}] thành công!", gv.MaGV, gv.HoTen);
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

        public bool XoaGiaoVien(string maGV, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Xóa giáo viên");

                string cleanMa = ValidationHelper.NormalizeCode(maGV);
                if (string.IsNullOrEmpty(cleanMa))
                {
                    thongBao = "Vui lòng chọn giáo viên cần xóa!";
                    return false;
                }

                _dal.DeleteGiaoVien(cleanMa);
                thongBao = string.Format("Đã xóa giáo viên [{0}] thành công!", cleanMa);
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

