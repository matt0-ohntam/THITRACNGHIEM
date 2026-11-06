/*==============================================================================
  HỆ THỐNG QUẢN LÝ THI TRẮC NGHIỆM PTIT (TRAC_NGHIEM)
  FILE: 01_Setup_TRAC_NGHIEM.sql
  PHỤ TRÁCH: NGƯỜI 1 – QUẢN TRỊ VÀ DỮ LIỆU NỀN
  MÔ TẢ:
    1. Xóa database TRAC_NGHIEM cũ (nếu có) và tạo mới sạch sẽ.
    2. Tạo 4 bảng nền của Người 1: LOP, MONHOC, SINHVIEN, GIAOVIEN.
    3. Tạo bảng quản trị tài khoản & phân quyền: TAIKHOAN.
    4. Tạo 3 bảng nghiệp vụ thi của Người 2: BODE, GIAOVIEN_DANGKY, BANGDIEM.
    5. Thiết lập đầy đủ PRIMARY KEY, FOREIGN KEY, UNIQUE, CHECK, DEFAULT.
    6. Tạo các INDEX tối ưu truy vấn tìm kiếm và lọc Subform.
    7. Tạo các VIEW phục vụ hiển thị dữ liệu.
    8. Tạo các TRIGGER bảo vệ toàn vẹn dữ liệu lịch sử thi.
    9. Tạo các STORED PROCEDURE có TRANSACTION (ACID).
   10. Nạp dữ liệu mẫu chuẩn (mật khẩu băm SHA-256).
==============================================================================*/

USE master;
GO

-- 1. XÓA DATABASE CŨ (NẾU TỒN TẠI) VÀ TẠO MỚI DATABASE TRAC_NGHIEM
IF DB_ID(N'TRAC_NGHIEM') IS NOT NULL
BEGIN
    ALTER DATABASE [TRAC_NGHIEM] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [TRAC_NGHIEM];
END
GO

CREATE DATABASE [TRAC_NGHIEM];
GO

USE [TRAC_NGHIEM];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/*==============================================================================
  PHẦN 2: TẠO CÁC BẢNG DỮ LIỆU NỀN (NGƯỜI 1 QUẢN LÝ)
==============================================================================*/

-- 2.1. Bảng LOP
CREATE TABLE dbo.LOP (
    MALOP   NCHAR(8)        NOT NULL,
    TENLOP  NVARCHAR(40)    NOT NULL,
    CONSTRAINT PK_Lop PRIMARY KEY (MALOP),
    CONSTRAINT UQ_Lop_TenLop UNIQUE (TENLOP),
    CONSTRAINT CK_Lop_MaLop_Upper CHECK (
        LEN(LTRIM(RTRIM(MALOP))) > 0
        AND MALOP = UPPER(MALOP) COLLATE Latin1_General_CS_AS
    ),
    CONSTRAINT CK_Lop_TenLop_NotEmpty CHECK (LEN(LTRIM(RTRIM(TENLOP))) > 0)
);
GO

-- 2.2. Bảng MONHOC (Có cột DAXOA hỗ trợ Soft Delete / Phục hồi bền vững)
CREATE TABLE dbo.MONHOC (
    MAMH    NCHAR(5)        NOT NULL,
    TENMH   NVARCHAR(40)    NOT NULL,
    DAXOA   BIT             NOT NULL CONSTRAINT DF_Monhoc_DaXoa DEFAULT (0),
    CONSTRAINT PK_Monhoc PRIMARY KEY (MAMH),
    CONSTRAINT UQ_Monhoc_TenMH UNIQUE (TENMH),
    CONSTRAINT CK_Monhoc_MaMH_Upper CHECK (
        LEN(LTRIM(RTRIM(MAMH))) > 0
        AND MAMH = UPPER(MAMH) COLLATE Latin1_General_CS_AS
    ),
    CONSTRAINT CK_Monhoc_TenMH_NotEmpty CHECK (LEN(LTRIM(RTRIM(TENMH))) > 0)
);
GO

-- 2.3. Bảng SINHVIEN (Có cột DAXOA hỗ trợ Soft Delete / Phục hồi bền vững)
CREATE TABLE dbo.SINHVIEN (
    MASV        NCHAR(8)        NOT NULL,
    HO          NVARCHAR(40)    NOT NULL,
    TEN         NVARCHAR(10)    NOT NULL,
    NGAYSINH    DATETIME        NULL,
    DIACHI      NVARCHAR(40)    NULL,
    MALOP       NCHAR(8)        NOT NULL,
    DAXOA       BIT             NOT NULL CONSTRAINT DF_Sinhvien_DaXoa DEFAULT (0),
    CONSTRAINT PK_Sinhvien PRIMARY KEY (MASV),
    CONSTRAINT FK_Sinhvien_Lop FOREIGN KEY (MALOP) REFERENCES dbo.LOP (MALOP),
    CONSTRAINT CK_Sinhvien_MaSV_Upper CHECK (
        LEN(LTRIM(RTRIM(MASV))) > 0
        AND MASV = UPPER(MASV) COLLATE Latin1_General_CS_AS
    ),
    CONSTRAINT CK_Sinhvien_Ho_NotEmpty CHECK (LEN(LTRIM(RTRIM(HO))) > 0),
    CONSTRAINT CK_Sinhvien_Ten_NotEmpty CHECK (LEN(LTRIM(RTRIM(TEN))) > 0),
    CONSTRAINT CK_Sinhvien_NgaySinh CHECK (
        NGAYSINH IS NULL OR (NGAYSINH >= '1950-01-01' AND NGAYSINH < GETDATE())
    )
);
GO

-- 2.4. Bảng GIAOVIEN
CREATE TABLE dbo.GIAOVIEN (
    MAGV    NCHAR(8)        NOT NULL,
    HO      NVARCHAR(40)    NOT NULL,
    TEN     NVARCHAR(10)    NOT NULL,
    SODTLL  NCHAR(12)       NULL,
    DIACHI  NVARCHAR(50)    NULL,
    CONSTRAINT PK_Giaovien PRIMARY KEY (MAGV),
    CONSTRAINT CK_Giaovien_MaGV_Upper CHECK (
        LEN(LTRIM(RTRIM(MAGV))) > 0
        AND MAGV = UPPER(MAGV) COLLATE Latin1_General_CS_AS
    ),
    CONSTRAINT CK_Giaovien_Ho_NotEmpty CHECK (LEN(LTRIM(RTRIM(HO))) > 0),
    CONSTRAINT CK_Giaovien_Ten_NotEmpty CHECK (LEN(LTRIM(RTRIM(TEN))) > 0),
    CONSTRAINT CK_Giaovien_SoDT CHECK (
        SODTLL IS NULL OR (
            LEN(LTRIM(RTRIM(SODTLL))) BETWEEN 9 AND 12
            AND LTRIM(RTRIM(SODTLL)) NOT LIKE '%[^0-9]%'
        )
    )
);
GO

