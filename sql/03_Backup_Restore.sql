/*==============================================================================
  HỆ THỐNG QUẢN LÝ THI TRẮC NGHIỆM PTIT (TRAC_NGHIEM)
  FILE: 03_Backup_Restore.sql
  MÔ TẢ:
    1. Thiết lập Certificate Signing tại [master] để tài khoản SQL Login
       của Giáo viên (role_GiaoVien) thực hiện Kiểm tra file .bak và Restore CSDL
       an toàn mà không cần cấp sysadmin cho từng giáo viên hay lưu mật khẩu sa.
    2. Kịch bản mẫu thực hiện Full Backup, Verify và Restore CSDL TRAC_NGHIEM.
==============================================================================*/

USE [master];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

/*==============================================================================
  PHẦN 1: STORED PROCEDURE KÝ CHỨNG THƯ SỐ PHỤC VỤ BACKUP / RESTORE TỪ WINFORMS
==============================================================================*/

IF OBJECT_ID(N'dbo.sp_VerifyBackup_TracNghiem', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_VerifyBackup_TracNghiem;
IF OBJECT_ID(N'dbo.sp_RestoreDatabase_TracNghiem', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_RestoreDatabase_TracNghiem;
IF OBJECT_ID(N'dbo.sp_DemKetNoi_TracNghiem', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DemKetNoi_TracNghiem;
IF DATABASE_PRINCIPAL_ID(N'user_Cert_BackupRestore_TracNghiem') IS NOT NULL
    DROP USER [user_Cert_BackupRestore_TracNghiem];
IF SUSER_ID(N'login_Cert_BackupRestore_TracNghiem') IS NOT NULL
    DROP LOGIN [login_Cert_BackupRestore_TracNghiem];
IF CERT_ID(N'cert_MasterBackupRestore_TracNghiem') IS NOT NULL
    DROP CERTIFICATE [cert_MasterBackupRestore_TracNghiem];
GO

CREATE CERTIFICATE [cert_MasterBackupRestore_TracNghiem]
    ENCRYPTION BY PASSWORD = N'Ptit_MasterBackupCert#2026!'
    WITH SUBJECT = N'Certificate ky Stored Procedure Backup Restore TRAC_NGHIEM tai master',
         EXPIRY_DATE = '2099-12-31';
GO

CREATE LOGIN [login_Cert_BackupRestore_TracNghiem]
    FROM CERTIFICATE [cert_MasterBackupRestore_TracNghiem];
ALTER SERVER ROLE [sysadmin] ADD MEMBER [login_Cert_BackupRestore_TracNghiem];

CREATE USER [user_Cert_BackupRestore_TracNghiem]
    FOR LOGIN [login_Cert_BackupRestore_TracNghiem];
GO

-- 1.1. Đếm số kết nối đang hoạt động vào TRAC_NGHIEM
CREATE OR ALTER PROCEDURE dbo.sp_DemKetNoi_TracNghiem
AS
BEGIN
    SET NOCOUNT ON;

    SELECT COUNT(*) AS SO_KET_NOI
    FROM sys.dm_exec_sessions
    WHERE database_id = DB_ID(N'TRAC_NGHIEM')
      AND session_id <> @@SPID
      AND is_user_process = 1;
END;
GO

-- 1.2. Xác thực file .bak (RESTORE VERIFYONLY & RESTORE HEADERONLY)
CREATE OR ALTER PROCEDURE dbo.sp_VerifyBackup_TracNghiem
    @BackupPath NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Caller NVARCHAR(128) = LTRIM(RTRIM(SUSER_SNAME()));

    -- Kiểm tra bắt buộc người gọi phải là sysadmin hoặc Giáo viên đang hoạt động (role_GiaoVien)
    IF ISNULL(IS_SRVROLEMEMBER(N'sysadmin', @Caller), 0) <> 1
       AND NOT EXISTS (
           SELECT 1
           FROM [TRAC_NGHIEM].dbo.GIAOVIEN gv
           INNER JOIN sys.server_principals sp
               ON sp.name = RTRIM(gv.MAGV) COLLATE DATABASE_DEFAULT
              AND sp.type = 'S'
              AND sp.is_disabled = 0
           INNER JOIN [TRAC_NGHIEM].sys.database_principals dp
               ON dp.sid = sp.sid
              AND dp.type = 'S'
           INNER JOIN [TRAC_NGHIEM].sys.database_role_members drm
               ON drm.member_principal_id = dp.principal_id
           INNER JOIN [TRAC_NGHIEM].sys.database_principals r
               ON drm.role_principal_id = r.principal_id
              AND r.name = N'role_GiaoVien'
           WHERE sp.name = @Caller COLLATE DATABASE_DEFAULT
       )
    BEGIN
        RAISERROR(N'Từ chối truy cập: Tài khoản [%s] không thuộc role_GiaoVien nên không có quyền kiểm tra file sao lưu!', 16, 1, @Caller);
        RETURN;
    END

    RESTORE VERIFYONLY FROM DISK = @BackupPath;
    RESTORE HEADERONLY FROM DISK = @BackupPath;
END;
GO

-- 1.3. Phục hồi (Restore) an toàn CSDL TRAC_NGHIEM từ file .bak
CREATE OR ALTER PROCEDURE dbo.sp_RestoreDatabase_TracNghiem
    @BackupPath NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Caller NVARCHAR(128) = LTRIM(RTRIM(SUSER_SNAME()));

    IF ISNULL(IS_SRVROLEMEMBER(N'sysadmin', @Caller), 0) <> 1
       AND NOT EXISTS (
           SELECT 1
           FROM [TRAC_NGHIEM].dbo.GIAOVIEN gv
           INNER JOIN sys.server_principals sp
               ON sp.name = RTRIM(gv.MAGV) COLLATE DATABASE_DEFAULT
              AND sp.type = 'S'
              AND sp.is_disabled = 0
           INNER JOIN [TRAC_NGHIEM].sys.database_principals dp
               ON dp.sid = sp.sid
              AND dp.type = 'S'
           INNER JOIN [TRAC_NGHIEM].sys.database_role_members drm
               ON drm.member_principal_id = dp.principal_id
           INNER JOIN [TRAC_NGHIEM].sys.database_principals r
               ON drm.role_principal_id = r.principal_id
              AND r.name = N'role_GiaoVien'
           WHERE sp.name = @Caller COLLATE DATABASE_DEFAULT
       )
    BEGIN
        RAISERROR(N'Từ chối truy cập: Tài khoản [%s] không thuộc role_GiaoVien nên không có quyền phục hồi (Restore) CSDL!', 16, 1, @Caller);
        RETURN;
    END

    -- Bước 1: Kiểm tra tính toàn vẹn của file .bak trước khi thao tác
    RESTORE VERIFYONLY FROM DISK = @BackupPath;

    BEGIN TRY
        -- Bước 2: Chuyển TRAC_NGHIEM sang SINGLE_USER để ngắt các kết nối đang chiếm giữ
        IF DB_ID(N'TRAC_NGHIEM') IS NOT NULL
        BEGIN
            ALTER DATABASE [TRAC_NGHIEM] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        END

        -- Bước 3: Thực hiện RESTORE DATABASE
        RESTORE DATABASE [TRAC_NGHIEM]
        FROM DISK = @BackupPath
        WITH REPLACE, RECOVERY;

        -- Bước 4: Đưa TRAC_NGHIEM trở lại chế độ đa người dùng (MULTI_USER)
        IF DB_ID(N'TRAC_NGHIEM') IS NOT NULL
        BEGIN
            ALTER DATABASE [TRAC_NGHIEM] SET MULTI_USER;
        END

        -- Bước 5: Tự động đồng bộ lại ánh xạ Database User <-> SQL Login sau khi Restore
        DECLARE @SyncSql NVARCHAR(MAX) = N'
        USE [TRAC_NGHIEM];
        DECLARE @UserName NVARCHAR(128);
        DECLARE cur_users CURSOR LOCAL FAST_FORWARD FOR
            SELECT dp.name
            FROM sys.database_principals dp
            INNER JOIN sys.server_principals sp
                ON sp.name = dp.name COLLATE DATABASE_DEFAULT
               AND sp.type = ''S''
            WHERE dp.type = ''S''
              AND dp.sid <> sp.sid;

        OPEN cur_users;
        FETCH NEXT FROM cur_users INTO @UserName;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @AlterCmd NVARCHAR(MAX) = N''ALTER USER '' + QUOTENAME(@UserName) + N'' WITH LOGIN = '' + QUOTENAME(@UserName) + N'';'';
            BEGIN TRY EXEC sys.sp_executesql @AlterCmd; END TRY BEGIN CATCH END CATCH
            FETCH NEXT FROM cur_users INTO @UserName;
        END
        CLOSE cur_users;
        DEALLOCATE cur_users;

        IF DATABASE_PRINCIPAL_ID(N''role_GiaoVien'') IS NOT NULL
            GRANT BACKUP DATABASE TO [role_GiaoVien];
        ';
        EXEC sys.sp_executesql @SyncSql;
    END TRY
    BEGIN CATCH
        IF DB_ID(N'TRAC_NGHIEM') IS NOT NULL
        BEGIN
            BEGIN TRY
                ALTER DATABASE [TRAC_NGHIEM] SET MULTI_USER;
            END TRY
            BEGIN CATCH
            END CATCH
        END
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrMsg, 16, 1);
        RETURN;
    END CATCH
END;
GO

ADD SIGNATURE TO dbo.sp_DemKetNoi_TracNghiem
    BY CERTIFICATE [cert_MasterBackupRestore_TracNghiem]
    WITH PASSWORD = N'Ptit_MasterBackupCert#2026!';

ADD SIGNATURE TO dbo.sp_VerifyBackup_TracNghiem
    BY CERTIFICATE [cert_MasterBackupRestore_TracNghiem]
    WITH PASSWORD = N'Ptit_MasterBackupCert#2026!';

ADD SIGNATURE TO dbo.sp_RestoreDatabase_TracNghiem
    BY CERTIFICATE [cert_MasterBackupRestore_TracNghiem]
    WITH PASSWORD = N'Ptit_MasterBackupCert#2026!';

GRANT EXECUTE ON dbo.sp_DemKetNoi_TracNghiem TO [public];
GRANT EXECUTE ON dbo.sp_VerifyBackup_TracNghiem TO [public];
GRANT EXECUTE ON dbo.sp_RestoreDatabase_TracNghiem TO [public];
GO

/*==============================================================================
  PHẦN 2: KỊCH BẢN MẪU SAO LƯU (FULL DATABASE BACKUP) VÀ KIỂM TRA FILE .BAK
==============================================================================*/

DECLARE @BackupPath NVARCHAR(500) = N'C:\THITRACNGHIEM\backup\TRAC_NGHIEM_Full.bak';

BACKUP DATABASE [TRAC_NGHIEM]
TO DISK = @BackupPath
WITH INIT,
     NAME = N'TRAC_NGHIEM-Full Database Backup',
     DESCRIPTION = N'Bản sao lưu đầy đủ cơ sở dữ liệu Thi Trắc Nghiệm PTIT (SQL Login & Role)',
     STATS = 10;
GO

EXEC master.dbo.sp_VerifyBackup_TracNghiem
    @BackupPath = N'C:\THITRACNGHIEM\backup\TRAC_NGHIEM_Full.bak';
GO
