using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Form Quản lý Giáo viên (Bảng GIAOVIEN).
    /// Đầy đủ chức năng theo đề gốc: Thêm, Xóa, Hiệu chỉnh (Sửa), Tìm, Ghi.
    /// (Tuân thủ đúng yêu cầu đề bài: Không tự thêm nút Phục hồi trên Form Giáo viên).
    /// </summary>
    public class FrmGiaoVien : Form
    {
        private enum EditMode
        {
            None,
            Adding,
            Editing
        }

        private readonly GiaoVienBLL _bll = new GiaoVienBLL();
        private EditMode _currentMode = EditMode.None;

        private Panel pnlToolbar;
        private Button btnThem;
        private Button btnHieuChinh;
        private Button btnXoa;
        private Button btnGhi;
        private Button btnLamMoi;

        private Panel pnlInputAndSearch;
        private GroupBox grpInput;
        private Label lblMaGV;
        private TextBox txtMaGV;
        private Label lblHo;
        private TextBox txtHo;
        private Label lblTen;
        private TextBox txtTen;
        private Label lblSoDT;
        private TextBox txtSoDT;
        private Label lblDiaChi;
        private TextBox txtDiaChi;
        private Label lblModeStatus;

        private GroupBox grpSearch;
        private Label lblSearchTitle;
        private TextBox txtTimKiem;
        private Button btnTim;
        private Button btnTatCa;

        private GroupBox grpGrid;
        private DataGridView dgvGiaoVien;

        public FrmGiaoVien()
        {
            InitializeComponent();
            LoadDanhSachGiaoVien();
            SetControlState(EditMode.None);
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.Padding = new Padding(4);

            // 1. Thanh công cụ (Thêm, Hiệu chỉnh, Xóa, Ghi, Làm mới)
            pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8)
            };

            btnThem = CreateActionButton("Thêm", Color.FromArgb(22, 163, 74), 12);
            btnHieuChinh = CreateActionButton("Hiệu chỉnh", Color.FromArgb(37, 99, 235), 144);
            btnXoa = CreateActionButton("Xóa", Color.FromArgb(220, 38, 38), 276);
            btnGhi = CreateActionButton("Ghi (Lưu)", Color.FromArgb(217, 119, 6), 408);
            btnLamMoi = CreateActionButton("Làm mới / Hủy", Color.FromArgb(71, 85, 105), 540);

            btnThem.Click += BtnThem_Click;
            btnHieuChinh.Click += BtnHieuChinh_Click;
            btnXoa.Click += BtnXoa_Click;
            btnGhi.Click += BtnGhi_Click;
            btnLamMoi.Click += BtnLamMoi_Click;

            pnlToolbar.Controls.AddRange(new Control[]
            {
                btnThem, btnHieuChinh, btnXoa, btnGhi, btnLamMoi
            });

            // 2. Khu vực nhập liệu & tìm kiếm
            pnlInputAndSearch = new Panel
            {
                Dock = DockStyle.Top,
                Height = 165,
                Padding = new Padding(0, 8, 0, 8)
            };

            grpInput = new GroupBox
            {
                Text = "Thông tin Giáo viên (Bảng GIAOVIEN)",
                Location = new Point(0, 6),
                Size = new Size(650, 150),
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            lblMaGV = CreateLabel("Mã GV (MAGV):", 16, 26, 120);
            txtMaGV = new TextBox
            {
                Location = new Point(16, 48),
                Size = new Size(120, 28),
                MaxLength = 8,
                CharacterCasing = CharacterCasing.Upper,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            lblHo = CreateLabel("Họ và tên đệm (HO):", 150, 26, 210);
            txtHo = new TextBox
            {
                Location = new Point(150, 48),
                Size = new Size(215, 28),
                MaxLength = 40,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };

            lblTen = CreateLabel("Tên (TEN):", 380, 26, 100);
            txtTen = new TextBox
            {
                Location = new Point(380, 48),
                Size = new Size(110, 28),
                MaxLength = 10,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };

            lblSoDT = CreateLabel("Số ĐT liên lạc (SODTLL):", 504, 26, 140);
            txtSoDT = new TextBox
            {
                Location = new Point(504, 48),
                Size = new Size(130, 28),
                MaxLength = 12,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };

            lblDiaChi = CreateLabel("Địa chỉ / Bộ môn (DIACHI - Tối đa 50 ký tự):", 16, 82, 320);
            txtDiaChi = new TextBox
            {
                Location = new Point(16, 104),
                Size = new Size(350, 28),
                MaxLength = 50,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };

            lblModeStatus = new Label
            {
                Text = "Chế độ: Xem dữ liệu",
                Location = new Point(380, 108),
                Size = new Size(255, 24),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(37, 99, 235)
            };

            grpInput.Controls.AddRange(new Control[]
            {
                lblMaGV, txtMaGV, lblHo, txtHo, lblTen, txtTen,
                lblSoDT, txtSoDT, lblDiaChi, txtDiaChi, lblModeStatus
            });

            grpSearch = new GroupBox
            {
                Text = "Tìm kiếm Giáo viên",
                Location = new Point(662, 6),
                Size = new Size(322, 150),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            lblSearchTitle = CreateLabel("Nhập Mã GV, Họ tên hoặc Số ĐT:", 16, 30, 280);
            txtTimKiem = new TextBox
            {
                Location = new Point(16, 55),
                Size = new Size(288, 28),
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };
            txtTimKiem.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnTim_Click(s, e);
                }
            };

            btnTim = new Button
            {
                Text = "Tìm kiếm",
                Location = new Point(16, 94),
                Size = new Size(138, 34),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTim.FlatAppearance.BorderSize = 0;
            btnTim.Click += BtnTim_Click;

            btnTatCa = new Button
            {
                Text = "Hiện tất cả",
                Location = new Point(166, 94),
                Size = new Size(138, 34),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTatCa.FlatAppearance.BorderSize = 0;
            btnTatCa.Click += (s, e) =>
            {
                txtTimKiem.Clear();
                LoadDanhSachGiaoVien();
            };

            grpSearch.Controls.AddRange(new Control[]
            {
                lblSearchTitle, txtTimKiem, btnTim, btnTatCa
            });

            pnlInputAndSearch.Controls.Add(grpInput);
            pnlInputAndSearch.Controls.Add(grpSearch);

            // 3. Bảng DataGridView
            grpGrid = new GroupBox
            {
                Text = "Danh sách Giáo viên trong hệ thống",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(10)
            };

            dgvGiaoVien = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };
            dgvGiaoVien.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvGiaoVien.RowTemplate.Height = 32;
            dgvGiaoVien.SelectionChanged += DgvGiaoVien_SelectionChanged;

            grpGrid.Controls.Add(dgvGiaoVien);

            this.Controls.Add(grpGrid);
            this.Controls.Add(pnlInputAndSearch);
            this.Controls.Add(pnlToolbar);
        }

        private Label CreateLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
        }

        private Button CreateActionButton(string text, Color backColor, int left)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(left, 8),
                Size = new Size(124, 38),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void SetControlState(EditMode mode)
        {
            _currentMode = mode;
            bool editing = (mode != EditMode.None);

            txtMaGV.ReadOnly = (mode != EditMode.Adding);
            txtHo.ReadOnly = !editing;
            txtTen.ReadOnly = !editing;
            txtSoDT.ReadOnly = !editing;
            txtDiaChi.ReadOnly = !editing;

            btnThem.Enabled = !editing;
            btnHieuChinh.Enabled = !editing && dgvGiaoVien.CurrentRow != null;
            btnXoa.Enabled = !editing && dgvGiaoVien.CurrentRow != null;
            btnGhi.Enabled = editing;
            dgvGiaoVien.Enabled = !editing;

            if (mode == EditMode.Adding)
            {
                lblModeStatus.Text = "Đang THÊM MỚI -> Nhấn [Ghi (Lưu)]";
                lblModeStatus.ForeColor = Color.FromArgb(22, 163, 74);
            }
            else if (mode == EditMode.Editing)
            {
                lblModeStatus.Text = "Đang HIỆU CHỈNH -> Nhấn [Ghi (Lưu)]";
                lblModeStatus.ForeColor = Color.FromArgb(217, 119, 6);
            }
            else
            {
                lblModeStatus.Text = "Chế độ: Xem dữ liệu";
                lblModeStatus.ForeColor = Color.FromArgb(37, 99, 235);
            }
        }

        private void LoadDanhSachGiaoVien()
        {
            DataTable dt = _bll.LayDanhSachGiaoVien();
            BindGrid(dt);
        }

        private void BindGrid(DataTable dt)
        {
            dgvGiaoVien.DataSource = dt;
            if (dgvGiaoVien.Columns.Contains("MAGV"))
                dgvGiaoVien.Columns["MAGV"].HeaderText = "Mã GV (MAGV)";
            if (dgvGiaoVien.Columns.Contains("HO"))
                dgvGiaoVien.Columns["HO"].HeaderText = "Họ và tên đệm";
            if (dgvGiaoVien.Columns.Contains("TEN"))
                dgvGiaoVien.Columns["TEN"].HeaderText = "Tên";
            if (dgvGiaoVien.Columns.Contains("HOTEN"))
                dgvGiaoVien.Columns["HOTEN"].Visible = false;
            if (dgvGiaoVien.Columns.Contains("SODTLL"))
                dgvGiaoVien.Columns["SODTLL"].HeaderText = "Số ĐT liên lạc";
            if (dgvGiaoVien.Columns.Contains("DIACHI"))
                dgvGiaoVien.Columns["DIACHI"].HeaderText = "Địa chỉ / Bộ môn";
            if (dgvGiaoVien.Columns.Contains("TENDANGNHAP"))
                dgvGiaoVien.Columns["TENDANGNHAP"].HeaderText = "Username (SQL Login)";
            if (dgvGiaoVien.Columns.Contains("TRANGTHAI_MOTA"))
                dgvGiaoVien.Columns["TRANGTHAI_MOTA"].HeaderText = "Trạng thái Login";
            if (dgvGiaoVien.Columns.Contains("TRANGTHAI_LOGIN"))
                dgvGiaoVien.Columns["TRANGTHAI_LOGIN"].Visible = false;
            if (dgvGiaoVien.Columns.Contains("CO_TAI_KHOAN"))
                dgvGiaoVien.Columns["CO_TAI_KHOAN"].Visible = false;
            if (dgvGiaoVien.Columns.Contains("SO_CAU_DA_SOAN"))
                dgvGiaoVien.Columns["SO_CAU_DA_SOAN"].HeaderText = "Số câu BODE";
            if (dgvGiaoVien.Columns.Contains("SO_LICH_DANG_KY"))
                dgvGiaoVien.Columns["SO_LICH_DANG_KY"].HeaderText = "Số lịch thi";
        }

        private void DgvGiaoVien_SelectionChanged(object sender, EventArgs e)
        {
            if (_currentMode != EditMode.None) return;

            if (dgvGiaoVien.CurrentRow != null && dgvGiaoVien.CurrentRow.Index >= 0)
            {
                DataGridViewRow row = dgvGiaoVien.CurrentRow;
                txtMaGV.Text = Convert.ToString(row.Cells["MAGV"].Value).Trim();
                txtHo.Text = Convert.ToString(row.Cells["HO"].Value).Trim();
                txtTen.Text = Convert.ToString(row.Cells["TEN"].Value).Trim();
                txtSoDT.Text = Convert.ToString(row.Cells["SODTLL"].Value).Trim();
                txtDiaChi.Text = Convert.ToString(row.Cells["DIACHI"].Value).Trim();
                btnHieuChinh.Enabled = true;
                btnXoa.Enabled = true;
            }
            else
            {
                txtMaGV.Clear();
                txtHo.Clear();
                txtTen.Clear();
                txtSoDT.Clear();
                txtDiaChi.Clear();
                btnHieuChinh.Enabled = false;
                btnXoa.Enabled = false;
            }
        }

        private void BtnThem_Click(object sender, EventArgs e)
        {
            txtMaGV.Clear();
            txtHo.Clear();
            txtTen.Clear();
            txtSoDT.Clear();
            txtDiaChi.Clear();
            SetControlState(EditMode.Adding);
            txtMaGV.Focus();
        }

        private void BtnHieuChinh_Click(object sender, EventArgs e)
        {
            if (dgvGiaoVien.CurrentRow == null) return;
            SetControlState(EditMode.Editing);
            txtHo.Focus();
        }

        private void BtnGhi_Click(object sender, EventArgs e)
        {
            GiaoVien gv = new GiaoVien
            {
                MaGV = txtMaGV.Text,
                Ho = txtHo.Text,
                Ten = txtTen.Text,
                SoDTLL = txtSoDT.Text,
                DiaChi = txtDiaChi.Text
            };

            string thongBao;
            bool ok = (_currentMode == EditMode.Adding)
                ? _bll.ThemGiaoVien(gv, out thongBao)
                : _bll.CapNhatGiaoVien(gv, out thongBao);

            if (ok)
            {
                MessageBox.Show(thongBao, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetControlState(EditMode.None);
                LoadDanhSachGiaoVien();
                SelectRowByMaGV(gv.MaGV);
            }
            else
            {
                MessageBox.Show(thongBao, "Lỗi dữ liệu Giáo viên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnXoa_Click(object sender, EventArgs e)
        {
            if (dgvGiaoVien.CurrentRow == null) return;

            string maGV = Convert.ToString(dgvGiaoVien.CurrentRow.Cells["MAGV"].Value).Trim();
            string hoTen = Convert.ToString(dgvGiaoVien.CurrentRow.Cells["HOTEN"].Value).Trim();

            DialogResult confirm = MessageBox.Show(
                string.Format("Bạn có chắc chắn muốn xóa giáo viên [{0} - {1}] không?", maGV, hoTen),
                "Xác nhận xóa giáo viên",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            string thongBao;
            if (_bll.XoaGiaoVien(maGV, out thongBao))
            {
                MessageBox.Show(thongBao, "Đã xóa giáo viên", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDanhSachGiaoVien();
            }
            else
            {
                MessageBox.Show(thongBao, "Không thể xóa giáo viên", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnTim_Click(object sender, EventArgs e)
        {
            DataTable dt = _bll.TimKiemGiaoVien(txtTimKiem.Text.Trim());
            BindGrid(dt);
        }

        private void BtnLamMoi_Click(object sender, EventArgs e)
        {
            SetControlState(EditMode.None);
            txtTimKiem.Clear();
            LoadDanhSachGiaoVien();
        }

        private void SelectRowByMaGV(string maGV)
        {
            foreach (DataGridViewRow row in dgvGiaoVien.Rows)
            {
                if (string.Equals(Convert.ToString(row.Cells["MAGV"].Value).Trim(), maGV.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    dgvGiaoVien.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }
    }
}

