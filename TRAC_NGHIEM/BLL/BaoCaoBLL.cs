using System;
using System.Data;
using System.Data.SqlClient;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Helpers;

namespace TRAC_NGHIEM.BLL
{
    /// <summary>
    /// Tầng xử lý nghiệp vụ (BLL) cho:
    /// - Mục 7b: Xem kết quả (in lại các câu đã thi).
    /// - Mục 8: Bảng điểm môn học (report in theo mẫu Học viện PTIT).
    /// </summary>
    public class BaoCaoBLL
    {
        private readonly BaoCaoDAL _dal = new BaoCaoDAL();

        public DataTable LayDanhSachBaiThiDaCo(string maLop = null, string maSV = null, string maMH = null)
        {
            // Nếu là Sinh viên, chỉ cho phép lấy danh sách bài thi của chính sinh viên đang đăng nhập
            if (SessionManager.IsSinhVien() && SessionManager.CurrentUser != null)
            {
                maSV = SessionManager.CurrentUser.MaSV;
            }
            return _dal.LayDanhSachBaiThiDaCo(maLop, maSV, maMH);
        }

        public bool XemKetQuaBaiThi(string maSV, string maMH, short lan, out DataRow headerRow, out DataTable dtChiTiet, out string thongBao)
        {
            headerRow = null;
            dtChiTiet = null;

            if (SessionManager.IsSinhVien() && SessionManager.CurrentUser != null)
            {
                if (!string.Equals((maSV ?? string.Empty).Trim(), SessionManager.CurrentUser.MaSV.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    thongBao = "Từ chối truy cập: Sinh viên chỉ được phép xem và in lại các câu đã thi của chính mình!";
                    return false;
                }
            }

            if (string.IsNullOrWhiteSpace(maSV) || string.IsNullOrWhiteSpace(maMH) || (lan != 1 && lan != 2))
            {
                thongBao = "Vui lòng chọn đầy đủ Sinh viên, Môn học và Lần thi (1 hoặc 2)!";
                return false;
            }

            try
            {
                DataTable dtHeader = _dal.LayThongTinHeaderKetQua(maSV, maMH, lan);
                if (dtHeader == null || dtHeader.Rows.Count == 0)
                {
                    thongBao = string.Format("Không tìm thấy kết quả bài thi của sinh viên [{0}] - Môn [{1}] - Lần [{2}]!", maSV, maMH, lan);
                    return false;
                }

                headerRow = dtHeader.Rows[0];
                dtChiTiet = _dal.LayChiTietCauHoiDaThi(maSV, maMH, lan);
                thongBao = string.Format("Đã tải thành công {0} câu hỏi đã thi của sinh viên [{1}].",
                    dtChiTiet != null ? dtChiTiet.Rows.Count : 0, maSV.Trim());
                return true;
            }
            catch (SqlException sqlEx)
            {
                thongBao = sqlEx.Message;
                return false;
            }
            catch (Exception ex)
            {
                thongBao = "Lỗi khi tải kết quả bài thi: " + ex.Message;
                return false;
            }
        }

        public bool LayBangDiemMonHoc(string maLop, string maMH, short lan, out DataTable dtBangDiem, out string thongBao)
        {
            dtBangDiem = null;

            try
            {
                SessionManager.EnsureGiaoVienPermission("Bảng điểm môn học");
            }
            catch (UnauthorizedAccessException authEx)
            {
                thongBao = authEx.Message;
                return false;
            }

            if (string.IsNullOrWhiteSpace(maLop) || string.IsNullOrWhiteSpace(maMH) || (lan != 1 && lan != 2))
            {
                thongBao = "Vui lòng chọn đầy đủ Tên lớp, Tên môn học và Lần thi (1 hoặc 2)!";
                return false;
            }

            try
            {
                dtBangDiem = _dal.LayBangDiemMonHoc(maLop, maMH, lan);
                thongBao = string.Format("Đã kết xuất Bảng điểm môn [{0}] - Lớp [{1}] - Lần thi [{2}] ({3} sinh viên).",
                    maMH.Trim(), maLop.Trim(), lan, dtBangDiem != null ? dtBangDiem.Rows.Count : 0);
                return true;
            }
            catch (SqlException sqlEx)
            {
                thongBao = sqlEx.Message;
                return false;
            }
            catch (Exception ex)
            {
                thongBao = "Lỗi khi tải Bảng điểm môn học: " + ex.Message;
                return false;
            }
        }
    }
}

