using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Giao diện chính (Main Dashboard) của Hệ thống Quản lý Thi Trắc Nghiệm PTIT.
    /// Tự động phân quyền giao diện và kiểm tra quyền nghiệp vụ giữa Giáo viên và Sinh viên.
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

            btnTaoTaiKhoan = CreateSidebarButton("  4. Tạo tài khoản Giảng viên", topOffset);
            topOffset += btnHeight + gap;

            btnDoiMatKhau = CreateSidebarButton("  5. Đổi mật khẩu", topOffset);
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
                Location = new Point(520, 18),
                Size = new Size(460, 26)
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
            btnTaoTaiKhoan.Click += (s, e) => NavigateToModule(btnTaoTaiKhoan, "TẠO TÀI KHOẢN GIẢNG VIÊN");
            btnDoiMatKhau.Click += (s, e) => NavigateToModule(btnDoiMatKhau, "ĐỔI MẬT KHẨU GIẢNG VIÊN");
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
        /// Áp dụng phân quyền chặt chẽ dựa trên SessionManager.CurrentUser
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
            lblUserRoleBadge.Text = isGV ? "QUYỀN: GIẢNG VIÊN (TOÀN QUYỀN)" : "QUYỀN: SINH VIÊN (THI & XEM ĐIỂM)";
            lblUserRoleBadge.BackColor = isGV ? Color.FromArgb(22, 101, 52) : Color.FromArgb(30, 64, 175);

            lblSessionDetails.Text = isGV
                ? string.Format("Tài khoản: {0} | Mã GV: {1} | Vai trò: Giảng viên", user.TenDangNhap, user.MaGV)
                : string.Format("Mã SV: {0} | Lớp: {1} ({2}) | Vai trò: Sinh viên", user.MaSV, user.MaLop, user.TenLop);

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
                else
                {
                    OpenChildForm(CreatePlaceholderModuleForm(title));
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

        private Form CreatePlaceholderModuleForm(string title)
        {
            Form frm = new Form { BackColor = Color.White };
            Label lbl = new Label
            {
                Text = string.Format("Chức năng [{0}] đã sẵn sàng tầng BLL/DAL và sẽ được gắn giao diện ở Giai đoạn tiếp theo.", title),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 12F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
            frm.Controls.Add(lbl);
            return frm;
        }

        public void ShowHomeDashboard()
        {
            HighlightActiveButton(btnTrangChu);
            lblCurrentPageTitle.Text = SessionManager.IsGiaoVien()
                ? "TRANG CHỦ QUẢN TRỊ & DỮ LIỆU NỀN (GIẢNG VIÊN)"
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
            DataTable dtStats = statBLL.LayThongKeDuLieuHienTai();

            Panel pnlWelcome = new Panel
            {
                Dock = DockStyle.Top,
                Height = 115,
                BackColor = Color.White,
                Padding = new Padding(22)
            };

            Label lblWelcomeTitle = new Label
            {
                Text = string.Format("Xin chào Giảng viên: {0} (Mã GV: {1})",
                    SessionManager.CurrentUser.HoTen, SessionManager.CurrentUser.MaGV),
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Location = new Point(20, 18),
                AutoSize = true
            };

            Label lblWelcomeDesc = new Label
            {
                Text = "Bạn đang đăng nhập với quyền GIẢNG VIÊN. Bạn có toàn quyền quản lý Môn học, Lớp, Sinh viên, Giáo viên, " +
                       "Tạo tài khoản giảng viên, Đổi mật khẩu và Sao lưu / Phục hồi cơ sở dữ liệu TRAC_NGHIEM.",
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(20, 54),
                Size = new Size(920, 45)
            };

            pnlWelcome.Controls.Add(lblWelcomeTitle);
            pnlWelcome.Controls.Add(lblWelcomeDesc);

            GroupBox grpStats = new GroupBox
            {
                Text = "Thống kê trực tiếp dữ liệu nền trong SQL Server (Database: TRAC_NGHIEM)",
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(15)
            };

            DataGridView dgvStats = new DataGridView
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
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };
            dgvStats.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvStats.RowTemplate.Height = 34;
            dgvStats.DataSource = dtStats;

            grpStats.Controls.Add(dgvStats);

            container.Controls.Add(grpStats);
            container.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 16 });
            container.Controls.Add(pnlWelcome);
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
                Text = string.Format("Sinh viên: {0}  |  Mã SV (Login): {1}  |  Lớp: {2} - {3}",
                    sv.HoTen, sv.MaSV, sv.MaLop, sv.TenLop),
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 64, 175),
                Location = new Point(20, 16),
                AutoSize = true
            };

            Label lblSvNote = new Label
            {
                Text = "Phân quyền bảo mật: Tài khoản Sinh viên chỉ được phép xem lịch thi của lớp mình và xem điểm thi của chính bản thân.\n" +
                       "Toàn bộ chức năng Quản trị dữ liệu nền (Môn học, Lớp, Sinh viên, Giáo viên, Tạo tài khoản, Backup/Restore) đã được khóa.",
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

            // Khu vực 1: Lịch thi của lớp sinh viên
            GroupBox grpLichThi = new GroupBox
            {
                Text = string.Format("Lịch thi trắc nghiệm đã đăng ký cho lớp [{0}]", sv.MaLop),
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            DataGridView dgvLichThi = CreateReadOnlyGrid();
            const string sqlLichThi = @"
                SELECT RTRIM(dk.MAMH) AS [Mã MH], mh.TENMH AS [Tên môn học],
                       dk.LAN AS [Lần thi], dk.TRINHDO AS [Trình độ],
                       dk.SOCAUTHI AS [Số câu], dk.THOIGIAN AS [Thời gian (phút)],
                       dk.NGAYTHI AS [Ngày thi]
                FROM dbo.GIAOVIEN_DANGKY dk
                INNER JOIN dbo.MONHOC mh ON dk.MAMH = mh.MAMH
                WHERE RTRIM(dk.MALOP) = @MaLop
                ORDER BY dk.NGAYTHI DESC;";
            dgvLichThi.DataSource = DatabaseConnection.ExecuteQuery(
                sqlLichThi,
                CommandType.Text,
                new SqlParameter("@MaLop", SqlDbType.NVarChar, 10) { Value = sv.MaLop });
            grpLichThi.Controls.Add(dgvLichThi);
            split.Panel1.Controls.Add(grpLichThi);

            // Khu vực 2: Kết quả điểm thi của riêng sinh viên đang đăng nhập
            GroupBox grpKetQua = new GroupBox
            {
                Text = string.Format("Kết quả điểm thi cá nhân của sinh viên [{0} - {1}] (Không hiển thị điểm của SV khác)", sv.MaSV, sv.HoTen),
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            DataGridView dgvDiem = CreateReadOnlyGrid();
            const string sqlDiem = @"
                SELECT RTRIM(bd.MAMH) AS [Mã MH], mh.TENMH AS [Tên môn học],
                       bd.LAN AS [Lần thi], bd.NGAYTHI AS [Ngày thi],
                       bd.DIEM AS [Điểm số], bd.BAITHI AS [Chi tiết bài làm]
                FROM dbo.BANGDIEM bd
                INNER JOIN dbo.MONHOC mh ON bd.MAMH = mh.MAMH
                WHERE RTRIM(bd.MASV) = @MaSV
                ORDER BY bd.NGAYTHI DESC;";
            dgvDiem.DataSource = DatabaseConnection.ExecuteQuery(
                sqlDiem,
                CommandType.Text,
                new SqlParameter("@MaSV", SqlDbType.NVarChar, 10) { Value = sv.MaSV });
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

