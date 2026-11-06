using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Form Quản lý Lớp và Sinh viên theo đúng mô hình Master–Detail / Subform.
    /// - Khu vực Master (Trên): Quản lý danh sách Lớp (Bảng LOP: Thêm, Xóa, Hiệu chỉnh, Tìm, Ghi).
    /// - Khu vực Subform (Dưới): Tự động hiển thị và quản lý Sinh viên thuộc Lớp đang chọn
    ///   (Bảng SINHVIEN: Thêm, Xóa, Hiệu chỉnh, Phục hồi, Tìm kiếm, Ghi).
    /// </summary>
    public class FrmLopSinhVien : Form
    {
        private enum SubEditMode
        {
            None,
            Adding,
            Editing
        }

        private class SvUndoItem
        {
            public string ActionType { get; set; } // "ADD", "EDIT", "DELETE"
            public SinhVien OldData { get; set; }
            public SinhVien NewData { get; set; }
        }

        private readonly LopBLL _lopBLL = new LopBLL();
        private readonly SinhVienBLL _svBLL = new SinhVienBLL();
        private readonly Stack<SvUndoItem> _svUndoStack = new Stack<SvUndoItem>();

        private SubEditMode _lopMode = SubEditMode.None;
        private SubEditMode _svMode = SubEditMode.None;
        private string _selectedMaLop = string.Empty;

        private SplitContainer splitMasterDetail;

        // --- KHU VỰC 1: MASTER (QUẢN LÝ LỚP) ---
        private GroupBox grpMasterLop;
        private Panel pnlLopTop;
        private Label lblMaLop;
        private TextBox txtMaLop;
        private Label lblTenLop;
        private TextBox txtTenLop;
        private Button btnThemLop;
        private Button btnSuaLop;
        private Button btnXoaLop;
        private Button btnGhiLop;
        private Button btnHuyLop;
        private TextBox txtTimLop;
        private Button btnTimLop;
        private DataGridView dgvLop;

        // --- KHU VỰC 2: DETAIL SUBFORM (QUẢN LÝ SINH VIÊN CỦA LỚP ĐANG CHỌN) ---
        private GroupBox grpSubformSinhVien;
        private Panel pnlSvInputAndButtons;
        private Label lblMaSV;
        private TextBox txtMaSV;
        private Label lblHoSV;
        private TextBox txtHoSV;
        private Label lblTenSV;
        private TextBox txtTenSV;
        private Label lblNgaySinh;
        private DateTimePicker dtpNgaySinh;
        private Label lblDiaChiSV;
        private TextBox txtDiaChiSV;
        private Label lblLopCuaSV;
        private ComboBox cboMaLopSV;

        private Button btnThemSV;
        private Button btnSuaSV;
        private Button btnXoaSV;
        private Button btnGhiSV;
        private Button btnPhucHoiSV;
        private Button btnLamMoiSV;

        private TextBox txtTimSV;
        private Button btnTimSV;
        private CheckBox chkTimTatCaLop;
        private Label lblSvStatus;

        private DataGridView dgvSinhVien;

        public FrmLopSinhVien()
        {
            InitializeComponent();
            LoadDanhSachLop();
            SetLopControlState(SubEditMode.None);
            SetSvControlState(SubEditMode.None);
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            splitMasterDetail = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 245,
                SplitterWidth = 6
            };

            // =========================================================================
            // 1. KHU VỰC MASTER: QUẢN LÝ LỚP (FORM CHÍNH)
            // =========================================================================
            grpMasterLop = new GroupBox
            {
                Text = "FORM CHÍNH (MASTER): QUẢN LÝ DANH SÁCH LỚP HỌC (BẢNG LOP)",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Padding = new Padding(10)
            };

            pnlLopTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            lblMaLop = new Label
            {
                Text = "Mã lớp (MALOP - In hoa):",
                Location = new Point(10, 10),
                Size = new Size(165, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txtMaLop = new TextBox
            {
                Location = new Point(10, 32),
                Size = new Size(135, 26),
                MaxLength = 8,
                CharacterCasing = CharacterCasing.Upper,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            lblTenLop = new Label
            {
                Text = "Tên lớp (TENLOP - Không trùng):",
                Location = new Point(158, 10),
                Size = new Size(230, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txtTenLop = new TextBox
            {
                Location = new Point(158, 32),
                Size = new Size(235, 26),
                MaxLength = 40,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };

            btnThemLop = CreateSmallButton("Thêm Lớp", Color.FromArgb(22, 163, 74), 408, 28, 86);
            btnSuaLop = CreateSmallButton("Sửa Lớp", Color.FromArgb(37, 99, 235), 498, 28, 80);
            btnXoaLop = CreateSmallButton("Xóa Lớp", Color.FromArgb(220, 38, 38), 582, 28, 80);
            btnGhiLop = CreateSmallButton("Ghi Lớp", Color.FromArgb(217, 119, 6), 666, 28, 82);
            btnHuyLop = CreateSmallButton("Làm mới", Color.FromArgb(100, 116, 139), 752, 28, 80);

            txtTimLop = new TextBox
            {
                Location = new Point(844, 31),
                Size = new Size(105, 26),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            btnTimLop = CreateSmallButton("Tìm Lớp", Color.FromArgb(15, 23, 42), 953, 28, 75);

            btnThemLop.Click += BtnThemLop_Click;
            btnSuaLop.Click += BtnSuaLop_Click;
            btnXoaLop.Click += BtnXoaLop_Click;
            btnGhiLop.Click += BtnGhiLop_Click;
            btnHuyLop.Click += BtnHuyLop_Click;
            btnTimLop.Click += BtnTimLop_Click;

            pnlLopTop.Controls.AddRange(new Control[]
            {
                lblMaLop, txtMaLop, lblTenLop, txtTenLop,
                btnThemLop, btnSuaLop, btnXoaLop, btnGhiLop, btnHuyLop,
                txtTimLop, btnTimLop
            });

            dgvLop = new DataGridView
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
            dgvLop.RowTemplate.Height = 28;
            dgvLop.SelectionChanged += DgvLop_SelectionChanged;

            grpMasterLop.Controls.Add(dgvLop);
            grpMasterLop.Controls.Add(pnlLopTop);
            splitMasterDetail.Panel1.Controls.Add(grpMasterLop);

            // =========================================================================
            // 2. KHU VỰC DETAIL (SUBFORM): QUẢN LÝ SINH VIÊN THUỘC LỚP ĐANG CHỌN
            // =========================================================================
            grpSubformSinhVien = new GroupBox
            {
                Text = "SUBFORM (DETAIL): DANH SÁCH SINH VIÊN THUỘC LỚP ĐANG CHỌN (BẢNG SINHVIEN)",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 64, 175),
                Padding = new Padding(10)
            };

            pnlSvInputAndButtons = new Panel
            {
                Dock = DockStyle.Top,
                Height = 132,
                BackColor = Color.FromArgb(248, 250, 252)
            };

            // Hàng 1: Các ô nhập thông tin Sinh viên
            lblMaSV = CreateFieldLabel("Mã SV (MASV):", 10, 8, 110);
            txtMaSV = new TextBox
            {
                Location = new Point(10, 28),
                Size = new Size(110, 26),
                MaxLength = 8,
                CharacterCasing = CharacterCasing.Upper,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            lblHoSV = CreateFieldLabel("Họ và tên đệm (HO):", 130, 8, 175);
            txtHoSV = new TextBox
            {
                Location = new Point(130, 28),
                Size = new Size(185, 26),
                MaxLength = 40,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            lblTenSV = CreateFieldLabel("Tên (TEN):", 325, 8, 95);
            txtTenSV = new TextBox
            {
                Location = new Point(325, 28),
                Size = new Size(105, 26),
                MaxLength = 10,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            lblNgaySinh = CreateFieldLabel("Ngày sinh:", 440, 8, 120);
            dtpNgaySinh = new DateTimePicker
            {
                Location = new Point(440, 28),
                Size = new Size(130, 26),
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                Value = new DateTime(2004, 1, 1),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            lblDiaChiSV = CreateFieldLabel("Địa chỉ (DIACHI):", 580, 8, 195);
            txtDiaChiSV = new TextBox
            {
                Location = new Point(580, 28),
                Size = new Size(215, 26),
                MaxLength = 40,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            lblLopCuaSV = CreateFieldLabel("Lớp (MALOP):", 805, 8, 150);
            cboMaLopSV = new ComboBox
            {
                Location = new Point(805, 28),
                Size = new Size(175, 26),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            // Hàng 2: Các nút thao tác riêng cho Sinh viên + Tìm kiếm Sinh viên
            btnThemSV = CreateSmallButton("Thêm SV", Color.FromArgb(22, 163, 74), 10, 68, 92);
            btnSuaSV = CreateSmallButton("Hiệu chỉnh SV", Color.FromArgb(37, 99, 235), 108, 68, 110);
            btnXoaSV = CreateSmallButton("Xóa SV", Color.FromArgb(220, 38, 38), 224, 68, 86);
            btnGhiSV = CreateSmallButton("Ghi SV", Color.FromArgb(217, 119, 6), 316, 68, 86);
            btnPhucHoiSV = CreateSmallButton("Phục hồi SV", Color.FromArgb(124, 58, 237), 408, 68, 102);
            btnLamMoiSV = CreateSmallButton("Làm mới SV", Color.FromArgb(100, 116, 139), 516, 68, 96);

            txtTimSV = new TextBox
            {
                Location = new Point(625, 71),
                Size = new Size(155, 26),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            txtTimSV.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    BtnTimSV_Click(s, e);
                }
            };

            btnTimSV = CreateSmallButton("Tìm SV", Color.FromArgb(15, 23, 42), 785, 68, 76);

            chkTimTatCaLop = new CheckBox
            {
                Text = "Tìm toàn trường",
                Location = new Point(868, 72),
                Size = new Size(130, 24),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            lblSvStatus = new Label
            {
                Text = "Subform: Chọn một lớp ở trên để xem và quản lý danh sách sinh viên của lớp đó.",
                Location = new Point(10, 106),
                Size = new Size(960, 20),
                Font = new Font("Segoe UI", 8.8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(30, 64, 175)
            };

            btnThemSV.Click += BtnThemSV_Click;
            btnSuaSV.Click += BtnSuaSV_Click;
            btnXoaSV.Click += BtnXoaSV_Click;
            btnGhiSV.Click += BtnGhiSV_Click;
            btnPhucHoiSV.Click += BtnPhucHoiSV_Click;
            btnLamMoiSV.Click += BtnLamMoiSV_Click;
            btnTimSV.Click += BtnTimSV_Click;

            pnlSvInputAndButtons.Controls.AddRange(new Control[]
            {
                lblMaSV, txtMaSV, lblHoSV, txtHoSV, lblTenSV, txtTenSV,
                lblNgaySinh, dtpNgaySinh, lblDiaChiSV, txtDiaChiSV, lblLopCuaSV, cboMaLopSV,
                btnThemSV, btnSuaSV, btnXoaSV, btnGhiSV, btnPhucHoiSV, btnLamMoiSV,
                txtTimSV, btnTimSV, chkTimTatCaLop, lblSvStatus
            });

            dgvSinhVien = new DataGridView
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
            dgvSinhVien.RowTemplate.Height = 29;
            dgvSinhVien.SelectionChanged += DgvSinhVien_SelectionChanged;

            grpSubformSinhVien.Controls.Add(dgvSinhVien);
            grpSubformSinhVien.Controls.Add(pnlSvInputAndButtons);
            splitMasterDetail.Panel2.Controls.Add(grpSubformSinhVien);

            this.Controls.Add(splitMasterDetail);
        }

        private Label CreateFieldLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 18),
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(51, 65, 85)
            };
        }

        private Button CreateSmallButton(string text, Color backColor, int x, int y, int width)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 32),
                BackColor = backColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        // =========================================================================
        // XỬ LÝ NGHIỆP VỤ FORM CHÍNH (QUẢN LÝ LỚP)
        // =========================================================================
        private void LoadDanhSachLop()
        {
            DataTable dtLop = _lopBLL.LayDanhSachLop();
            BindLopGrid(dtLop);
            BindComboBoxLop(dtLop);
        }

        private void BindLopGrid(DataTable dtLop)
        {
            dgvLop.DataSource = dtLop;
            if (dgvLop.Columns.Contains("MALOP"))
                dgvLop.Columns["MALOP"].HeaderText = "Mã lớp (MALOP)";
            if (dgvLop.Columns.Contains("TENLOP"))
                dgvLop.Columns["TENLOP"].HeaderText = "Tên lớp học (TENLOP)";
            if (dgvLop.Columns.Contains("SI_SO"))
                dgvLop.Columns["SI_SO"].HeaderText = "Sĩ số SV hiện tại";
            if (dgvLop.Columns.Contains("SO_LICH_THI"))
                dgvLop.Columns["SO_LICH_THI"].HeaderText = "Số lịch đăng ký thi";
        }

        private void BindComboBoxLop(DataTable dtLop)
        {
            string prevSelected = cboMaLopSV.SelectedValue != null ? cboMaLopSV.SelectedValue.ToString() : _selectedMaLop;
            cboMaLopSV.DataSource = dtLop.Copy();
            cboMaLopSV.DisplayMember = "MALOP";
            cboMaLopSV.ValueMember = "MALOP";

            if (!string.IsNullOrEmpty(prevSelected))
            {
                cboMaLopSV.SelectedValue = prevSelected;
            }
        }

        private void SetLopControlState(SubEditMode mode)
        {
            _lopMode = mode;
            bool editing = (mode != SubEditMode.None);

            txtMaLop.ReadOnly = (mode != SubEditMode.Adding);
            txtTenLop.ReadOnly = !editing;

            btnThemLop.Enabled = !editing;
            btnSuaLop.Enabled = !editing && dgvLop.CurrentRow != null;
            btnXoaLop.Enabled = !editing && dgvLop.CurrentRow != null;
            btnGhiLop.Enabled = editing;
            dgvLop.Enabled = !editing;
        }

        private void DgvLop_SelectionChanged(object sender, EventArgs e)
        {
            if (_lopMode != SubEditMode.None) return;

            if (dgvLop.CurrentRow != null && dgvLop.CurrentRow.Index >= 0)
            {
                _selectedMaLop = Convert.ToString(dgvLop.CurrentRow.Cells["MALOP"].Value).Trim();
                string tenLop = Convert.ToString(dgvLop.CurrentRow.Cells["TENLOP"].Value).Trim();

                txtMaLop.Text = _selectedMaLop;
                txtTenLop.Text = tenLop;

                if (cboMaLopSV.DataSource != null)
                {
                    cboMaLopSV.SelectedValue = _selectedMaLop;
                }

                grpSubformSinhVien.Text = string.Format(
                    "SUBFORM (DETAIL): DANH SÁCH SINH VIÊN THUỘC LỚP [{0} - {1}]",
                    _selectedMaLop, tenLop);

                LoadSinhVienTheoLop(_selectedMaLop);
                btnSuaLop.Enabled = true;
                btnXoaLop.Enabled = true;
            }
        }

        private void BtnThemLop_Click(object sender, EventArgs e)
        {
            txtMaLop.Clear();
            txtTenLop.Clear();
            SetLopControlState(SubEditMode.Adding);
            txtMaLop.Focus();
        }

        private void BtnSuaLop_Click(object sender, EventArgs e)
        {
            if (dgvLop.CurrentRow == null) return;
            SetLopControlState(SubEditMode.Editing);
            txtTenLop.Focus();
            txtTenLop.SelectAll();
        }

        private void BtnGhiLop_Click(object sender, EventArgs e)
        {
            string maLop = ValidationHelper.NormalizeCode(txtMaLop.Text);
            string tenLop = ValidationHelper.NormalizeText(txtTenLop.Text);
            string thongBao;

            bool ok = (_lopMode == SubEditMode.Adding)
                ? _lopBLL.ThemLop(maLop, tenLop, out thongBao)
                : _lopBLL.CapNhatLop(maLop, tenLop, out thongBao);

            if (ok)
            {
                MessageBox.Show(thongBao, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                SetLopControlState(SubEditMode.None);
                LoadDanhSachLop();
                SelectLopRow(maLop);
            }
            else
            {
                MessageBox.Show(thongBao, "Lỗi dữ liệu Lớp", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnXoaLop_Click(object sender, EventArgs e)
        {
            if (dgvLop.CurrentRow == null) return;
            string maLop = Convert.ToString(dgvLop.CurrentRow.Cells["MALOP"].Value).Trim();
            string tenLop = Convert.ToString(dgvLop.CurrentRow.Cells["TENLOP"].Value).Trim();

            DialogResult confirm = MessageBox.Show(
                string.Format("Bạn có chắc chắn muốn xóa lớp [{0} - {1}] không?", maLop, tenLop),
                "Xác nhận xóa lớp",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            string thongBao;
            if (_lopBLL.XoaLop(maLop, out thongBao))
            {
                MessageBox.Show(thongBao, "Đã xóa lớp", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDanhSachLop();
            }
            else
            {
                MessageBox.Show(thongBao, "Không thể xóa lớp", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnHuyLop_Click(object sender, EventArgs e)
        {
            SetLopControlState(SubEditMode.None);
            txtTimLop.Clear();
            LoadDanhSachLop();
        }

        private void BtnTimLop_Click(object sender, EventArgs e)
        {
            DataTable dt = _lopBLL.TimKiemLop(txtTimLop.Text.Trim());
            BindLopGrid(dt);
        }

        private void SelectLopRow(string maLop)
        {
            foreach (DataGridViewRow row in dgvLop.Rows)
            {
                if (string.Equals(Convert.ToString(row.Cells["MALOP"].Value).Trim(), maLop.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    dgvLop.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }

        // =========================================================================
        // XỬ LÝ NGHIỆP VỤ SUBFORM (QUẢN LÝ SINH VIÊN CỦA LỚP ĐANG CHỌN)
        // =========================================================================
        private void LoadSinhVienTheoLop(string maLop)
        {
            DataTable dtSV = _svBLL.LaySinhVienTheoLop(maLop);
            BindSinhVienGrid(dtSV);
            UpdateSvRestoreStatus();
        }

        private void BindSinhVienGrid(DataTable dtSV)
        {
            dgvSinhVien.DataSource = dtSV;
            if (dgvSinhVien.Columns.Contains("MASV"))
                dgvSinhVien.Columns["MASV"].HeaderText = "Mã SV (MASV)";
            if (dgvSinhVien.Columns.Contains("HO"))
                dgvSinhVien.Columns["HO"].HeaderText = "Họ và tên đệm";
            if (dgvSinhVien.Columns.Contains("TEN"))
                dgvSinhVien.Columns["TEN"].HeaderText = "Tên";
            if (dgvSinhVien.Columns.Contains("HOTEN"))
                dgvSinhVien.Columns["HOTEN"].Visible = false;
            if (dgvSinhVien.Columns.Contains("NGAYSINH"))
            {
                dgvSinhVien.Columns["NGAYSINH"].HeaderText = "Ngày sinh";
                dgvSinhVien.Columns["NGAYSINH"].DefaultCellStyle.Format = "dd/MM/yyyy";
            }
            if (dgvSinhVien.Columns.Contains("DIACHI"))
                dgvSinhVien.Columns["DIACHI"].HeaderText = "Địa chỉ";
            if (dgvSinhVien.Columns.Contains("MALOP"))
                dgvSinhVien.Columns["MALOP"].HeaderText = "Mã lớp";
            if (dgvSinhVien.Columns.Contains("TENLOP"))
                dgvSinhVien.Columns["TENLOP"].HeaderText = "Tên lớp";
            if (dgvSinhVien.Columns.Contains("SO_LAN_THI"))
                dgvSinhVien.Columns["SO_LAN_THI"].HeaderText = "Số bài thi";
        }

        private void SetSvControlState(SubEditMode mode)
        {
            _svMode = mode;
            bool editing = (mode != SubEditMode.None);

            txtMaSV.ReadOnly = (mode != SubEditMode.Adding);
            txtHoSV.ReadOnly = !editing;
            txtTenSV.ReadOnly = !editing;
            dtpNgaySinh.Enabled = editing;
            txtDiaChiSV.ReadOnly = !editing;
            cboMaLopSV.Enabled = editing;

            btnThemSV.Enabled = !editing && !string.IsNullOrEmpty(_selectedMaLop);
            btnSuaSV.Enabled = !editing && dgvSinhVien.CurrentRow != null;
            btnXoaSV.Enabled = !editing && dgvSinhVien.CurrentRow != null;
            btnGhiSV.Enabled = editing;
            dgvSinhVien.Enabled = !editing;

            UpdateSvRestoreStatus();
        }

        private void UpdateSvRestoreStatus()
        {
            int deletedInDb = 0;
            try
            {
                DataTable dtDel = _svBLL.LaySinhVienDaXoa(_selectedMaLop);
                deletedInDb = dtDel != null ? dtDel.Rows.Count : 0;
            }
            catch
            {
                deletedInDb = 0;
            }

            btnPhucHoiSV.Enabled = (_svMode == SubEditMode.None) && (_svUndoStack.Count > 0 || deletedInDb > 0);
            lblSvStatus.Text = string.Format(
                "Đang lọc theo lớp: [{0}] | Hoàn tác trong phiên: {1} | Sinh viên đã xóa chờ khôi phục: {2}",
                string.IsNullOrEmpty(_selectedMaLop) ? "Tất cả" : _selectedMaLop,
                _svUndoStack.Count,
                deletedInDb);
        }

        private void DgvSinhVien_SelectionChanged(object sender, EventArgs e)
        {
            if (_svMode != SubEditMode.None) return;

            if (dgvSinhVien.CurrentRow != null && dgvSinhVien.CurrentRow.Index >= 0)
            {
                DataGridViewRow row = dgvSinhVien.CurrentRow;
                txtMaSV.Text = Convert.ToString(row.Cells["MASV"].Value).Trim();
                txtHoSV.Text = Convert.ToString(row.Cells["HO"].Value).Trim();
                txtTenSV.Text = Convert.ToString(row.Cells["TEN"].Value).Trim();

                object dobObj = row.Cells["NGAYSINH"].Value;
                if (dobObj != null && dobObj != DBNull.Value)
                {
                    dtpNgaySinh.Value = Convert.ToDateTime(dobObj);
                }

                txtDiaChiSV.Text = Convert.ToString(row.Cells["DIACHI"].Value).Trim();
                string maLopRow = Convert.ToString(row.Cells["MALOP"].Value).Trim();
                if (!string.IsNullOrEmpty(maLopRow))
                {
                    cboMaLopSV.SelectedValue = maLopRow;
                }

                btnSuaSV.Enabled = true;
                btnXoaSV.Enabled = true;
            }
            else
            {
                txtMaSV.Clear();
                txtHoSV.Clear();
                txtTenSV.Clear();
                txtDiaChiSV.Clear();
                if (!string.IsNullOrEmpty(_selectedMaLop))
                {
                    cboMaLopSV.SelectedValue = _selectedMaLop;
                }
                btnSuaSV.Enabled = false;
                btnXoaSV.Enabled = false;
            }
        }

        private void BtnThemSV_Click(object sender, EventArgs e)
        {
            txtMaSV.Clear();
            txtHoSV.Clear();
            txtTenSV.Clear();
            txtDiaChiSV.Clear();
            dtpNgaySinh.Value = new DateTime(2004, 1, 1);

            if (!string.IsNullOrEmpty(_selectedMaLop))
            {
                cboMaLopSV.SelectedValue = _selectedMaLop;
            }

            SetSvControlState(SubEditMode.Adding);
            txtMaSV.Focus();
        }

        private void BtnSuaSV_Click(object sender, EventArgs e)
        {
            if (dgvSinhVien.CurrentRow == null) return;
            SetSvControlState(SubEditMode.Editing);
            txtHoSV.Focus();
        }

        private SinhVien CaptureCurrentRowSinhVien()
        {
            if (dgvSinhVien.CurrentRow == null) return null;
            DataGridViewRow row = dgvSinhVien.CurrentRow;
            return new SinhVien
            {
                MaSV = Convert.ToString(row.Cells["MASV"].Value).Trim(),
                Ho = Convert.ToString(row.Cells["HO"].Value).Trim(),
                Ten = Convert.ToString(row.Cells["TEN"].Value).Trim(),
                NgaySinh = (row.Cells["NGAYSINH"].Value != null && row.Cells["NGAYSINH"].Value != DBNull.Value)
                    ? (DateTime?)Convert.ToDateTime(row.Cells["NGAYSINH"].Value)
                    : null,
                DiaChi = Convert.ToString(row.Cells["DIACHI"].Value).Trim(),
                MaLop = Convert.ToString(row.Cells["MALOP"].Value).Trim()
            };
        }

        private void BtnGhiSV_Click(object sender, EventArgs e)
        {
            SinhVien sv = new SinhVien
            {
                MaSV = txtMaSV.Text,
                Ho = txtHoSV.Text,
                Ten = txtTenSV.Text,
                NgaySinh = dtpNgaySinh.Value.Date,
                DiaChi = txtDiaChiSV.Text,
                MaLop = cboMaLopSV.SelectedValue != null ? cboMaLopSV.SelectedValue.ToString() : _selectedMaLop
            };

            string thongBao;
            if (_svMode == SubEditMode.Adding)
            {
                if (_svBLL.ThemSinhVien(sv, out thongBao))
                {
                    _svUndoStack.Push(new SvUndoItem
                    {
                        ActionType = "ADD",
                        NewData = sv
                    });

                    MessageBox.Show(thongBao, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetSvControlState(SubEditMode.None);
                    LoadDanhSachLop();
                    SelectLopRow(sv.MaLop);
                    LoadSinhVienTheoLop(sv.MaLop);
                }
                else
                {
                    MessageBox.Show(thongBao, "Lỗi thêm sinh viên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (_svMode == SubEditMode.Editing)
            {
                SinhVien oldData = CaptureCurrentRowSinhVien();
                if (_svBLL.CapNhatSinhVien(sv, out thongBao))
                {
                    _svUndoStack.Push(new SvUndoItem
                    {
                        ActionType = "EDIT",
                        OldData = oldData,
                        NewData = sv
                    });

                    MessageBox.Show(thongBao, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetSvControlState(SubEditMode.None);
                    LoadDanhSachLop();
                    SelectLopRow(sv.MaLop);
                    LoadSinhVienTheoLop(sv.MaLop);
                }
                else
                {
                    MessageBox.Show(thongBao, "Lỗi cập nhật sinh viên", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnXoaSV_Click(object sender, EventArgs e)
        {
            if (dgvSinhVien.CurrentRow == null) return;
            SinhVien oldData = CaptureCurrentRowSinhVien();

            DialogResult confirm = MessageBox.Show(
                string.Format("Bạn có chắc chắn muốn xóa sinh viên [{0} - {1}] khỏi lớp [{2}] không?\n(Có thể nhấn 'Phục hồi SV' để khôi phục)",
                    oldData.MaSV, oldData.HoTen, oldData.MaLop),
                "Xác nhận xóa sinh viên",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            string thongBao;
            if (_svBLL.XoaSinhVien(oldData.MaSV, out thongBao))
            {
                _svUndoStack.Push(new SvUndoItem
                {
                    ActionType = "DELETE",
                    OldData = oldData
                });

                MessageBox.Show(thongBao, "Đã xóa sinh viên", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDanhSachLop();
                SelectLopRow(_selectedMaLop);
                LoadSinhVienTheoLop(_selectedMaLop);
            }
            else
            {
                MessageBox.Show(thongBao, "Không thể xóa sinh viên", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPhucHoiSV_Click(object sender, EventArgs e)
        {
            string thongBao = string.Empty;

            if (_svUndoStack.Count > 0)
            {
                SvUndoItem item = _svUndoStack.Peek();
                string targetMaSV = item.OldData != null ? item.OldData.MaSV : item.NewData.MaSV;

                DialogResult ask = MessageBox.Show(
                    string.Format("Bạn có muốn phục hồi thao tác gần nhất [{0}] đối với sinh viên [{1}] không?",
                        item.ActionType, targetMaSV),
                    "Xác nhận phục hồi sinh viên",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (ask == DialogResult.Yes)
                {
                    _svUndoStack.Pop();
                    bool ok = false;
                    if (item.ActionType == "DELETE")
                    {
                        ok = _svBLL.PhucHoiSinhVien(targetMaSV, out thongBao);
                    }
                    else if (item.ActionType == "EDIT" && item.OldData != null)
                    {
                        ok = _svBLL.CapNhatSinhVien(item.OldData, out thongBao);
                    }
                    else if (item.ActionType == "ADD")
                    {
                        ok = _svBLL.HoanTacThemSinhVien(targetMaSV, out thongBao);
                    }

                    MessageBox.Show(thongBao, ok ? "Phục hồi thành công" : "Thông báo",
                        MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

                    LoadDanhSachLop();
                    SelectLopRow(_selectedMaLop);
                    LoadSinhVienTheoLop(_selectedMaLop);
                    return;
                }
            }

            DataTable dtDeleted = _svBLL.LaySinhVienDaXoa(_selectedMaLop);
            if (dtDeleted != null && dtDeleted.Rows.Count > 0)
            {
                string maSV = Convert.ToString(dtDeleted.Rows[0]["MASV"]).Trim();
                string hoTen = Convert.ToString(dtDeleted.Rows[0]["HOTEN"]).Trim();

                DialogResult askDb = MessageBox.Show(
                    string.Format("Phát hiện sinh viên đã xóa tạm trong CSDL: [{0} - {1}].\nBạn có muốn khôi phục sinh viên này không?",
                        maSV, hoTen),
                    "Khôi phục sinh viên",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (askDb == DialogResult.Yes)
                {
                    if (_svBLL.PhucHoiSinhVien(maSV, out thongBao))
                    {
                        MessageBox.Show(thongBao, "Phục hồi thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDanhSachLop();
                        SelectLopRow(_selectedMaLop);
                        LoadSinhVienTheoLop(_selectedMaLop);
                    }
                    else
                    {
                        MessageBox.Show(thongBao, "Lỗi phục hồi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            else if (_svUndoStack.Count == 0)
            {
                MessageBox.Show("Không có sinh viên nào đang chờ phục hồi!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnTimSV_Click(object sender, EventArgs e)
        {
            string scopeLop = chkTimTatCaLop.Checked ? string.Empty : _selectedMaLop;
            DataTable dt = _svBLL.TimKiemSinhVien(scopeLop, txtTimSV.Text.Trim());
            BindSinhVienGrid(dt);
        }

        private void BtnLamMoiSV_Click(object sender, EventArgs e)
        {
            SetSvControlState(SubEditMode.None);
            txtTimSV.Clear();
            LoadSinhVienTheoLop(_selectedMaLop);
        }
    }
}