-- 2.5. Bảng TAIKHOAN (Quản lý đăng nhập, mật khẩu SHA-256, phân quyền, trạng thái)
CREATE TABLE dbo.TAIKHOAN (
    TENDANGNHAP NVARCHAR(50)    NOT NULL,
    MATKHAU     NVARCHAR(100)   NOT NULL,
    MAGV        NCHAR(8)        NULL,
    MASV        NCHAR(8)        NULL,
    QUYEN       NVARCHAR(20)    NOT NULL,
    TRANGTHAI   BIT             NOT NULL CONSTRAINT DF_TaiKhoan_TrangThai DEFAULT (1),
    NGAYTAO     DATETIME        NOT NULL CONSTRAINT DF_TaiKhoan_NgayTao DEFAULT (GETDATE()),
    CONSTRAINT PK_TaiKhoan PRIMARY KEY (TENDANGNHAP),
    CONSTRAINT FK_TaiKhoan_Giaovien FOREIGN KEY (MAGV) REFERENCES dbo.GIAOVIEN (MAGV),
    CONSTRAINT FK_TaiKhoan_Sinhvien FOREIGN KEY (MASV) REFERENCES dbo.SINHVIEN (MASV),
    CONSTRAINT CK_TaiKhoan_Quyen CHECK (QUYEN IN (N'GIANGVIEN', N'SINHVIEN')),
    CONSTRAINT CK_TaiKhoan_LienKet CHECK (
        (QUYEN = N'GIANGVIEN' AND MAGV IS NOT NULL AND MASV IS NULL)
        OR
        (QUYEN = N'SINHVIEN' AND MASV IS NOT NULL AND MAGV IS NULL)
    )
);
GO

/*==============================================================================
  PHẦN 3: TẠO CÁC BẢNG NGHIỆP VỤ THI (HỢP ĐỒNG TÍCH HỢP VỚI NGƯỜI 2)
==============================================================================*/

-- 3.1. Bảng BODE (Ngân hàng câu hỏi trắc nghiệm)
CREATE TABLE dbo.BODE (
    CAUHOI  INT             IDENTITY(1,1) NOT NULL,
    MAMH    NCHAR(5)        NOT NULL,
    TRINHDO NCHAR(1)        NOT NULL,
    NOIDUNG NVARCHAR(500)   NOT NULL,
    A       NVARCHAR(200)   NOT NULL,
    B       NVARCHAR(200)   NOT NULL,
    C       NVARCHAR(200)   NOT NULL,
    D       NVARCHAR(200)   NOT NULL,
    DAP_AN  NCHAR(1)        NOT NULL,
    MAGV    NCHAR(8)        NOT NULL,
    CONSTRAINT PK_BODE PRIMARY KEY (CAUHOI),
    CONSTRAINT FK_BODE_Monhoc FOREIGN KEY (MAMH) REFERENCES dbo.MONHOC (MAMH),
    CONSTRAINT FK_BODE_Giaovien FOREIGN KEY (MAGV) REFERENCES dbo.GIAOVIEN (MAGV),
    CONSTRAINT CK_BODE_TrinhDo CHECK (TRINHDO IN (N'A', N'B', N'C')),
    CONSTRAINT CK_BODE_DapAn CHECK (DAP_AN IN (N'A', N'B', N'C', N'D'))
);
GO

-- 3.2. Bảng GIAOVIEN_DANGKY (Lịch chuẩn bị thi cho lớp)
CREATE TABLE dbo.GIAOVIEN_DANGKY (
    MAGV        NCHAR(8)    NOT NULL,
    MALOP       NCHAR(8)    NOT NULL,
    MAMH        NCHAR(5)    NOT NULL,
    TRINHDO     NCHAR(1)    NOT NULL,
    NGAYTHI     DATETIME    NOT NULL CONSTRAINT DF_GVDK_NgayThi DEFAULT (GETDATE()),
    LAN         SMALLINT    NOT NULL,
    SOCAUTHI    SMALLINT    NOT NULL,
    THOIGIAN    SMALLINT    NOT NULL,
    CONSTRAINT PK_Giaovien_Dangky PRIMARY KEY (MALOP, MAMH, LAN),
    CONSTRAINT FK_GVDK_Giaovien FOREIGN KEY (MAGV) REFERENCES dbo.GIAOVIEN (MAGV),
    CONSTRAINT FK_GVDK_Lop FOREIGN KEY (MALOP) REFERENCES dbo.LOP (MALOP),
    CONSTRAINT FK_GVDK_Monhoc FOREIGN KEY (MAMH) REFERENCES dbo.MONHOC (MAMH),
    CONSTRAINT CK_GVDK_TrinhDo CHECK (TRINHDO IN (N'A', N'B', N'C')),
    CONSTRAINT CK_GVDK_Lan CHECK (LAN >= 1 AND LAN <= 2),
    CONSTRAINT CK_GVDK_SoCauThi CHECK (SOCAUTHI >= 10 AND SOCAUTHI <= 100),
    CONSTRAINT CK_GVDK_ThoiGian CHECK (THOIGIAN >= 5 AND THOIGIAN <= 60)
);
GO

-- 3.3. Bảng BANGDIEM (Kết quả và bài thi của sinh viên)
CREATE TABLE dbo.BANGDIEM (
    MASV    NCHAR(8)        NOT NULL,
    MAMH    NCHAR(5)        NOT NULL,
    LAN     SMALLINT        NOT NULL,
    NGAYTHI DATETIME        NOT NULL CONSTRAINT DF_BangDiem_NgayThi DEFAULT (GETDATE()),
    DIEM    FLOAT           NOT NULL,
    BAITHI  NVARCHAR(MAX)   NULL,
    CONSTRAINT PK_BangDiem PRIMARY KEY (MASV, MAMH, LAN),
    CONSTRAINT FK_BangDiem_Sinhvien FOREIGN KEY (MASV) REFERENCES dbo.SINHVIEN (MASV),
    CONSTRAINT FK_BangDiem_Monhoc FOREIGN KEY (MAMH) REFERENCES dbo.MONHOC (MAMH),
    CONSTRAINT CK_BangDiem_Lan CHECK (LAN >= 1 AND LAN <= 2),
    CONSTRAINT CK_BangDiem_Diem CHECK (DIEM >= 0 AND DIEM <= 10)
);
GO

/*==============================================================================
  PHẦN 4: THIẾT LẬP INDEX (CHỈ MỤC TỐI ƯU HIỆU NĂNG VÀ RÀNG BUỘC 1-1)
==============================================================================*/

-- 4.1. Filtered Unique Index đảm bảo mỗi Giảng viên / Sinh viên chỉ có tối đa 1 tài khoản
CREATE UNIQUE NONCLUSTERED INDEX UX_TaiKhoan_MAGV
ON dbo.TAIKHOAN (MAGV)
WHERE MAGV IS NOT NULL;
GO

