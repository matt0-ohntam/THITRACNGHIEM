/*==============================================================================
  HỆ THỐNG QUẢN LÝ THI TRẮC NGHIỆM PTIT (TRAC_NGHIEM)
  FILE: 03_Backup_Restore.sql
  MÔ TẢ: Script mẫu thực hiện Sao lưu (Full Backup) và Phục hồi (Restore) CSDL
==============================================================================*/

-- 1. KỊCH BẢN SAO LƯU (FULL DATABASE BACKUP)
USE master;
GO

DECLARE @BackupPath NVARCHAR(500) = N'C:\THITRACNGHIEM\backup\TRAC_NGHIEM_Full.bak';

BACKUP DATABASE [TRAC_NGHIEM]
TO DISK = @BackupPath
WITH FORMAT,
     INIT,
     NAME = N'TRAC_NGHIEM-Full Database Backup',
     DESCRIPTION = N'Bản sao lưu đầy đủ cơ sở dữ liệu Thi Trắc Nghiệm PTIT',
     STATS = 10;
GO

-- Kiểm tra tính hợp lệ của file Backup trước khi Restore
RESTORE VERIFYONLY
FROM DISK = N'C:\THITRACNGHIEM\backup\TRAC_NGHIEM_Full.bak';
GO

/*
-- 2. KỊCH BẢN PHỤC HỒI (RESTORE DATABASE AN TOÀN)
-- Lưu ý: Chỉ chạy đoạn dưới khi người dùng xác nhận Restore ghi đè dữ liệu hiện tại!
USE master;
GO

ALTER DATABASE [TRAC_NGHIEM] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;

RESTORE DATABASE [TRAC_NGHIEM]
FROM DISK = N'C:\THITRACNGHIEM\backup\TRAC_NGHIEM_Full.bak'
WITH REPLACE, RECOVERY, STATS = 10;

ALTER DATABASE [TRAC_NGHIEM] SET MULTI_USER;
GO
*/

