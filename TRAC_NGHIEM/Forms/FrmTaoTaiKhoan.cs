using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.Helpers;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Form Tạo Tài Khoản Người Dùng.
    /// Quy định bắt buộc của đề bài:
    /// - Chỉ giáo viên mới có quyền truy cập.
    /// - Chỉ cho phép tạo tài khoản cho giảng viên (mỗi giảng viên chỉ có 1 tài khoản).
    /// - Mật khẩu được băm bằng SHA-256 trước khi lưu xuống SQL Server.
    /// </summary>
    public class FrmTaoTaiKhoan : Form
    {
        private readonly TaiKhoanBLL _bll = new TaiKhoanBLL();

        private SplitContainer splitLayout;
        private GroupBox grpCreateAccount;
        private Label lblChonGV;
        private ComboBox cboGiangVien;
        private Label lblUsername;
        private TextBox txtUsername;
        private Label lblPassword;
        private TextBox txtPassword;
        private Label lblConfirmPassword;
        private TextBox txtConfirmPassword;
        private CheckBox chkTrangThaiHoatDong;
        private CheckBox chkShowPass;
        private Button btnTaoTaiKhoan;
        private Button btnLamMoi;
        private Label lblNotePolicy;

        private GroupBox grpExistingAccounts;
        private Panel pnlGridActions;
        private Button btnKhoaMoTaiKhoan;
        private DataGridView dgvTaiKhoanGV;

        public FrmTaoTaiKhoan()
        {
            SessionManager.EnsureGiaoVienPermission("Tạo tài khoản giảng viên");
            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.Padding = new Padding(4);

            splitLayout = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 420,
                SplitterWidth = 8
            };

            // 1. Khung bên trái: Form tạo tài khoản cho giảng viên
            grpCreateAccount = new GroupBox
            {
                Text = "Cấp tài khoản mới cho Giảng viên",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Padding = new Padding(18)
            };

            lblChonGV = CreateFieldLabel("1. Chọn Giảng viên (chưa có tài khoản):", 22, 36);
            cboGiangVien = new ComboBox
            {
                Location = new Point(22, 62),
                Size = new Size(365, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };
            cboGiangVien.SelectedIndexChanged += CboGiangVien_SelectedIndexChanged;

            lblUsername = CreateFieldLabel("2. Tên đăng nhập (Login Name):", 22, 108);
            txtUsername = new TextBox
            {
                Location = new Point(22, 134),
                Size = new Size(365, 30),
                MaxLength = 50,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };

            lblPassword = CreateFieldLabel("3. Mật khẩu (Tối thiểu 6 ký tự - Băm SHA-256):", 22, 180);
            txtPassword = new TextBox
            {
                Location = new Point(22, 206),
                Size = new Size(365, 30),
                MaxLength = 50,
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular)
            };

            lblConfirmPassword = CreateFieldLabel("4. Xác nhận mật khẩu:", 22, 252);
            txtConfirmPassword = new TextBox
            {
                Location = new Point(22, 278),
                Size = new Size(365, 30),
                MaxLength = 50,
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular)
            };

            chkTrangThaiHoatDong = new CheckBox
            {
                Text = "Kích hoạt tài khoản ngay (Hoạt động)",
                Checked = true,
                Location = new Point(22, 322),
                Size = new Size(350, 26),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 101, 52)
            };

            chkShowPass = new CheckBox
            {
                Text = "Hiển thị mật khẩu đang nhập",
                Checked = false,
                Location = new Point(22, 352),
                Size = new Size(300, 24),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105)
            };
            chkShowPass.CheckedChanged += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !chkShowPass.Checked;
                txtConfirmPassword.UseSystemPasswordChar = !chkShowPass.Checked;
            };

            btnTaoTaiKhoan = new Button
            {
                Text = "TẠO TÀI KHOẢN",
                Location = new Point(22, 392),
                Size = new Size(220, 44),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTaoTaiKhoan.FlatAppearance.BorderSize = 0;
            btnTaoTaiKhoan.Click += BtnTaoTaiKhoan_Click;

            btnLamMoi = new Button
            {
                Text = "LÀM MỚI",
                Location = new Point(252, 392),
                Size = new Size(135, 44),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLamMoi.FlatAppearance.BorderSize = 0;
            btnLamMoi.Click += (s, e) => LoadData();

            lblNotePolicy = new Label
            {
                Text = "Quy tắc bảo mật:\n" +
                       "• Chỉ cấp tài khoản cho Giảng viên (1 GV - 1 Tài khoản).\n" +
                       "• Tài khoản Sinh viên tự động lấy MASV làm Login Name.\n" +
                       "• Mật khẩu được băm một chiều bằng SHA-256, không hiển thị và không lưu plaintext.",
                Location = new Point(22, 452),
                Size = new Size(365, 105),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            grpCreateAccount.Controls.AddRange(new Control[]
            {
                lblChonGV, cboGiangVien, lblUsername, txtUsername,
                lblPassword, txtPassword, lblConfirmPassword, txtConfirmPassword,
                chkTrangThaiHoatDong, chkShowPass, btnTaoTaiKhoan, btnLamMoi, lblNotePolicy
            });

            splitLayout.Panel1.Controls.Add(grpCreateAccount);

            // 2. Khung bên phải: Danh sách tài khoản giảng viên đã cấp
            grpExistingAccounts = new GroupBox
            {
                Text = "Danh sách Tài khoản Giảng viên hiện có (Không hiển thị mật khẩu đã lưu)",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(12)
            };

            pnlGridActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                Padding = new Padding(4)
            };

            btnKhoaMoTaiKhoan = new Button
            {
                Text = "Đổi trạng thái (Khóa / Mở khóa) tài khoản đang chọn",
                Dock = DockStyle.Right,
                Width = 340,
                BackColor = Color.FromArgb(217, 119, 6),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnKhoaMoTaiKhoan.FlatAppearance.BorderSize = 0;
            btnKhoaMoTaiKhoan.Click += BtnKhoaMoTaiKhoan_Click;
            pnlGridActions.Controls.Add(btnKhoaMoTaiKhoan);

            dgvTaiKhoanGV = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            dgvTaiKhoanGV.RowTemplate.Height = 32;

            grpExistingAccounts.Controls.Add(dgvTaiKhoanGV);
            grpExistingAccounts.Controls.Add(pnlGridActions);
            splitLayout.Panel2.Controls.Add(grpExistingAccounts);

            this.Controls.Add(splitLayout);
        }

        private Label CreateFieldLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(365, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
        }

        private void LoadData()
        {
            txtPassword.Clear();
            txtConfirmPassword.Clear();
            chkTrangThaiHoatDong.Checked = true;

            DataTable dtGVChuaCoTK = _bll.LayGiaoVienChuaCoTaiKhoan();
            cboGiangVien.DataSource = dtGVChuaCoTK;
            cboGiangVien.DisplayMember = "HIENTHI";
            cboGiangVien.ValueMember = "MAGV";

            if (dtGVChuaCoTK.Rows.Count > 0)
            {
                cboGiangVien.Enabled = true;
                btnTaoTaiKhoan.Enabled = true;
                CboGiangVien_SelectedIndexChanged(null, EventArgs.Empty);
            }
            else
            {
                cboGiangVien.Enabled = false;
                btnTaoTaiKhoan.Enabled = false;
                txtUsername.Clear();
            }

            DataTable dtAccounts = _bll.LayDanhSachTaiKhoanGiaoVien();
            dgvTaiKhoanGV.DataSource = dtAccounts;

            if (dgvTaiKhoanGV.Columns.Contains("TENDANGNHAP"))
                dgvTaiKhoanGV.Columns["TENDANGNHAP"].HeaderText = "Tên đăng nhập";
            if (dgvTaiKhoanGV.Columns.Contains("MAGV"))
                dgvTaiKhoanGV.Columns["MAGV"].HeaderText = "Mã GV";
            if (dgvTaiKhoanGV.Columns.Contains("HOTEN_GV"))
                dgvTaiKhoanGV.Columns["HOTEN_GV"].HeaderText = "Họ tên Giảng viên";
            if (dgvTaiKhoanGV.Columns.Contains("QUYEN"))
                dgvTaiKhoanGV.Columns["QUYEN"].HeaderText = "Vai trò";
            if (dgvTaiKhoanGV.Columns.Contains("TRANGTHAI_TEXT"))
                dgvTaiKhoanGV.Columns["TRANGTHAI_TEXT"].HeaderText = "Trạng thái";
            if (dgvTaiKhoanGV.Columns.Contains("TRANGTHAI"))
                dgvTaiKhoanGV.Columns["TRANGTHAI"].Visible = false;
            if (dgvTaiKhoanGV.Columns.Contains("NGAYTAO"))
            {
                dgvTaiKhoanGV.Columns["NGAYTAO"].HeaderText = "Ngày tạo";
                dgvTaiKhoanGV.Columns["NGAYTAO"].DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            }
        }

        private void CboGiangVien_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cboGiangVien.SelectedValue != null)
            {
                string maGV = cboGiangVien.SelectedValue.ToString().Trim().ToLowerInvariant();
                if (!maGV.Contains("system.data"))
                {
                    txtUsername.Text = maGV;
                }
            }
        }

        private void BtnTaoTaiKhoan_Click(object sender, EventArgs e)
        {
            if (cboGiangVien.SelectedValue == null)
            {
                MessageBox.Show("Không còn giảng viên nào chưa có tài khoản! Bạn có thể thêm giảng viên mới ở mục Quản lý Giáo viên trước.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string maGV = cboGiangVien.SelectedValue.ToString().Trim();
            string username = txtUsername.Text.Trim();
            string pass = txtPassword.Text;
            string confirm = txtConfirmPassword.Text;
            bool active = chkTrangThaiHoatDong.Checked;

            string thongBao;
            if (_bll.TaoTaiKhoanGiaoVien(maGV, username, pass, confirm, active, out thongBao))
            {
                MessageBox.Show(thongBao, "Cấp tài khoản thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            }
            else
            {
                MessageBox.Show(thongBao, "Không thể tạo tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnKhoaMoTaiKhoan_Click(object sender, EventArgs e)
        {
            if (dgvTaiKhoanGV.CurrentRow == null) return;

            string username = Convert.ToString(dgvTaiKhoanGV.CurrentRow.Cells["TENDANGNHAP"].Value).Trim();
            bool currentStatus = Convert.ToBoolean(dgvTaiKhoanGV.CurrentRow.Cells["TRANGTHAI"].Value);
            bool newStatus = !currentStatus;

            string thongBao;
            if (_bll.CapNhatTrangThaiTaiKhoan(username, newStatus, out thongBao))
            {
                MessageBox.Show(thongBao, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadData();
            }
            else
            {
                MessageBox.Show(thongBao, "Không thể đổi trạng thái", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}

