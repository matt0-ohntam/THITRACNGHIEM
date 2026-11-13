using System;
using System.Drawing;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Form Đổi mật khẩu dành riêng cho Giáo viên đang đăng nhập.
    /// Sử dụng cơ chế gốc của SQL Server Login (ALTER LOGIN ... WITH PASSWORD ... OLD_PASSWORD ...).
    /// Không lưu mật khẩu vào bất kỳ bảng nào và không cho phép đổi mật khẩu của người khác.
    /// </summary>
    public class FrmDoiMatKhau : Form
    {
        private readonly TaiKhoanBLL _bll = new TaiKhoanBLL();
        private bool _isSaving = false;

        private Panel pnlCard;
        private Label lblHeader;
        private Label lblCurrentUserInfo;
        private Label lblOldPass;
        private TextBox txtOldPass;
        private Label lblNewPass;
        private TextBox txtNewPass;
        private Label lblConfirmPass;
        private TextBox txtConfirmPass;
        private CheckBox chkShowPass;
        private Button btnSave;
        private Button btnClear;

        public FrmDoiMatKhau()
        {
            SessionManager.EnsureGiaoVienPermission("Đổi mật khẩu tài khoản SQL Login");
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

            pnlCard = new Panel
            {
                Size = new Size(540, 450),
                Location = new Point(60, 30),
                BackColor = Color.White,
                Padding = new Padding(30)
            };

            TaiKhoan user = SessionManager.CurrentUser;

            lblHeader = new Label
            {
                Text = "ĐỔI MẬT KHẨU SQL SERVER LOGIN",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Location = new Point(30, 24),
                Size = new Size(480, 32)
            };

            lblCurrentUserInfo = new Label
            {
                Text = string.Format("SQL Login hiện tại: {0}  |  Giáo viên: {1} ({2})  |  Role: {3}",
                    user.TenDangNhap, user.HoTen, user.MaGV, user.DatabaseRole),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 64, 175),
                Location = new Point(30, 62),
                Size = new Size(480, 26)
            };

            lblOldPass = CreateLabel("1. Mật khẩu SQL Login hiện tại:", 30, 105);
            txtOldPass = CreatePasswordBox(30, 130);

            lblNewPass = CreateLabel("2. Mật khẩu mới (Tối thiểu 6 ký tự, khác mật khẩu cũ):", 30, 178);
            txtNewPass = CreatePasswordBox(30, 203);

            lblConfirmPass = CreateLabel("3. Xác nhận mật khẩu mới:", 30, 251);
            txtConfirmPass = CreatePasswordBox(30, 276);

            chkShowPass = new CheckBox
            {
                Text = "Hiển thị mật khẩu khi đang nhập",
                Location = new Point(30, 320),
                Size = new Size(260, 24),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            chkShowPass.CheckedChanged += (s, e) =>
            {
                bool hide = !chkShowPass.Checked;
                txtOldPass.UseSystemPasswordChar = hide;
                txtNewPass.UseSystemPasswordChar = hide;
                txtConfirmPass.UseSystemPasswordChar = hide;
            };

            btnSave = new Button
            {
                Text = "CẬP NHẬT MẬT KHẨU",
                Location = new Point(30, 360),
                Size = new Size(260, 44),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnClear = new Button
            {
                Text = "NHẬP LẠI",
                Location = new Point(305, 360),
                Size = new Size(175, 44),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) =>
            {
                txtOldPass.Clear();
                txtNewPass.Clear();
                txtConfirmPass.Clear();
                chkShowPass.Checked = false;
                txtOldPass.Focus();
            };

            pnlCard.Controls.AddRange(new Control[]
            {
                lblHeader, lblCurrentUserInfo,
                lblOldPass, txtOldPass,
                lblNewPass, txtNewPass,
                lblConfirmPass, txtConfirmPass,
                chkShowPass, btnSave, btnClear
            });

            this.Controls.Add(pnlCard);
        }

        private Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(460, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
        }

        private TextBox CreatePasswordBox(int x, int y)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(450, 30),
                MaxLength = 128,
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular)
            };
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (_isSaving) return;

            try
            {
                _isSaving = true;
                btnSave.Enabled = false;

                string thongBao;
                if (_bll.DoiMatKhau(txtOldPass.Text, txtNewPass.Text, txtConfirmPass.Text, out thongBao))
                {
                    chkShowPass.Checked = false;
                    txtOldPass.Clear();
                    txtNewPass.Clear();
                    txtConfirmPass.Clear();
                    MessageBox.Show(thongBao, "Đổi mật khẩu thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(thongBao, "Không thể đổi mật khẩu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                _isSaving = false;
                btnSave.Enabled = true;
            }
        }
    }
}