CREATE UNIQUE NONCLUSTERED INDEX UX_TaiKhoan_MASV
ON dbo.TAIKHOAN (MASV)
WHERE MASV IS NOT NULL;
GO

-- 4.2. Index tối ưu lọc danh sách sinh viên theo lớp trên Subform
CREATE NONCLUSTERED INDEX IX_SinhVien_MaLop_DaXoa
ON dbo.SINHVIEN (MALOP, DAXOA)
INCLUDE (HO, TEN, NGAYSINH, DIACHI);
GO

-- 4.3. Index tối ưu tìm kiếm Sinh viên và Giáo viên theo Tên, Họ
CREATE NONCLUSTERED INDEX IX_SinhVien_Ten_Ho
ON dbo.SINHVIEN (TEN, HO);
GO

CREATE NONCLUSTERED INDEX IX_GiaoVien_Ten_Ho
ON dbo.GIAOVIEN (TEN, HO);
GO

-- 4.4. Index tối ưu kiểm tra câu hỏi theo Môn học & Trình độ
CREATE NONCLUSTERED INDEX IX_BODE_MaMH_TrinhDo
ON dbo.BODE (MAMH, TRINHDO);
GO

/*==============================================================================
  PHẦN 5: TẠO CÁC VIEW PHỤC VỤ TRUY VẤN & BÁO CÁO
==============================================================================*/

-- 5.1. View Danh sách môn học (chỉ lấy các môn chưa xóa mềm + thống kê tham chiếu)
CREATE VIEW dbo.vw_DanhSachMonHoc
AS
SELECT
    RTRIM(m.MAMH) AS MAMH,
    m.TENMH,
    m.DAXOA,
    (SELECT COUNT(*) FROM dbo.BODE b WHERE b.MAMH = m.MAMH) AS SO_CAU_HOI,
    (SELECT COUNT(*) FROM dbo.GIAOVIEN_DANGKY dk WHERE dk.MAMH = m.MAMH) AS SO_LICH_THI,
    (SELECT COUNT(*) FROM dbo.BANGDIEM bd WHERE bd.MAMH = m.MAMH) AS SO_BAI_THI
FROM dbo.MONHOC m
WHERE m.DAXOA = 0;
GO

-- 5.2. View Danh sách lớp kèm sĩ số sinh viên hiện tại
CREATE VIEW dbo.vw_DanhSachLop
AS
SELECT
    RTRIM(l.MALOP) AS MALOP,
    l.TENLOP,
    (SELECT COUNT(*) FROM dbo.SINHVIEN sv WHERE sv.MALOP = l.MALOP AND sv.DAXOA = 0) AS SI_SO,
    (SELECT COUNT(*) FROM dbo.GIAOVIEN_DANGKY dk WHERE dk.MALOP = l.MALOP) AS SO_LICH_THI
FROM dbo.LOP l;
GO

-- 5.3. View Danh sách sinh viên theo lớp
CREATE VIEW dbo.vw_DanhSachSinhVienTheoLop
AS
SELECT
    RTRIM(sv.MASV) AS MASV,
    sv.HO,
    sv.TEN,
    (sv.HO + N' ' + sv.TEN) AS HOTEN,
    sv.NGAYSINH,
    sv.DIACHI,
    RTRIM(sv.MALOP) AS MALOP,
    l.TENLOP,
    sv.DAXOA,
    ISNULL(tk.TRANGTHAI, 0) AS TRANGTHAI_TK,
    (SELECT COUNT(*) FROM dbo.BANGDIEM bd WHERE bd.MASV = sv.MASV) AS SO_LAN_THI
FROM dbo.SINHVIEN sv
INNER JOIN dbo.LOP l ON sv.MALOP = l.MALOP
LEFT JOIN dbo.TAIKHOAN tk ON tk.MASV = sv.MASV
WHERE sv.DAXOA = 0;
GO

-- 5.4. View Danh sách giáo viên kèm trạng thái tài khoản
CREATE VIEW dbo.vw_DanhSachGiaoVien
AS
SELECT
    RTRIM(gv.MAGV) AS MAGV,
    gv.HO,
    gv.TEN,
    (gv.HO + N' ' + gv.TEN) AS HOTEN,
    RTRIM(ISNULL(gv.SODTLL, N'')) AS SODTLL,
    ISNULL(gv.DIACHI, N'') AS DIACHI,
    tk.TENDANGNHAP,
    CASE WHEN tk.TENDANGNHAP IS NOT NULL THEN 1 ELSE 0 END AS CO_TAI_KHOAN,
    ISNULL(tk.TRANGTHAI, 0) AS TRANGTHAI_TK,
    (SELECT COUNT(*) FROM dbo.BODE b WHERE b.MAGV = gv.MAGV) AS SO_CAU_DA_SOAN,
    (SELECT COUNT(*) FROM dbo.GIAOVIEN_DANGKY dk WHERE dk.MAGV = gv.MAGV) AS SO_LICH_DANG_KY
FROM dbo.GIAOVIEN gv
LEFT JOIN dbo.TAIKHOAN tk ON tk.MAGV = gv.MAGV;
GO

/*==============================================================================
  PHẦN 6: TẠO CÁC TRIGGER BẢO VỆ DỮ LIỆU CÓ LỊCH SỬ THI
==============================================================================*/

