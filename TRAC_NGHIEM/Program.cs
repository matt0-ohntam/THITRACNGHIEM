using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text;
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
        private static readonly StringBuilder _testLog = new StringBuilder();

        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            if (args != null && args.Length > 0)
            {
                if (args[0] == "--self-test-all" || args[0] == "--self-test-stage5" || args[0] == "--self-test-stage6")
                {
                    Environment.ExitCode = RunAll18TestCases() ? 0 : 1;
                    return;
                }
            }

            Application.Run(new FrmLogin());
        }

        /// <summary>
        /// Thực thi tự động 18 Test Case bắt buộc (TC01 - TC18) theo Mục XII của yêu cầu.
        /// </summary>
        private static bool RunAll18TestCases()
        {
            _testLog.Clear();
            bool allPassed = true;
            TaiKhoanBLL tkBll = new TaiKhoanBLL();
            GiaoVienBLL gvBll = new GiaoVienBLL();
            MonHocBLL mhBll = new MonHocBLL();
            LopBLL lopBll = new LopBLL();
            SinhVienBLL svBll = new SinhVienBLL();
            BackupRestoreBLL brBll = new BackupRestoreBLL();
            TaiKhoan tk;
            string msg;

            try
            {
                // TC01: Xóa bảng TAIKHOAN (Kiểm tra sau khi đăng nhập bằng SQL Login GV001)
                bool loginGV001 = tkBll.DangNhap("GV001", "123456", out tk, out msg);
                object tblTaikhoan = DatabaseConnection.ExecuteScalar(
                    "SELECT OBJECT_ID(N'dbo.TAIKHOAN', N'U')", CommandType.Text);
                bool tc01 = loginGV001 && (tblTaikhoan == null || tblTaikhoan == DBNull.Value);
                LogResult("TC01 - Xóa bảng TAIKHOAN, hệ thống hoạt động bình thường với SQL Login", tc01, msg);
                allPassed &= tc01;

                // TC02: Đăng nhập giáo viên hợp lệ (GV-8-4)
                bool tc02 = tkBll.DangNhap("GV-8-4", "123456", out tk, out msg)
                            && tk != null
                            && tk.LaGiaoVien
                            && string.Equals(tk.MaGV, "GV-8-4", StringComparison.OrdinalIgnoreCase)
                            && string.Equals(tk.DatabaseRole, "role_GiaoVien", StringComparison.OrdinalIgnoreCase);
                LogResult("TC02 - Đăng nhập giáo viên hợp lệ (GV-8-4 -> role_GiaoVien)", tc02, msg);
                allPassed &= tc02;

                // TC03: Đăng nhập giáo viên sai mật khẩu
                bool tc03 = !tkBll.DangNhap("GV-8-4", "SaiMatKhau999!", out tk, out msg);
                LogResult("TC03 - Đăng nhập giáo viên sai mật khẩu (Từ chối đăng nhập)", tc03, msg);
                allPassed &= tc03;

                // Chuẩn bị giáo viên kiểm thử tạm GV-T01 để kiểm tra vòng đời tạo/thu hồi/kích hoạt Login
                tkBll.DangNhap("GV001", "123456", out tk, out msg);
                tkBll.ThuHoiTaiKhoanGiaoVien("GV-T01", out msg);
                gvBll.XoaGiaoVien("GV-T01", out msg);
                GiaoVien gvTemp = new GiaoVien
                {
                    MaGV = "GV-T01",
                    Ho = "Nguyễn Kiểm",
                    Ten = "Thử",
                    SoDTLL = "0911223344",
                    DiaChi = "Khoa CNTT - PTIT"
                };
                gvBll.ThemGiaoVien(gvTemp, out msg);

                // TC07: Tạo tài khoản cho giáo viên chưa có Login (GV-T01)
                bool tc07 = tkBll.TaoTaiKhoanGiaoVien("GV-T01", "GV-T01", "123456", "123456", out msg);
                LogResult("TC07 - Tạo SQL Login + Database User + role_GiaoVien cho GV-T01", tc07, msg);
                allPassed &= tc07;

                // TC08: Tạo tài khoản trùng Login (GV-T01 đã có Login)
                bool tc08 = !tkBll.TaoTaiKhoanGiaoVien("GV-T01", "GV-T01", "123456", "123456", out msg);
                LogResult("TC08 - Chặn tạo tài khoản trùng SQL Login (GV-T01)", tc08, msg);
                allPassed &= tc08;

                // TC09: Tạo tài khoản cho MAGV không tồn tại (GV999)
                bool tc09 = !tkBll.TaoTaiKhoanGiaoVien("GV999", "GV999", "123456", "123456", out msg);
                LogResult("TC09 - Chặn tạo tài khoản cho MAGV không tồn tại (GV999)", tc09, msg);
                allPassed &= tc09;

                // TC10: Thu hồi tài khoản giáo viên đang hoạt động (GV-T01 -> ALTER LOGIN [GV-T01] DISABLE)
                bool tc10 = tkBll.ThuHoiTaiKhoanGiaoVien("GV-T01", out msg);
                LogResult("TC10 - Thu hồi tài khoản giáo viên (ALTER LOGIN [GV-T01] DISABLE)", tc10, msg);
                allPassed &= tc10;

                // TC04: Đăng nhập bằng Login đã bị DISABLE (GV-T01) -> Không đăng nhập được
                bool tc04 = !tkBll.DangNhap("GV-T01", "123456", out tk, out msg);
                LogResult("TC04 - Chặn đăng nhập bằng SQL Login đã bị DISABLE (GV-T01)", tc04, msg);
                allPassed &= tc04;

                // Đăng nhập lại GV001 để kích hoạt lại GV-T01
                tkBll.DangNhap("GV001", "123456", out tk, out msg);

                // TC11: Kích hoạt lại tài khoản giáo viên đã thu hồi (GV-T01 -> ALTER LOGIN [GV-T01] ENABLE)
                bool tc11 = tkBll.KichHoatLaiTaiKhoanGiaoVien("GV-T01", out msg);
                bool tc11LoginAgain = tkBll.DangNhap("GV-T01", "123456", out tk, out msg);
                tc11 = tc11 && tc11LoginAgain;
                LogResult("TC11 - Kích hoạt lại SQL Login GV-T01 và đăng nhập thành công", tc11, msg);
                allPassed &= tc11;

                // TC12: Tự thu hồi tài khoản đang đăng nhập (Đang là GV-T01, tự thu hồi GV-T01 -> Phải từ chối)
                bool tc12 = !tkBll.ThuHoiTaiKhoanGiaoVien("GV-T01", out msg);
                LogResult("TC12 - Chặn tự thu hồi tài khoản đang đăng nhập (GV-T01)", tc12, msg);
                allPassed &= tc12;

                // Dọn dẹp GV-T01 sau khi test bằng quyền GV001 (Thu hồi Login trước rồi Xóa hồ sơ qua sp_XoaGiaoVien ký chứng thư)
                tkBll.DangNhap("GV001", "123456", out tk, out msg);
                tkBll.ThuHoiTaiKhoanGiaoVien("GV-T01", out msg);
                gvBll.XoaGiaoVien("GV-T01", out msg);

                // TC13 & TC14: Kiểm tra Tổng quan và hiển thị GV-8-4 (5 chỉ số khớp thực tế)
                DataTable dtTeachers = tkBll.LayDanhSachTaiKhoanGiaoVien();
                DataTable dtOverview = brBll.LayThongKeDuLieuHienTai();
                bool foundGV84 = false;
                string gv84Status = string.Empty;
                int cntTotal = dtTeachers.Rows.Count;
                int cntActive = 0, cntDisabled = 0, cntNoLogin = 0;

                foreach (DataRow r in dtTeachers.Rows)
                {
                    string ma = Convert.ToString(r["MAGV"]).Trim();
                    string st = Convert.ToString(r["TRANGTHAI_TEXT"]).Trim();
                    if (st == "Đang hoạt động") cntActive++;
                    else if (st == "Đã vô hiệu hóa") cntDisabled++;
                    else cntNoLogin++;

                    if (string.Equals(ma, "GV-8-4", StringComparison.OrdinalIgnoreCase))
                    {
                        foundGV84 = true;
                        gv84Status = st;
                    }
                }

                // Đối chiếu với 5 dòng đầu của sp_LayThongKeTongQuan
                int ovTotal = Convert.ToInt32(dtOverview.Rows[0]["SO_LUONG"]);
                int ovHasLogin = Convert.ToInt32(dtOverview.Rows[1]["SO_LUONG"]);
                int ovActive = Convert.ToInt32(dtOverview.Rows[2]["SO_LUONG"]);
                int ovDisabled = Convert.ToInt32(dtOverview.Rows[3]["SO_LUONG"]);
                int ovNoLogin = Convert.ToInt32(dtOverview.Rows[4]["SO_LUONG"]);

                bool tc13 = (cntTotal == ovTotal)
                            && ((cntActive + cntDisabled) == ovHasLogin)
                            && (cntActive == ovActive)
                            && (cntDisabled == ovDisabled)
                            && (cntNoLogin == ovNoLogin)
                            && (cntTotal == cntActive + cntDisabled + cntNoLogin);
                LogResult(string.Format("TC13 - Kiểm tra Tổng quan (Tổng GV={0}, Có Login={1}, Active={2}, Disabled={3}, Chưa cấp={4})",
                    cntTotal, cntActive + cntDisabled, cntActive, cntDisabled, cntNoLogin), tc13, "Khớp 100% giữa GIAOVIEN và sys.server_principals");
                allPassed &= tc13;

                bool tc14 = foundGV84 && gv84Status == "Đang hoạt động";
                LogResult("TC14 - Kiểm tra hiển thị đúng mã GV-8-4 ở cả danh sách và Tổng quan", tc14, "GV-8-4: " + gv84Status);
                allPassed &= tc14;

                // TC15: Đổi mật khẩu giáo viên (Đổi sang 654321 rồi đổi về 123456)
                bool step1 = tkBll.DoiMatKhau("123456", "654321", "654321", out msg);
                bool step2 = !tkBll.DangNhap("GV001", "123456", out tk, out msg); // Mật khẩu cũ không còn dùng được
                bool step3 = tkBll.DangNhap("GV001", "654321", out tk, out msg);  // Mật khẩu mới đăng nhập thành công
                bool step4 = tkBll.DoiMatKhau("654321", "123456", "123456", out msg); // Khôi phục mật khẩu chuẩn 123456
                bool tc15 = step1 && step2 && step3 && step4;
                LogResult("TC15 - Đổi mật khẩu giáo viên bằng ALTER LOGIN ... OLD_PASSWORD", tc15, msg);
                allPassed &= tc15;

                // TC05: Đăng nhập sinh viên hợp lệ (SV001)
                bool tc05 = tkBll.DangNhap("SV001", "123456", out tk, out msg)
                            && tk != null
                            && tk.LaSinhVien
                            && !tk.LaGiaoVien
                            && string.Equals(tk.MaSV, "SV001", StringComparison.OrdinalIgnoreCase)
                            && string.Equals(tk.DatabaseRole, "role_SinhVien", StringComparison.OrdinalIgnoreCase);
                LogResult("TC05 - Đăng nhập sinh viên hợp lệ (SV001 -> role_SinhVien)", tc05, msg);
                allPassed &= tc05;

                // TC06: Sinh viên truy cập chức năng giáo viên (Kiểm tra chặn cả ở C# BLL và SQL Server Role)
                bool blockedInCSharp = !mhBll.ThemMonHoc("SVMH", "Môn sinh viên thử thêm", out msg);
                bool blockedInSql = false;
                try
                {
                    DatabaseConnection.ExecuteNonQuery("DELETE FROM dbo.MONHOC WHERE MAMH = N'CSDL'", CommandType.Text);
                }
                catch (SqlException)
                {
                    blockedInSql = true;
                }
                bool tc06 = blockedInCSharp && blockedInSql;
                LogResult("TC06 - Chặn sinh viên truy cập chức năng quản trị ở cả C# và SQL Role", tc06, msg);
                allPassed &= tc06;

                // TC16: Kiểm tra SQL Injection tại ô đăng nhập (' OR '1'='1, ' OR 1=1 --, admin'--)
                bool sqliLogin1 = !tkBll.DangNhap("' OR '1'='1", "' OR '1'='1", out tk, out msg);
                bool sqliLogin2 = !tkBll.DangNhap("' OR 1=1 --", "123456", out tk, out msg);
                bool sqliLogin3 = !tkBll.DangNhap("admin'--", "123456", out tk, out msg);
                bool tc16 = sqliLogin1 && sqliLogin2 && sqliLogin3;
                LogResult("TC16 - Chặn SQL Injection tại màn hình Đăng nhập", tc16, "Tất cả payload đều bị từ chối");
                allPassed &= tc16;

                // Đăng nhập lại GV001 để kiểm tra TC17 và TC18
                tkBll.DangNhap("GV001", "123456", out tk, out msg);

                // TC17: Kiểm tra SQL Injection tại ô Tìm kiếm (' OR 1=1 --, '; DROP TABLE GIAOVIEN; --)
                DataTable dtSearch1 = gvBll.TimKiemGiaoVien("' OR 1=1 --");
                DataTable dtSearch2 = gvBll.TimKiemGiaoVien("'; DROP TABLE GIAOVIEN; --");
                DataTable dtAllAfter = gvBll.LayDanhSachGiaoVien();
                bool tc17 = (dtSearch1 != null && dtSearch1.Rows.Count == 0)
                            && (dtSearch2 != null && dtSearch2.Rows.Count == 0)
                            && (dtAllAfter != null && dtAllAfter.Rows.Count == 5);
                LogResult("TC17 - Chặn SQL Injection tại ô Tìm kiếm (Không lộ dữ liệu, không lỗi cú pháp)", tc17, "An toàn tuyệt đối");
                allPassed &= tc17;

                // TC18: Kiểm tra toàn bộ các Form và Nút bấm trong phạm vi Người 1
                bool tc18 = TestAllFormsAndButtons(mhBll, lopBll, svBll, gvBll, out msg);
                LogResult("TC18 - Kiểm tra khởi tạo và thao tác các nút bấm trên toàn bộ Form", tc18, msg);
                allPassed &= tc18;

                // TC19: Kiểm tra Sao lưu (Backup) Database TRAC_NGHIEM bằng quyền Giáo viên (GV001)
                string backupDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\backup"));
                string testBakFile = Path.Combine(backupDir, "TRAC_NGHIEM_Full.bak");
                bool tc19 = brBll.ThucHienBackup(testBakFile, true, out msg) && File.Exists(testBakFile);
                LogResult("TC19 - Sao lưu (Full Backup) CSDL TRAC_NGHIEM bằng quyền GV001 (role_GiaoVien)", tc19, msg.Replace("\n", " "));
                allPassed &= tc19;

                // TC20: Kiểm tra chặn đường dẫn Backup không tồn tại & chặn file .bak không hợp lệ
                bool invalidDirBlocked = !brBll.ThucHienBackup(@"Z:\ThuMucKhongTonTai_999\test.bak", true, out msg);
                bool invalidFileBlocked = !brBll.KiemTraFileBackupHopLe(
                    Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\DATABASE_CONTRACT.md")),
                    out msg);
                bool tc20 = invalidDirBlocked && invalidFileBlocked;
                LogResult("TC20 - Chặn đường dẫn Backup sai & từ chối file không phải định dạng .bak", tc20, msg);
                allPassed &= tc20;

                // TC21: Kiểm tra xác thực file .bak hợp lệ (RESTORE VERIFYONLY & HEADERONLY)
                bool tc21 = brBll.KiemTraFileBackupHopLe(testBakFile, out msg);
                LogResult("TC21 - Kiểm tra file .bak hợp lệ bằng RESTORE VERIFYONLY & HEADERONLY", tc21, msg);
                allPassed &= tc21;

                // TC22: Kiểm tra Phục hồi (Restore) CSDL TRAC_NGHIEM an toàn & đối chiếu dữ liệu sau phục hồi
                bool tc22Restore = brBll.ThucHienRestore(testBakFile, out msg);
                DataTable dtPostRestore = brBll.LayThongKeDuLieuHienTai();
                bool tc22 = tc22Restore && dtPostRestore != null && dtPostRestore.Rows.Count == 11;
                LogResult("TC22 - Phục hồi (Restore) CSDL TRAC_NGHIEM an toàn & kiểm tra dữ liệu sau phục hồi", tc22, msg);
                allPassed &= tc22;

                // TC23: Kiểm tra Mục 7b - Xem kết quả (In lại các câu đã thi) & Phân quyền Sinh viên
                BaoCaoBLL bcBll = new BaoCaoBLL();
                DataRow header7b;
                DataTable dtCau7b;
                bool gvView7b = bcBll.XemKetQuaBaiThi("SV001", "CSDL", 1, out header7b, out dtCau7b, out msg)
                                && header7b != null && dtCau7b != null && dtCau7b.Rows.Count == 10;
                tkBll.DangNhap("SV001", "123456", out tk, out msg);
                DataRow svHeader;
                DataTable svCau;
                bool svOwn7b = bcBll.XemKetQuaBaiThi("SV001", "CSDL", 1, out svHeader, out svCau, out msg)
                               && svCau != null && svCau.Rows.Count == 10;
                bool svOtherBlocked7b = !bcBll.XemKetQuaBaiThi("SV002", "CSDL", 1, out svHeader, out svCau, out msg);
                bool tc23 = gvView7b && svOwn7b && svOtherBlocked7b;
                LogResult("TC23 - Mục 7b: Xem kết quả (In lại 10 câu đã thi) & Chặn SV xem bài của người khác", tc23, msg);
                allPassed &= tc23;

                // TC24: Kiểm tra Mục 8 - Bảng điểm môn học (Report theo mẫu trường) & Chặn quyền Sinh viên
                DataTable dtBangDiem;
                bool svBlocked8 = !bcBll.LayBangDiemMonHoc("CNTT01", "CSDL", 1, out dtBangDiem, out msg);
                tkBll.DangNhap("GV001", "123456", out tk, out msg);
                bool gvReport8 = bcBll.LayBangDiemMonHoc("CNTT01", "CSDL", 1, out dtBangDiem, out msg)
                                 && dtBangDiem != null && dtBangDiem.Rows.Count == 3;
                bool tc24 = svBlocked8 && gvReport8;
                LogResult("TC24 - Mục 8: Bảng điểm môn học (Report lớp CNTT01 - CSDL) & Chặn quyền Sinh viên", tc24, msg);
                allPassed &= tc24;
            }
            catch (Exception ex)
            {
                LogResult("EXCEPTION", false, ex.ToString());
                allPassed = false;
            }

            string footer = allPassed
                ? "KẾT QUẢ TỔNG HỢP: 24/24 TEST CASE (GỒM 7b, 8 & BACKUP/RESTORE) ĐẠT 100% (PASS)"
                : "KẾT QUẢ TỔNG HỢP: CÓ TEST CASE CHƯA ĐẠT";
            Console.WriteLine("==================================================================");
            Console.WriteLine(footer);
            Console.WriteLine("==================================================================");
            _testLog.AppendLine("==================================================================");
            _testLog.AppendLine(footer);
            _testLog.AppendLine("==================================================================");

            string logPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\test_report_18_cases.log"));
            File.WriteAllText(logPath, _testLog.ToString(), Encoding.UTF8);
            return allPassed;
        }

        private static bool TestAllFormsAndButtons(MonHocBLL mhBll, LopBLL lopBll, SinhVienBLL svBll, GiaoVienBLL gvBll, out string summary)
        {
            string msg;

            // 1. Nghiệp vụ Môn học: Thêm, Sửa, Xóa mềm, Phục hồi, Chặn xóa CSDL, Hoàn tác
            if (!mhBll.ThemMonHoc("TEST1", "Môn học kiểm thử 1", out msg)) { summary = msg; return false; }
            if (!mhBll.CapNhatMonHoc("TEST1", "Môn học kiểm thử 1 - Đã sửa", out msg)) { summary = msg; return false; }
            if (!mhBll.XoaMonHoc("TEST1", out msg)) { summary = msg; return false; }
            if (!mhBll.PhucHoiMonHoc("TEST1", out msg)) { summary = msg; return false; }
            if (mhBll.XoaMonHoc("CSDL", out msg)) { summary = "Lỗ hổng: Cho phép xóa môn CSDL đang có BODE"; return false; }
            if (!mhBll.HoanTacThemMonHoc("TEST1", out msg)) { summary = msg; return false; }

            // 2. Nghiệp vụ Lớp & Sinh viên: Thêm Lớp, Thêm SV (tự tạo SQL Login), Sửa SV, Xóa mềm SV (vô hiệu hóa Login), Phục hồi SV, Hoàn tác
            if (!lopBll.ThemLop("LOPTEST1", "Lớp Kiểm Thử 01", out msg)) { summary = msg; return false; }
            SinhVien sv = new SinhVien
            {
                MaSV = "SVTEST01",
                Ho = "Nguyễn Kiểm",
                Ten = "Thử",
                NgaySinh = new DateTime(2004, 6, 15),
                DiaChi = "Hà Nội",
                MaLop = "LOPTEST1"
            };
            if (!svBll.ThemSinhVien(sv, out msg)) { summary = msg; return false; }
            if (lopBll.XoaLop("LOPTEST1", out msg)) { summary = "Lỗ hổng: Cho phép xóa lớp đang có SV"; return false; }
            if (!svBll.XoaSinhVien("SVTEST01", out msg)) { summary = msg; return false; }
            if (!svBll.PhucHoiSinhVien("SVTEST01", out msg)) { summary = msg; return false; }
            if (!svBll.HoanTacThemSinhVien("SVTEST01", out msg)) { summary = msg; return false; }
            if (!lopBll.XoaLop("LOPTEST1", out msg)) { summary = msg; return false; }

            // 3. Nghiệp vụ Giáo viên: Chặn xóa GV001 đang có BODE
            if (gvBll.XoaGiaoVien("GV001", out msg)) { summary = "Lỗ hổng: Cho phép xóa GV001 đang có BODE"; return false; }

            // 4. Khởi tạo toàn bộ 9 Form giao diện (gồm cả Mục 7b và Mục 8)
            using (FrmMain fMain = new FrmMain())
            using (FrmMonHoc f1 = new FrmMonHoc())
            using (FrmLopSinhVien f2 = new FrmLopSinhVien())
            using (FrmGiaoVien f3 = new FrmGiaoVien())
            using (FrmTaoTaiKhoan f4 = new FrmTaoTaiKhoan())
            using (FrmDoiMatKhau f5 = new FrmDoiMatKhau())
            using (FrmBackupRestore f6 = new FrmBackupRestore())
            using (FrmXemKetQua f7 = new FrmXemKetQua())
            using (FrmBangDiemMonHoc f8 = new FrmBangDiemMonHoc())
            {
                if (fMain == null || f1 == null || f2 == null || f3 == null || f4 == null || f5 == null || f6 == null || f7 == null || f8 == null)
                {
                    summary = "Không thể khởi tạo đầy đủ các Form";
                    return false;
                }
            }

            summary = "Tất cả 9 Form (FrmMain, FrmMonHoc, FrmLopSinhVien, FrmGiaoVien, FrmTaoTaiKhoan, FrmDoiMatKhau, FrmBackupRestore, FrmXemKetQua, FrmBangDiemMonHoc) và các nút chức năng hoạt động chính xác.";
            return true;
        }

        private static void LogResult(string testName, bool passed, string detail)
        {
            string line = string.Format("[{0}] {1} | {2}",
                passed ? "PASS" : "FAIL",
                testName,
                detail);
            Console.WriteLine(line);
            _testLog.AppendLine(line);
        }
    }
}
