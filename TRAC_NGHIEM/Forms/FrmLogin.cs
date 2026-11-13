using System;
using System.Drawing;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Form Đăng nhập hệ thống Quản lý Thi Trắc Nghiệm PTIT.
    /// Xác thực trực tiếp bằng SQL Server Authentication (SQL Server Login):
    /// - Giáo viên: Username = MAGV (thuộc role_GiaoVien).
    /// - Sinh viên: Username = MASV (thuộc role_SinhVien).
    /// </summary>
    public class FrmLogin : Form
    {
        private Panel pnlLeftBanner;
        private Label lblBrandTitle;
        private Label lblBrandSubTitle;
        private Label lblBrandDesc;

        private Panel pnlRightForm;
        private Label lblFormHeader;
        private Label lblFormSubHeader;
        private Label lblUsername;
        private TextBox txtUsername;
        private Label lblPassword;
        private TextBox txtPassword;
        private CheckBox chkShowPassword;
        private Button btnLogin;
        private Button btnExit;
        private Label lblStatusMessage;

        private readonly TaiKhoanBLL _taiKhoanBLL = new TaiKhoanBLL();
        private bool _isLoggingIn = false;

        public FrmLogin()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Đăng nhập SQL Server - Hệ thống Quản lý Thi Trắc Nghiệm PTIT";
            this.Size = new Size(820, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.White;
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

            // 1. Panel bên trái: Thương hiệu PTIT
            pnlLeftBanner = new Panel
            {
                Dock = DockStyle.Left,
                Width = 330,
                BackColor = Color.FromArgb(185, 28, 28),
                Padding = new Padding(28)
            };

            lblBrandTitle = new Label
            {
                Text = "PTIT EXAM SYSTEM",
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(25, 45),
                Size = new Size(280, 40)
            };

            lblBrandSubTitle = new Label
            {
                Text = "HỆ THỐNG QUẢN LÝ\nTHI TRẮC NGHIỆM",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(254, 240, 138),
                Location = new Point(25, 95),
                Size = new Size(280, 60)
            };

            lblBrandDesc = new Label
            {
                Text = "Học viện Công nghệ Bưu chính Viễn thông\nXác thực bằng SQL Server Login & Role",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(254, 226, 226),
                Location = new Point(25, 165),
                Size = new Size(280, 50)
            };

            pnlLeftBanner.Controls.Add(lblBrandTitle);
            pnlLeftBanner.Controls.Add(lblBrandSubTitle);
            pnlLeftBanner.Controls.Add(lblBrandDesc);

            // 2. Panel bên phải: Khung nhập thông tin đăng nhập SQL Server
            pnlRightForm = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(40)
            };

            lblFormHeader = new Label
            {
                Text = "ĐĂNG NHẬP HỆ THỐNG",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(45, 38),
                Size = new Size(380, 35)
            };

            lblFormSubHeader = new Label
            {
                Text = "Giáo viên đăng nhập bằng MAGV | Sinh viên đăng nhập bằng MASV",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(45, 75),
                Size = new Size(390, 25)
            };

            lblUsername = new Label
            {
                Text = "Tài khoản SQL Login (MAGV hoặc MASV):",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(45, 118),
                Size = new Size(380, 24)
            };

            txtUsername = new TextBox
            {
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Location = new Point(45, 145),
                Size = new Size(380, 30),
                MaxLength = 50,
                CharacterCasing = CharacterCasing.Upper
            };

            lblPassword = new Label
            {
                Text = "Mật khẩu SQL Server:",
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85),
                Location = new Point(45, 190),
                Size = new Size(380, 24)
            };

            txtPassword = new TextBox
            {
                Font = new Font("Segoe UI", 11F, FontStyle.Regular),
                Location = new Point(45, 217),
                Size = new Size(380, 30),
                MaxLength = 128,
                UseSystemPasswordChar = true
            };

            chkShowPassword = new CheckBox
            {
                Text = "Hiển thị mật khẩu",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(45, 255),
                Size = new Size(180, 24),
                Cursor = Cursors.Hand
            };
            chkShowPassword.CheckedChanged += ChkShowPassword_CheckedChanged;

            lblStatusMessage = new Label
            {
                Text = string.Empty,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(220, 38, 38),
                Location = new Point(45, 285),
                Size = new Size(380, 42)
            };

            btnLogin = new Button
            {
                Text = "ĐĂNG NHẬP",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(185, 28, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(45, 340),
                Size = new Size(215, 44),
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.Click += BtnLogin_Click;

            btnExit = new Button
            {
                Text = "THOÁT",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(275, 340),
                Size = new Size(150, 44),
                Cursor = Cursors.Hand
            };
            btnExit.FlatAppearance.BorderSize = 0;
            btnExit.Click += BtnExit_Click;

            pnlRightForm.Controls.Add(lblFormHeader);
            pnlRightForm.Controls.Add(lblFormSubHeader);
            pnlRightForm.Controls.Add(lblUsername);
            pnlRightForm.Controls.Add(txtUsername);
            pnlRightForm.Controls.Add(lblPassword);
            pnlRightForm.Controls.Add(txtPassword);
            pnlRightForm.Controls.Add(chkShowPassword);
            pnlRightForm.Controls.Add(lblStatusMessage);
            pnlRightForm.Controls.Add(btnLogin);
            pnlRightForm.Controls.Add(btnExit);

            this.Controls.Add(pnlRightForm);
            this.Controls.Add(pnlLeftBanner);

            this.AcceptButton = btnLogin;
            this.CancelButton = btnExit;
        }

        private void ChkShowPassword_CheckedChanged(object sender, EventArgs e)
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (_isLoggingIn) return;

            try
            {
                _isLoggingIn = true;
                btnLogin.Enabled = false;
                lblStatusMessage.Text = string.Empty;

                string username = txtUsername.Text.Trim();
                string password = txtPassword.Text;

                TaiKhoan taiKhoan;
                string thongBao;

                bool success = _taiKhoanBLL.DangNhap(username, password, out taiKhoan, out thongBao);
                if (!success)
                {
                    lblStatusMessage.Text = thongBao;
                    MessageBox.Show(thongBao, "Đăng nhập không thành công", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtPassword.SelectAll();
                    txtPassword.Focus();
                    return;
                }

                // Chuyển sang giao diện FrmMain tương ứng với quyền SQL Server Role
                this.Hide();
                using (FrmMain frmMain = new FrmMain())
                {
                    DialogResult result = frmMain.ShowDialog(this);
                    if (result == DialogResult.Retry)
                    {
                        // Người dùng chọn Đăng xuất -> Giải phóng kết nối SQL và hiển thị lại FrmLogin
                        SessionManager.ClearSession();
                        txtPassword.Clear();
                        lblStatusMessage.Text = string.Empty;
                        this.Show();
                        txtUsername.Focus();
                    }
                    else
                    {
                        SessionManager.ClearSession();
                        this.Close();
                    }
                }
            }
            finally
            {
                _isLoggingIn = false;
                if (!this.IsDisposed)
                {
                    btnLogin.Enabled = true;
                }
            }
        }

        private void BtnExit_Click(object sender, EventArgs e)
        {
            DialogResult confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn thoát chương trình Quản lý Thi Trắc Nghiệm PTIT không?",
                "Xác nhận thoát",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                SessionManager.ClearSession();
                Application.Exit();
            }
        }
    }
}
