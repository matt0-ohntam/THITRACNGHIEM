using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.BLL
{
    /// <summary>
    /// Lớp xử lý nghiệp vụ Quản lý Lớp học (Form chính trong mô hình Master-Detail)
    /// </summary>
    public class LopBLL
    {
        private readonly LopDAL _dal = new LopDAL();

        public DataTable LayDanhSachLop()
        {
            return _dal.GetAllLop();
        }

        public DataTable TimKiemLop(string tuKhoa)
        {
            if (string.IsNullOrWhiteSpace(tuKhoa))
            {
                return _dal.GetAllLop();
            }
            return _dal.SearchLop(tuKhoa);
        }

        public bool ThemLop(string maLop, string tenLop, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Thêm lớp học");

                string errCode;
                if (!ValidationHelper.IsValidCode(maLop, 8, out errCode, "Mã lớp (MALOP)"))
                {
                    thongBao = errCode;
                    return false;
                }

                string cleanTen = ValidationHelper.NormalizeText(tenLop);
                if (string.IsNullOrEmpty(cleanTen))
                {
                    thongBao = "Tên lớp (TENLOP) không được để trống!";
                    return false;
                }
                if (cleanTen.Length > 40)
                {
                    thongBao = "Tên lớp không được vượt quá 40 ký tự!";
                    return false;
                }

                Lop lop = new Lop(maLop, cleanTen);
                _dal.InsertLop(lop);
                thongBao = string.Format("Thêm lớp [{0} - {1}] thành công!", lop.MaLop, lop.TenLop);
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

        public bool CapNhatLop(string maLop, string tenLop, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Hiệu chỉnh lớp học");

                string errCode;
                if (!ValidationHelper.IsValidCode(maLop, 8, out errCode, "Mã lớp (MALOP)"))
                {
                    thongBao = errCode;
                    return false;
                }

                string cleanTen = ValidationHelper.NormalizeText(tenLop);
                if (string.IsNullOrEmpty(cleanTen))
                {
                    thongBao = "Tên lớp (TENLOP) không được để trống!";
                    return false;
                }
                if (cleanTen.Length > 40)
                {
                    thongBao = "Tên lớp không được vượt quá 40 ký tự!";
                    return false;
                }

                Lop lop = new Lop(maLop, cleanTen);
                _dal.UpdateLop(lop);
                thongBao = string.Format("Cập nhật thông tin lớp [{0}] thành công!", lop.MaLop);
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

        public bool XoaLop(string maLop, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Xóa lớp học");

                string cleanMa = ValidationHelper.NormalizeCode(maLop);
                if (string.IsNullOrEmpty(cleanMa))
                {
                    thongBao = "Vui lòng chọn lớp học cần xóa!";
                    return false;
                }

                _dal.DeleteLop(cleanMa);
                thongBao = string.Format("Đã xóa lớp [{0}] thành công!", cleanMa);
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