-- 6.1. Trigger bảo vệ không cho xóa mềm hoặc xóa cứng Môn học đã có dữ liệu thi
CREATE TRIGGER dbo.trg_MonHoc_ProtectHistory
ON dbo.MONHOC
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- Kiểm tra khi UPDATE DAXOA từ 0 -> 1 hoặc khi DELETE
    IF EXISTS (
        SELECT 1
        FROM deleted d
        LEFT JOIN inserted i ON d.MAMH = i.MAMH
        WHERE (i.MAMH IS NULL OR (d.DAXOA = 0 AND i.DAXOA = 1))
          AND (
              EXISTS (SELECT 1 FROM dbo.BODE b WHERE b.MAMH = d.MAMH)
              OR EXISTS (SELECT 1 FROM dbo.GIAOVIEN_DANGKY dk WHERE dk.MAMH = d.MAMH)
              OR EXISTS (SELECT 1 FROM dbo.BANGDIEM bd WHERE bd.MAMH = d.MAMH)
          )
    )
    BEGIN
        RAISERROR(N'Không thể xóa môn học vì môn học này đang được tham chiếu bởi Bộ đề, Lịch đăng ký thi hoặc Bảng điểm!', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END
END;
GO

-- 6.2. Trigger bảo vệ không cho xóa mềm hoặc xóa cứng Sinh viên đã có điểm thi
--      đồng thời tự động khóa/mở tài khoản sinh viên khi xóa mềm / phục hồi
CREATE TRIGGER dbo.trg_SinhVien_ProtectAndSyncAccount
ON dbo.SINHVIEN
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Chặn xóa mềm / xóa cứng nếu sinh viên đã có lịch sử thi trong BANGDIEM
    IF EXISTS (
        SELECT 1
        FROM deleted d
        LEFT JOIN inserted i ON d.MASV = i.MASV
        WHERE (i.MASV IS NULL OR (d.DAXOA = 0 AND i.DAXOA = 1))
          AND EXISTS (SELECT 1 FROM dbo.BANGDIEM bd WHERE bd.MASV = d.MASV)
    )
    BEGIN
        RAISERROR(N'Không thể xóa sinh viên vì sinh viên này đã có lịch sử điểm thi trong hệ thống!', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END

    -- 2. Đồng bộ trạng thái tài khoản khi DAXOA thay đổi (Soft Delete / Phục hồi)
    IF UPDATE(DAXOA)
    BEGIN
        UPDATE tk
        SET tk.TRANGTHAI = CASE WHEN i.DAXOA = 1 THEN 0 ELSE 1 END
        FROM dbo.TAIKHOAN tk
        INNER JOIN inserted i ON tk.MASV = i.MASV;
    END
END;
GO

/*==============================================================================
  PHẦN 7: TẠO CÁC STORED PROCEDURE NGHIỆP VỤ (NGƯỜI 1)
==============================================================================*/

-- 7.1. Stored Procedure Đăng nhập
CREATE PROCEDURE dbo.sp_DangNhap
    @TenDangNhap    NVARCHAR(50),
    @MatKhauHash    NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @UserTrim NVARCHAR(50) = LTRIM(RTRIM(@TenDangNhap));

    SELECT
        tk.TENDANGNHAP,
        tk.QUYEN,
        tk.TRANGTHAI,
        RTRIM(ISNULL(tk.MAGV, N'')) AS MAGV,
        RTRIM(ISNULL(tk.MASV, N'')) AS MASV,
        CASE
            WHEN tk.QUYEN = N'GIANGVIEN' THEN (gv.HO + N' ' + gv.TEN)
            WHEN tk.QUYEN = N'SINHVIEN'  THEN (sv.HO + N' ' + sv.TEN)
            ELSE tk.TENDANGNHAP
        END AS HOTEN,
        RTRIM(ISNULL(sv.MALOP, N'')) AS MALOP,
        ISNULL(l.TENLOP, N'') AS TENLOP,
        ISNULL(sv.DAXOA, 0) AS SV_DAXOA,
        CASE
            WHEN tk.MATKHAU = @MatKhauHash THEN 1
            ELSE 0
        END AS KHOP_MATKHAU
    FROM dbo.TAIKHOAN tk
    LEFT JOIN dbo.GIAOVIEN gv ON tk.MAGV = gv.MAGV
    LEFT JOIN dbo.SINHVIEN sv ON tk.MASV = sv.MASV
    LEFT JOIN dbo.LOP l ON sv.MALOP = l.MALOP
    WHERE tk.TENDANGNHAP = @UserTrim
       OR (tk.QUYEN = N'SINHVIEN' AND RTRIM(tk.MASV) = UPPER(@UserTrim));
END;
GO

-- 7.2. Stored Procedure Thêm Môn Học (Nếu môn học từng bị xóa mềm thì cho phép khôi phục hoặc báo rõ)
CREATE PROCEDURE dbo.sp_ThemMonHoc
    @MaMH   NCHAR(5),
    @TenMH  NVARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaMH = UPPER(LTRIM(RTRIM(@MaMH)));
    SET @TenMH = LTRIM(RTRIM(@TenMH));

    IF LEN(@MaMH) = 0 OR LEN(@TenMH) = 0
    BEGIN
        RAISERROR(N'Mã môn học và Tên môn học không được để trống!', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.MONHOC WHERE RTRIM(MAMH) = RTRIM(@MaMH) AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Mã môn học [%s] đã tồn tại!', 16, 1, @MaMH);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.MONHOC WHERE RTRIM(MAMH) = RTRIM(@MaMH) AND DAXOA = 1)
    BEGIN
        RAISERROR(N'Mã môn học [%s] đang nằm trong danh sách đã xóa tạm. Vui lòng sử dụng chức năng Phục hồi!', 16, 1, @MaMH);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.MONHOC WHERE TENMH = @TenMH AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Tên môn học [%s] đã tồn tại!', 16, 1, @TenMH);
        RETURN;
    END

    INSERT INTO dbo.MONHOC (MAMH, TENMH, DAXOA)
    VALUES (@MaMH, @TenMH, 0);
END;
GO

-- 7.3. Stored Procedure Cập Nhật Môn Học
CREATE PROCEDURE dbo.sp_CapNhatMonHoc
    @MaMH   NCHAR(5),
    @TenMH  NVARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaMH = UPPER(LTRIM(RTRIM(@MaMH)));
    SET @TenMH = LTRIM(RTRIM(@TenMH));

    IF NOT EXISTS (SELECT 1 FROM dbo.MONHOC WHERE RTRIM(MAMH) = RTRIM(@MaMH) AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Không tìm thấy môn học [%s] để cập nhật!', 16, 1, @MaMH);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.MONHOC WHERE TENMH = @TenMH AND RTRIM(MAMH) <> RTRIM(@MaMH))
    BEGIN
        RAISERROR(N'Tên môn học [%s] đã bị trùng với môn học khác!', 16, 1, @TenMH);
        RETURN;
    END

    UPDATE dbo.MONHOC
    SET TENMH = @TenMH
    WHERE RTRIM(MAMH) = RTRIM(@MaMH);
END;
GO

-- 7.4. Stored Procedure Xóa Môn Học (Soft Delete có kiểm tra tham chiếu)
CREATE PROCEDURE dbo.sp_XoaMonHoc
    @MaMH   NCHAR(5)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaMH = UPPER(LTRIM(RTRIM(@MaMH)));

    IF NOT EXISTS (SELECT 1 FROM dbo.MONHOC WHERE RTRIM(MAMH) = RTRIM(@MaMH) AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Môn học [%s] không tồn tại hoặc đã bị xóa!', 16, 1, @MaMH);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.BODE WHERE RTRIM(MAMH) = RTRIM(@MaMH))
       OR EXISTS (SELECT 1 FROM dbo.GIAOVIEN_DANGKY WHERE RTRIM(MAMH) = RTRIM(@MaMH))
       OR EXISTS (SELECT 1 FROM dbo.BANGDIEM WHERE RTRIM(MAMH) = RTRIM(@MaMH))
    BEGIN
        RAISERROR(N'Không thể xóa môn học [%s] vì đã có dữ liệu thi (Bộ đề, Lịch đăng ký hoặc Bảng điểm) tham chiếu!', 16, 1, @MaMH);
        RETURN;
    END

    UPDATE dbo.MONHOC
    SET DAXOA = 1
    WHERE RTRIM(MAMH) = RTRIM(@MaMH);
END;
GO

-- 7.5. Stored Procedure Phục Hồi Môn Học đã xóa mềm
CREATE PROCEDURE dbo.sp_PhucHoiMonHoc
    @MaMH   NCHAR(5)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaMH = UPPER(LTRIM(RTRIM(@MaMH)));

    IF NOT EXISTS (SELECT 1 FROM dbo.MONHOC WHERE RTRIM(MAMH) = RTRIM(@MaMH) AND DAXOA = 1)
    BEGIN
        RAISERROR(N'Không tìm thấy môn học [%s] trong danh sách chờ phục hồi!', 16, 1, @MaMH);
        RETURN;
    END

    UPDATE dbo.MONHOC
    SET DAXOA = 0
    WHERE RTRIM(MAMH) = RTRIM(@MaMH);
END;
GO

-- 7.6. Stored Procedure Quản lý Lớp (Thêm, Cập nhật, Xóa)
CREATE PROCEDURE dbo.sp_ThemLop
    @MaLop  NCHAR(8),
    @TenLop NVARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaLop = UPPER(LTRIM(RTRIM(@MaLop)));
    SET @TenLop = LTRIM(RTRIM(@TenLop));

    IF EXISTS (SELECT 1 FROM dbo.LOP WHERE RTRIM(MALOP) = RTRIM(@MaLop))
    BEGIN
        RAISERROR(N'Mã lớp [%s] đã tồn tại!', 16, 1, @MaLop);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.LOP WHERE TENLOP = @TenLop)
    BEGIN
        RAISERROR(N'Tên lớp [%s] đã tồn tại!', 16, 1, @TenLop);
        RETURN;
    END

    INSERT INTO dbo.LOP (MALOP, TENLOP)
    VALUES (@MaLop, @TenLop);
END;
GO

CREATE PROCEDURE dbo.sp_CapNhatLop
    @MaLop  NCHAR(8),
    @TenLop NVARCHAR(40)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaLop = UPPER(LTRIM(RTRIM(@MaLop)));
    SET @TenLop = LTRIM(RTRIM(@TenLop));

    IF NOT EXISTS (SELECT 1 FROM dbo.LOP WHERE RTRIM(MALOP) = RTRIM(@MaLop))
    BEGIN
        RAISERROR(N'Không tìm thấy mã lớp [%s]!', 16, 1, @MaLop);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.LOP WHERE TENLOP = @TenLop AND RTRIM(MALOP) <> RTRIM(@MaLop))
    BEGIN
        RAISERROR(N'Tên lớp [%s] đã bị trùng với lớp khác!', 16, 1, @TenLop);
        RETURN;
    END

    UPDATE dbo.LOP
    SET TENLOP = @TenLop
    WHERE RTRIM(MALOP) = RTRIM(@MaLop);
END;
GO

CREATE PROCEDURE dbo.sp_XoaLop
    @MaLop  NCHAR(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaLop = UPPER(LTRIM(RTRIM(@MaLop)));

    IF EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MALOP) = RTRIM(@MaLop) AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Không thể xóa lớp [%s] vì lớp đang có sinh viên theo học!', 16, 1, @MaLop);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.GIAOVIEN_DANGKY WHERE RTRIM(MALOP) = RTRIM(@MaLop))
    BEGIN
        RAISERROR(N'Không thể xóa lớp [%s] vì lớp đã có lịch đăng ký thi!', 16, 1, @MaLop);
        RETURN;
    END

    -- Nếu có sinh viên đã xóa mềm thuộc lớp này mà chưa từng thi, dọn tài khoản và SV xóa mềm trước hoặc chặn xóa
    IF EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MALOP) = RTRIM(@MaLop))
    BEGIN
        RAISERROR(N'Không thể xóa lớp [%s] vì còn hồ sơ sinh viên (kể cả sinh viên chờ phục hồi) thuộc lớp này!', 16, 1, @MaLop);
        RETURN;
    END

    DELETE FROM dbo.LOP WHERE RTRIM(MALOP) = RTRIM(@MaLop);
END;
GO

-- 7.7. Stored Procedure Thêm Sinh Viên (Sử dụng TRANSACTION tự động cấp tài khoản Sinh viên)
CREATE PROCEDURE dbo.sp_ThemSinhVien
    @MaSV           NCHAR(8),
    @Ho             NVARCHAR(40),
    @Ten            NVARCHAR(10),
    @NgaySinh       DATETIME = NULL,
    @DiaChi         NVARCHAR(40) = NULL,
    @MaLop          NCHAR(8),
    @MatKhauMacDinh NVARCHAR(100) -- Chuỗi SHA-256 của mật khẩu mặc định (123456)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaSV = UPPER(LTRIM(RTRIM(@MaSV)));
    SET @Ho = LTRIM(RTRIM(@Ho));
    SET @Ten = LTRIM(RTRIM(@Ten));
    SET @MaLop = UPPER(LTRIM(RTRIM(@MaLop)));

    IF NOT EXISTS (SELECT 1 FROM dbo.LOP WHERE RTRIM(MALOP) = RTRIM(@MaLop))
    BEGIN
        RAISERROR(N'Lớp [%s] không tồn tại!', 16, 1, @MaLop);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = RTRIM(@MaSV) AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Mã sinh viên [%s] đã tồn tại!', 16, 1, @MaSV);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = RTRIM(@MaSV) AND DAXOA = 1)
    BEGIN
        RAISERROR(N'Mã sinh viên [%s] đang nằm trong danh sách đã xóa tạm. Vui lòng sử dụng chức năng Phục hồi!', 16, 1, @MaSV);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Bước 1: Thêm hồ sơ sinh viên vào bảng SINHVIEN
        INSERT INTO dbo.SINHVIEN (MASV, HO, TEN, NGAYSINH, DIACHI, MALOP, DAXOA)
        VALUES (@MaSV, @Ho, @Ten, @NgaySinh, @DiaChi, @MaLop, 0);

        -- Bước 2: Tự động khởi tạo tài khoản đăng nhập cho sinh viên (Login Name = MASV)
        IF NOT EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE TENDANGNHAP = RTRIM(@MaSV))
        BEGIN
            INSERT INTO dbo.TAIKHOAN (TENDANGNHAP, MATKHAU, MAGV, MASV, QUYEN, TRANGTHAI)
            VALUES (RTRIM(@MaSV), @MatKhauMacDinh, NULL, @MaSV, N'SINHVIEN', 1);
        END

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrMsg, 16, 1);
    END CATCH
