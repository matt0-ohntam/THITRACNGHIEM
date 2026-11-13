using System;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using TRAC_NGHIEM.BLL;
using TRAC_NGHIEM.Helpers;

namespace TRAC_NGHIEM.Forms
{
    /// <summary>
    /// Form Sao lưu (Backup) và Phục hồi (Restore) Cơ sở dữ liệu TRAC_NGHIEM (Giai đoạn 7).
    /// Đáp ứng đầy đủ yêu cầu mục VI.8 & VII:
    /// - Sao lưu (Full Backup) database TRAC_NGHIEM ra file .bak (WITH INIT / WITH NOINIT).
    /// - Kiểm tra tính hợp lệ của file .bak bằng RESTORE VERIFYONLY & RESTORE HEADERONLY.
    /// - Phục hồi (Restore) an toàn: cảnh báo xác nhận, ngắt kết nối đang chiếm giữ
    ///   (SINGLE_USER WITH ROLLBACK IMMEDIATE), thực hiện RESTORE WITH REPLACE, RECOVERY,
    ///   đưa về MULTI_USER, đồng bộ lại SQL Login/User và kiểm tra dữ liệu ngay sau khi phục hồi.
    /// </summary>
    public class FrmBackupRestore : Form
    {
        private readonly BackupRestoreBLL _bll = new BackupRestoreBLL();
        private bool _isBusy = false;
        private string _defaultBackupDir;

        private Panel pnlHeaderStatus;
        private Label lblDbInfo;
        private Label lblPermissionStatus;
        private Label lblActiveConnections;
        private Button btnRefreshStatus;

        private Panel pnlTopActions;
        private GroupBox grpBackup;
        private Label lblDatabaseTarget;
        private TextBox txtDatabaseName;
        private Label lblBackupPath;
        private TextBox txtBackupPath;
        private Button btnBrowseBackup;
        private CheckBox chkOverwrite;
        private Button btnExecuteBackup;
        private Label lblBackupStatus;

        private GroupBox grpRestore;
        private Label lblRestorePath;
        private TextBox txtRestorePath;
        private Button btnBrowseRestore;
        private Button btnVerifyBackup;
        private Button btnExecuteRestore;
        private Label lblRestoreVerifyResult;
        private Label lblRestoreWarning;

        private SplitContainer splitBottom;
        private GroupBox grpBackupFiles;
        private DataGridView dgvBackupFiles;
        private GroupBox grpDataHealth;
        private DataGridView dgvDataHealth;

        public FrmBackupRestore()
        {
            SessionManager.EnsureGiaoVienPermission("Sao lưu và Phục hồi cơ sở dữ liệu (Backup & Restore)");
            InitBackupDirectory();
            InitializeComponent();
            RefreshAllData();
        }

        private void InitBackupDirectory()
        {
            try
            {
                string candidate = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\backup"));
                if (!Directory.Exists(candidate))
                {
                    Directory.CreateDirectory(candidate);
                }
                _defaultBackupDir = candidate;
            }
            catch
            {
                _defaultBackupDir = AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(241, 245, 249);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.Padding = new Padding(4);

            // 1. Thanh trạng thái kết nối & quyền SQL Server
            pnlHeaderStatus = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.White,
                Padding = new Padding(16, 10, 16, 10)
            };

            lblDbInfo = new Label
            {
                Text = "CSDL: TRAC_NGHIEM",
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Location = new Point(16, 10),
                AutoSize = true
            };

            lblPermissionStatus = new Label
            {
                Text = "Quyền SQL Server: Đang kiểm tra...",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 163, 74),
                Location = new Point(16, 34),
                AutoSize = true
            };

            lblActiveConnections = new Label
            {
                Text = "Số phiên kết nối khác đang mở: 0",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(71, 85, 105),
                Location = new Point(460, 34),
                AutoSize = true
            };

