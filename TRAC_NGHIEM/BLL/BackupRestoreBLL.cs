using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using TRAC_NGHIEM.DAL;
using TRAC_NGHIEM.Helpers;

namespace TRAC_NGHIEM.BLL
{
    /// <summary>
    /// Lớp xử lý nghiệp vụ Sao lưu (Backup) và Phục hồi (Restore) Database TRAC_NGHIEM
    /// </summary>
    public class BackupRestoreBLL
    {
        private readonly BackupRestoreDAL _dal = new BackupRestoreDAL();

        public bool KiemTraQuyenSqlServer()
        {
            return _dal.CheckSqlServerBackupPermission();
        }

        public int DemKetNoiDangHoatDong()
        {
            return _dal.GetActiveConnectionsCount();
        }

        public DataTable LayThongKeDuLieuHienTai()
        {
            return _dal.GetDatabaseHealthSummary();
        }

        public bool ThucHienBackup(string filePath, bool ghiDeFileCu, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Sao lưu cơ sở dữ liệu (Backup)");

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    thongBao = "Vui lòng chọn đường dẫn lưu file sao lưu (.bak)!";
                    return false;
                }

                string fullPath = Path.GetFullPath(filePath.Trim());
                string directory = Path.GetDirectoryName(fullPath);

                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                {
                    thongBao = string.Format("Thư mục lưu file không tồn tại: [{0}]!", directory);
                    return false;
                }

                if (!fullPath.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                {
                    fullPath += ".bak";
                }

                if (!_dal.CheckSqlServerBackupPermission())
                {
                    thongBao = "Tài khoản kết nối SQL Server hiện tại không có quyền thực hiện BACKUP DATABASE (cần quyền sysadmin, db_owner hoặc db_backupoperator)!";
                    return false;
                }

                _dal.BackupDatabase(fullPath, ghiDeFileCu);
                thongBao = string.Format("Sao lưu (Backup) cơ sở dữ liệu [TRAC_NGHIEM] thành công!\nĐường dẫn file: {0}", fullPath);
                return true;
            }
            catch (SqlException sqlEx)
            {
                thongBao = "Lỗi SQL Server khi Backup: " + DatabaseConnection.FormatSqlExceptionMessage(sqlEx);
                return false;
            }
            catch (Exception ex)
            {
                thongBao = "Lỗi khi thực hiện Backup: " + ex.Message;
                return false;
            }
        }

        public bool KiemTraFileBackupHopLe(string filePath, out string thongTinBanSaoLuu)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Kiểm tra file Backup");

                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath.Trim()))
                {
                    thongTinBanSaoLuu = "File sao lưu (.bak) không tồn tại trên ổ đĩa!";
                    return false;
                }

                DataTable header = _dal.VerifyAndReadBackupHeader(filePath.Trim());
                if (header != null && header.Rows.Count > 0)
                {
                    DataRow r = header.Rows[0];
                    string dbName = r.Table.Columns.Contains("DatabaseName") ? Convert.ToString(r["DatabaseName"]) : "TRAC_NGHIEM";
                    string finishDate = r.Table.Columns.Contains("BackupFinishDate") ? Convert.ToString(r["BackupFinishDate"]) : "N/A";
                    string serverName = r.Table.Columns.Contains("ServerName") ? Convert.ToString(r["ServerName"]) : "N/A";

                    thongTinBanSaoLuu = string.Format("File hợp lệ | DB: {0} | Máy chủ: {1} | Ngày sao lưu: {2}",
                        dbName, serverName, finishDate);
                    return true;
                }

                thongTinBanSaoLuu = "File sao lưu hợp lệ (Đã xác thực bằng RESTORE VERIFYONLY).";
                return true;
            }
            catch (SqlException sqlEx)
            {
                thongTinBanSaoLuu = "File sao lưu (.bak) KHÔNG hợp lệ hoặc bị hỏng: " + sqlEx.Message;
                return false;
            }
            catch (Exception ex)
            {
                thongTinBanSaoLuu = "Không thể đọc file sao lưu: " + ex.Message;
                return false;
            }
        }

        public bool ThucHienRestore(string filePath, out string thongBao)
        {
            try
            {
                SessionManager.EnsureGiaoVienPermission("Phục hồi cơ sở dữ liệu (Restore)");

                string info;
                if (!KiemTraFileBackupHopLe(filePath, out info))
                {
                    thongBao = info;
                    return false;
                }

                if (!_dal.CheckSqlServerBackupPermission())
                {
                    thongBao = "Tài khoản kết nối SQL Server hiện tại không có quyền RESTORE DATABASE (cần quyền sysadmin hoặc dbcreator)!";
                    return false;
                }

                _dal.RestoreDatabase(filePath.Trim());

                // Kiểm tra kết nối lại ngay sau khi Restore
                string testErr;
                if (!DatabaseConnection.TestConnection(out testErr))
                {
                    thongBao = "Đã chạy lệnh Restore nhưng gặp lỗi khi kết nối lại database: " + testErr;
                    return false;
                }

                thongBao = "Phục hồi (Restore) cơ sở dữ liệu [TRAC_NGHIEM] thành công và đã kết nối lại an toàn!";
                return true;
            }
            catch (SqlException sqlEx)
            {
                thongBao = "Lỗi SQL Server khi Restore: " + DatabaseConnection.FormatSqlExceptionMessage(sqlEx);
                return false;
            }
            catch (Exception ex)
            {
                thongBao = "Lỗi khi thực hiện Restore: " + ex.Message;
                return false;
            }
        }
    }
}