END;
GO

-- 7.8. Stored Procedure Cập Nhật Sinh Viên
CREATE PROCEDURE dbo.sp_CapNhatSinhVien
    @MaSV       NCHAR(8),
    @Ho         NVARCHAR(40),
    @Ten        NVARCHAR(10),
    @NgaySinh   DATETIME = NULL,
    @DiaChi     NVARCHAR(40) = NULL,
    @MaLop      NCHAR(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaSV = UPPER(LTRIM(RTRIM(@MaSV)));
    SET @Ho = LTRIM(RTRIM(@Ho));
    SET @Ten = LTRIM(RTRIM(@Ten));
    SET @MaLop = UPPER(LTRIM(RTRIM(@MaLop)));

    IF NOT EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = RTRIM(@MaSV) AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Không tìm thấy sinh viên [%s]!', 16, 1, @MaSV);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.LOP WHERE RTRIM(MALOP) = RTRIM(@MaLop))
    BEGIN
        RAISERROR(N'Lớp chuyển đến [%s] không tồn tại!', 16, 1, @MaLop);
        RETURN;
    END

    UPDATE dbo.SINHVIEN
    SET HO = @Ho,
        TEN = @Ten,
        NGAYSINH = @NgaySinh,
        DIACHI = @DiaChi,
        MALOP = @MaLop
    WHERE RTRIM(MASV) = RTRIM(@MaSV);
END;
GO

-- 7.9. Stored Procedure Xóa Sinh Viên (Soft Delete + Khóa Tài Khoản qua Trigger/Transaction)
CREATE PROCEDURE dbo.sp_XoaSinhVien
    @MaSV   NCHAR(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaSV = UPPER(LTRIM(RTRIM(@MaSV)));

    IF NOT EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = RTRIM(@MaSV) AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Sinh viên [%s] không tồn tại hoặc đã bị xóa!', 16, 1, @MaSV);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.BANGDIEM WHERE RTRIM(MASV) = RTRIM(@MaSV))
    BEGIN
        RAISERROR(N'Không thể xóa sinh viên [%s] vì sinh viên đã có kết quả điểm thi!', 16, 1, @MaSV);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.SINHVIEN
        SET DAXOA = 1
        WHERE RTRIM(MASV) = RTRIM(@MaSV);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrMsg, 16, 1);
    END CATCH
END;
GO

-- 7.10. Stored Procedure Phục Hồi Sinh Viên
CREATE PROCEDURE dbo.sp_PhucHoiSinhVien
    @MaSV   NCHAR(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaSV = UPPER(LTRIM(RTRIM(@MaSV)));

    IF NOT EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = RTRIM(@MaSV) AND DAXOA = 1)
    BEGIN
        RAISERROR(N'Không tìm thấy sinh viên [%s] trong danh sách chờ phục hồi!', 16, 1, @MaSV);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        UPDATE dbo.SINHVIEN
        SET DAXOA = 0
        WHERE RTRIM(MASV) = RTRIM(@MaSV);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrMsg, 16, 1);
    END CATCH
