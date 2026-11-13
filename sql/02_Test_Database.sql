/*==============================================================================
  HỆ THỐNG QUẢN LÝ THI TRẮC NGHIỆM PTIT (TRAC_NGHIEM)
  FILE: 02_Test_Database.sql
  MÔ TẢ: Script tự động kiểm thử Views, Stored Procedures, Triggers,
         SQL Server Logins, Database Roles và Ràng buộc (Không dùng TAIKHOAN)
==============================================================================*/

USE [TRAC_NGHIEM];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;

PRINT N'1. KIỂM TRA DANH SÁCH BẢNG TRONG DATABASE (XÁC NHẬN KHÔNG CÒN BẢNG TAIKHOAN):';
SELECT name AS TEN_BANG, create_date AS NGAY_TAO
FROM sys.tables
ORDER BY name;
GO

PRINT N'2. KIỂM TRA CÁC VIEW DỮ LIỆU:';
SELECT * FROM dbo.vw_DanhSachLop;
SELECT * FROM dbo.vw_DanhSachMonHoc;
SELECT * FROM dbo.vw_DanhSachSinhVienTheoLop WHERE MALOP = N'CNTT01';
SELECT * FROM dbo.vw_DanhSachGiaoVien;
GO

PRINT N'3. KIỂM TRA DANH SÁCH TÀI KHOẢN GIÁO VIÊN & THỐNG KÊ TỔNG QUAN (GỒM GV-8-4):';
EXEC dbo.sp_LayDanhSachTaiKhoanGiaoVien;
EXEC dbo.sp_LayThongKeTongQuan;
GO

PRINT N'4. KIỂM TRA TẠO LOGIN, THU HỒI LOGIN VÀ KÍCH HOẠT LẠI LOGIN CHO GIÁO VIÊN (GV003):';
IF DATABASE_PRINCIPAL_ID(N'GV003') IS NOT NULL DROP USER [GV003];
IF SUSER_ID(N'GV003') IS NOT NULL DROP LOGIN [GV003];
EXEC dbo.sp_TaoTaiKhoanGiaoVien @MaGV = N'GV003', @TenDangNhap = N'GV003', @MatKhauPlain = N'123456';
EXEC dbo.sp_ThuHoiTaiKhoanGiaoVien @MaGV = N'GV003';
EXEC dbo.sp_KichHoatLaiTaiKhoanGiaoVien @MaGV = N'GV003';
-- Dọn lại trạng thái GV003 chưa có tài khoản để phục vụ test trên giao diện:
IF DATABASE_PRINCIPAL_ID(N'GV003') IS NOT NULL DROP USER [GV003];
IF SUSER_ID(N'GV003') IS NOT NULL DROP LOGIN [GV003];
GO

PRINT N'5. KIỂM TRA THÊM MÔN HỌC, XÓA MỀM VÀ PHỤC HỒI (sp_ThemMonHoc, sp_XoaMonHoc, sp_PhucHoiMonHoc):';
EXEC dbo.sp_ThemMonHoc @MaMH = N'KTMT', @TenMH = N'Kiến trúc máy tính';
EXEC dbo.sp_XoaMonHoc @MaMH = N'KTMT';
EXEC dbo.sp_PhucHoiMonHoc @MaMH = N'KTMT';
DELETE FROM dbo.MONHOC WHERE RTRIM(MAMH) = N'KTMT';
GO

PRINT N'6. KIỂM TRA CHẶN XÓA MÔN HỌC ĐÃ CÓ ĐỀ THI (CSDL):';
BEGIN TRY
    EXEC dbo.sp_XoaMonHoc @MaMH = N'CSDL';
END TRY
BEGIN CATCH
    PRINT N'[PASS - Đã chặn xóa CSDL]: ' + ERROR_MESSAGE();
END CATCH
GO

PRINT N'7. KIỂM TRA THÊM SINH VIÊN TỰ ĐỘNG CẤP SQL LOGIN & ROLE_SINHVIEN (sp_ThemSinhVien):';
EXEC dbo.sp_ThemSinhVien
    @MaSV = N'SV099',
    @Ho = N'Trần Quốc',
    @Ten = N'Toản',
    @NgaySinh = '2004-10-20',
    @DiaChi = N'TP. Thủ Đức',
    @MaLop = N'CNTT01',
    @MatKhauMacDinh = N'123456';

SELECT name AS SQL_LOGIN, is_disabled FROM sys.server_principals WHERE name = N'SV099';
EXEC dbo.sp_XoaSinhVien @MaSV = N'SV099';
SELECT name AS SQL_LOGIN_SAU_XOA_MEM, is_disabled FROM sys.server_principals WHERE name = N'SV099';
EXEC dbo.sp_PhucHoiSinhVien @MaSV = N'SV099';
SELECT name AS SQL_LOGIN_SAU_PHUC_HOI, is_disabled FROM sys.server_principals WHERE name = N'SV099';
EXEC dbo.sp_HoanTacThemSinhVien @MaSV = N'SV099';
GO

PRINT N'=== HOÀN TẤT KIỂM THỬ DATABASE TRAC_NGHIEM ===';
GO
