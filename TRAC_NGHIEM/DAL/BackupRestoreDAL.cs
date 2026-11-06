using System;
using System.Data;
using System.Data.SqlClient;

namespace TRAC_NGHIEM.DAL
{
    /// <summary>
    /// Lớp truy cập dữ liệu Sao lưu (Backup) và Phục hồi (Restore) Database TRAC_NGHIEM
    /// Kết nối thông qua database 'master' để tránh khóa kết nối khi Restore.
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
                    THEN 1 ELSE 0
                END;";

            object result = DatabaseConnection.ExecuteScalar(sql, CommandType.Text);
            return result != null && Convert.ToInt32(result) == 1;
        }

        /// <summary>
        /// Thực hiện Full Backup database TRAC_NGHIEM ra file .bak
        /// </summary>
        public void BackupDatabase(string backupFilePath, bool overwriteExisting)
        {
            string initClause = overwriteExisting ? "INIT" : "NOINIT";
            string sql = string.Format(@"
                BACKUP DATABASE [{0}]
                TO DISK = @BackupPath
                WITH {1},
                     NAME = N'TRAC_NGHIEM-Full Database Backup',
                     DESCRIPTION = N'Sao lưu từ phần mềm Quản lý Thi Trắc Nghiệm PTIT';",
                TargetDatabaseName, initClause);

            using (SqlConnection masterConn = new SqlConnection(DatabaseConnection.GetMasterConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand(sql, masterConn))
                {
                    cmd.CommandTimeout = 180;
                    cmd.Parameters.Add(new SqlParameter("@BackupPath", SqlDbType.NVarChar, 500) { Value = backupFilePath });
                    masterConn.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Kiểm tra file .bak có phải là bản sao lưu hợp lệ của SQL Server không (RESTORE VERIFYONLY & HEADERONLY)
        /// </summary>
        public DataTable VerifyAndReadBackupHeader(string backupFilePath)
        {
            using (SqlConnection masterConn = new SqlConnection(DatabaseConnection.GetMasterConnectionString()))
            {
                masterConn.Open();

                // Bước 1: Kiểm tra tính toàn vẹn của file .bak
                using (SqlCommand verifyCmd = new SqlCommand("RESTORE VERIFYONLY FROM DISK = @BackupPath;", masterConn))
                {
                    verifyCmd.CommandTimeout = 60;
                    verifyCmd.Parameters.Add(new SqlParameter("@BackupPath", SqlDbType.NVarChar, 500) { Value = backupFilePath });
                    verifyCmd.ExecuteNonQuery();
                }

                // Bước 2: Đọc thông tin Header của file .bak
                DataTable headerTable = new DataTable();
                using (SqlCommand headerCmd = new SqlCommand("RESTORE HEADERONLY FROM DISK = @BackupPath;", masterConn))
                {
                    headerCmd.CommandTimeout = 60;
                    headerCmd.Parameters.Add(new SqlParameter("@BackupPath", SqlDbType.NVarChar, 500) { Value = backupFilePath });
                    using (SqlDataAdapter adapter = new SqlDataAdapter(headerCmd))
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
            const string sql = @"
                SELECT COUNT(*)
                FROM sys.dm_exec_sessions
                WHERE database_id = DB_ID(N'TRAC_NGHIEM')
                  AND session_id <> @@SPID;";

            using (SqlConnection masterConn = new SqlConnection(DatabaseConnection.GetMasterConnectionString()))
            {
                using (SqlCommand cmd = new SqlCommand(sql, masterConn))
                {
                    masterConn.Open();
                    object res = cmd.ExecuteScalar();
                    return res != null ? Convert.ToInt32(res) : 0;
                }
            }
        }

        /// <summary>
        /// Thực hiện Restore an toàn database TRAC_NGHIEM từ file .bak:
        /// 1. Xóa Connection Pool của ứng dụng
        /// 2. Chuyển TRAC_NGHIEM sang SINGLE_USER WITH ROLLBACK IMMEDIATE để ngắt các kết nối đang giữ khóa
        /// 3. Thực hiện RESTORE DATABASE ... WITH REPLACE, RECOVERY
        /// 4. Đưa TRAC_NGHIEM trở lại chế độ MULTI_USER
        /// </summary>
        public void RestoreDatabase(string backupFilePath)
        {
            SqlConnection.ClearAllPools();

            using (SqlConnection masterConn = new SqlConnection(DatabaseConnection.GetMasterConnectionString()))
            {
                masterConn.Open();

                try
                {
                    const string setSingleUserSql = @"
                        IF DB_ID(N'TRAC_NGHIEM') IS NOT NULL
                        BEGIN
                            ALTER DATABASE [TRAC_NGHIEM] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                        END";

                    using (SqlCommand singleCmd = new SqlCommand(setSingleUserSql, masterConn))
                    {
                        singleCmd.CommandTimeout = 60;
                        singleCmd.ExecuteNonQuery();
                    }

                    const string restoreSql = @"
                        RESTORE DATABASE [TRAC_NGHIEM]
                        FROM DISK = @BackupPath
                        WITH REPLACE, RECOVERY;";

                    using (SqlCommand restoreCmd = new SqlCommand(restoreSql, masterConn))
                    {
                        restoreCmd.CommandTimeout = 300;
                        restoreCmd.Parameters.Add(new SqlParameter("@BackupPath", SqlDbType.NVarChar, 500) { Value = backupFilePath });
                        restoreCmd.ExecuteNonQuery();
                    }
                }
                finally
                {
                    // Đảm bảo luôn đưa database về MULTI_USER kể cả khi có ngoại lệ
                    const string setMultiUserSql = @"
                        IF DB_ID(N'TRAC_NGHIEM') IS NOT NULL
                        BEGIN
                            ALTER DATABASE [TRAC_NGHIEM] SET MULTI_USER;
                        END";

                    using (SqlCommand multiCmd = new SqlCommand(setMultiUserSql, masterConn))
                    {
                        multiCmd.CommandTimeout = 60;
                        multiCmd.ExecuteNonQuery();
                    }
                }
            }
        }

        /// <summary>
        /// Kiểm tra nhanh thống kê số lượng bản ghi các bảng sau khi Restore
        /// </summary>
        public DataTable GetDatabaseHealthSummary()
        {
            const string sql = @"
                SELECT N'Lớp học (LOP)' AS BANG, COUNT(*) AS SO_BAN_GHI FROM dbo.LOP
                UNION ALL
                SELECT N'Môn học đang dùng (MONHOC)', COUNT(*) FROM dbo.MONHOC WHERE DAXOA = 0
                UNION ALL
                SELECT N'Sinh viên đang học (SINHVIEN)', COUNT(*) FROM dbo.SINHVIEN WHERE DAXOA = 0
                UNION ALL
                SELECT N'Giáo viên (GIAOVIEN)', COUNT(*) FROM dbo.GIAOVIEN
                UNION ALL
                SELECT N'Tài khoản (TAIKHOAN)', COUNT(*) FROM dbo.TAIKHOAN
                UNION ALL
                SELECT N'Câu hỏi bộ đề (BODE)', COUNT(*) FROM dbo.BODE
                UNION ALL
                SELECT N'Lịch đăng ký thi (GIAOVIEN_DANGKY)', COUNT(*) FROM dbo.GIAOVIEN_DANGKY
                UNION ALL
                SELECT N'Bảng điểm (BANGDIEM)', COUNT(*) FROM dbo.BANGDIEM;";

            return DatabaseConnection.ExecuteQuery(sql, CommandType.Text);
        }
    }
}