END;
GO

-- 7.11. Stored Procedure Quản lý Giáo Viên (Thêm, Cập nhật, Xóa)
CREATE PROCEDURE dbo.sp_ThemGiaoVien
    @MaGV   NCHAR(8),
    @Ho     NVARCHAR(40),
    @Ten    NVARCHAR(10),
    @SoDTLL NCHAR(12) = NULL,
    @DiaChi NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaGV = UPPER(LTRIM(RTRIM(@MaGV)));
    SET @Ho = LTRIM(RTRIM(@Ho));
    SET @Ten = LTRIM(RTRIM(@Ten));

    IF EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = RTRIM(@MaGV))
    BEGIN
        RAISERROR(N'Mã giáo viên [%s] đã tồn tại!', 16, 1, @MaGV);
        RETURN;
    END

    INSERT INTO dbo.GIAOVIEN (MAGV, HO, TEN, SODTLL, DIACHI)
    VALUES (@MaGV, @Ho, @Ten, @SoDTLL, @DiaChi);
END;
GO

CREATE PROCEDURE dbo.sp_CapNhatGiaoVien
    @MaGV   NCHAR(8),
    @Ho     NVARCHAR(40),
    @Ten    NVARCHAR(10),
    @SoDTLL NCHAR(12) = NULL,
    @DiaChi NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaGV = UPPER(LTRIM(RTRIM(@MaGV)));
    SET @Ho = LTRIM(RTRIM(@Ho));
    SET @Ten = LTRIM(RTRIM(@Ten));

    IF NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = RTRIM(@MaGV))
    BEGIN
        RAISERROR(N'Không tìm thấy mã giáo viên [%s]!', 16, 1, @MaGV);
        RETURN;
    END

    UPDATE dbo.GIAOVIEN
    SET HO = @Ho,
        TEN = @Ten,
        SODTLL = @SoDTLL,
        DIACHI = @DiaChi
    WHERE RTRIM(MAGV) = RTRIM(@MaGV);
END;
GO

CREATE PROCEDURE dbo.sp_XoaGiaoVien
    @MaGV   NCHAR(8)
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaGV = UPPER(LTRIM(RTRIM(@MaGV)));

    IF EXISTS (SELECT 1 FROM dbo.BODE WHERE RTRIM(MAGV) = RTRIM(@MaGV))
    BEGIN
        RAISERROR(N'Không thể xóa giáo viên [%s] vì giáo viên đã biên soạn câu hỏi trong Bộ đề!', 16, 1, @MaGV);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.GIAOVIEN_DANGKY WHERE RTRIM(MAGV) = RTRIM(@MaGV))
    BEGIN
        RAISERROR(N'Không thể xóa giáo viên [%s] vì giáo viên đã có lịch đăng ký thi!', 16, 1, @MaGV);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE RTRIM(MAGV) = RTRIM(@MaGV))
    BEGIN
        RAISERROR(N'Không thể xóa giáo viên [%s] vì giáo viên đang có tài khoản đăng nhập liên kết!', 16, 1, @MaGV);
        RETURN;
    END

    DELETE FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = RTRIM(@MaGV);
END;
GO