            btnRefreshStatus = new Button
            {
                Text = "Làm mới trạng thái",
                Size = new Size(155, 36),
                Location = new Point(820, 14),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnRefreshStatus.FlatAppearance.BorderSize = 0;
            btnRefreshStatus.Click += (s, e) => RefreshAllData();

            pnlHeaderStatus.Controls.AddRange(new Control[]
            {
                lblDbInfo, lblPermissionStatus, lblActiveConnections, btnRefreshStatus
            });

            // 2. Khu vực thao tác Backup (Trái) & Restore (Phải)
            pnlTopActions = new Panel
            {
                Dock = DockStyle.Top,
                Height = 235,
                Padding = new Padding(0, 8, 0, 8)
            };

            TableLayoutPanel tblTop = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1
            };
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48F));
            tblTop.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52F));
            tblTop.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // 2.1. GroupBox BACKUP
            grpBackup = new GroupBox
            {
                Text = "1. SAO LƯU CƠ SỞ DỮ LIỆU (FULL DATABASE BACKUP)",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 101, 52),
                Margin = new Padding(0, 0, 6, 0),
                Padding = new Padding(12)
            };

            lblDatabaseTarget = new Label
            {
                Text = "Database cần sao lưu:",
                Location = new Point(14, 28),
                Size = new Size(150, 22),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txtDatabaseName = new TextBox
            {
                Text = "TRAC_NGHIEM",
                Location = new Point(168, 25),
                Size = new Size(180, 26),
                ReadOnly = true,
                BackColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };

            lblBackupPath = new Label
            {
                Text = "Đường dẫn lưu file sao lưu (.bak):",
                Location = new Point(14, 58),
                Size = new Size(280, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txtBackupPath = new TextBox
            {
                Location = new Point(14, 80),
                Size = new Size(330, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Text = GenerateDefaultBackupFilePath(),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };

            btnBrowseBackup = new Button
            {
                Text = "Chọn nơi lưu...",
                Location = new Point(352, 78),
                Size = new Size(105, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBrowseBackup.FlatAppearance.BorderSize = 0;
            btnBrowseBackup.Click += BtnBrowseBackup_Click;

            chkOverwrite = new CheckBox
            {
                Text = "Ghi đè file cũ nếu trùng tên (WITH INIT)",
                Checked = true,
                Location = new Point(14, 114),
                Size = new Size(340, 24),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            btnExecuteBackup = new Button
            {
                Text = "THỰC HIỆN BACKUP (.BAK)",
                Location = new Point(14, 144),
                Size = new Size(235, 38),
                BackColor = Color.FromArgb(22, 163, 74),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExecuteBackup.FlatAppearance.BorderSize = 0;
            btnExecuteBackup.Click += BtnExecuteBackup_Click;

            lblBackupStatus = new Label
            {
                Text = "Sẵn sàng sao lưu CSDL TRAC_NGHIEM.",
                Location = new Point(14, 188),
                Size = new Size(440, 22),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Italic),
                ForeColor = Color.FromArgb(22, 101, 52)
            };

            grpBackup.Controls.AddRange(new Control[]
            {
                lblDatabaseTarget, txtDatabaseName, lblBackupPath, txtBackupPath,
                btnBrowseBackup, chkOverwrite, btnExecuteBackup, lblBackupStatus
            });

            // 2.2. GroupBox RESTORE
            grpRestore = new GroupBox
            {
                Text = "2. PHỤC HỒI CƠ SỞ DỮ LIỆU (RESTORE DATABASE AN TOÀN)",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(185, 28, 28),
                Margin = new Padding(6, 0, 0, 0),
                Padding = new Padding(12)
            };

            lblRestorePath = new Label
            {
                Text = "Chọn file sao lưu (.bak) cần phục hồi (hoặc bấm chọn ở bảng bên dưới):",
                Location = new Point(14, 28),
                Size = new Size(460, 20),
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(51, 65, 85)
            };

            txtRestorePath = new TextBox
            {
                Location = new Point(14, 52),
                Size = new Size(370, 26),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };

            btnBrowseRestore = new Button
            {
                Text = "Chọn file .bak...",
                Location = new Point(392, 50),
                Size = new Size(105, 30),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(100, 116, 139),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBrowseRestore.FlatAppearance.BorderSize = 0;
            btnBrowseRestore.Click += BtnBrowseRestore_Click;

            lblRestoreVerifyResult = new Label
            {
                Text = "Trạng thái file: Chưa kiểm tra (Nhấn [Kiểm tra file .bak] trước khi Restore)",
                Location = new Point(14, 86),
                Size = new Size(480, 24),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 8.8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(37, 99, 235)
            };

            lblRestoreWarning = new Label
            {
                Text = "Lưu ý: Khi Restore, hệ thống tự động đóng các kết nối đang mở (SINGLE_USER WITH ROLLBACK IMMEDIATE), khôi phục dữ liệu và đưa về MULTI_USER.",
                Location = new Point(14, 110),
                Size = new Size(480, 32),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic),
                ForeColor = Color.FromArgb(100, 116, 139)
            };

            btnVerifyBackup = new Button
            {
                Text = "1. Kiểm tra file .bak",
                Location = new Point(14, 148),
                Size = new Size(185, 38),
                BackColor = Color.FromArgb(37, 99, 235),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnVerifyBackup.FlatAppearance.BorderSize = 0;
            btnVerifyBackup.Click += BtnVerifyBackup_Click;

            btnExecuteRestore = new Button
            {
                Text = "2. THỰC HIỆN RESTORE",
                Location = new Point(212, 148),
                Size = new Size(235, 38),
                BackColor = Color.FromArgb(220, 38, 38),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnExecuteRestore.FlatAppearance.BorderSize = 0;
            btnExecuteRestore.Click += BtnExecuteRestore_Click;

            grpRestore.Controls.AddRange(new Control[]
            {
                lblRestorePath, txtRestorePath, btnBrowseRestore,
                lblRestoreVerifyResult, lblRestoreWarning, btnVerifyBackup, btnExecuteRestore
            });

            tblTop.Controls.Add(grpBackup, 0, 0);
            tblTop.Controls.Add(grpRestore, 1, 0);
            pnlTopActions.Controls.Add(tblTop);

            // 3. Khu vực bên dưới: Danh sách file .bak sẵn có & Thống kê kiểm tra dữ liệu sau phục hồi
            splitBottom = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 470,
                SplitterWidth = 8
            };

            grpBackupFiles = new GroupBox
            {
                Text = "Danh sách các bản sao lưu (.bak) trong thư mục backup/ (Chọn dòng để lấy đường dẫn Restore)",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10)
            };

            dgvBackupFiles = CreateReadOnlyGrid();
            dgvBackupFiles.SelectionChanged += DgvBackupFiles_SelectionChanged;
            grpBackupFiles.Controls.Add(dgvBackupFiles);
            splitBottom.Panel1.Controls.Add(grpBackupFiles);

            grpDataHealth = new GroupBox
            {
                Text = "Kiểm tra số liệu dữ liệu nền trong TRAC_NGHIEM (Đối chiếu trước & sau khi Restore)",
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 41, 59),
                Padding = new Padding(10)
            };

            dgvDataHealth = CreateReadOnlyGrid();
            grpDataHealth.Controls.Add(dgvDataHealth);
            splitBottom.Panel2.Controls.Add(grpDataHealth);

            this.Controls.Add(splitBottom);
            this.Controls.Add(pnlTopActions);
            this.Controls.Add(pnlHeaderStatus);
        }

        private DataGridView CreateReadOnlyGrid()
        {
            DataGridView dgv = new DataGridView
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
                Font = new Font("Segoe UI", 9F, FontStyle.Regular)
            };
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            dgv.RowTemplate.Height = 28;
            return dgv;
        }

        private string GenerateDefaultBackupFilePath()
        {
            return Path.Combine(_defaultBackupDir, string.Format("TRAC_NGHIEM_{0:yyyyMMdd_HHmm}.bak", DateTime.Now));
        }

        public void RefreshAllData()
        {
            var user = SessionManager.CurrentUser;
            lblDbInfo.Text = string.Format("CSDL: TRAC_NGHIEM  |  SQL Login: {0} ({1})  |  Role: {2}",
                user != null ? user.TenDangNhap : "N/A",
                user != null ? user.HoTen : "N/A",
                user != null ? user.DatabaseRole : "N/A");

            bool hasPerm = _bll.KiemTraQuyenSqlServer();
            lblPermissionStatus.Text = hasPerm
                ? "Quyền SQL Server: Đủ quyền thực hiện BACKUP & RESTORE (Qua Role & Certificate Signing)"
                : "Quyền SQL Server: Không đủ quyền BACKUP / RESTORE";
            lblPermissionStatus.ForeColor = hasPerm ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);

            int activeConns = _bll.DemKetNoiDangHoatDong();
            lblActiveConnections.Text = string.Format("Số kết nối khác đang mở vào TRAC_NGHIEM: {0}", activeConns);

            LoadBackupFileList();
            LoadDatabaseHealthGrid();
        }

        private void LoadBackupFileList()
        {
            DataTable dtFiles = new DataTable();
            dtFiles.Columns.Add("TEN_FILE", typeof(string));
            dtFiles.Columns.Add("KICH_THUOC_KB", typeof(long));
            dtFiles.Columns.Add("NGAY_CAP_NHAT", typeof(string));
            dtFiles.Columns.Add("DUONG_DAN", typeof(string));

            if (Directory.Exists(_defaultBackupDir))
            {
                FileInfo[] files = new DirectoryInfo(_defaultBackupDir).GetFiles("*.bak");
                Array.Sort(files, (a, b) => b.LastWriteTime.CompareTo(a.LastWriteTime));
                foreach (FileInfo fi in files)
                {
                    dtFiles.Rows.Add(
                        fi.Name,
                        fi.Length / 1024,
                        fi.LastWriteTime.ToString("dd/MM/yyyy HH:mm:ss"),
                        fi.FullName);
                }
            }

            dgvBackupFiles.DataSource = dtFiles;
            if (dgvBackupFiles.Columns.Contains("TEN_FILE"))
                dgvBackupFiles.Columns["TEN_FILE"].HeaderText = "Tên file (.bak)";
            if (dgvBackupFiles.Columns.Contains("KICH_THUOC_KB"))
                dgvBackupFiles.Columns["KICH_THUOC_KB"].HeaderText = "Dung lượng (KB)";
            if (dgvBackupFiles.Columns.Contains("NGAY_CAP_NHAT"))
                dgvBackupFiles.Columns["NGAY_CAP_NHAT"].HeaderText = "Thời gian sao lưu";
            if (dgvBackupFiles.Columns.Contains("DUONG_DAN"))
                dgvBackupFiles.Columns["DUONG_DAN"].Visible = false;
        }

        private void LoadDatabaseHealthGrid()
        {
            DataTable dtHealth = _bll.LayThongKeDuLieuHienTai();
            dgvDataHealth.DataSource = dtHealth;
            if (dgvDataHealth.Columns.Contains("THU_TU"))
                dgvDataHealth.Columns["THU_TU"].Visible = false;
            if (dgvDataHealth.Columns.Contains("CHI_TIEU"))
                dgvDataHealth.Columns["CHI_TIEU"].HeaderText = "Hạng mục dữ liệu nền & tài khoản";
            if (dgvDataHealth.Columns.Contains("SO_LUONG"))
                dgvDataHealth.Columns["SO_LUONG"].HeaderText = "Số lượng hiện tại";
        }

        private void DgvBackupFiles_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvBackupFiles.CurrentRow != null && dgvBackupFiles.CurrentRow.Index >= 0)
            {
                string fullPath = Convert.ToString(dgvBackupFiles.CurrentRow.Cells["DUONG_DAN"].Value);
                if (!string.IsNullOrWhiteSpace(fullPath))
                {
                    txtRestorePath.Text = fullPath;
                }
            }
        }

        private void BtnBrowseBackup_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = "Chọn vị trí lưu file sao lưu (.bak) cho CSDL TRAC_NGHIEM";
                sfd.Filter = "SQL Server Backup File (*.bak)|*.bak";
                sfd.InitialDirectory = _defaultBackupDir;
                sfd.FileName = string.Format("TRAC_NGHIEM_{0:yyyyMMdd_HHmm}.bak", DateTime.Now);
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    txtBackupPath.Text = sfd.FileName;
                }
            }
        }

        private void BtnBrowseRestore_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Chọn file sao lưu (.bak) để phục hồi CSDL TRAC_NGHIEM";
                ofd.Filter = "SQL Server Backup File (*.bak)|*.bak";
                ofd.InitialDirectory = _defaultBackupDir;
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    txtRestorePath.Text = ofd.FileName;
                }
            }
        }

        private void BtnExecuteBackup_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            try
            {
                _isBusy = true;
                btnExecuteBackup.Enabled = false;
                lblBackupStatus.Text = "Đang thực hiện sao lưu cơ sở dữ liệu TRAC_NGHIEM...";
                lblBackupStatus.ForeColor = Color.FromArgb(37, 99, 235);

                string thongBao;
                bool ok = _bll.ThucHienBackup(txtBackupPath.Text, chkOverwrite.Checked, out thongBao);

                lblBackupStatus.Text = ok
                    ? "Sao lưu thành công lúc " + DateTime.Now.ToString("HH:mm:ss dd/MM/yyyy")
                    : "Sao lưu thất bại: " + thongBao;
                lblBackupStatus.ForeColor = ok ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);

                MessageBox.Show(thongBao, ok ? "Backup Database thành công" : "Không thể Backup Database",
                    MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

                if (ok)
                {
                    RefreshAllData();
                }
            }
            finally
            {
                _isBusy = false;
                btnExecuteBackup.Enabled = true;
            }
        }

        private void BtnVerifyBackup_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            string thongTin;
            bool ok = _bll.KiemTraFileBackupHopLe(txtRestorePath.Text, out thongTin);
            lblRestoreVerifyResult.Text = thongTin;
            lblRestoreVerifyResult.ForeColor = ok ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);

            MessageBox.Show(thongTin, ok ? "File sao lưu (.bak) hợp lệ" : "File sao lưu không hợp lệ",
                MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }

        private void BtnExecuteRestore_Click(object sender, EventArgs e)
        {
            if (_isBusy) return;

            string filePath = txtRestorePath.Text.Trim();
            if (string.IsNullOrWhiteSpace(filePath))
            {
                MessageBox.Show("Vui lòng chọn file sao lưu (.bak) trước khi thực hiện Restore!",
                    "Thiếu đường dẫn file", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int activeConns = _bll.DemKetNoiDangHoatDong();
            DialogResult confirm = MessageBox.Show(
                string.Format("CẢNH BÁO QUAN TRỌNG TRƯỚC KHI PHỤC HỒI (RESTORE):\n\n" +
                              "• File sao lưu: {0}\n" +
                              "• Số kết nối khác đang mở vào TRAC_NGHIEM: {1}\n\n" +
                              "Thao tác Restore sẽ GHI ĐÈ toàn bộ dữ liệu hiện tại của database [TRAC_NGHIEM] bằng dữ liệu trong file sao lưu.\n" +
                              "Bạn có chắc chắn muốn thực hiện Phục hồi (Restore) ngay bây giờ không?",
                    filePath, activeConns),
                "Xác nhận Phục hồi Cơ sở dữ liệu (Restore Database)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            try
            {
                _isBusy = true;
                btnExecuteRestore.Enabled = false;
                btnVerifyBackup.Enabled = false;
                lblRestoreVerifyResult.Text = "Đang thực hiện Restore CSDL TRAC_NGHIEM (Vui lòng đợi)...";
                lblRestoreVerifyResult.ForeColor = Color.FromArgb(217, 119, 6);

                string thongBao;
                bool ok = _bll.ThucHienRestore(filePath, out thongBao);

                lblRestoreVerifyResult.Text = ok
                    ? "Đã phục hồi (Restore) và kiểm tra kết nối lại CSDL thành công!"
                    : "Restore thất bại: " + thongBao;
                lblRestoreVerifyResult.ForeColor = ok ? Color.FromArgb(22, 163, 74) : Color.FromArgb(220, 38, 38);

                MessageBox.Show(thongBao, ok ? "Restore Database thành công" : "Lỗi khi Restore Database",
                    MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Error);

                if (ok)
                {
                    RefreshAllData();
                }
            }
            finally
            {
                _isBusy = false;
                btnExecuteRestore.Enabled = true;
                btnVerifyBackup.Enabled = true;
            }
        }
    }
}

