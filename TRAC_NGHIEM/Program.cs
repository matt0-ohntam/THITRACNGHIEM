using System;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Forms;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string connError;
            if (!DatabaseConnection.TestConnection(out connError))
            {
                MessageBox.Show(
                    "Không thể kết nối đến Cơ sở dữ liệu TRAC_NGHIEM!\n\nChi tiết: " + connError,
                    "Lỗi kết nối SQL Server",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (args != null && args.Length > 0 && args[0] == "--self-test-stage5")
            {
                Environment.ExitCode = RunStage5SelfTest() ? 0 : 1;
                return;
            }

            if (args != null && args.Length > 0 && args[0] == "--self-test-stage6")
            {
                Environment.ExitCode = RunStage6SelfTest() ? 0 : 1;
                return;
            }

            Application.Run(new FrmLogin());
        }

        private static bool RunStage5SelfTest()
        {
            TaiKhoanBLL bll = new TaiKhoanBLL();
            TaiKhoan tk;
            string msg;

            // 1. Test đăng nhập sai mật khẩu
            if (bll.DangNhap("admin", "wrongpass", out tk, out msg)) return false;

            // 2. Test đăng nhập tài khoản không tồn tại
            if (bll.DangNhap("khongtontai", "123456", out tk, out msg)) return false;

            // 3. Test đăng nhập Sinh viên bằng MASV (SV001 / 123456)
            if (!bll.DangNhap("SV001", "123456", out tk, out msg)) return false;
            if (!SessionManager.IsSinhVien() || SessionManager.IsGiaoVien()) return false;

            // 4. Kiểm tra chặn quyền ở tầng BLL khi đang là Sinh viên
            bool blockedCorrectly = false;
            try
            {
                SessionManager.EnsureGiaoVienPermission("Quản lý môn học");
            }
            catch (UnauthorizedAccessException)
            {
                blockedCorrectly = true;
            }
            if (!blockedCorrectly) return false;

            // 5. Test đăng nhập Giảng viên (admin / 123456)
            if (!bll.DangNhap("admin", "123456", out tk, out msg)) return false;
            if (!SessionManager.IsGiaoVien() || SessionManager.IsSinhVien()) return false;

            return true;
        }

        private static bool RunStage6SelfTest()
        {
            TaiKhoanBLL tkBll = new TaiKhoanBLL();
            TaiKhoan tk;
            string msg;

            if (!tkBll.DangNhap("admin", "123456", out tk, out msg)) return false;

            // 1. Test MonHocBLL: Thêm, Sửa, Xóa mềm, Phục hồi, Chặn xóa CSDL, Hoàn tác
            MonHocBLL mhBll = new MonHocBLL();
            if (!mhBll.ThemMonHoc("TEST1", "Môn học kiểm thử 1", out msg)) return false;
            if (!mhBll.CapNhatMonHoc("TEST1", "Môn học kiểm thử 1 - Đã sửa", out msg)) return false;
            if (!mhBll.XoaMonHoc("TEST1", out msg)) return false;
            if (!mhBll.PhucHoiMonHoc("TEST1", out msg)) return false;
            if (mhBll.XoaMonHoc("CSDL", out msg)) return false; // Phải chặn xóa CSDL vì có BODE/BANGDIEM
            if (!mhBll.HoanTacThemMonHoc("TEST1", out msg)) return false;

            // 2. Test LopBLL & SinhVienBLL: Thêm Lớp, Thêm SV, Sửa SV, Xóa mềm SV, Phục hồi SV, Chặn xóa lớp đang có SV
            LopBLL lopBll = new LopBLL();
            SinhVienBLL svBll = new SinhVienBLL();
            if (!lopBll.ThemLop("LOPTEST1", "Lớp Kiểm Thử 01", out msg)) return false;

            SinhVien sv = new SinhVien
            {
                MaSV = "SVTEST01",
                Ho = "Nguyễn Kiểm",
                Ten = "Thử",
                NgaySinh = new DateTime(2004, 6, 15),
                DiaChi = "Hà Nội",
                MaLop = "LOPTEST1"
            };
            if (!svBll.ThemSinhVien(sv, out msg)) return false;
            if (lopBll.XoaLop("LOPTEST1", out msg)) return false; // Phải chặn xóa lớp đang có SV
            if (!svBll.XoaSinhVien("SVTEST01", out msg)) return false;
            if (!svBll.PhucHoiSinhVien("SVTEST01", out msg)) return false;
            if (!svBll.HoanTacThemSinhVien("SVTEST01", out msg)) return false;
            if (!lopBll.XoaLop("LOPTEST1", out msg)) return false; // Xóa lớp rỗng thành công

            // 3. Test GiaoVienBLL & TaiKhoanBLL: Thêm GV, Chặn xóa GV001 (đang có BODE + TK), Đổi mật khẩu
            GiaoVienBLL gvBll = new GiaoVienBLL();
            if (gvBll.XoaGiaoVien("GV001", out msg)) return false; // Phải chặn xóa GV001
            if (!tkBll.DoiMatKhau("123456", "654321", "654321", out msg)) return false;
            if (!tkBll.DoiMatKhau("654321", "123456", "123456", out msg)) return false; // Đổi lại 123456

            // 4. Kiểm tra khởi tạo được cả 5 Form không lỗi
            using (FrmMonHoc f1 = new FrmMonHoc())
            using (FrmLopSinhVien f2 = new FrmLopSinhVien())
            using (FrmGiaoVien f3 = new FrmGiaoVien())
            using (FrmTaoTaiKhoan f4 = new FrmTaoTaiKhoan())
            using (FrmDoiMatKhau f5 = new FrmDoiMatKhau())
            {
                if (f1 == null || f2 == null || f3 == null || f4 == null || f5 == null) return false;
            }

            return true;
        }
    }
}