-- 7.12. Stored Procedure Tạo Tài Khoản Giảng Viên (Transaction + Kiểm tra quyền & 1 GV - 1 TK)
CREATE PROCEDURE dbo.sp_TaoTaiKhoanGiaoVien
    @NguoiThucHien  NVARCHAR(50),
    @MaGV           NCHAR(8),
    @TenDangNhap    NVARCHAR(50),
    @MatKhauHash    NVARCHAR(100),
    @TrangThai      BIT = 1
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaGV = UPPER(LTRIM(RTRIM(@MaGV)));
    SET @TenDangNhap = LTRIM(RTRIM(@TenDangNhap));

    -- 1. Kiểm tra người thực hiện có phải là GIANGVIEN đang hoạt động không
    IF NOT EXISTS (
        SELECT 1 FROM dbo.TAIKHOAN
        WHERE TENDANGNHAP = LTRIM(RTRIM(@NguoiThucHien))
          AND QUYEN = N'GIANGVIEN'
          AND TRANGTHAI = 1
    )
    BEGIN
        RAISERROR(N'Truy cập bị từ chối: Chỉ giảng viên đang hoạt động mới có quyền tạo tài khoản!', 16, 1);
        RETURN;
    END

    -- 2. Kiểm tra giảng viên có tồn tại trong bảng GIAOVIEN không
    IF NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = RTRIM(@MaGV))
    BEGIN
        RAISERROR(N'Mã giảng viên [%s] không tồn tại trong hệ thống!', 16, 1, @MaGV);
        RETURN;
    END

    -- 3. Kiểm tra mỗi giảng viên chỉ có duy nhất 1 tài khoản
    IF EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE RTRIM(MAGV) = RTRIM(@MaGV))
    BEGIN
        RAISERROR(N'Giảng viên [%s] đã được cấp tài khoản trước đó. Mỗi giảng viên chỉ có một tài khoản!', 16, 1, @MaGV);
        RETURN;
    END

    -- 4. Kiểm tra trùng tên đăng nhập (hoặc trùng với mã sinh viên)
    IF EXISTS (SELECT 1 FROM dbo.TAIKHOAN WHERE TENDANGNHAP = @TenDangNhap)
       OR EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = UPPER(@TenDangNhap))
    BEGIN
        RAISERROR(N'Tên đăng nhập [%s] đã tồn tại hoặc trùng với mã sinh viên!', 16, 1, @TenDangNhap);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.TAIKHOAN (TENDANGNHAP, MATKHAU, MAGV, MASV, QUYEN, TRANGTHAI)
        VALUES (@TenDangNhap, @MatKhauHash, @MaGV, NULL, N'GIANGVIEN', @TrangThai);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;

        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(@ErrMsg, 16, 1);
    END CATCH
END;
GO

-- 7.13. Stored Procedure Đổi Mật Khẩu (Chỉ dành cho giảng viên đã có tài khoản)
CREATE PROCEDURE dbo.sp_DoiMatKhau
    @TenDangNhap        NVARCHAR(50),
    @MatKhauHienTaiHash NVARCHAR(100),
    @MatKhauMoiHash     NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;
    SET @TenDangNhap = LTRIM(RTRIM(@TenDangNhap));

    DECLARE @CurrentHash NVARCHAR(100);
    DECLARE @Quyen NVARCHAR(20);
    DECLARE @TrangThai BIT;

    SELECT
        @CurrentHash = MATKHAU,
        @Quyen = QUYEN,
        @TrangThai = TRANGTHAI
    FROM dbo.TAIKHOAN
    WHERE TENDANGNHAP = @TenDangNhap;

    IF @CurrentHash IS NULL
    BEGIN
        RAISERROR(N'Tài khoản không tồn tại!', 16, 1);
        RETURN;
    END

    IF @Quyen <> N'GIANGVIEN'
    BEGIN
        RAISERROR(N'Theo quy định hệ thống, chức năng đổi mật khẩu chỉ dành cho giảng viên!', 16, 1);
        RETURN;
    END

    IF @TrangThai = 0
    BEGIN
        RAISERROR(N'Tài khoản đang bị khóa, không thể đổi mật khẩu!', 16, 1);
        RETURN;
    END

    IF @CurrentHash <> @MatKhauHienTaiHash
    BEGIN
        RAISERROR(N'Mật khẩu hiện tại không chính xác!', 16, 1);
        RETURN;
    END

    IF @CurrentHash = @MatKhauMoiHash
    BEGIN
        RAISERROR(N'Mật khẩu mới không được trùng với mật khẩu hiện tại!', 16, 1);
        RETURN;
    END

    UPDATE dbo.TAIKHOAN
    SET MATKHAU = @MatKhauMoiHash
    WHERE TENDANGNHAP = @TenDangNhap;
END;
GO

/*==============================================================================
  PHẦN 8: NẠP DỮ LIỆU MẪU CHUẨN NHẤT QUÁN (MẬT KHẨU ĐÃ BĂM SHA-256)
  Lưu ý: Chuỗi SHA-256 của '123456' là:
  8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92
==============================================================================*/

DECLARE @PwdHash123456 NVARCHAR(100) = CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', '123456'), 2);

-- 8.1. Dữ liệu bảng LOP (4 lớp)
INSERT INTO dbo.LOP (MALOP, TENLOP) VALUES
(N'CNTT01  ', N'Công nghệ thông tin 01'),
(N'CNTT02  ', N'Công nghệ thông tin 02'),
(N'ATTT01  ', N'An toàn thông tin 01'),
(N'KTPM01  ', N'Kỹ thuật phần mềm 01'); -- Lớp trống chưa có SV để test xóa Lớp

-- 8.2. Dữ liệu bảng MONHOC (5 môn học)
INSERT INTO dbo.MONHOC (MAMH, TENMH, DAXOA) VALUES
(N'CSDL ', N'Cơ sở dữ liệu', 0),
(N'CTDL ', N'Cấu trúc dữ liệu và giải thuật', 0),
(N'MMT  ', N'Mạng máy tính', 0),
(N'LTHDT', N'Lập trình hướng đối tượng', 0),
(N'THDC ', N'Tin học đại cương', 0); -- Môn chưa có đề thi để test sửa/xóa/phục hồi

-- 8.3. Dữ liệu bảng GIAOVIEN (4 giảng viên)
INSERT INTO dbo.GIAOVIEN (MAGV, HO, TEN, SODTLL, DIACHI) VALUES
(N'GV001   ', N'Nguyễn Văn', N'An',   N'0901234567  ', N'Khoa CNTT1 - PTIT Hà Nội'),
(N'GV002   ', N'Trần Thị',   N'Bình', N'0912345678  ', N'Khoa CNTT2 - PTIT TP.HCM'),
(N'GV003   ', N'Lê Hoàng',   N'Cường',N'0987654321  ', N'Khoa An toàn thông tin - PTIT'),
(N'GV004   ', N'Phạm Minh',  N'Đức',  N'0977889900  ', N'Bộ môn Công nghệ phần mềm - PTIT'); -- GV003, GV004 chưa có tài khoản để test Tạo Tài Khoản

