/*==============================================================================
  HỆ THỐNG QUẢN LÝ THI TRẮC NGHIỆM PTIT (TRAC_NGHIEM)
  FILE: 02_Test_Database.sql
  MÔ TẢ: Script tự động kiểm thử Views, Stored Procedures, Triggers và Ràng buộc
==============================================================================*/

USE [TRAC_NGHIEM];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
PRINT N'1. KIỂM TRA CÁC VIEW DỮ LIỆU:';
SELECT * FROM dbo.vw_DanhSachLop;
SELECT * FROM dbo.vw_DanhSachMonHoc;
SELECT * FROM dbo.vw_DanhSachSinhVienTheoLop WHERE MALOP = N'CNTT01';
SELECT * FROM dbo.vw_DanhSachGiaoVien;
GO

PRINT N'2. KIỂM TRA STORED PROCEDURE ĐĂNG NHẬP (sp_DangNhap):';
DECLARE @Hash123456 NVARCHAR(100) = CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2);
EXEC dbo.sp_DangNhap @TenDangNhap = N'admin', @MatKhauHash = @Hash123456;
EXEC dbo.sp_DangNhap @TenDangNhap = N'SV001', @MatKhauHash = @Hash123456;
GO

PRINT N'3. KIỂM TRA THÊM MÔN HỌC, XÓA MỀM VÀ PHỤC HỒI (sp_ThemMonHoc, sp_XoaMonHoc, sp_PhucHoiMonHoc):';
EXEC dbo.sp_ThemMonHoc @MaMH = N'KTMT', @TenMH = N'Kiến trúc máy tính';
EXEC dbo.sp_XoaMonHoc @MaMH = N'KTMT';
EXEC dbo.sp_PhucHoiMonHoc @MaMH = N'KTMT';
-- Dọn dữ liệu test:
DELETE FROM dbo.MONHOC WHERE RTRIM(MAMH) = N'KTMT';
GO

PRINT N'4. KIỂM TRA CHẶN XÓA MÔN HỌC ĐÃ CÓ ĐỀ THI (CSDL):';
BEGIN TRY
    EXEC dbo.sp_XoaMonHoc @MaMH = N'CSDL';
END TRY
BEGIN CATCH
    PRINT N'[PASS - Đã chặn xóa CSDL]: ' + ERROR_MESSAGE();
END CATCH
GO

PRINT N'5. KIỂM TRA THÊM SINH VIÊN TỰ ĐỘNG TẠO TÀI KHOẢN QUA TRANSACTION (sp_ThemSinhVien):';
DECLARE @HashDefault NVARCHAR(100) = CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2);
EXEC dbo.sp_ThemSinhVien
    @MaSV = N'SV099',
    @Ho = N'Trần Quốc',
    @Ten = N'Toản',
    @NgaySinh = '2004-10-20',
    @DiaChi = N'TP. Thủ Đức',
    @MaLop = N'CNTT01',
    @MatKhauMacDinh = @HashDefault;

-- Kiểm tra tài khoản SV099 đã tự động sinh ra chưa:
SELECT TENDANGNHAP, RTRIM(MASV) AS MASV, QUYEN, TRANGTHAI FROM dbo.TAIKHOAN WHERE TENDANGNHAP = N'SV099';

-- Xóa mềm SV099 -> Tài khoản SV099 tự động chuyển TRANGTHAI = 0 qua Trigger:
EXEC dbo.sp_XoaSinhVien @MaSV = N'SV099';
SELECT TENDANGNHAP, RTRIM(MASV) AS MASV, QUYEN, TRANGTHAI AS TRANGTHAI_SAU_XOA_MEM FROM dbo.TAIKHOAN WHERE TENDANGNHAP = N'SV099';

-- Phục hồi SV099 -> Tài khoản SV099 tự động chuyển TRANGTHAI = 1:
EXEC dbo.sp_PhucHoiSinhVien @MaSV = N'SV099';
SELECT TENDANGNHAP, RTRIM(MASV) AS MASV, QUYEN, TRANGTHAI AS TRANGTHAI_SAU_PHUC_HOI FROM dbo.TAIKHOAN WHERE TENDANGNHAP = N'SV099';

-- Dọn dữ liệu test SV099:
DELETE FROM dbo.TAIKHOAN WHERE TENDANGNHAP = N'SV099';
DELETE FROM dbo.SINHVIEN WHERE RTRIM(MASV) = N'SV099';
GO

PRINT N'=== HOÀN TẤT KIỂM THỬ DATABASE TRAC_NGHIEM ===';
GO
