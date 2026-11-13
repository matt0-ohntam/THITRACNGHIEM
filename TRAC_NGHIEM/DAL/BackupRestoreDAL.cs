using System;
using System.Data;
using System.Data.SqlClient;

namespace TRAC_NGHIEM.DAL
{
    /// <summary>
    /// Lớp truy cập dữ liệu Sao lưu (Backup), Phục hồi (Restore) và Thống kê Tổng quan Database TRAC_NGHIEM.
    /// Sử dụng cơ chế Stored Procedure ký chứng thư số (Certificate Signing) tại master và TRAC_NGHIEM
    /// để tài khoản SQL Login thuộc role_GiaoVien thực hiện Backup/Restore an toàn mà không cần tài khoản sa.
    /// </summary>
    public class BackupRestoreDAL
    {
        public const string TargetDatabaseName = "TRAC_NGHIEM";

        /// <summary>
        /// Kiểm tra quyền Backup/Restore của tài khoản SQL Server hiện tại
        /// </summary>
        public bool CheckSqlServerBackupPermission()
        {
            const string sql = @"
                SELECT CASE
                    WHEN IS_SRVROLEMEMBER('sysadmin') = 1
                      OR IS_SRVROLEMEMBER('dbcreator') = 1
                      OR IS_MEMBER('db_owner') = 1
                      OR IS_MEMBER('db_backupoperator') = 1
                      OR IS_MEMBER('role_GiaoVien') = 1
                      OR HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'BACKUP DATABASE') = 1
                    THEN 1 ELSE 0
                END;";

            object result = DatabaseConnection.ExecuteScalar(sql, CommandType.Text);
            return result != null && Convert.ToInt32(result) == 1;
        }

        /// <summary>
        /// Thực hiện Full Backup database TRAC_NGHIEM ra file .bak (sử dụng SqlParameter cho đường dẫn file)
        /// </summary>
        public void BackupDatabase(string backupFilePath, bool overwriteExisting)
        {
            const string sqlOverwrite = @"
                BACKUP DATABASE [TRAC_NGHIEM]
                TO DISK = @BackupPath
                WITH INIT,
                     NAME = N'TRAC_NGHIEM-Full Database Backup',
                     DESCRIPTION = N'Sao lưu từ phần mềm Quản lý Thi Trắc Nghiệm PTIT';";

            const string sqlAppend = @"
                BACKUP DATABASE [TRAC_NGHIEM]
                TO DISK = @BackupPath
                WITH NOINIT,
                     NAME = N'TRAC_NGHIEM-Full Database Backup',
                     DESCRIPTION = N'Sao lưu từ phần mềm Quản lý Thi Trắc Nghiệm PTIT';";

            string sqlToRun = overwriteExisting ? sqlOverwrite : sqlAppend;

            using (SqlConnection conn = DatabaseConnection.GetConnection())
            {
                using (SqlCommand cmd = new SqlCommand(sqlToRun, conn))
                {
                    cmd.CommandTimeout = 180;
                    cmd.Parameters.Add(new SqlParameter("@BackupPath", SqlDbType.NVarChar, 500) { Value = backupFilePath });
                    conn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Kiểm tra file .bak hợp lệ bằng RESTORE VERIFYONLY & RESTORE HEADERONLY
        /// thông qua Stored Procedure ký chứng thư số master.dbo.sp_VerifyBackup_TracNghiem
        /// </summary>
        public DataTable VerifyAndReadBackupHeader(string backupFilePath)
        {
            using (SqlConnection masterConn = new SqlConnection(DatabaseConnection.GetMasterConnectionString()))
            {
                masterConn.Open();
                DataTable headerTable = new DataTable();

                using (SqlCommand cmd = new SqlCommand("dbo.sp_VerifyBackup_TracNghiem", masterConn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandTimeout = 90;
                    cmd.Parameters.Add(new SqlParameter("@BackupPath", SqlDbType.NVarChar, 500) { Value = backupFilePath });

                    using (SqlDataAdapter adapter = new SqlDataAdapter(cmd))
                    {
                        adapter.Fill(headerTable);
                    }
                }

                return headerTable;
            }
        }

        /// <summary>
        /// Đếm số kết nối đang hoạt động vào database TRAC_NGHIEM
        /// </summary>
        public int GetActiveConnectionsCount()
        {
            try
            {
                using (SqlConnection masterConn = new SqlConnection(DatabaseConnection.GetMasterConnectionString()))
                {
                    masterConn.Open();
                    using (SqlCommand cmd = new SqlCommand("dbo.sp_DemKetNoi_TracNghiem", masterConn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandTimeout = 15;
                        object res = cmd.ExecuteScalar();
                        return res != null && res != DBNull.Value ? Convert.ToInt32(res) : 0;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Thực hiện Restore an toàn database TRAC_NGHIEM từ file .bak
        /// thông qua Stored Procedure ký chứng thư số master.dbo.sp_RestoreDatabase_TracNghiem
        /// </summary>
        public void RestoreDatabase(string backupFilePath)
        {
            SqlConnection.ClearAllPools();

            using (SqlConnection masterConn = new SqlConnection(DatabaseConnection.GetMasterConnectionString()))
            {
                masterConn.Open();

                using (SqlCommand restoreCmd = new SqlCommand("dbo.sp_RestoreDatabase_TracNghiem", masterConn))
                {
                    restoreCmd.CommandType = CommandType.StoredProcedure;
                    restoreCmd.CommandTimeout = 300;
                    restoreCmd.Parameters.Add(new SqlParameter("@BackupPath", SqlDbType.NVarChar, 500) { Value = backupFilePath });
                    restoreCmd.ExecuteNonQuery();
                }
            }

            SqlConnection.ClearAllPools();
        }

        /// <summary>
        /// Lấy thống kê Tổng quan hệ thống từ Stored Procedure dbo.sp_LayThongKeTongQuan
        /// (Kết hợp từ GIAOVIEN, sys.server_principals, sys.database_principals và role_GiaoVien)
        /// </summary>
        public DataTable GetDatabaseHealthSummary()
        {
            return DatabaseConnection.ExecuteQuery("dbo.sp_LayThongKeTongQuan", CommandType.StoredProcedure);
        }
    }
}