-- 8.4. Dữ liệu bảng SINHVIEN (6 sinh viên)
INSERT INTO dbo.SINHVIEN (MASV, HO, TEN, NGAYSINH, DIACHI, MALOP, DAXOA) VALUES
(N'SV001   ', N'Lê Văn',       N'Nam',   '2004-05-12', N'Quận 9, TP. Thủ Đức', N'CNTT01  ', 0),
(N'SV002   ', N'Phạm Thị',     N'Hoa',   '2004-08-20', N'Quận Bình Thạnh, TP.HCM', N'CNTT01  ', 0),
(N'SV003   ', N'Hoàng Minh',   N'Tuấn',  '2004-02-15', N'Quận 1, TP.HCM', N'CNTT01  ', 0),
(N'SV004   ', N'Đỗ Ngọc',      N'Lan',   '2004-11-03', N'TP. Biên Hòa, Đồng Nai', N'CNTT02  ', 0),
(N'SV005   ', N'Vũ Quốc',      N'Khánh', '2004-09-02', N'TP. Dĩ An, Bình Dương', N'CNTT02  ', 0),
(N'SV006   ', N'Ngô Gia',      N'Bảo',   '2004-04-30', N'Quận Tân Bình, TP.HCM', N'ATTT01  ', 0);

-- 8.5. Dữ liệu bảng TAIKHOAN (Mật khẩu mặc định đều là '123456' đã băm SHA-256)
-- Tài khoản giảng viên: admin (GV001), gv002 (GV002)
INSERT INTO dbo.TAIKHOAN (TENDANGNHAP, MATKHAU, MAGV, MASV, QUYEN, TRANGTHAI) VALUES
(N'admin', @PwdHash123456, N'GV001   ', NULL, N'GIANGVIEN', 1),
(N'gv002', @PwdHash123456, N'GV002   ', NULL, N'GIANGVIEN', 1);

-- Tài khoản sinh viên (Login Name = MASV)
INSERT INTO dbo.TAIKHOAN (TENDANGNHAP, MATKHAU, MAGV, MASV, QUYEN, TRANGTHAI) VALUES
(N'SV001', @PwdHash123456, NULL, N'SV001   ', N'SINHVIEN', 1),
(N'SV002', @PwdHash123456, NULL, N'SV002   ', N'SINHVIEN', 1),
(N'SV003', @PwdHash123456, NULL, N'SV003   ', N'SINHVIEN', 1),
(N'SV004', @PwdHash123456, NULL, N'SV004   ', N'SINHVIEN', 1),
(N'SV005', @PwdHash123456, NULL, N'SV005   ', N'SINHVIEN', 1),
(N'SV006', @PwdHash123456, NULL, N'SV006   ', N'SINHVIEN', 1);

-- 8.6. Dữ liệu mẫu bảng BODE (12 câu hỏi môn CSDL và CTDL để tích hợp Người 2)
INSERT INTO dbo.BODE (MAMH, TRINHDO, NOIDUNG, A, B, C, D, DAP_AN, MAGV) VALUES
(N'CSDL ', N'A', N'Khóa chính (Primary Key) trong bảng quan hệ có đặc điểm gì?', N'Có thể nhận giá trị NULL', N'Xác định duy nhất một bộ và không được NULL', N'Có thể trùng lặp dữ liệu', N'Chỉ dùng cho kiểu số nguyên', N'B', N'GV001   '),
(N'CSDL ', N'A', N'Câu lệnh SQL nào dùng để trích xuất dữ liệu từ bảng?', N'SELECT', N'UPDATE', N'DELETE', N'INSERT', N'A', N'GV001   '),
(N'CSDL ', N'A', N'Ràng buộc FOREIGN KEY dùng để đảm bảo tính chất nào?', N'Toàn vẹn miền giá trị', N'Toàn vẹn thực thể', N'Toàn vẹn tham chiếu', N'Toàn vẹn giao tác', N'C', N'GV001   '),
(N'CSDL ', N'A', N'Một lược đồ quan hệ đạt chuẩn 3NF khi nào?', N'Chứa thuộc tính đa trị', N'Đạt 2NF và không có phụ thuộc bắc cầu vào khóa chính', N'Không có khóa chính', N'Có ít nhất 2 khóa ngoại', N'B', N'GV001   '),
(N'CSDL ', N'A', N'Từ khóa nào trong SQL dùng để loại bỏ các dòng trùng nhau trong kết quả SELECT?', N'UNIQUE', N'GROUP', N'DISTINCT', N'DIFFERENT', N'C', N'GV001   '),
(N'CSDL ', N'A', N'Trong thuộc tính ACID của Transaction, chữ A viết tắt của từ gì?', N'Availability', N'Atomicity', N'Authorization', N'Authentication', N'B', N'GV001   '),
(N'CSDL ', N'A', N'Lệnh nào dùng để thu hồi toàn bộ thay đổi trong một Transaction chưa Commit?', N'ROLLBACK', N'COMMIT', N'RETURN', N'SAVEPOINT', N'A', N'GV001   '),
(N'CSDL ', N'B', N'Hàm tổng hợp nào trả về số lượng dòng thỏa mãn điều kiện?', N'SUM()', N'AVG()', N'COUNT()', N'MAX()', N'C', N'GV002   '),
(N'CSDL ', N'B', N'Mệnh đề nào dùng để lọc dữ liệu sau khi đã GROUP BY?', N'WHERE', N'HAVING', N'ORDER BY', N'TOP', N'B', N'GV002   '),
(N'CSDL ', N'B', N'Lệnh BACKUP DATABASE trong SQL Server tạo ra file có phần mở rộng phổ biến là gì?', N'.mdf', N'.ldf', N'.bak', N'.sql', N'C', N'GV002   '),
(N'CTDL ', N'A', N'Cấu trúc dữ liệu Ngăn xếp (Stack) hoạt động theo nguyên tắc nào?', N'FIFO', N'LIFO', N'LILO', N'Random', N'B', N'GV002   '),
(N'CTDL ', N'A', N'Độ phức tạp thời gian trung bình của thuật toán Tìm kiếm nhị phân (Binary Search) là?', N'O(1)', N'O(log n)', N'O(n)', N'O(n^2)', N'B', N'GV002   ');

-- 8.7. Dữ liệu mẫu bảng GIAOVIEN_DANGKY
INSERT INTO dbo.GIAOVIEN_DANGKY (MAGV, MALOP, MAMH, TRINHDO, NGAYTHI, LAN, SOCAUTHI, THOIGIAN) VALUES
(N'GV001   ', N'CNTT01  ', N'CSDL ', N'A', GETDATE(), 1, 10, 15);

-- 8.8. Dữ liệu mẫu bảng BANGDIEM
INSERT INTO dbo.BANGDIEM (MASV, MAMH, LAN, NGAYTHI, DIEM, BAITHI) VALUES
(N'SV001   ', N'CSDL ', 1, GETDATE(), 9.0, N'1:B;2:A;3:C;4:B;5:C;6:B;7:A;8:C;9:B;10:A'),
(N'SV002   ', N'CSDL ', 1, GETDATE(), 8.0, N'1:B;2:A;3:C;4:B;5:C;6:B;7:A;8:A;9:B;10:B');
GO

PRINT N'=== ĐÃ KHỞI TẠO THÀNH CÔNG CƠ SỞ DỮ LIỆU TRAC_NGHIEM MỚI ===';
GO
