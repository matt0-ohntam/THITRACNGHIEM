using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.Helpers;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Form Quản lý Môn học (Bảng MONHOC).
    /// Đầy đủ chức năng theo đề gốc: Thêm, Xóa, Hiệu chỉnh (Sửa), Phục hồi (Undo + Soft Delete), Tìm, Ghi.
    /// </summary>
    public class FrmMonHoc : Form
    {
        private enum EditMode
        {
            None,
            Adding,
            Editing
        }

        private class UndoAction
        {
            public string ActionType { get; set; } // "ADD", "EDIT", "DELETE"
            public string MaMH { get; set; }
            public string OldTenMH { get; set; }
            public string NewTenMH { get; set; }
        }

        private readonly MonHocBLL _bll = new MonHocBLL();
        private readonly Stack<UndoAction> _undoStack = new Stack<UndoAction>();
        private EditMode _currentMode = EditMode.None;

        private Panel pnlToolbar;
        private Button btnThem;
        private Button btnHieuChinh;
        private Button btnXoa;
        private Button btnGhi;
        private Button btnPhucHoi;
        private Button btnLamMoi;

        private Panel pnlSearchAndInput;
        private GroupBox grpInput;
        private Label lblMaMH;
        private TextBox txtMaMH;
        private Label lblTenMH;
        private TextBox txtTenMH;
        private Label lblModeStatus;

        private GroupBox grpSearch;
        private Label lblTimKiem;
        private TextBox txtTimKiem;
        private Button btnTim;
        private Button btnHienTatCa;
        private Label lblTrashHint;

        private GroupBox grpGrid;
        private DataGridView dgvMonHoc;

        public FrmMonHoc()
        {
            InitializeComponent();
            LoadDanhSachMonHoc();
            SetControlState(EditMode.None);
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.Padding = new Padding(4);

            // 1. Thanh công cụ chức năng (Toolbar)
            pnlToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(12, 8, 12, 8)
            };

            btnThem = CreateActionButton("Thêm", Color.FromArgb(22, 163, 74), 12);
            btnHieuChinh = CreateActionButton("Hiệu chỉnh", Color.FromArgb(37, 99, 235), 140);
            btnXoa = CreateActionButton("Xóa", Color.FromArgb(220, 38, 38), 268);
            btnGhi = CreateActionButton("Ghi (Lưu)", Color.FromArgb(217, 119, 6), 396);
            btnPhucHoi = CreateActionButton("Phục hồi", Color.FromArgb(124, 58, 237), 524);
            btnLamMoi = CreateActionButton("Làm mới / Hủy", Color.FromArgb(71, 85, 105), 652);

            btnThem.Click += BtnThem_Click;
            btnHieuChinh.Click += BtnHieuChinh_Click;
            btnXoa.Click += BtnXoa_Click;
            btnGhi.Click += BtnGhi_Click;
            btnPhucHoi.Click += BtnPhucHoi_Click;
            btnLamMoi.Click += BtnLamMoi_Click;

            pnlToolbar.Controls.AddRange(new Control[]
            {
                btnThem, btnHieuChinh, btnXoa, btnGhi, btnPhucHoi, btnLamMoi
            });

            // 2. Khu vực Nhập liệu & Tìm kiếm
            pnlSearchAndInput = new Panel
            {
                Dock = DockStyle.Top,
                Height = 148,
                Padding = new Padding(0, 10, 0, 10)
            };

            grpInput = new GroupBox
            {
                Text = "Thông tin Môn học (Bảng MONHOC)",
                Location = new Point(0, 8),
                Size = new Size(560, 132),
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            lblMaMH = new Label
            {
                Text = "Mã môn học (MAMH - Tối đa 5 ký tự):",
                Location = new Point(16, 30),
                Size = new Size(255, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            txtMaMH = new TextBox
            {
                Location = new Point(16, 54),
                Size = new Size(165, 28),
                MaxLength = 5,
                CharacterCasing = CharacterCasing.Upper,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold)
            };

            lblTenMH = new Label
            {
                Text = "Tên môn học (TENMH - Duy nhất):",
                Location = new Point(200, 30),
                Size = new Size(340, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            txtTenMH = new TextBox
            {
                Location = new Point(200, 54),
                Size = new Size(340, 28),
                MaxLength = 40,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Regular)
            };

            lblModeStatus = new Label
            {
                Text = "Chế độ: Xem dữ liệu",
                Location = new Point(16, 94),
                Size = new Size(524, 24),
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(37, 99, 235)
            };

            grpInput.Controls.AddRange(new Control[]
            {
                lblMaMH, txtMaMH, lblTenMH, txtTenMH, lblModeStatus
            });

            grpSearch = new GroupBox
            {
                Text = "Tìm kiếm & Trạng thái Phục hồi",
                Location = new Point(574, 8),
                Size = new Size(410, 132),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            lblTimKiem = new Label
            {
                Text = "Nhập Mã MH hoặc Tên môn học cần tìm:",
                Location = new Point(16, 30),
                Size = new Size(360, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };

            txtTimKiem = new TextBox
            {
                Location = new Point(16, 54),
                Size = new Size(210, 28),
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
                Text = "Tìm",
                Location = new Point(234, 52),
                Size = new Size(78, 31),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTim.FlatAppearance.BorderSize = 0;
            btnTim.Click += BtnTim_Click;

            btnHienTatCa = new Button
            {
                Text = "Tất cả",
                Location = new Point(318, 52),
                Size = new Size(78, 31),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnHienTatCa.FlatAppearance.BorderSize = 0;
            btnHienTatCa.Click += (s, e) =>
            {
                txtTimKiem.Clear();
                LoadDanhSachMonHoc();
            };

            lblTrashHint = new Label
            {
                Text = "Số thao tác/môn học có thể phục hồi: 0",
                Location = new Point(16, 94),
                Size = new Size(380, 24),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(124, 58, 237)
            };

            grpSearch.Controls.AddRange(new Control[]
            {
                lblTimKiem, txtTimKiem, btnTim, btnHienTatCa, lblTrashHint
            });

            pnlSearchAndInput.Controls.Add(grpInput);
            pnlSearchAndInput.Controls.Add(grpSearch);

            // 3. Bảng dữ liệu DataGridView
            grpGrid = new GroupBox
            {
                Text = "Danh sách Môn học hiện có",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                Padding = new Padding(10)
            };

            dgvMonHoc = new DataGridView
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
            dgvMonHoc.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            dgvMonHoc.RowTemplate.Height = 32;
            dgvMonHoc.SelectionChanged += DgvMonHoc_SelectionChanged;

            grpGrid.Controls.Add(dgvMonHoc);

            this.Controls.Add(grpGrid);
            this.Controls.Add(pnlSearchAndInput);
            this.Controls.Add(pnlToolbar);
        }

        private Button CreateActionButton(string text, Color backColor, int left)
        {
            Button btn = new Button
            {
                Text = text,
                Location = new Point(left, 8),
                Size = new Size(120, 38),
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
            bool isEditingOrAdding = (mode != EditMode.None);

            txtMaMH.ReadOnly = (mode != EditMode.Adding);
            txtTenMH.ReadOnly = !isEditingOrAdding;

            btnThem.Enabled = !isEditingOrAdding;
            btnHieuChinh.Enabled = !isEditingOrAdding && dgvMonHoc.CurrentRow != null;
            btnXoa.Enabled = !isEditingOrAdding && dgvMonHoc.CurrentRow != null;
            btnGhi.Enabled = isEditingOrAdding;
            dgvMonHoc.Enabled = !isEditingOrAdding;

            UpdateRestoreStatus();

            if (mode == EditMode.Adding)
            {
                lblModeStatus.Text = "Chế độ: Đang THÊM MỚI môn học -> Nhập Mã MH, Tên MH rồi nhấn [Ghi (Lưu)]";
                lblModeStatus.ForeColor = Color.FromArgb(22, 163, 74);
            }
            else if (mode == EditMode.Editing)
            {
                lblModeStatus.Text = "Chế độ: Đang HIỆU CHỈNH môn học -> Sửa Tên MH rồi nhấn [Ghi (Lưu)]";
                lblModeStatus.ForeColor = Color.FromArgb(217, 119, 6);
            }
            else
            {
                lblModeStatus.Text = "Chế độ: Xem dữ liệu (Chọn dòng trên bảng để Sửa hoặc Xóa)";
                lblModeStatus.ForeColor = Color.FromArgb(37, 99, 235);
            }
        }

        private void UpdateRestoreStatus()
        {
            int softDeletedCount = 0;
            try
            {
                DataTable dtDeleted = _bll.LayDanhSachMonHocDaXoa();
                softDeletedCount = dtDeleted != null ? dtDeleted.Rows.Count : 0;
            }
            catch
            {
                softDeletedCount = 0;
            }

            btnPhucHoi.Enabled = (_currentMode == EditMode.None) && (_undoStack.Count > 0 || softDeletedCount > 0);
            lblTrashHint.Text = string.Format("Hoàn tác phiên: {0} | Môn học đã xóa chờ khôi phục: {1}",
                _undoStack.Count, softDeletedCount);
        }

        private void LoadDanhSachMonHoc()
        {
            DataTable dt = _bll.LayDanhSachMonHoc();
            BindGrid(dt);
        }

        private void BindGrid(DataTable dt)
        {
            dgvMonHoc.DataSource = dt;
            if (dgvMonHoc.Columns.Contains("MAMH"))
                dgvMonHoc.Columns["MAMH"].HeaderText = "Mã môn học (MAMH)";
            if (dgvMonHoc.Columns.Contains("TENMH"))
                dgvMonHoc.Columns["TENMH"].HeaderText = "Tên môn học (TENMH)";
            if (dgvMonHoc.Columns.Contains("SO_CAU_HOI"))
                dgvMonHoc.Columns["SO_CAU_HOI"].HeaderText = "Số câu hỏi BODE";
            if (dgvMonHoc.Columns.Contains("SO_LICH_THI"))
                dgvMonHoc.Columns["SO_LICH_THI"].HeaderText = "Số lịch đăng ký";
            if (dgvMonHoc.Columns.Contains("SO_BAI_THI"))
                dgvMonHoc.Columns["SO_BAI_THI"].HeaderText = "Số bài thi";

            UpdateRestoreStatus();
        }

        private void DgvMonHoc_SelectionChanged(object sender, EventArgs e)
        {
            if (_currentMode != EditMode.None) return;

            if (dgvMonHoc.CurrentRow != null && dgvMonHoc.CurrentRow.Index >= 0)
            {
                txtMaMH.Text = Convert.ToString(dgvMonHoc.CurrentRow.Cells["MAMH"].Value).Trim();
                txtTenMH.Text = Convert.ToString(dgvMonHoc.CurrentRow.Cells["TENMH"].Value).Trim();
                btnHieuChinh.Enabled = true;
                btnXoa.Enabled = true;
            }
            else
            {
                txtMaMH.Clear();
                txtTenMH.Clear();
                btnHieuChinh.Enabled = false;
                btnXoa.Enabled = false;
            }
        }

        private void BtnThem_Click(object sender, EventArgs e)
        {
            txtMaMH.Clear();
            txtTenMH.Clear();
            SetControlState(EditMode.Adding);
            txtMaMH.Focus();
        }

        private void BtnHieuChinh_Click(object sender, EventArgs e)
        {
            if (dgvMonHoc.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn một môn học trên bảng để hiệu chỉnh!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            SetControlState(EditMode.Editing);
            txtTenMH.Focus();
            txtTenMH.SelectAll();
        }

        private void BtnGhi_Click(object sender, EventArgs e)
        {
            string maMH = ValidationHelper.NormalizeCode(txtMaMH.Text);
            string tenMH = ValidationHelper.NormalizeText(txtTenMH.Text);
            string thongBao;

            if (_currentMode == EditMode.Adding)
            {
                if (_bll.ThemMonHoc(maMH, tenMH, out thongBao))
                {
                    _undoStack.Push(new UndoAction
                    {
                        ActionType = "ADD",
                        MaMH = maMH,
                        NewTenMH = tenMH
                    });

                    MessageBox.Show(thongBao, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetControlState(EditMode.None);
                    LoadDanhSachMonHoc();
                    SelectRowByMaMH(maMH);
                }
                else
                {
                    MessageBox.Show(thongBao, "Lỗi nhập liệu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (_currentMode == EditMode.Editing)
            {
                string oldTen = dgvMonHoc.CurrentRow != null
                    ? Convert.ToString(dgvMonHoc.CurrentRow.Cells["TENMH"].Value).Trim()
                    : string.Empty;

                if (_bll.CapNhatMonHoc(maMH, tenMH, out thongBao))
                {
                    _undoStack.Push(new UndoAction
                    {
                        ActionType = "EDIT",
                        MaMH = maMH,
                        OldTenMH = oldTen,
                        NewTenMH = tenMH
                    });

                    MessageBox.Show(thongBao, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    SetControlState(EditMode.None);
                    LoadDanhSachMonHoc();
                    SelectRowByMaMH(maMH);
                }
                else
                {
                    MessageBox.Show(thongBao, "Lỗi cập nhật", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnXoa_Click(object sender, EventArgs e)
        {
            if (dgvMonHoc.CurrentRow == null)
            {
                MessageBox.Show("Vui lòng chọn môn học cần xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string maMH = Convert.ToString(dgvMonHoc.CurrentRow.Cells["MAMH"].Value).Trim();
            string tenMH = Convert.ToString(dgvMonHoc.CurrentRow.Cells["TENMH"].Value).Trim();

            DialogResult confirm = MessageBox.Show(
                string.Format("Bạn có chắc chắn muốn xóa môn học [{0} - {1}] không?\n(Dữ liệu sẽ được lưu tạm để có thể Phục hồi an toàn)", maMH, tenMH),
                "Xác nhận xóa môn học",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            string thongBao;
            if (_bll.XoaMonHoc(maMH, out thongBao))
            {
                _undoStack.Push(new UndoAction
                {
                    ActionType = "DELETE",
                    MaMH = maMH,
                    OldTenMH = tenMH
                });

                MessageBox.Show(thongBao, "Đã xóa môn học", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadDanhSachMonHoc();
            }
            else
            {
                MessageBox.Show(thongBao, "Không thể xóa môn học", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPhucHoi_Click(object sender, EventArgs e)
        {
            string thongBao = string.Empty;

            // 1. Ưu tiên phục hồi môn học đã xóa mềm trong CSDL nếu có
            DataTable dtDeleted = _bll.LayDanhSachMonHocDaXoa();
            if (_undoStack.Count > 0)
            {
                UndoAction last = _undoStack.Peek();
                DialogResult askUndo = MessageBox.Show(
                    string.Format("Bạn có muốn phục hồi thao tác gần nhất [{0}] trên môn học [{1}] không?",
                        last.ActionType, last.MaMH),
                    "Xác nhận phục hồi",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (askUndo == DialogResult.Yes)
                {
                    _undoStack.Pop();
                    bool ok = false;
                    if (last.ActionType == "DELETE")
                    {
                        ok = _bll.PhucHoiMonHoc(last.MaMH, out thongBao);
                    }
                    else if (last.ActionType == "EDIT")
                    {
                        ok = _bll.CapNhatMonHoc(last.MaMH, last.OldTenMH, out thongBao);
                    }
                    else if (last.ActionType == "ADD")
                    {
                        ok = _bll.HoanTacThemMonHoc(last.MaMH, out thongBao);
                    }

                    MessageBox.Show(thongBao, ok ? "Phục hồi thành công" : "Thông báo",
                        MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
                    LoadDanhSachMonHoc();
                    SelectRowByMaMH(last.MaMH);
                    return;
                }
            }

            // 2. Nếu có môn học đã xóa mềm trong DB (kể cả từ phiên làm việc trước)
            if (dtDeleted != null && dtDeleted.Rows.Count > 0)
            {
                string maMHRestore = Convert.ToString(dtDeleted.Rows[0]["MAMH"]).Trim();
                string tenMHRestore = Convert.ToString(dtDeleted.Rows[0]["TENMH"]).Trim();

                DialogResult askRestoreDb = MessageBox.Show(
                    string.Format("Phát hiện môn học đã xóa tạm trong CSDL: [{0} - {1}].\nBạn có muốn khôi phục môn học này không?",
                        maMHRestore, tenMHRestore),
                    "Khôi phục dữ liệu đã xóa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (askRestoreDb == DialogResult.Yes)
                {
                    if (_bll.PhucHoiMonHoc(maMHRestore, out thongBao))
                    {
                        MessageBox.Show(thongBao, "Phục hồi thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadDanhSachMonHoc();
                        SelectRowByMaMH(maMHRestore);
                    }
                    else
                    {
                        MessageBox.Show(thongBao, "Lỗi phục hồi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            else if (_undoStack.Count == 0)
            {
                MessageBox.Show("Hiện không có dữ liệu môn học nào cần phục hồi!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnTim_Click(object sender, EventArgs e)
        {
            string keyword = txtTimKiem.Text.Trim();
            DataTable dt = _bll.TimKiemMonHoc(keyword);
            BindGrid(dt);
        }

        private void BtnLamMoi_Click(object sender, EventArgs e)
        {
            SetControlState(EditMode.None);
            txtTimKiem.Clear();
            LoadDanhSachMonHoc();
            DgvMonHoc_SelectionChanged(null, EventArgs.Empty);
        }

        private void SelectRowByMaMH(string maMH)
        {
            foreach (DataGridViewRow row in dgvMonHoc.Rows)
            {
                if (string.Equals(Convert.ToString(row.Cells["MAMH"].Value).Trim(), maMH.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    dgvMonHoc.CurrentCell = row.Cells[0];
                    break;
                }
            }
        }
    }
}
