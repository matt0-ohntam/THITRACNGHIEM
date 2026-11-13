using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.Helpers;
using TRAC_NGHIEM.Models;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Mục 7b: Xem kết quả (In lại các câu đã thi của sinh viên).
    /// Hỗ trợ cả 2 vai trò:
    /// - Sinh viên (role_SinhVien): Chỉ được xem và in lại các câu đã thi của chính mình (SUSER_SNAME() = MASV).
    /// - Giáo viên (role_GiaoVien): Chọn Lớp / Sinh viên / Môn học / Lần thi bất kỳ để xem và in lại phiếu bài thi.
    /// Hiển thị đúng mẫu trong đề gốc (De2-ThiTN.doc):
    ///   Lớp, Họ tên, Mã số, Môn thi, Ngày thi, Lần thi
    ///   Bảng: STT | Câu số (trong bộ đề) | Nội dung câu hỏi + Các chọn lựa (A, B, C, D) | Đã chọn | Đáp án
    /// </summary>
    public class FrmXemKetQua : Form
    {
        private readonly BaoCaoBLL _bll = new BaoCaoBLL();
        private DataTable _dtDanhSachPhieu;
        private DataRow _currentHeader;
        private DataTable _currentQuestions;
        private int _printRowIndex = 0;

        private Panel pnlFilter;
        private Label lblChonPhieu;
        private ComboBox cboPhieuThi;
        private Button btnTaiKetQua;
        private Button btnInPhieu;
        private Button btnXuatHtml;

        private Panel pnlReportHeader;
        private Label lblReportTitle;
        private Label lblLopInfo;
        private Label lblHoTenInfo;
        private Label lblMaSoInfo;
        private Label lblMonThiInfo;
        private Label lblNgayThiInfo;
        private Label lblLanThiInfo;
        private Label lblDiemTongKet;

        private GroupBox grpChiTietCauHoi;
        private DataGridView dgvChiTietCau;

        public FrmXemKetQua()
        {
            InitializeComponent();
            LoadDanhSachPhieuThi();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.Padding = new Padding(4);

            // 1. Thanh chọn bài thi & nút In phiếu kết quả
            pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.White,
                Padding = new Padding(16, 10, 16, 10)
            };

            bool isSV = SessionManager.IsSinhVien();
            lblChonPhieu = new Label
            {
                Text = isSV
                    ? "Bài thi đã hoàn thành của bạn:"
                    : "Chọn Bài thi (Sinh viên / Môn / Lần thi):",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(16, 22),
                AutoSize = true
            };

            cboPhieuThi = new ComboBox
            {
                Location = new Point(285, 18),
                Size = new Size(375, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            cboPhieuThi.SelectedIndexChanged += (s, e) => HienThiPhieuDangChon();

            btnTaiKetQua = new Button
            {
                Text = "Làm mới",
                Location = new Point(670, 16),
                Size = new Size(95, 34),
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnTaiKetQua.FlatAppearance.BorderSize = 0;
            btnTaiKetQua.Click += (s, e) => LoadDanhSachPhieuThi();

            btnInPhieu = new Button
            {
                Text = "Xem trước & In Kết quả",
                Location = new Point(773, 16),
                Size = new Size(175, 34),
                BackColor = Color.FromArgb(185, 28, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInPhieu.FlatAppearance.BorderSize = 0;
            btnInPhieu.Click += BtnInPhieu_Click;

            btnXuatHtml = new Button
            {
                Text = "Xuất HTML",
                Location = new Point(956, 16),
                Size = new Size(95, 34),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnXuatHtml.FlatAppearance.BorderSize = 0;
            btnXuatHtml.Click += BtnXuatHtml_Click;

            pnlFilter.Controls.AddRange(new Control[]
            {
                lblChonPhieu, cboPhieuThi, btnTaiKetQua, btnInPhieu, btnXuatHtml
            });

            // 2. Khung Thông tin Phiếu kết quả (Theo đúng mẫu trong đề De2-ThiTN.doc)
            pnlReportHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 142,
                BackColor = Color.White,
                Padding = new Padding(20, 10, 20, 10)
            };

            lblReportTitle = new Label
            {
                Text = "PHIẾU KẾT QUẢ THI TRẮC NGHIỆM (CHI TIẾT CÁC CÂU ĐÃ THI)",
                Font = new Font("Segoe UI", 12.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Location = new Point(20, 10),
                AutoSize = true
            };

            lblLopInfo = CreateInfoLabel("Lớp\t\t: ---", 20, 44, 460);
            lblDiemTongKet = new Label
            {
                Text = "Điểm số: --- / 10",
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                Location = new Point(520, 42),
                Size = new Size(440, 26)
            };

            lblHoTenInfo = CreateInfoLabel("Họ tên\t\t: ---", 20, 74, 460);
            lblMaSoInfo = CreateInfoLabel("Mã số SV\t: ---", 520, 74, 380);

            lblMonThiInfo = CreateInfoLabel("Môn thi\t\t: ---", 20, 104, 460);
            lblNgayThiInfo = CreateInfoLabel("Ngày thi\t: ---", 520, 104, 240);
            lblLanThiInfo = CreateInfoLabel("Lần thi: ---", 770, 104, 180);

            pnlReportHeader.Controls.AddRange(new Control[]
            {
                lblReportTitle, lblLopInfo, lblDiemTongKet,
                lblHoTenInfo, lblMaSoInfo,
                lblMonThiInfo, lblNgayThiInfo, lblLanThiInfo
            });

            // 3. Bảng chi tiết các câu hỏi đã thi
            grpChiTietCauHoi = new GroupBox
            {
                Text = "Danh sách các câu hỏi đã thi & Đáp án đối chiếu (Bảng BODE + BANGDIEM.BAITHI)",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(12, 26, 12, 12)
            };

            dgvChiTietCau = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(15, 23, 42)
            };
            dgvChiTietCau.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgvChiTietCau.DefaultCellStyle.Padding = new Padding(4, 6, 4, 6);
            dgvChiTietCau.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvChiTietCau.CellFormatting += DgvChiTietCau_CellFormatting;

            grpChiTietCauHoi.Controls.Add(dgvChiTietCau);

            this.Controls.Add(grpChiTietCauHoi);
            this.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
            this.Controls.Add(pnlReportHeader);
            this.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
            this.Controls.Add(pnlFilter);
        }

        private Label CreateInfoLabel(string text, int x, int y, int width)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 24),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59)
            };
        }

        private void LoadDanhSachPhieuThi()
        {
            _dtDanhSachPhieu = _bll.LayDanhSachBaiThiDaCo();
            cboPhieuThi.Items.Clear();

            if (_dtDanhSachPhieu != null && _dtDanhSachPhieu.Rows.Count > 0)
            {
                foreach (DataRow row in _dtDanhSachPhieu.Rows)
                {
                    cboPhieuThi.Items.Add(Convert.ToString(row["HIENTHI_PHIEU"]));
                }
                cboPhieuThi.SelectedIndex = 0;
            }
            else
            {
                _currentHeader = null;
                _currentQuestions = null;
                dgvChiTietCau.DataSource = null;
                lblLopInfo.Text = "Lớp\t\t: (Chưa có dữ liệu bài thi)";
            }
        }

        private void HienThiPhieuDangChon()
        {
            int idx = cboPhieuThi.SelectedIndex;
            if (_dtDanhSachPhieu == null || idx < 0 || idx >= _dtDanhSachPhieu.Rows.Count)
                return;

            DataRow selected = _dtDanhSachPhieu.Rows[idx];
            string maSV = Convert.ToString(selected["MASV"]).Trim();
            string maMH = Convert.ToString(selected["MAMH"]).Trim();
            short lan = Convert.ToInt16(selected["LAN"]);

            DataRow headerRow;
            DataTable dtQuestions;
            string thongBao;
            if (_bll.XemKetQuaBaiThi(maSV, maMH, lan, out headerRow, out dtQuestions, out thongBao))
            {
                _currentHeader = headerRow;
                _currentQuestions = dtQuestions;

                string tenLop = Convert.ToString(headerRow["TENLOP"]).Trim();
                string maLop = Convert.ToString(headerRow["MALOP"]).Trim();
                string hoTen = Convert.ToString(headerRow["HOTEN"]).Trim();
                string tenMH = Convert.ToString(headerRow["TENMH"]).Trim();
                string trinhDo = Convert.ToString(headerRow["TRINHDO"]).Trim();
                DateTime ngayThi = Convert.ToDateTime(headerRow["NGAYTHI"]);
                double diem = Convert.ToDouble(headerRow["DIEM"]);

                int soCauDung = 0;
                int tongSoCau = dtQuestions != null ? dtQuestions.Rows.Count : 0;
                if (dtQuestions != null)
                {
                    foreach (DataRow q in dtQuestions.Rows)
                    {
                        if (string.Equals(Convert.ToString(q["KET_QUA"]).Trim(), "Đúng", StringComparison.OrdinalIgnoreCase))
                            soCauDung++;
                    }
                }

                lblLopInfo.Text = string.Format("Lớp\t\t: {0} ({1})", tenLop, maLop);
                lblHoTenInfo.Text = string.Format("Họ tên\t\t: {0}", hoTen);
                lblMaSoInfo.Text = string.Format("Mã số SV\t: {0}", maSV);
                lblMonThiInfo.Text = string.Format("Môn thi\t\t: {0} ({1} - Trình độ {2})", tenMH, maMH, trinhDo);
                lblNgayThiInfo.Text = string.Format("Ngày thi\t: {0:dd/MM/yyyy}", ngayThi);
                lblLanThiInfo.Text = string.Format("Lần thi: {0}", lan);
                lblDiemTongKet.Text = string.Format("Điểm đạt được: {0:0.0} / 10   (Số câu đúng: {1}/{2})", diem, soCauDung, tongSoCau);

                dgvChiTietCau.DataSource = dtQuestions;
                ConfigureGridColumns();
            }
            else
            {
                MessageBox.Show(thongBao, "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ConfigureGridColumns()
        {
            if (dgvChiTietCau.Columns.Contains("STT"))
            {
                dgvChiTietCau.Columns["STT"].HeaderText = "STT";
                dgvChiTietCau.Columns["STT"].FillWeight = 8;
            }
            if (dgvChiTietCau.Columns.Contains("CAUHOI"))
            {
                dgvChiTietCau.Columns["CAUHOI"].HeaderText = "Câu số (trong bộ đề)";
                dgvChiTietCau.Columns["CAUHOI"].FillWeight = 14;
            }
            if (dgvChiTietCau.Columns.Contains("NOIDUNG_VA_LUA_CHON"))
            {
                dgvChiTietCau.Columns["NOIDUNG_VA_LUA_CHON"].HeaderText = "Nội dung câu hỏi & Các chọn lựa (A, B, C, D)";
                dgvChiTietCau.Columns["NOIDUNG_VA_LUA_CHON"].FillWeight = 52;
            }
            if (dgvChiTietCau.Columns.Contains("DA_CHON"))
            {
                dgvChiTietCau.Columns["DA_CHON"].HeaderText = "Đã chọn";
                dgvChiTietCau.Columns["DA_CHON"].FillWeight = 9;
            }
            if (dgvChiTietCau.Columns.Contains("DAP_AN"))
            {
                dgvChiTietCau.Columns["DAP_AN"].HeaderText = "Đáp án";
                dgvChiTietCau.Columns["DAP_AN"].FillWeight = 9;
            }
            if (dgvChiTietCau.Columns.Contains("KET_QUA"))
            {
                dgvChiTietCau.Columns["KET_QUA"].HeaderText = "Kết quả";
                dgvChiTietCau.Columns["KET_QUA"].FillWeight = 8;
            }

            string[] hiddenCols = { "TRINHDO", "NOIDUNG", "A", "B", "C", "D" };
            foreach (string col in hiddenCols)
            {
                if (dgvChiTietCau.Columns.Contains(col))
                {
                    dgvChiTietCau.Columns[col].Visible = false;
                }
            }
        }

        private void DgvChiTietCau_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !dgvChiTietCau.Columns.Contains("KET_QUA")) return;

            string ketQua = Convert.ToString(dgvChiTietCau.Rows[e.RowIndex].Cells["KET_QUA"].Value).Trim();
            if (dgvChiTietCau.Columns[e.ColumnIndex].Name == "KET_QUA" ||
                dgvChiTietCau.Columns[e.ColumnIndex].Name == "DA_CHON")
            {
                if (string.Equals(ketQua, "Đúng", StringComparison.OrdinalIgnoreCase))
                {
                    e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle.ForeColor = Color.FromArgb(185, 28, 28);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
            }
        }

        private void BtnInPhieu_Click(object sender, EventArgs e)
        {
            if (_currentHeader == null || _currentQuestions == null || _currentQuestions.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu bài thi để in!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _printRowIndex = 0;
            PrintDocument pd = new PrintDocument();
            pd.DocumentName = "PhieuKetQuaThiTracNghiem_" + Convert.ToString(_currentHeader["MASV"]).Trim();
            pd.PrintPage += PrintDocument_PrintPage;

            using (PrintPreviewDialog preview = new PrintPreviewDialog())
            {
                preview.Document = pd;
                preview.Text = "Xem trước bản in - Mục 7b: Kết quả các câu đã thi";
                preview.WindowState = FormWindowState.Maximized;
                preview.ShowDialog(this);
            }
        }

        private void PrintDocument_PrintPage(object sender, PrintPageEventArgs e)
        {
            Graphics g = e.Graphics;
            int left = e.MarginBounds.Left;
            int top = e.MarginBounds.Top;
            int width = e.MarginBounds.Width;
            int y = top;

            using (Font fSchool = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (Font fTitle = new Font("Segoe UI", 14F, FontStyle.Bold))
            using (Font fInfo = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (Font fHeader = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (Font fCell = new Font("Segoe UI", 9F, FontStyle.Regular))
            using (Pen penBorder = new Pen(Color.FromArgb(100, 116, 139), 1F))
            using (Brush brushHeader = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                if (_printRowIndex == 0)
                {
                    g.DrawString("HỌC VIỆN CÔNG NGHỆ BƯU CHÍNH VIỄN THÔNG (PTIT)", fSchool, Brushes.DarkRed, left, y);
                    y += 28;
                    g.DrawString("PHIẾU KẾT QUẢ THI TRẮC NGHIỆM (CHI TIẾT BÀI THI)", fTitle, Brushes.Black, left + 80, y);
                    y += 36;

                    g.DrawString(string.Format("Lớp: {0} ({1})", _currentHeader["TENLOP"], _currentHeader["MALOP"]), fInfo, Brushes.Black, left, y);
                    g.DrawString(string.Format("Điểm đạt được: {0:0.0} / 10", Convert.ToDouble(_currentHeader["DIEM"])), fInfo, Brushes.DarkGreen, left + 400, y);
                    y += 24;

                    g.DrawString(string.Format("Họ tên: {0}", _currentHeader["HOTEN"]), fInfo, Brushes.Black, left, y);
                    g.DrawString(string.Format("Mã số: {0}", _currentHeader["MASV"]), fInfo, Brushes.Black, left + 400, y);
                    y += 24;

                    g.DrawString(string.Format("Môn thi: {0} (Trình độ {1})", _currentHeader["TENMH"], _currentHeader["TRINHDO"]), fInfo, Brushes.Black, left, y);
                    g.DrawString(string.Format("Ngày thi: {0:dd/MM/yyyy}     Lần thi: {1}", Convert.ToDateTime(_currentHeader["NGAYTHI"]), _currentHeader["LAN"]), fInfo, Brushes.Black, left + 400, y);
                    y += 32;
                }

                int wStt = 45;
                int wCauSo = 75;
                int wDaChon = 65;
                int wDapAn = 65;
                int wNoiDung = width - (wStt + wCauSo + wDaChon + wDapAn);

                Rectangle rHeader = new Rectangle(left, y, width, 34);
                g.FillRectangle(brushHeader, rHeader);
                g.DrawRectangle(penBorder, rHeader);

                int x = left;
                g.DrawString("STT", fHeader, Brushes.Black, new RectangleF(x + 4, y + 7, wStt, 24));
                x += wStt;
                g.DrawLine(penBorder, x, y, x, y + 34);
                g.DrawString("Câu số", fHeader, Brushes.Black, new RectangleF(x + 4, y + 7, wCauSo, 24));
                x += wCauSo;
                g.DrawLine(penBorder, x, y, x, y + 34);
                g.DrawString("Nội dung câu hỏi & Các chọn lựa", fHeader, Brushes.Black, new RectangleF(x + 6, y + 7, wNoiDung, 24));
                x += wNoiDung;
                g.DrawLine(penBorder, x, y, x, y + 34);
                g.DrawString("Đã chọn", fHeader, Brushes.Black, new RectangleF(x + 4, y + 7, wDaChon, 24));
                x += wDaChon;
                g.DrawLine(penBorder, x, y, x, y + 34);
                g.DrawString("Đáp án", fHeader, Brushes.Black, new RectangleF(x + 4, y + 7, wDapAn, 24));
                y += 34;

                while (_printRowIndex < _currentQuestions.Rows.Count)
                {
                    DataRow r = _currentQuestions.Rows[_printRowIndex];
                    string nd = Convert.ToString(r["NOIDUNG_VA_LUA_CHON"]);
                    SizeF sz = g.MeasureString(nd, fCell, wNoiDung - 12);
                    int rowHeight = Math.Max(42, (int)Math.Ceiling(sz.Height) + 14);

                    if (y + rowHeight > e.MarginBounds.Bottom)
                    {
                        e.HasMorePages = true;
                        return;
                    }

                    g.DrawRectangle(penBorder, left, y, width, rowHeight);
                    int cx = left;
                    g.DrawString(Convert.ToString(r["STT"]), fCell, Brushes.Black, new RectangleF(cx + 6, y + 6, wStt - 8, rowHeight));
                    cx += wStt;
                    g.DrawLine(penBorder, cx, y, cx, y + rowHeight);

                    g.DrawString(Convert.ToString(r["CAUHOI"]), fCell, Brushes.Black, new RectangleF(cx + 8, y + 6, wCauSo - 10, rowHeight));
                    cx += wCauSo;
                    g.DrawLine(penBorder, cx, y, cx, y + rowHeight);

                    g.DrawString(nd, fCell, Brushes.Black, new RectangleF(cx + 6, y + 6, wNoiDung - 12, rowHeight));
                    cx += wNoiDung;
                    g.DrawLine(penBorder, cx, y, cx, y + rowHeight);

                    g.DrawString(Convert.ToString(r["DA_CHON"]), fHeader, Brushes.Black, new RectangleF(cx + 14, y + 6, wDaChon - 10, rowHeight));
                    cx += wDaChon;
                    g.DrawLine(penBorder, cx, y, cx, y + rowHeight);

                    g.DrawString(Convert.ToString(r["DAP_AN"]), fHeader, Brushes.DarkGreen, new RectangleF(cx + 14, y + 6, wDapAn - 10, rowHeight));

                    y += rowHeight;
                    _printRowIndex++;
                }

                e.HasMorePages = false;
            }
        }

        private void BtnXuatHtml_Click(object sender, EventArgs e)
        {
            if (_currentHeader == null || _currentQuestions == null)
            {
                MessageBox.Show("Không có dữ liệu bài thi để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Xuất Phiếu kết quả bài thi (HTML)";
                sfd.Filter = "HTML Report (*.html)|*.html";
                sfd.FileName = string.Format("PhieuKetQua_{0}_{1}_Lan{2}.html",
                    Convert.ToString(_currentHeader["MASV"]).Trim(),
                    Convert.ToString(_currentHeader["MAMH"]).Trim(),
                    _currentHeader["LAN"]);

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Phiếu kết quả bài thi PTIT</title>");
                    sb.AppendLine("<style>body{font-family:'Segoe UI',Arial,sans-serif;margin:30px;} table{width:100%;border-collapse:collapse;margin-top:16px;} th,td{border:1px solid #475569;padding:8px;text-align:left;} th{background:#f1f5f9;}</style></head><body>");
                    sb.AppendLine("<h2>HỌC VIỆN CÔNG NGHỆ BƯU CHÍNH VIỄN THÔNG (PTIT)</h2>");
                    sb.AppendLine("<h3>PHIẾU KẾT QUẢ THI TRẮC NGHIỆM (IN LẠI CÁC CÂU ĐÃ THI)</h3>");
                    sb.AppendFormat("<p><b>Lớp:</b> {0} ({1}) &nbsp;&nbsp;|&nbsp;&nbsp; <b>Điểm số:</b> {2:0.0} / 10</p>",
                        _currentHeader["TENLOP"], _currentHeader["MALOP"], Convert.ToDouble(_currentHeader["DIEM"]));
                    sb.AppendFormat("<p><b>Họ tên:</b> {0} &nbsp;&nbsp;|&nbsp;&nbsp; <b>Mã số SV:</b> {1}</p>",
                        _currentHeader["HOTEN"], _currentHeader["MASV"]);
                    sb.AppendFormat("<p><b>Môn thi:</b> {0} &nbsp;&nbsp;|&nbsp;&nbsp; <b>Ngày thi:</b> {1:dd/MM/yyyy} &nbsp;&nbsp;|&nbsp;&nbsp; <b>Lần thi:</b> {2}</p>",
                        _currentHeader["TENMH"], Convert.ToDateTime(_currentHeader["NGAYTHI"]), _currentHeader["LAN"]);
                    sb.AppendLine("<table><thead><tr><th>STT</th><th>Câu số (trong bộ đề)</th><th>Nội dung câu hỏi & Các chọn lựa</th><th>Đã chọn</th><th>Đáp án</th></tr></thead><tbody>");

                    foreach (DataRow r in _currentQuestions.Rows)
                    {
                        sb.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}<br/><i>A. {3} | B. {4} | C. {5} | D. {6}</i></td><td><b>{7}</b></td><td><b>{8}</b></td></tr>",
                            r["STT"], r["CAUHOI"], r["NOIDUNG"], r["A"], r["B"], r["C"], r["D"], r["DA_CHON"], r["DAP_AN"]);
                    }
                    sb.AppendLine("</tbody></table></body></html>");
                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Đã xuất Phiếu kết quả bài thi thành công:\n" + sfd.FileName, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}

