using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.BLL
{
    /// <summary>
    /// Lớp xử lý nghiệp vụ Quản lý Môn học (Thêm, Xóa mềm, Hiệu chỉnh, Phục hồi, Tìm kiếm)
    /// </summary>
    public class MonHocBLL
    {
        private readonly MonHocDAL _dal = new MonHocDAL();

        public DataTable LayDanhSachMonHoc()
        {
            return _dal.GetAllMonHoc();
        }

        public DataTable LayDanhSachMonHocDaXoa()
        {
            SessionManager.EnsureGiaoVienPermission("Xem môn học chờ phục hồi");
            return _dal.GetDeletedMonHoc();
        }

        public DataTable TimKiemMonHoc(string tuKhoa)
        {
            if (string.IsNullOrWhiteSpace(tuKhoa))
            {
                return _dal.GetAllMonHoc();
            }
            return _dal.SearchMonHoc(tuKhoa);
        }

        public bool ThemMonHoc(string maMH, string tenMH, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Thêm môn học");

                string errCode;
                if (!ValidationHelper.IsValidCode(maMH, 5, out errCode, "Mã môn học (MAMH)"))
                {
                    thongBao = errCode;
                    return false;
                }

                string cleanTen = ValidationHelper.NormalizeText(tenMH);
                if (string.IsNullOrEmpty(cleanTen))
                {
                    thongBao = "Tên môn học (TENMH) không được để trống!";
                    return false;
                }
                if (cleanTen.Length > 40)
                {
                    thongBao = "Tên môn học không được vượt quá 40 ký tự!";
                    return false;
                }

                MonHoc mh = new MonHoc(maMH, cleanTen);
                _dal.InsertMonHoc(mh);
                thongBao = string.Format("Thêm môn học [{0} - {1}] thành công!", mh.MaMH, mh.TenMH);
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

        public bool CapNhatMonHoc(string maMH, string tenMH, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Hiệu chỉnh môn học");

                string errCode;
                if (!ValidationHelper.IsValidCode(maMH, 5, out errCode, "Mã môn học (MAMH)"))
                {
                    thongBao = errCode;
                    return false;
                }

                string cleanTen = ValidationHelper.NormalizeText(tenMH);
                if (string.IsNullOrEmpty(cleanTen))
                {
                    thongBao = "Tên môn học (TENMH) không được để trống!";
                    return false;
                }
                if (cleanTen.Length > 40)
                {
                    thongBao = "Tên môn học không được vượt quá 40 ký tự!";
                    return false;
                }

                MonHoc mh = new MonHoc(maMH, cleanTen);
                _dal.UpdateMonHoc(mh);
                thongBao = string.Format("Cập nhật môn học [{0}] thành công!", mh.MaMH);
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

        public bool XoaMonHoc(string maMH, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Xóa môn học");

                string cleanMa = ValidationHelper.NormalizeCode(maMH);
                if (string.IsNullOrEmpty(cleanMa))
                {
                    thongBao = "Vui lòng chọn môn học cần xóa!";
                    return false;
                }

                _dal.SoftDeleteMonHoc(cleanMa);
                thongBao = string.Format("Đã xóa môn học [{0}]. Bạn có thể nhấn 'Phục hồi' nếu muốn khôi phục lại!", cleanMa);
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

        public bool PhucHoiMonHoc(string maMH, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Phục hồi môn học");

                string cleanMa = ValidationHelper.NormalizeCode(maMH);
                if (string.IsNullOrEmpty(cleanMa))
                {
                    thongBao = "Không xác định được mã môn học cần phục hồi!";
                    return false;
                }

                _dal.RestoreMonHoc(cleanMa);
                thongBao = string.Format("Phục hồi môn học [{0}] thành công!", cleanMa);
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

        public bool HoanTacThemMonHoc(string maMH, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Phục hồi thao tác thêm môn học");
                string cleanMa = ValidationHelper.NormalizeCode(maMH);
                _dal.HardDeleteMonHocIfUnused(cleanMa);
                thongBao = string.Format("Đã hoàn tác thao tác thêm môn học [{0}].", cleanMa);
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

