using System;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.Helpers;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Mục 8: Bảng điểm môn học (Report in theo mẫu Học viện Công nghệ Bưu chính Viễn thông - PTIT).
    /// Giáo viên chọn Tên lớp, Tên môn học, Lần thi (1 hoặc 2); chương trình kết xuất và in Bảng điểm thi hết môn của lớp đã chọn.
    /// </summary>
    public class FrmBangDiemMonHoc : Form
    {
        private readonly BaoCaoBLL _baoCaoBLL = new BaoCaoBLL();
        private readonly LopBLL _lopBLL = new LopBLL();
        private readonly MonHocBLL _monHocBLL = new MonHocBLL();

        private DataTable _currentBangDiem;
        private string _selectedMaLop = string.Empty;
        private string _selectedTenLop = string.Empty;
        private string _selectedMaMH = string.Empty;
        private string _selectedTenMH = string.Empty;
        private short _selectedLanThi = 1;
        private int _printRowIndex = 0;

        private Panel pnlFilter;
        private Label lblLop;
        private ComboBox cboLop;
        private Label lblMonHoc;
        private ComboBox cboMonHoc;
        private Label lblLanThi;
        private ComboBox cboLanThi;
        private Button btnXemBangDiem;
        private Button btnInReport;
        private Button btnXuatHtml;

        private Panel pnlReportHeader;
        private Label lblSchoolTitle;
        private Label lblReportMainTitle;
        private Label lblSubInfo;
        private Label lblSummaryMetrics;

        private GroupBox grpBangDiem;
        private DataGridView dgvBangDiem;

        public FrmBangDiemMonHoc()
        {
            SessionManager.EnsureGiaoVienPermission("Bảng điểm môn học");
            InitializeComponent();
            LoadFilterSelections();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            this.Padding = new Padding(4);

            // 1. Thanh công cụ lọc Lớp - Môn học - Lần thi
            pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 72,
                BackColor = Color.White,
                Padding = new Padding(14, 10, 14, 10)
            };

            lblLop = new Label
            {
                Text = "Lớp:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(14, 24),
                AutoSize = true
            };

            cboLop = new ComboBox
            {
                Location = new Point(54, 20),
                Size = new Size(215, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            cboLop.SelectedIndexChanged += (s, e) => KetXuatBangDiem();

            lblMonHoc = new Label
            {
                Text = "Môn học:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(280, 24),
                AutoSize = true
            };

            cboMonHoc = new ComboBox
            {
                Location = new Point(352, 20),
                Size = new Size(225, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            cboMonHoc.SelectedIndexChanged += (s, e) => KetXuatBangDiem();

            lblLanThi = new Label
            {
                Text = "Lần:",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(588, 24),
                AutoSize = true
            };

            cboLanThi = new ComboBox
            {
                Location = new Point(626, 20),
                Size = new Size(80, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular)
            };
            cboLanThi.Items.Add("Lần 1");
            cboLanThi.Items.Add("Lần 2");
            cboLanThi.SelectedIndex = 0;
            cboLanThi.SelectedIndexChanged += (s, e) => KetXuatBangDiem();

            btnXemBangDiem = new Button
            {
                Text = "Kết xuất",
                Location = new Point(716, 18),
                Size = new Size(88, 34),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnXemBangDiem.FlatAppearance.BorderSize = 0;
            btnXemBangDiem.Click += (s, e) => KetXuatBangDiem();

            btnInReport = new Button
            {
                Text = "In / Xem trước Report",
                Location = new Point(810, 18),
                Size = new Size(155, 34),
                BackColor = Color.FromArgb(185, 28, 28),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInReport.FlatAppearance.BorderSize = 0;
            btnInReport.Click += BtnInReport_Click;

            btnXuatHtml = new Button
            {
                Text = "Xuất HTML",
                Location = new Point(970, 18),
                Size = new Size(86, 34),
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
                lblLop, cboLop, lblMonHoc, cboMonHoc, lblLanThi, cboLanThi,
                btnXemBangDiem, btnInReport, btnXuatHtml
            });

            // 2. Tiêu đề Mẫu Bảng điểm Học viện Công nghệ Bưu chính Viễn thông (PTIT)
            pnlReportHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 128,
                BackColor = Color.White,
                Padding = new Padding(18, 10, 18, 10)
            };

            lblSchoolTitle = new Label
            {
                Text = "HỌC VIỆN CÔNG NGHỆ BƯU CHÍNH VIỄN THÔNG (PTIT)  -  PHÒNG KHẢO THÍ & ĐẢM BẢO CHẤT LƯỢNG",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 116, 139),
                Location = new Point(18, 10),
                AutoSize = true
            };

            lblReportMainTitle = new Label
            {
                Text = "BẢNG ĐIỂM THI HẾT MÔN",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Location = new Point(18, 34),
                AutoSize = true
            };

            lblSubInfo = new Label
            {
                Text = "Lớp: ---   |   Môn học: ---   |   Lần thi: 1",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Location = new Point(18, 66),
                Size = new Size(960, 24)
            };

            lblSummaryMetrics = new Label
            {
                Text = "Tổng số SV: 0   |   Đã dự thi: 0   |   Chưa thi: 0   |   Điểm TB: 0.0   |   Số SV Đạt (>= 5.0): 0",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(22, 101, 52),
                Location = new Point(18, 94),
                Size = new Size(960, 24)
            };

            pnlReportHeader.Controls.AddRange(new Control[]
            {
                lblSchoolTitle, lblReportMainTitle, lblSubInfo, lblSummaryMetrics
            });

            // 3. Bảng điểm chi tiết của lớp
            grpBangDiem = new GroupBox
            {
                Text = "Danh sách điểm thi hết môn của sinh viên thuộc lớp đã chọn",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(12, 26, 12, 12)
            };

            dgvBangDiem = new DataGridView
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
            dgvBangDiem.RowTemplate.Height = 32;
            dgvBangDiem.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgvBangDiem.CellFormatting += DgvBangDiem_CellFormatting;

            grpBangDiem.Controls.Add(dgvBangDiem);

            this.Controls.Add(grpBangDiem);
            this.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
            this.Controls.Add(pnlReportHeader);
            this.Controls.Add(new Panel { Dock = DockStyle.Top, Height = 8 });
            this.Controls.Add(pnlFilter);
        }

        private void LoadFilterSelections()
        {
            DataTable dtLop = _lopBLL.LayDanhSachLop();
            if (dtLop != null)
            {
                if (!dtLop.Columns.Contains("HIENTHI_LOP"))
                {
                    dtLop.Columns.Add("HIENTHI_LOP", typeof(string));
                    foreach (DataRow r in dtLop.Rows)
                    {
                        r["HIENTHI_LOP"] = string.Format("{0} - {1}", Convert.ToString(r["MALOP"]).Trim(), Convert.ToString(r["TENLOP"]).Trim());
                    }
                }
                cboLop.DataSource = dtLop;
                cboLop.DisplayMember = "HIENTHI_LOP";
                cboLop.ValueMember = "MALOP";
            }

            DataTable dtMH = _monHocBLL.LayDanhSachMonHoc();
            if (dtMH != null)
            {
                if (!dtMH.Columns.Contains("HIENTHI_MH"))
                {
                    dtMH.Columns.Add("HIENTHI_MH", typeof(string));
                    foreach (DataRow r in dtMH.Rows)
                    {
                        r["HIENTHI_MH"] = string.Format("{0} - {1}", Convert.ToString(r["MAMH"]).Trim(), Convert.ToString(r["TENMH"]).Trim());
                    }
                }
                cboMonHoc.DataSource = dtMH;
                cboMonHoc.DisplayMember = "HIENTHI_MH";
                cboMonHoc.ValueMember = "MAMH";
            }

            KetXuatBangDiem();
        }

        private void KetXuatBangDiem()
        {
            if (cboLop.SelectedValue == null || cboMonHoc.SelectedValue == null) return;

            string maLop = cboLop.SelectedValue.ToString().Trim();
            string maMH = cboMonHoc.SelectedValue.ToString().Trim();
            if (maLop.Contains("SYSTEM.DATA") || maMH.Contains("SYSTEM.DATA")) return;

            short lan = (short)(cboLanThi.SelectedIndex == 1 ? 2 : 1);

            DataTable dt;
            string thongBao;
            if (_baoCaoBLL.LayBangDiemMonHoc(maLop, maMH, lan, out dt, out thongBao))
            {
                _currentBangDiem = dt;
                _selectedMaLop = maLop;
                _selectedMaMH = maMH;
                _selectedLanThi = lan;

                DataRowView drvLop = cboLop.SelectedItem as DataRowView;
                DataRowView drvMH = cboMonHoc.SelectedItem as DataRowView;
                _selectedTenLop = drvLop != null ? Convert.ToString(drvLop["TENLOP"]).Trim() : maLop;
                _selectedTenMH = drvMH != null ? Convert.ToString(drvMH["TENMH"]).Trim() : maMH;

                lblSubInfo.Text = string.Format("Lớp: {0} ({1})     |     Môn học: {2} ({3})     |     Lần thi: {4}",
                    _selectedTenLop, _selectedMaLop, _selectedTenMH, _selectedMaMH, _selectedLanThi);

                int tongSV = dt != null ? dt.Rows.Count : 0;
                int daThi = 0;
                int soDat = 0;
                double tongDiem = 0;

                if (dt != null)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        if (Convert.ToInt32(r["CO_DIEM"]) == 1 && r["DIEM"] != DBNull.Value)
                        {
                            daThi++;
                            double d = Convert.ToDouble(r["DIEM"]);
                            tongDiem += d;
                            if (d >= 5.0) soDat++;
                        }
                    }
                }

                int chuaThi = tongSV - daThi;
                double diemTB = daThi > 0 ? (tongDiem / daThi) : 0.0;

                lblSummaryMetrics.Text = string.Format(
                    "Sĩ số lớp: {0} SV   |   Đã dự thi: {1} SV   |   Chưa dự thi: {2} SV   |   Điểm TB (SV dự thi): {3:0.00}   |   Đạt (>= 5.0): {4}/{1} SV",
                    tongSV, daThi, chuaThi, diemTB, soDat);

                dgvBangDiem.DataSource = dt;
                ConfigureGridColumns();
            }
        }

        private void ConfigureGridColumns()
        {
            if (dgvBangDiem.Columns.Contains("STT"))
            {
                dgvBangDiem.Columns["STT"].HeaderText = "STT";
                dgvBangDiem.Columns["STT"].FillWeight = 7;
            }
            if (dgvBangDiem.Columns.Contains("MASV"))
            {
                dgvBangDiem.Columns["MASV"].HeaderText = "Mã SV";
                dgvBangDiem.Columns["MASV"].FillWeight = 13;
            }
            if (dgvBangDiem.Columns.Contains("HO"))
            {
                dgvBangDiem.Columns["HO"].HeaderText = "Họ và đệm";
                dgvBangDiem.Columns["HO"].FillWeight = 22;
            }
            if (dgvBangDiem.Columns.Contains("TEN"))
            {
                dgvBangDiem.Columns["TEN"].HeaderText = "Tên";
                dgvBangDiem.Columns["TEN"].FillWeight = 12;
            }
            if (dgvBangDiem.Columns.Contains("NGAYSINH"))
            {
                dgvBangDiem.Columns["NGAYSINH"].HeaderText = "Ngày sinh";
                dgvBangDiem.Columns["NGAYSINH"].DefaultCellStyle.Format = "dd/MM/yyyy";
                dgvBangDiem.Columns["NGAYSINH"].FillWeight = 14;
            }
            if (dgvBangDiem.Columns.Contains("DIEM_SO_TEXT"))
            {
                dgvBangDiem.Columns["DIEM_SO_TEXT"].HeaderText = "Điểm số";
                dgvBangDiem.Columns["DIEM_SO_TEXT"].FillWeight = 12;
            }
            if (dgvBangDiem.Columns.Contains("DIEM_CHU"))
            {
                dgvBangDiem.Columns["DIEM_CHU"].HeaderText = "Điểm chữ";
                dgvBangDiem.Columns["DIEM_CHU"].FillWeight = 16;
            }
            if (dgvBangDiem.Columns.Contains("GHI_CHU"))
            {
                dgvBangDiem.Columns["GHI_CHU"].HeaderText = "Ghi chú / Kết quả";
                dgvBangDiem.Columns["GHI_CHU"].FillWeight = 16;
            }

            string[] hiddenCols = { "HOTEN", "NGAYTHI", "CO_DIEM", "DIEM", "TENLOP", "TENMH", "TRINHDO" };
            foreach (string col in hiddenCols)
            {
                if (dgvBangDiem.Columns.Contains(col))
                {
                    dgvBangDiem.Columns[col].Visible = false;
                }
            }
        }

        private void DgvBangDiem_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || !dgvBangDiem.Columns.Contains("CO_DIEM")) return;

            int coDiem = Convert.ToInt32(dgvBangDiem.Rows[e.RowIndex].Cells["CO_DIEM"].Value);
            string colName = dgvBangDiem.Columns[e.ColumnIndex].Name;
            if (colName == "DIEM_SO_TEXT" || colName == "GHI_CHU")
            {
                if (coDiem == 1)
                {
                    e.CellStyle.ForeColor = Color.FromArgb(22, 101, 52);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
                }
                else
                {
                    e.CellStyle.ForeColor = Color.FromArgb(180, 83, 9);
                    e.CellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
                }
            }
        }

        private void BtnInReport_Click(object sender, EventArgs e)
        {
            if (_currentBangDiem == null || _currentBangDiem.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu sinh viên trong lớp để in Bảng điểm!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _printRowIndex = 0;
            PrintDocument pd = new PrintDocument();
            pd.DocumentName = string.Format("BangDiem_{0}_{1}_Lan{2}", _selectedMaLop, _selectedMaMH, _selectedLanThi);
            pd.PrintPage += PrintDocument_PrintPage;

            using (PrintPreviewDialog preview = new PrintPreviewDialog())
            {
                preview.Document = pd;
                preview.Text = "Xem trước bản in - Mục 8: Bảng điểm thi hết môn (Mẫu Học viện PTIT)";
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

            using (Font fHeaderSmall = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (Font fHeaderSub = new Font("Segoe UI", 9F, FontStyle.Regular))
            using (Font fTitle = new Font("Segoe UI", 15F, FontStyle.Bold))
            using (Font fInfo = new Font("Segoe UI", 10F, FontStyle.Bold))
            using (Font fTableHeader = new Font("Segoe UI", 9.5F, FontStyle.Bold))
            using (Font fCell = new Font("Segoe UI", 9.5F, FontStyle.Regular))
            using (Pen penBorder = new Pen(Color.FromArgb(71, 85, 105), 1F))
            using (Brush brushHeader = new SolidBrush(Color.FromArgb(241, 245, 249)))
            {
                if (_printRowIndex == 0)
                {
                    g.DrawString("BỘ THÔNG TIN VÀ TRUYỀN THÔNG", fHeaderSub, Brushes.Black, left, y);
                    g.DrawString("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM", fHeaderSmall, Brushes.Black, left + 350, y);
                    y += 18;
                    g.DrawString("HỌC VIỆN CÔNG NGHỆ BƯU CHÍNH VIỄN THÔNG", fHeaderSmall, Brushes.DarkRed, left, y);
                    g.DrawString("Độc lập - Tự do - Hạnh phúc", fHeaderSmall, Brushes.Black, left + 400, y);
                    y += 38;

                    g.DrawString("BẢNG ĐIỂM THI HẾT MÔN", fTitle, Brushes.Black, left + 185, y);
                    y += 36;

                    g.DrawString(string.Format("Lớp: {0} ({1})", _selectedTenLop, _selectedMaLop), fInfo, Brushes.Black, left, y);
                    g.DrawString(string.Format("Lần thi: {0}", _selectedLanThi), fInfo, Brushes.Black, left + 450, y);
                    y += 24;

                    g.DrawString(string.Format("Môn học: {0} ({1})", _selectedTenMH, _selectedMaMH), fInfo, Brushes.Black, left, y);
                    g.DrawString(string.Format("Ngày in: {0:dd/MM/yyyy}", DateTime.Now), fInfo, Brushes.Black, left + 450, y);
                    y += 30;
                }

                int wStt = 45;
                int wMaSV = 90;
                int wHo = 155;
                int wTen = 80;
                int wDiemSo = 80;
                int wDiemChu = 115;
                int wGhiChu = width - (wStt + wMaSV + wHo + wTen + wDiemSo + wDiemChu);

                Rectangle rHeader = new Rectangle(left, y, width, 32);
                g.FillRectangle(brushHeader, rHeader);
                g.DrawRectangle(penBorder, rHeader);

                int x = left;
                g.DrawString("STT", fTableHeader, Brushes.Black, x + 6, y + 7);
                x += wStt; g.DrawLine(penBorder, x, y, x, y + 32);
                g.DrawString("Mã SV", fTableHeader, Brushes.Black, x + 6, y + 7);
                x += wMaSV; g.DrawLine(penBorder, x, y, x, y + 32);
                g.DrawString("Họ và đệm", fTableHeader, Brushes.Black, x + 6, y + 7);
                x += wHo; g.DrawLine(penBorder, x, y, x, y + 32);
                g.DrawString("Tên", fTableHeader, Brushes.Black, x + 6, y + 7);
                x += wTen; g.DrawLine(penBorder, x, y, x, y + 32);
                g.DrawString("Điểm số", fTableHeader, Brushes.Black, x + 6, y + 7);
                x += wDiemSo; g.DrawLine(penBorder, x, y, x, y + 32);
                g.DrawString("Điểm chữ", fTableHeader, Brushes.Black, x + 6, y + 7);
                x += wDiemChu; g.DrawLine(penBorder, x, y, x, y + 32);
                g.DrawString("Ghi chú", fTableHeader, Brushes.Black, x + 6, y + 7);
                y += 32;

                while (_printRowIndex < _currentBangDiem.Rows.Count)
                {
                    int rowH = 30;
                    if (y + rowH + 100 > e.MarginBounds.Bottom)
                    {
                        e.HasMorePages = true;
                        return;
                    }

                    DataRow r = _currentBangDiem.Rows[_printRowIndex];
                    g.DrawRectangle(penBorder, left, y, width, rowH);

                    int cx = left;
                    g.DrawString(Convert.ToString(r["STT"]), fCell, Brushes.Black, cx + 8, y + 6);
                    cx += wStt; g.DrawLine(penBorder, cx, y, cx, y + rowH);

                    g.DrawString(Convert.ToString(r["MASV"]), fCell, Brushes.Black, cx + 6, y + 6);
                    cx += wMaSV; g.DrawLine(penBorder, cx, y, cx, y + rowH);

                    g.DrawString(Convert.ToString(r["HO"]), fCell, Brushes.Black, cx + 6, y + 6);
                    cx += wHo; g.DrawLine(penBorder, cx, y, cx, y + rowH);

                    g.DrawString(Convert.ToString(r["TEN"]), fCell, Brushes.Black, cx + 6, y + 6);
                    cx += wTen; g.DrawLine(penBorder, cx, y, cx, y + rowH);

                    g.DrawString(Convert.ToString(r["DIEM_SO_TEXT"]), fTableHeader, Brushes.Black, cx + 8, y + 6);
                    cx += wDiemSo; g.DrawLine(penBorder, cx, y, cx, y + rowH);

                    g.DrawString(Convert.ToString(r["DIEM_CHU"]), fCell, Brushes.Black, cx + 6, y + 6);
                    cx += wDiemChu; g.DrawLine(penBorder, cx, y, cx, y + rowH);

                    g.DrawString(Convert.ToString(r["GHI_CHU"]), fCell, Brushes.Black, cx + 6, y + 6);

                    y += rowH;
                    _printRowIndex++;
                }

                y += 18;
                g.DrawString(lblSummaryMetrics.Text, fHeaderSmall, Brushes.Black, left, y);
                y += 34;

                g.DrawString("PHÒNG KHẢO THÍ & ĐBCLGD", fTableHeader, Brushes.Black, left + 40, y);
                g.DrawString("GIẢNG VIÊN LẬP BẢNG ĐIỂM", fTableHeader, Brushes.Black, left + 420, y);
                y += 55;
                if (SessionManager.CurrentUser != null)
                {
                    g.DrawString(SessionManager.CurrentUser.HoTen, fTableHeader, Brushes.Black, left + 435, y);
                }

                e.HasMorePages = false;
            }
        }

        private void BtnXuatHtml_Click(object sender, EventArgs e)
        {
            if (_currentBangDiem == null || _currentBangDiem.Rows.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu Bảng điểm để xuất!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Xuất Bảng điểm môn học (HTML)";
                sfd.Filter = "HTML Report (*.html)|*.html";
                sfd.FileName = string.Format("BangDiem_{0}_{1}_Lan{2}.html", _selectedMaLop, _selectedMaMH, _selectedLanThi);

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><title>Bảng điểm thi hết môn - PTIT</title>");
                    sb.AppendLine("<style>body{font-family:'Segoe UI',Arial,sans-serif;margin:30px;} h2,h3{text-align:center;margin:6px 0;} table{width:100%;border-collapse:collapse;margin-top:16px;} th,td{border:1px solid #334155;padding:8px;text-align:left;} th{background:#f1f5f9;}</style></head><body>");
                    sb.AppendLine("<h3>HỌC VIỆN CÔNG NGHỆ BƯU CHÍNH VIỄN THÔNG (PTIT)</h3>");
                    sb.AppendLine("<h2>BẢNG ĐIỂM THI HẾT MÔN</h2>");
                    sb.AppendFormat("<p style='text-align:center;'><b>Lớp:</b> {0} ({1}) &nbsp;&nbsp;|&nbsp;&nbsp; <b>Môn học:</b> {2} ({3}) &nbsp;&nbsp;|&nbsp;&nbsp; <b>Lần thi:</b> {4}</p>",
                        _selectedTenLop, _selectedMaLop, _selectedTenMH, _selectedMaMH, _selectedLanThi);
                    sb.AppendLine("<table><thead><tr><th>STT</th><th>Mã SV</th><th>Họ và đệm</th><th>Tên</th><th>Ngày sinh</th><th>Điểm số</th><th>Điểm chữ</th><th>Ghi chú</th></tr></thead><tbody>");

                    foreach (DataRow r in _currentBangDiem.Rows)
                    {
                        DateTime ns = Convert.ToDateTime(r["NGAYSINH"]);
                        sb.AppendFormat("<tr><td>{0}</td><td>{1}</td><td>{2}</td><td>{3}</td><td>{4:dd/MM/yyyy}</td><td><b>{5}</b></td><td>{6}</td><td>{7}</td></tr>",
                            r["STT"], r["MASV"], r["HO"], r["TEN"], ns, r["DIEM_SO_TEXT"], r["DIEM_CHU"], r["GHI_CHU"]);
                    }
                    sb.AppendLine("</tbody></table>");
                    sb.AppendFormat("<p style='margin-top:16px;'><b>{0}</b></p>", lblSummaryMetrics.Text);
                    sb.AppendLine("</body></html>");
                    File.WriteAllText(sfd.FileName, sb.ToString(), Encoding.UTF8);
                    MessageBox.Show("Đã xuất Bảng điểm môn học thành công:\n" + sfd.FileName, "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}

