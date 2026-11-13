using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Giao diện chính (Main Dashboard) của Hệ thống Quản lý Thi Trắc Nghiệm PTIT.
    /// Phân quyền theo SQL Server Login & Database Role (role_GiaoVien / role_SinhVien).
    /// </summary>
    public class FrmMain : Form
    {
        private Panel pnlSidebar;
        private Panel pnlSidebarHeader;
        private Label lblAppLogo;
        private Label lblUserRoleBadge;
        private Label lblUserNameInfo;

        private Button btnTrangChu;
        private Button btnMonHoc;
        private Button btnLopSinhVien;
        private Button btnGiaoVien;
        private Button btnTaoTaiKhoan;
        private Button btnDoiMatKhau;
        private Button btnBackupRestore;
        private Button btnDangXuat;

        private Panel pnlTopBar;
        private Label lblCurrentPageTitle;
        private Label lblSessionDetails;

        private Panel pnlContent;
        private Form _activeChildForm;
        private Button _activeMenuButton;

        public Action<Button> OnNavigateModule;

        public FrmMain()
        {
            InitializeComponent();
            ApplyRolePermissions();
            ShowHomeDashboard();
        }

        private void InitializeComponent()
        {
            this.Text = "Hệ thống Quản lý Thi Trắc Nghiệm - Học viện Công nghệ Bưu chính Viễn thông (PTIT)";
            this.Size = new Size(1280, 760);
            this.MinimumSize = new Size(1100, 680);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

            // 1. Sidebar bên trái
            pnlSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 260,
                BackColor = Color.FromArgb(15, 23, 42)
            };

            pnlSidebarHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 135,
                BackColor = Color.FromArgb(185, 28, 28),
                Padding = new Padding(16)
            };

            lblAppLogo = new Label
            {
                Text = "PTIT EXAM SYSTEM",
                Font = new Font("Segoe UI", 13.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(16, 14),
                Size = new Size(228, 28)
            };

            lblUserNameInfo = new Label
            {
                Text = "Người dùng",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(254, 249, 195),
                Location = new Point(16, 48),
                Size = new Size(228, 42)
            };

            lblUserRoleBadge = new Label
            {
                Text = "VAI TRÒ",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(127, 29, 29),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(16, 95),
                Size = new Size(228, 26)
            };

            pnlSidebarHeader.Controls.Add(lblAppLogo);
            pnlSidebarHeader.Controls.Add(lblUserNameInfo);
            pnlSidebarHeader.Controls.Add(lblUserRoleBadge);

            int topOffset = 148;
            int btnHeight = 46;
            int gap = 4;

            btnTrangChu = CreateSidebarButton("  Trang chủ (Tổng quan)", topOffset);
            topOffset += btnHeight + gap;

            btnMonHoc = CreateSidebarButton("  1. Quản lý Môn học", topOffset);
            topOffset += btnHeight + gap;

            btnLopSinhVien = CreateSidebarButton("  2. Quản lý Lớp & Sinh viên", topOffset);
            topOffset += btnHeight + gap;

            btnGiaoVien = CreateSidebarButton("  3. Quản lý Giáo viên", topOffset);
            topOffset += btnHeight + gap;

            btnTaoTaiKhoan = CreateSidebarButton("  4. Quản lý TK Giảng viên", topOffset);
            topOffset += btnHeight + gap;

            btnDoiMatKhau = CreateSidebarButton("  5. Đổi mật khẩu SQL Login", topOffset);
            topOffset += btnHeight + gap;

            btnBackupRestore = CreateSidebarButton("  6. Backup & Restore CSDL", topOffset);

            btnDangXuat = new Button
            {
                Text = "  Đăng xuất phiên làm việc",
                Dock = DockStyle.Bottom,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(127, 29, 29),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnDangXuat.FlatAppearance.BorderSize = 0;
            btnDangXuat.Click += BtnDangXuat_Click;

            pnlSidebar.Controls.Add(btnTrangChu);
            pnlSidebar.Controls.Add(btnMonHoc);
            pnlSidebar.Controls.Add(btnLopSinhVien);
            pnlSidebar.Controls.Add(btnGiaoVien);
            pnlSidebar.Controls.Add(btnTaoTaiKhoan);
            pnlSidebar.Controls.Add(btnDoiMatKhau);
            pnlSidebar.Controls.Add(btnBackupRestore);
            pnlSidebar.Controls.Add(btnDangXuat);
            pnlSidebar.Controls.Add(pnlSidebarHeader);

            // 2. Thanh tiêu đề phía trên (Top Bar)
            pnlTopBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(24, 0, 24, 0)
            };

            lblCurrentPageTitle = new Label
            {
                Text = "TRANG CHỦ HỆ THỐNG",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                AutoSize = true,
                Location = new Point(24, 16)
            };

            lblSessionDetails = new Label
            {
                Text = string.Empty,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TextAlign = ContentAlignment.MiddleRight,
                Location = new Point(460, 18),
                Size = new Size(520, 26)
            };

            pnlTopBar.Controls.Add(lblCurrentPageTitle);
            pnlTopBar.Controls.Add(lblSessionDetails);

            // 3. Vùng hiển thị nội dung chính (Content Workspace)
            pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(241, 245, 249),
                Padding = new Padding(18)
            };

            this.Controls.Add(pnlContent);
            this.Controls.Add(pnlTopBar);
            this.Controls.Add(pnlSidebar);

            // Sự kiện điều hướng menu
            btnTrangChu.Click += (s, e) => ShowHomeDashboard();
            btnMonHoc.Click += (s, e) => NavigateToModule(btnMonHoc, "QUẢN LÝ MÔN HỌC");
            btnLopSinhVien.Click += (s, e) => NavigateToModule(btnLopSinhVien, "QUẢN LÝ LỚP VÀ SINH VIÊN (SUBFORM)");
            btnGiaoVien.Click += (s, e) => NavigateToModule(btnGiaoVien, "QUẢN LÝ GIÁO VIÊN");
            btnTaoTaiKhoan.Click += (s, e) => NavigateToModule(btnTaoTaiKhoan, "QUẢN LÝ TÀI KHOẢN GIẢNG VIÊN (SQL LOGIN)");
            btnDoiMatKhau.Click += (s, e) => NavigateToModule(btnDoiMatKhau, "ĐỔI MẬT KHẨU TÀI KHOẢN SQL LOGIN");
            btnBackupRestore.Click += (s, e) => NavigateToModule(btnBackupRestore, "SAO LƯU & PHỤC HỒI CSDL (BACKUP / RESTORE)");
        }

        private Button CreateSidebarButton(string text, int top)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(0, top),
                Size = new Size(260, 46),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 41, 59);
            return btn;
        }

        private void HighlightActiveButton(Button activeBtn)
        {
            Button[] allButtons = new Button[]
            {
                btnTrangChu, btnMonHoc, btnLopSinhVien, btnGiaoVien,
                btnTaoTaiKhoan, btnDoiMatKhau, btnBackupRestore
            };

            foreach (Button btn in allButtons)
            {
                if (btn == activeBtn)
                {
                    btn.BackColor = Color.FromArgb(37, 99, 235);
                    btn.ForeColor = Color.White;
                    btn.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
                }
                else
                {
                    btn.BackColor = Color.FromArgb(15, 23, 42);
                    btn.ForeColor = Color.FromArgb(226, 232, 240);
                    btn.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
                }
            }
            _activeMenuButton = activeBtn;
        }

        /// <summary>
        /// Áp dụng phân quyền chặt chẽ dựa trên SQL Server Role (role_GiaoVien / role_SinhVien)
        /// </summary>
        private void ApplyRolePermissions()
        {
            if (!SessionManager.IsLoggedIn)
            {
                this.Close();
                return;
            }

            TaiKhoan user = SessionManager.CurrentUser;
            bool isGV = user.LaGiaoVien;

            lblUserNameInfo.Text = string.Format("{0}\n({1})", user.HoTen, user.MaNguoiDung);
            lblUserRoleBadge.Text = isGV ? "ROLE: role_GiaoVien" : "ROLE: role_SinhVien";
            lblUserRoleBadge.BackColor = isGV ? Color.FromArgb(22, 101, 52) : Color.FromArgb(30, 64, 175);

            lblSessionDetails.Text = isGV
                ? string.Format("SQL Login: {0} | DB User: {1} | Role: {2}", user.TenDangNhap, user.DbUserName, user.DatabaseRole)
                : string.Format("SQL Login (MASV): {0} | Lớp: {1} | Role: {2}", user.MaSV, user.MaLop, user.DatabaseRole);

            // Ẩn và khóa toàn bộ chức năng Quản trị nếu là Sinh viên
            btnMonHoc.Visible = isGV;
            btnMonHoc.Enabled = isGV;

            btnLopSinhVien.Visible = isGV;
            btnLopSinhVien.Enabled = isGV;

            btnGiaoVien.Visible = isGV;
            btnGiaoVien.Enabled = isGV;

            btnTaoTaiKhoan.Visible = isGV;
            btnTaoTaiKhoan.Enabled = isGV;

            btnDoiMatKhau.Visible = isGV;
            btnDoiMatKhau.Enabled = isGV;

            btnBackupRestore.Visible = isGV;
            btnBackupRestore.Enabled = isGV;
        }

        private void NavigateToModule(Button clickedButton, string title)
        {
            try
            {
                // Kiểm tra quyền ở tầng xử lý nghiệp vụ trước khi mở bất kỳ form quản trị nào
                SessionManager.EnsureGiaoVienPermission(title);

                HighlightActiveButton(clickedButton);
                lblCurrentPageTitle.Text = title;

                if (OnNavigateModule != null)
                {
                    OnNavigateModule(clickedButton);
                    return;
                }

                if (clickedButton == btnMonHoc)
                {
                    OpenChildForm(new FrmMonHoc());
                }
                else if (clickedButton == btnLopSinhVien)
                {
                    OpenChildForm(new FrmLopSinhVien());
                }
                else if (clickedButton == btnGiaoVien)
                {
                    OpenChildForm(new FrmGiaoVien());
                }
                else if (clickedButton == btnTaoTaiKhoan)
                {
                    OpenChildForm(new FrmTaoTaiKhoan());
                }
                else if (clickedButton == btnDoiMatKhau)
                {
                    OpenChildForm(new FrmDoiMatKhau());
                }
                else if (clickedButton == btnBackupRestore)
                {
                    OpenChildForm(new FrmBackupRestore());
                }
            }
            catch (UnauthorizedAccessException authEx)
            {
                MessageBox.Show(authEx.Message, "Từ chối truy cập", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        public void OpenChildForm(Form childForm)
        {
            if (_activeChildForm != null)
            {
                _activeChildForm.Close();
                _activeChildForm.Dispose();
                _activeChildForm = null;
            }

            pnlContent.Controls.Clear();
            _activeChildForm = childForm;
            childForm.TopLevel = false;
            childForm.FormBorderStyle = FormBorderStyle.None;
            childForm.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(childForm);
            childForm.Show();
        }

        public void ShowHomeDashboard()
        {
            HighlightActiveButton(btnTrangChu);
            lblCurrentPageTitle.Text = SessionManager.IsGiaoVien()
                ? "TRANG CHỦ TỔNG QUAN QUẢN TRỊ & TRẠNG THÁI SQL LOGIN (GIẢNG VIÊN)"
                : "TRANG CHỦ CỔNG THÔNG TIN THI TRẮC NGHIỆM (SINH VIÊN)";

            Form dashboard = new Form
            {
                BackColor = Color.FromArgb(241, 245, 249)
            };

            if (SessionManager.IsGiaoVien())
            {
                BuildGiaoVienDashboard(dashboard);
            }
            else
            {
                BuildSinhVienDashboard(dashboard);
            }

            OpenChildForm(dashboard);
        }

        private void BuildGiaoVienDashboard(Form container)
        {
            BackupRestoreBLL statBLL = new BackupRestoreBLL();
            TaiKhoanBLL tkBLL = new TaiKhoanBLL();

            DataTable dtStats = statBLL.LayThongKeDuLieuHienTai();
            DataTable dtTeachers = tkBLL.LayDanhSachTaiKhoanGiaoVien();

            int tongGiaoVien = dtTeachers != null ? dtTeachers.Rows.Count : 0;
            int soDangHoatDong = 0;
            int soDaVoHieuHoa = 0;
            int soChuaTao = 0;

            if (dtTeachers != null)
            {
                foreach (DataRow row in dtTeachers.Rows)
                {
                    string st = Convert.ToString(row["TRANGTHAI_TEXT"]).Trim();
                    if (string.Equals(st, "Đang hoạt động", StringComparison.OrdinalIgnoreCase))
                        soDangHoatDong++;
                    else if (string.Equals(st, "Đã vô hiệu hóa", StringComparison.OrdinalIgnoreCase))
                        soDaVoHieuHoa++;
                    else
                        soChuaTao++;
                }
            }

            int soCoLogin = soDangHoatDong + soDaVoHieuHoa;

            // 1. Header chào mừng + Nút Làm mới
            Panel pnlWelcome = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = Color.White,
                Padding = new Padding(18, 12, 18, 12)
            };

            Label lblWelcomeTitle = new Label
            {
                Text = string.Format("Xin chào Giảng viên: {0} (MAGV / SQL Login: {1} | Role: {2})",
                    SessionManager.CurrentUser.HoTen,
                    SessionManager.CurrentUser.MaGV,
                    SessionManager.CurrentUser.DatabaseRole),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Location = new Point(18, 12),
                AutoSize = true
            };

            Label lblWelcomeDesc = new Label
            {
                Text = "Số liệu Tổng quan bên dưới được đối chiếu trực tiếp từ bảng GIAOVIEN kết hợp sys.server_principals, sys.database_principals và role_GiaoVien.",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(18, 46),
                Size = new Size(680, 34)
            };

            Button btnRefreshOverview = new Button
            {
                Text = "Làm mới Tổng quan",
                Size = new Size(155, 36),
                Location = new Point(710, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefreshOverview.FlatAppearance.BorderSize = 0;
            btnRefreshOverview.Click += (s, e) => ShowHomeDashboard();

            Button btnOpenAccountMgr = new Button
            {
                Text = "Mở Quản lý TK Login",
                Size = new Size(165, 36),
                Location = new Point(875, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOpenAccountMgr.FlatAppearance.BorderSize = 0;
            btnOpenAccountMgr.Click += (s, e) => NavigateToModule(btnTaoTaiKhoan, "QUẢN LÝ TÀI KHOẢN GIẢNG VIÊN (SQL LOGIN)");

            pnlWelcome.Controls.Add(lblWelcomeTitle);
            pnlWelcome.Controls.Add(lblWelcomeDesc);
            pnlWelcome.Controls.Add(btnRefreshOverview);
            pnlWelcome.Controls.Add(btnOpenAccountMgr);

            // 2. Hàng 5 thẻ số liệu Tổng quan Giáo viên & SQL Login (Theo đúng Mục VII)
            TableLayoutPanel tblCards = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 96,
                ColumnCount = 5,
                RowCount = 1,
                Padding = new Padding(0, 8, 0, 8)
            };
            for (int i = 0; i < 5; i++)
            {
                tblCards.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            }
            tblCards.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            tblCards.Controls.Add(CreateMetricCard("1. Tổng số giáo viên", tongGiaoVien.ToString(), "Bảng GIAOVIEN", Color.FromArgb(30, 41, 59)), 0, 0);
            tblCards.Controls.Add(CreateMetricCard("2. Số GV có SQL Login", soCoLogin.ToString(), "sys.server_principals", Color.FromArgb(37, 99, 235)), 1, 0);
            tblCards.Controls.Add(CreateMetricCard("3. Login đang hoạt động", soDangHoatDong.ToString(), "is_disabled = 0", Color.FromArgb(22, 163, 74)), 2, 0);
            tblCards.Controls.Add(CreateMetricCard("4. Login đã vô hiệu hóa", soDaVoHieuHoa.ToString(), "is_disabled = 1", Color.FromArgb(220, 38, 38)), 3, 0);
            tblCards.Controls.Add(CreateMetricCard("5. GV chưa cấp Login", soChuaTao.ToString(), "Chưa có SQL Login", Color.FromArgb(217, 119, 6)), 4, 0);

            // 3. Khu vực 2 bảng đối chiếu chi tiết
            SplitContainer splitOverview = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 215
            };

            GroupBox grpTeacherLogins = new GroupBox
            {
                Text = "Danh sách đối chiếu Giáo viên & SQL Server Login (Bao gồm GV-8-4)",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10)
            };

            DataGridView dgvTeacherLogins = CreateReadOnlyGrid();
            dgvTeacherLogins.DataSource = dtTeachers;
            dgvTeacherLogins.DataBindingComplete += (s, e) =>
            {
                if (dgvTeacherLogins.Columns.Contains("MAGV"))
                    dgvTeacherLogins.Columns["MAGV"].HeaderText = "MAGV";
                if (dgvTeacherLogins.Columns.Contains("HO"))
                    dgvTeacherLogins.Columns["HO"].HeaderText = "HO (Họ đệm)";
                if (dgvTeacherLogins.Columns.Contains("TEN"))
                    dgvTeacherLogins.Columns["TEN"].HeaderText = "TEN (Tên)";
                if (dgvTeacherLogins.Columns.Contains("TENDANGNHAP"))
                    dgvTeacherLogins.Columns["TENDANGNHAP"].HeaderText = "Username (SQL Login)";
                if (dgvTeacherLogins.Columns.Contains("TRANGTHAI_TEXT"))
                    dgvTeacherLogins.Columns["TRANGTHAI_TEXT"].HeaderText = "Trạng thái Login";
                if (dgvTeacherLogins.Columns.Contains("HANHDONG_TEXT"))
                    dgvTeacherLogins.Columns["HANHDONG_TEXT"].HeaderText = "Hành động tương ứng";

                string[] hiddenCols = { "HOTEN_GV", "TRANGTHAI_CODE", "CO_LOGIN", "CO_DB_USER", "THUOC_ROLE_GV", "IS_DISABLED", "NGAYTAO" };
                foreach (string col in hiddenCols)
                {
                    if (dgvTeacherLogins.Columns.Contains(col))
                    {
                        dgvTeacherLogins.Columns[col].Visible = false;
                    }
                }
            };
            grpTeacherLogins.Controls.Add(dgvTeacherLogins);
            splitOverview.Panel1.Controls.Add(grpTeacherLogins);

            GroupBox grpStats = new GroupBox
            {
                Text = "Thống kê tổng hợp toàn bộ dữ liệu nền trong SQL Server (Database: TRAC_NGHIEM)",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10)
            };

            DataGridView dgvStats = CreateReadOnlyGrid();
            dgvStats.DataSource = dtStats;
            grpStats.Controls.Add(dgvStats);
            splitOverview.Panel2.Controls.Add(grpStats);

            container.Controls.Add(splitOverview);
            container.Controls.Add(tblCards);
            container.Controls.Add(pnlWelcome);
        }

        private Panel CreateMetricCard(string title, string numberText, string subtitle, Color accentColor)
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(4, 0, 4, 0),
                Padding = new Padding(10, 6, 10, 6)
            };

            Label lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(10, 6),
                AutoSize = true
            };

            Label lblValue = new Label
            {
                Text = numberText,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                ForeColor = accentColor,
                Location = new Point(10, 26),
                AutoSize = true
            };

            Label lblSub = new Label
            {
                Text = subtitle,
                Font = new Font("Segoe UI", 8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(10, 58),
                AutoSize = true
            };

            card.Controls.Add(lblTitle);
            card.Controls.Add(lblValue);
            card.Controls.Add(lblSub);
            return card;
        }

        private void BuildSinhVienDashboard(Form container)
        {
            TaiKhoan sv = SessionManager.CurrentUser;

            Panel pnlInfo = new Panel
            {
                Dock = DockStyle.Top,
                Height = 125,
                BackColor = Color.White,
                Padding = new Padding(20)
            };

            Label lblSvHeader = new Label
            {
                Text = string.Format("Sinh viên: {0}  |  SQL Login (MASV): {1}  |  Lớp: {2} - {3}",
                    sv.HoTen, sv.MaSV, sv.MaLop, sv.TenLop),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 64, 175),
                Location = new Point(20, 16),
                AutoSize = true
            };

            Label lblSvNote = new Label
            {
                Text = "Phân quyền bảo mật (SQL Role: role_SinhVien): Tài khoản Sinh viên chỉ được phép xem lịch thi của lớp mình và kết quả điểm thi cá nhân.\n" +
                       "Toàn bộ chức năng Quản trị dữ liệu nền (Môn học, Lớp, Sinh viên, Giáo viên, Quản lý Login, Backup/Restore) bị chặn ở cả giao diện C# và SQL Server.",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, 52),
                Size = new Size(920, 52)
            };

            pnlInfo.Controls.Add(lblSvHeader);
            pnlInfo.Controls.Add(lblSvNote);

            SplitContainer split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 210
            };

            // Khu vực 1: Lịch thi của lớp sinh viên (Stored Procedure)
            GroupBox grpLichThi = new GroupBox
            {
                Text = string.Format("Lịch thi trắc nghiệm đã đăng ký cho lớp [{0}]", sv.MaLop),
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            DataGridView dgvLichThi = CreateReadOnlyGrid();
            dgvLichThi.DataSource = DatabaseConnection.ExecuteQuery(
                "dbo.sp_LayLichThiSinhVien",
                CommandType.StoredProcedure,
                new SqlParameter("@MaLop", SqlDbType.NChar, 8) { Value = sv.MaLop });
            grpLichThi.Controls.Add(dgvLichThi);
            split.Panel1.Controls.Add(grpLichThi);

            // Khu vực 2: Kết quả điểm thi của riêng sinh viên đang đăng nhập (Stored Procedure kiểm tra SUSER_SNAME())
            GroupBox grpKetQua = new GroupBox
            {
                Text = string.Format("Kết quả điểm thi cá nhân của sinh viên [{0} - {1}] (Đối chiếu SUSER_SNAME() = MASV)", sv.MaSV, sv.HoTen),
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            DataGridView dgvDiem = CreateReadOnlyGrid();
            dgvDiem.DataSource = DatabaseConnection.ExecuteQuery(
                "dbo.sp_LayKetQuaThiCaNhan",
                CommandType.StoredProcedure,
                new SqlParameter("@MaSV", SqlDbType.NChar, 8) { Value = sv.MaSV });
            grpKetQua.Controls.Add(dgvDiem);
            split.Panel2.Controls.Add(grpKetQua);

            container.Controls.Add(split);
            container.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 12 });
            container.Controls.Add(pnlInfo);
        }

        private DataGridView CreateReadOnlyGrid()
        {
            DataGridView dgv = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            dgv.RowTemplate.Height = 30;
            return dgv;
        }

        private void BtnDangXuat_Click(object sender, EventArgs e)
        {
            DialogResult confirm = MessageBox.Show(
                "Bạn có muốn đăng xuất khỏi phiên làm việc hiện tại không?",
                "Xác nhận đăng xuất",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                SessionManager.ClearSession();
                this.DialogResult = DialogResult.Retry;
                this.Close();
            }
        }

        public Button BtnMonHoc { get { return btnMonHoc; } }
        public Button BtnLopSinhVien { get { return btnLopSinhVien; } }
        public Button BtnGiaoVien { get { return btnGiaoVien; } }
        public Button BtnTaoTaiKhoan { get { return btnTaoTaiKhoan; } }
        public Button BtnDoiMatKhau { get { return btnDoiMatKhau; } }
        public Button BtnBackupRestore { get { return btnBackupRestore; } }
    }
}
