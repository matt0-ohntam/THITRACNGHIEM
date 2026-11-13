using System;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.Helpers;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Form Quản lý Tài khoản Giáo viên bằng SQL Server Login, Database User và role_GiaoVien.
    /// - ComboBox "Chọn Giáo viên" hiển thị TẤT CẢ giáo viên kèm trạng thái.
    /// - Chuyển đổi tức thời (0ms, không giật/nhảy lại dòng cũ) khi chọn trong ComboBox hoặc click trên bảng.
    /// - Khi chọn giáo viên chưa có tài khoản -> Nút hành động chính là [TẠO TÀI KHOẢN] (Xanh lá).
    /// - Khi chọn giáo viên đã tạo tài khoản (Đang hoạt động) -> Nút hành động chính tự động chuyển thành [THU HỒI TÀI KHOẢN] (Đỏ).
    /// - Khi chọn giáo viên đã bị thu hồi (Đã vô hiệu hóa) -> Nút hành động chính tự động chuyển thành [KÍCH HOẠT LẠI] (Xanh dương).
    /// </summary>
    public class FrmTaoTaiKhoan : Form
    {
        private readonly TaiKhoanBLL _bll = new TaiKhoanBLL();
        private bool _isBusy = false;
        private bool _isSyncingSelection = false;
        private int _currentSelectedStatusCode = 0;

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
        private CheckBox chkShowPass;
        private Button btnTaoTaiKhoan;
        private Button btnLamMoi;
        private Label lblNotePolicy;

        private GroupBox grpExistingAccounts;
        private Panel pnlGridActions;
        private Label lblSelectedTeacherStatus;
        private Button btnContextualCreate;
        private Button btnThuHoiTaiKhoan;
        private Button btnKichHoatLaiTaiKhoan;
        private DataGridView dgvTaiKhoanGV;

        public FrmTaoTaiKhoan()
        {
            SessionManager.EnsureGiaoVienPermission("Quản lý tài khoản SQL Server Login của Giáo viên");
            this.DoubleBuffered = true;
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
                SplitterDistance = 395,
                SplitterWidth = 8
            };

            // 1. Khung bên trái: Chọn bất kỳ Giáo viên nào để Tạo / Thu hồi / Kích hoạt lại SQL Login
            grpCreateAccount = new GroupBox
            {
                Text = "Tạo / Thu hồi SQL Server Login cho Giáo viên",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Padding = new Padding(16, 26, 16, 16)
            };

            lblChonGV = CreateFieldLabel("1. Chọn Giáo viên (Tất cả giảng viên):", 18, 34);
            cboGiangVien = new ComboBox
            {
                Location = new Point(18, 58),
                Size = new Size(350, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };
            cboGiangVien.SelectedIndexChanged += CboGiangVien_SelectedIndexChanged;

            lblUsername = CreateFieldLabel("2. Tên SQL Login (Mặc định = MAGV):", 18, 102);
            txtUsername = new TextBox
            {
                Location = new Point(18, 126),
                Size = new Size(350, 30),
                MaxLength = 8,
                ReadOnly = true,
                BackColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };

            lblPassword = CreateFieldLabel("3. Mật khẩu SQL Login (Tối thiểu 6 ký tự):", 18, 170);
            txtPassword = new TextBox
            {
                Location = new Point(18, 194),
                Size = new Size(350, 30),
                MaxLength = 128,
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular)
            };

            lblConfirmPassword = CreateFieldLabel("4. Xác nhận mật khẩu:", 18, 238);
            txtConfirmPassword = new TextBox
            {
                Location = new Point(18, 262),
                Size = new Size(350, 30),
                MaxLength = 128,
                UseSystemPasswordChar = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular)
            };

            chkShowPass = new CheckBox
            {
                Text = "Hiển thị mật khẩu đang nhập",
                Checked = false,
                Location = new Point(18, 304),
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
                Location = new Point(18, 342),
                Size = new Size(210, 44),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTaoTaiKhoan.FlatAppearance.BorderSize = 0;
            btnTaoTaiKhoan.Click += BtnMainAction_Click;

            btnLamMoi = new Button
            {
                Text = "LÀM MỚI",
                Location = new Point(238, 342),
                Size = new Size(130, 44),
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
                Text = "Cơ chế Bảo mật SQL Server:\n" +
                       "• Tạo trực tiếp SQL Server Login & Database User (Không lưu bảng TAIKHOAN).\n" +
                       "• Username luôn bằng MAGV và được gán vào role_GiaoVien.\n" +
                       "• Chọn GV chưa có tài khoản -> Nút [TẠO TÀI KHOẢN].\n" +
                       "• Chọn GV đã có tài khoản -> Nút tự chuyển sang [THU HỒI TÀI KHOẢN].",
                Location = new Point(18, 400),
                Size = new Size(350, 140),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(71, 85, 105)
            };

            grpCreateAccount.Controls.AddRange(new Control[]
            {
                lblChonGV, cboGiangVien, lblUsername, txtUsername,
                lblPassword, txtPassword, lblConfirmPassword, txtConfirmPassword,
                chkShowPass, btnTaoTaiKhoan, btnLamMoi, lblNotePolicy
            });

            splitLayout.Panel1.Controls.Add(grpCreateAccount);

            // 2. Khung bên phải: Danh sách Giáo viên & Trạng thái SQL Server Login
            grpExistingAccounts = new GroupBox
            {
                Text = "Danh sách Giáo viên & Trạng thái SQL Server Login",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(12, 28, 12, 12)
            };

            pnlGridActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = Color.FromArgb(248, 250, 252),
                Padding = new Padding(10, 6, 10, 6)
            };

            lblSelectedTeacherStatus = new Label
            {
                Text = "Chọn một giáo viên trên bảng để thực hiện hành động tương ứng.",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 64, 175)
            };

            btnContextualCreate = new Button
            {
                Text = "Tạo tài khoản",
                Dock = DockStyle.Right,
                Width = 175,
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnContextualCreate.FlatAppearance.BorderSize = 0;
            btnContextualCreate.Click += BtnContextualCreate_Click;

            btnThuHoiTaiKhoan = new Button
            {
                Text = "Thu hồi tài khoản",
                Dock = DockStyle.Right,
                Width = 175,
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnThuHoiTaiKhoan.FlatAppearance.BorderSize = 0;
            btnThuHoiTaiKhoan.Click += BtnThuHoiTaiKhoan_Click;

            btnKichHoatLaiTaiKhoan = new Button
            {
                Text = "Kích hoạt lại tài khoản",
                Dock = DockStyle.Right,
                Width = 190,
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Visible = false
            };
            btnKichHoatLaiTaiKhoan.FlatAppearance.BorderSize = 0;
            btnKichHoatLaiTaiKhoan.Click += BtnKichHoatLaiTaiKhoan_Click;

            pnlGridActions.Controls.Add(lblSelectedTeacherStatus);
            pnlGridActions.Controls.Add(btnContextualCreate);
            pnlGridActions.Controls.Add(btnThuHoiTaiKhoan);
            pnlGridActions.Controls.Add(btnKichHoatLaiTaiKhoan);

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
            EnableDoubleBuffer(dgvTaiKhoanGV);
            dgvTaiKhoanGV.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvTaiKhoanGV.RowTemplate.Height = 32;
            dgvTaiKhoanGV.SelectionChanged += DgvTaiKhoanGV_SelectionChanged;

            grpExistingAccounts.Controls.Add(dgvTaiKhoanGV);
            grpExistingAccounts.Controls.Add(pnlGridActions);
            splitLayout.Panel2.Controls.Add(grpExistingAccounts);

            this.Controls.Add(splitLayout);
        }

        private static void EnableDoubleBuffer(Control ctrl)
        {
            PropertyInfo prop = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            if (prop != null)
            {
                prop.SetValue(ctrl, true, null);
            }
        }

        private Label CreateFieldLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(350, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
        }

        private void LoadData(string selectMaGV = null)
        {
            txtPassword.Clear();
            txtConfirmPassword.Clear();

            DataTable dtAccounts = _bll.LayDanhSachTaiKhoanGiaoVien();
            if (dtAccounts != null)
            {
                if (!dtAccounts.Columns.Contains("HIENTHI_ALL"))
                {
                    dtAccounts.Columns.Add("HIENTHI_ALL", typeof(string));
                }

                foreach (DataRow r in dtAccounts.Rows)
                {
                    string ma = Convert.ToString(r["MAGV"]).Trim();
                    string ho = Convert.ToString(r["HO"]).Trim();
                    string ten = Convert.ToString(r["TEN"]).Trim();
                    string st = Convert.ToString(r["TRANGTHAI_TEXT"]).Trim();
                    r["MAGV"] = ma; // Chuẩn hóa bỏ khoảng trắng thừa của NCHAR(8) để SelectedValue khớp 100%
                    r["HIENTHI_ALL"] = string.Format("{0} - {1} {2} ({3})", ma, ho, ten, st);
                }
            }

            _isSyncingSelection = true;
            try
            {
                dgvTaiKhoanGV.DataSource = dtAccounts;
                ConfigureGridColumns();

                cboGiangVien.DataSource = dtAccounts != null ? dtAccounts.Copy() : null;
                cboGiangVien.DisplayMember = "HIENTHI_ALL";
                cboGiangVien.ValueMember = "MAGV";
                cboGiangVien.Enabled = dtAccounts != null && dtAccounts.Rows.Count > 0;
            }
            finally
            {
                _isSyncingSelection = false;
            }

            string targetMaGV = selectMaGV;
            if (string.IsNullOrEmpty(targetMaGV) && dtAccounts != null && dtAccounts.Rows.Count > 0)
            {
                targetMaGV = Convert.ToString(dtAccounts.Rows[0]["MAGV"]).Trim();
            }

            if (!string.IsNullOrEmpty(targetMaGV))
            {
                SyncSelectionByMaGV(targetMaGV, true, true);
            }
        }

        private void ConfigureGridColumns()
        {
            if (dgvTaiKhoanGV.Columns.Contains("MAGV"))
                dgvTaiKhoanGV.Columns["MAGV"].HeaderText = "MAGV";
            if (dgvTaiKhoanGV.Columns.Contains("HO"))
                dgvTaiKhoanGV.Columns["HO"].HeaderText = "HO";
            if (dgvTaiKhoanGV.Columns.Contains("TEN"))
                dgvTaiKhoanGV.Columns["TEN"].HeaderText = "TEN";
            if (dgvTaiKhoanGV.Columns.Contains("HOTEN_GV"))
                dgvTaiKhoanGV.Columns["HOTEN_GV"].Visible = false;
            if (dgvTaiKhoanGV.Columns.Contains("TENDANGNHAP"))
                dgvTaiKhoanGV.Columns["TENDANGNHAP"].HeaderText = "Username";
            if (dgvTaiKhoanGV.Columns.Contains("TRANGTHAI_TEXT"))
                dgvTaiKhoanGV.Columns["TRANGTHAI_TEXT"].HeaderText = "Trạng thái";
            if (dgvTaiKhoanGV.Columns.Contains("HANHDONG_TEXT"))
                dgvTaiKhoanGV.Columns["HANHDONG_TEXT"].HeaderText = "Hành động";

            string[] hiddenCols = { "TRANGTHAI_CODE", "CO_LOGIN", "CO_DB_USER", "THUOC_ROLE_GV", "IS_DISABLED", "NGAYTAO", "HIENTHI_ALL" };
            foreach (string col in hiddenCols)
            {
                if (dgvTaiKhoanGV.Columns.Contains(col))
                {
                    dgvTaiKhoanGV.Columns[col].Visible = false;
                }
            }
        }

        /// <summary>
        /// Khi người dùng chọn một giáo viên trong ComboBox:
        /// Lấy trực tiếp dữ liệu từ dòng đang chọn trong ComboBox và cập nhật giao diện ngay lập tức,
        /// đồng thời đánh dấu dòng tương ứng trên DataGridView trong chế độ khóa sự kiện (_isSyncingSelection = true)
        /// để tránh việc DataGridView bắn ngược sự kiện làm giật/nhảy lại dòng cũ.
        /// </summary>
        private void CboGiangVien_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_isSyncingSelection) return;

            DataRowView drv = cboGiangVien.SelectedItem as DataRowView;
            if (drv == null) return;

            string maGV = Convert.ToString(drv["MAGV"]).Trim();
            SyncSelectionByMaGV(maGV, false, true);
        }

        /// <summary>
        /// Khi người dùng click chọn một dòng trên bảng DataGridView bên phải:
        /// Đồng bộ sang ComboBox bên trái mà không gây vòng lặp sự kiện.
        /// </summary>
        private void DgvTaiKhoanGV_SelectionChanged(object sender, EventArgs e)
        {
            if (_isSyncingSelection) return;

            if (dgvTaiKhoanGV.CurrentRow == null || dgvTaiKhoanGV.CurrentRow.Index < 0)
            {
                btnContextualCreate.Visible = false;
                btnThuHoiTaiKhoan.Visible = false;
                btnKichHoatLaiTaiKhoan.Visible = false;
                lblSelectedTeacherStatus.Text = "Chọn một giáo viên trên bảng để thao tác.";
                return;
            }

            string maGV = Convert.ToString(dgvTaiKhoanGV.CurrentRow.Cells["MAGV"].Value).Trim();
            SyncSelectionByMaGV(maGV, true, false);
        }

        private void SyncSelectionByMaGV(string maGV, bool updateCombo, bool updateGrid)
        {
            if (string.IsNullOrEmpty(maGV)) return;

            _isSyncingSelection = true;
            try
            {
                if (updateCombo && cboGiangVien.DataSource != null)
                {
                    cboGiangVien.SelectedValue = maGV;
                }

                DataGridViewRow matchedRow = null;
                foreach (DataGridViewRow row in dgvTaiKhoanGV.Rows)
                {
                    if (string.Equals(Convert.ToString(row.Cells["MAGV"].Value).Trim(), maGV, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedRow = row;
                        break;
                    }
                }

                if (matchedRow != null)
                {
                    if (updateGrid)
                    {
                        // Đặt CurrentCell trước khi đặt Selected để CurrentRow luôn trỏ đúng dòng mới
                        dgvTaiKhoanGV.CurrentCell = matchedRow.Cells[0];
                        matchedRow.Selected = true;
                    }

                    string hoTen = matchedRow.DataGridView.Columns.Contains("HOTEN_GV")
                        ? Convert.ToString(matchedRow.Cells["HOTEN_GV"].Value).Trim()
                        : (Convert.ToString(matchedRow.Cells["HO"].Value).Trim() + " " + Convert.ToString(matchedRow.Cells["TEN"].Value).Trim());
                    int statusCode = matchedRow.DataGridView.Columns.Contains("TRANGTHAI_CODE")
                        ? Convert.ToInt32(matchedRow.Cells["TRANGTHAI_CODE"].Value)
                        : 0;
                    string statusText = Convert.ToString(matchedRow.Cells["TRANGTHAI_TEXT"].Value).Trim();

                    ApplyTeacherStateToUI(maGV, hoTen, statusCode, statusText);
                }
            }
            finally
            {
                _isSyncingSelection = false;
            }
        }

        private void ApplyTeacherStateToUI(string maGV, string hoTen, int statusCode, string statusText)
        {
            _currentSelectedStatusCode = statusCode;
            txtUsername.Text = maGV;
            lblSelectedTeacherStatus.Text = string.Format("Đang chọn: {0} - {1} ({2})", maGV, hoTen, statusText);

            if (statusCode == 0) // Chưa tạo SQL Login
            {
                btnContextualCreate.Visible = true;
                btnThuHoiTaiKhoan.Visible = false;
                btnKichHoatLaiTaiKhoan.Visible = false;

                lblPassword.Text = "3. Mật khẩu SQL Login (Tối thiểu 6 ký tự):";
                txtPassword.Enabled = true;
                txtConfirmPassword.Enabled = true;
                chkShowPass.Enabled = true;

                btnTaoTaiKhoan.Text = "TẠO TÀI KHOẢN";
                btnTaoTaiKhoan.BackColor = Color.FromArgb(22, 163, 74);
                btnTaoTaiKhoan.Enabled = true;
            }
            else if (statusCode == 1) // Đang hoạt động -> Nút Thu hồi tài khoản
            {
                btnContextualCreate.Visible = false;
                btnThuHoiTaiKhoan.Visible = true;
                btnKichHoatLaiTaiKhoan.Visible = false;

                lblPassword.Text = "3. Mật khẩu SQL Login (Đã cấp tài khoản):";
                txtPassword.Clear();
                txtConfirmPassword.Clear();
                txtPassword.Enabled = false;
                txtConfirmPassword.Enabled = false;
                chkShowPass.Enabled = false;

                btnTaoTaiKhoan.Text = "THU HỒI TÀI KHOẢN";
                btnTaoTaiKhoan.BackColor = Color.FromArgb(220, 38, 38);
                btnTaoTaiKhoan.Enabled = true;
            }
            else if (statusCode == 2) // Đã vô hiệu hóa -> Nút Kích hoạt lại
            {
                btnContextualCreate.Visible = false;
                btnThuHoiTaiKhoan.Visible = false;
                btnKichHoatLaiTaiKhoan.Visible = true;

                lblPassword.Text = "3. Mật khẩu SQL Login (Đang bị vô hiệu hóa):";
                txtPassword.Clear();
                txtConfirmPassword.Clear();
                txtPassword.Enabled = false;
                txtConfirmPassword.Enabled = false;
                chkShowPass.Enabled = false;

                btnTaoTaiKhoan.Text = "KÍCH HOẠT LẠI";
                btnTaoTaiKhoan.BackColor = Color.FromArgb(37, 99, 235);
                btnTaoTaiKhoan.Enabled = true;
            }
        }

        private void BtnContextualCreate_Click(object sender, EventArgs e)
        {
            if (dgvTaiKhoanGV.CurrentRow == null) return;
            string maGV = Convert.ToString(dgvTaiKhoanGV.CurrentRow.Cells["MAGV"].Value).Trim();
            SyncSelectionByMaGV(maGV, true, false);
            if (txtPassword.Enabled)
            {
                txtPassword.Focus();
            }
        }

        private void BtnMainAction_Click(object sender, EventArgs e)
        {
            if (_currentSelectedStatusCode == 1)
            {
                BtnThuHoiTaiKhoan_Click(sender, e);
                return;
            }
            if (_currentSelectedStatusCode == 2)
            {
                BtnKichHoatLaiTaiKhoan_Click(sender, e);
                return;
            }

            BtnTaoTaiKhoan_Click(sender, e);
        }

        private void BtnTaoTaiKhoan_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            if (cboGiangVien.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn một giáo viên trong danh sách!",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                _isBusy = true;
                btnTaoTaiKhoan.Enabled = false;

                string maGV = cboGiangVien.SelectedValue.ToString().Trim();
                string username = txtUsername.Text.Trim();
                string pass = txtPassword.Text;
                string confirm = txtConfirmPassword.Text;

                string thongBao;
                if (_bll.TaoTaiKhoanGiaoVien(maGV, username, pass, confirm, out thongBao))
                {
                    MessageBox.Show(thongBao, "Tạo SQL Server Login thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData(maGV);
                }
                else
                {
                    MessageBox.Show(thongBao, "Không thể tạo tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                _isBusy = false;
                btnTaoTaiKhoan.Enabled = cboGiangVien.Items.Count > 0;
            }
        }

        private void BtnThuHoiTaiKhoan_Click(object sender, EventArgs e)
        {
            if (_isBusy || dgvTaiKhoanGV.CurrentRow == null) return;

            string maGV = Convert.ToString(dgvTaiKhoanGV.CurrentRow.Cells["MAGV"].Value).Trim();
            string loginName = Convert.ToString(dgvTaiKhoanGV.CurrentRow.Cells["TENDANGNHAP"].Value).Trim();
            string hoTen = Convert.ToString(dgvTaiKhoanGV.CurrentRow.Cells["HO"].Value).Trim() + " " +
                           Convert.ToString(dgvTaiKhoanGV.CurrentRow.Cells["TEN"].Value).Trim();

            DialogResult confirm = MessageBox.Show(
                string.Format("Bạn có chắc chắn muốn THU HỒI (vô hiệu hóa) tài khoản SQL Server Login [{0}] của giáo viên [{1} - {2}] không?\n\n" +
                              "• Sau khi thu hồi, giáo viên này không thể đăng nhập mới.\n" +
                              "• Hồ sơ giáo viên, bộ đề và lịch sử thi vẫn được giữ nguyên.",
                    loginName, maGV, hoTen),
                "Xác nhận thu hồi tài khoản SQL Login",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _isBusy = true;
                btnThuHoiTaiKhoan.Enabled = false;
                btnTaoTaiKhoan.Enabled = false;

                string thongBao;
                if (_bll.ThuHoiTaiKhoanGiaoVien(maGV, out thongBao))
                {
                    MessageBox.Show(thongBao, "Đã thu hồi tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData(maGV);
                }
                else
                {
                    MessageBox.Show(thongBao, "Không thể thu hồi tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                _isBusy = false;
                btnThuHoiTaiKhoan.Enabled = true;
                btnTaoTaiKhoan.Enabled = true;
            }
        }

        private void BtnKichHoatLaiTaiKhoan_Click(object sender, EventArgs e)
        {
            if (_isBusy || dgvTaiKhoanGV.CurrentRow == null) return;

            string maGV = Convert.ToString(dgvTaiKhoanGV.CurrentRow.Cells["MAGV"].Value).Trim();
            string hoTen = Convert.ToString(dgvTaiKhoanGV.CurrentRow.Cells["HO"].Value).Trim() + " " +
                           Convert.ToString(dgvTaiKhoanGV.CurrentRow.Cells["TEN"].Value).Trim();

            DialogResult confirm = MessageBox.Show(
                string.Format("Bạn có muốn KÍCH HOẠT LẠI tài khoản SQL Server Login [{0}] cho giáo viên [{0} - {1}] không?",
                    maGV, hoTen),
                "Xác nhận kích hoạt lại SQL Login",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _isBusy = true;
                btnKichHoatLaiTaiKhoan.Enabled = false;
                btnTaoTaiKhoan.Enabled = false;

                string thongBao;
                if (_bll.KichHoatLaiTaiKhoanGiaoVien(maGV, out thongBao))
                {
                    MessageBox.Show(thongBao, "Kích hoạt lại thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadData(maGV);
                }
                else
                {
                    MessageBox.Show(thongBao, "Không thể kích hoạt lại tài khoản", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            finally
            {
                _isBusy = false;
                btnKichHoatLaiTaiKhoan.Enabled = true;
                btnTaoTaiKhoan.Enabled = true;
            }
        }
    }
}
