/*==============================================================================
  HỆ THỐNG QUẢN LÝ THI TRẮC NGHIỆM PTIT (TRAC_NGHIEM)
  FILE: 01_Setup_TRAC_NGHIEM.sql
  PHỤ TRÁCH: NGƯỜI 1 – QUẢN TRỊ VÀ DỮ LIỆU NỀN
  MÔ TẢ:
    1. Tạo mới cơ sở dữ liệu TRAC_NGHIEM (hoặc cập nhật nếu chạy lại).
    2. Tạo đúng 4 bảng nền của Người 1: LOP, MONHOC, SINHVIEN, GIAOVIEN.
    3. Tạo đúng 3 bảng nghiệp vụ thi của Người 2: BODE, GIAOVIEN_DANGKY, BANGDIEM.
       (TUYỆT ĐỐI KHÔNG SỬ DỤNG BẢNG TAIKHOAN HAY VAITRO).
    4. Thiết lập đầy đủ PRIMARY KEY, FOREIGN KEY, UNIQUE, CHECK, DEFAULT, INDEX.
    5. Tạo Database Role: role_GiaoVien, role_SinhVien và phân quyền tối thiểu.
    6. Tạo các VIEW, TRIGGER và STORED PROCEDURE bảo mật, chống SQL Injection.
    7. Thiết lập Certificate Signing (cert_QuanLyTaiKhoan_TracNghiem) cho các
       Stored Procedure quản trị Login/User mà không cần quyền sa/sysadmin ở ứng dụng.
    8. Nạp dữ liệu mẫu và khởi tạo SQL Server Login + Database User cho
       Giáo viên (GV001, GV002, GV-8-4) và Sinh viên (SV001..SV006).
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
  PHẦN 2: TẠO CÁC BẢNG DỮ LIỆU NỀN (NGƯỜI 1 QUẢN LÝ) - KHÔNG CÓ BẢNG TAIKHOAN
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
  PHẦN 4: THIẾT LẬP INDEX & DATABASE ROLE
==============================================================================*/

CREATE NONCLUSTERED INDEX IX_SinhVien_MaLop_DaXoa
ON dbo.SINHVIEN (MALOP, DAXOA)
INCLUDE (HO, TEN, NGAYSINH, DIACHI);
GO

CREATE NONCLUSTERED INDEX IX_SinhVien_Ten_Ho
ON dbo.SINHVIEN (TEN, HO);
GO

CREATE NONCLUSTERED INDEX IX_GiaoVien_Ten_Ho
ON dbo.GIAOVIEN (TEN, HO);
GO

CREATE NONCLUSTERED INDEX IX_BODE_MaMH_TrinhDo
ON dbo.BODE (MAMH, TRINHDO);
GO

-- Tạo 2 Database Role chuẩn theo đề
CREATE ROLE [role_GiaoVien];
CREATE ROLE [role_SinhVien];
GO

/*==============================================================================
  PHẦN 5: TẠO CÁC VIEW PHỤC VỤ TRUY VẤN & BÁO CÁO
==============================================================================*/

-- 5.1. View Danh sách môn học
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

-- 5.3. View Danh sách sinh viên theo lớp (Đối chiếu trạng thái SQL Login)
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
    CASE
        WHEN sp.principal_id IS NOT NULL AND sp.is_disabled = 0 AND dp.principal_id IS NOT NULL THEN 1
        ELSE 0
    END AS TRANGTHAI_TK,
    (SELECT COUNT(*) FROM dbo.BANGDIEM bd WHERE bd.MASV = sv.MASV) AS SO_LAN_THI
FROM dbo.SINHVIEN sv
INNER JOIN dbo.LOP l ON sv.MALOP = l.MALOP
LEFT JOIN sys.server_principals sp
    ON sp.name = RTRIM(sv.MASV) COLLATE DATABASE_DEFAULT
   AND sp.type = 'S'
LEFT JOIN sys.database_principals dp
    ON dp.sid = sp.sid
   AND dp.type = 'S'
   AND dp.name = RTRIM(sv.MASV) COLLATE DATABASE_DEFAULT
WHERE sv.DAXOA = 0;
GO

-- 5.4. View Danh sách giáo viên kèm trạng thái SQL Server Login
CREATE VIEW dbo.vw_DanhSachGiaoVien
AS
SELECT
    RTRIM(gv.MAGV) AS MAGV,
    gv.HO,
    gv.TEN,
    (gv.HO + N' ' + gv.TEN) AS HOTEN,
    RTRIM(ISNULL(gv.SODTLL, N'')) AS SODTLL,
    ISNULL(gv.DIACHI, N'') AS DIACHI,
    CASE
        WHEN sp.principal_id IS NOT NULL
         AND dp.principal_id IS NOT NULL
         AND EXISTS (
             SELECT 1
             FROM sys.database_role_members drm
             INNER JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
             WHERE drm.member_principal_id = dp.principal_id
               AND r.name = N'role_GiaoVien'
         )
        THEN sp.name
        ELSE NULL
    END AS TENDANGNHAP,
    CASE
        WHEN sp.principal_id IS NOT NULL
         AND dp.principal_id IS NOT NULL
         AND EXISTS (
             SELECT 1
             FROM sys.database_role_members drm
             INNER JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
             WHERE drm.member_principal_id = dp.principal_id
               AND r.name = N'role_GiaoVien'
         )
        THEN 1
        ELSE 0
    END AS CO_TAI_KHOAN,
    CASE
        WHEN sp.principal_id IS NOT NULL
         AND dp.principal_id IS NOT NULL
         AND sp.is_disabled = 0
         AND EXISTS (
             SELECT 1
             FROM sys.database_role_members drm
             INNER JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
             WHERE drm.member_principal_id = dp.principal_id
               AND r.name = N'role_GiaoVien'
         )
        THEN 1
        ELSE 0
    END AS TRANGTHAI_TK,
    CASE
        WHEN sp.principal_id IS NULL OR dp.principal_id IS NULL
          OR NOT EXISTS (
             SELECT 1
             FROM sys.database_role_members drm
             INNER JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
             WHERE drm.member_principal_id = dp.principal_id
               AND r.name = N'role_GiaoVien'
          )
        THEN N'Chưa tạo'
        WHEN sp.is_disabled = 1 THEN N'Đã vô hiệu hóa'
        ELSE N'Đang hoạt động'
    END AS TRANGTHAI_MOTA,
    (SELECT COUNT(*) FROM dbo.BODE b WHERE b.MAGV = gv.MAGV) AS SO_CAU_DA_SOAN,
    (SELECT COUNT(*) FROM dbo.GIAOVIEN_DANGKY dk WHERE dk.MAGV = gv.MAGV) AS SO_LICH_DANG_KY
FROM dbo.GIAOVIEN gv
LEFT JOIN sys.server_principals sp
    ON sp.name = RTRIM(gv.MAGV) COLLATE DATABASE_DEFAULT
   AND sp.type = 'S'
LEFT JOIN sys.database_principals dp
    ON dp.sid = sp.sid
   AND dp.type = 'S'
   AND dp.name = RTRIM(gv.MAGV) COLLATE DATABASE_DEFAULT;
GO

/*==============================================================================
  PHẦN 6: TẠO CÁC TRIGGER BẢO VỆ DỮ LIỆU CÓ LỊCH SỬ THI
==============================================================================*/

CREATE TRIGGER dbo.trg_MonHoc_ProtectHistory
ON dbo.MONHOC
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

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

CREATE TRIGGER dbo.trg_SinhVien_ProtectAndSyncAccount
ON dbo.SINHVIEN
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;

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
END;
GO

/*==============================================================================
  PHẦN 7: TẠO CÁC STORED PROCEDURE NGHIỆP VỤ & QUẢN TRỊ SQL SERVER LOGIN
==============================================================================*/

-- 7.1. Stored Procedure lấy thông tin phiên đăng nhập từ SQL Server Login hiện tại
CREATE PROCEDURE dbo.sp_LayThongTinDangNhapHienTai
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LoginName NVARCHAR(128) = LTRIM(RTRIM(SUSER_SNAME()));
    DECLARE @DbUserName NVARCHAR(128) = LTRIM(RTRIM(USER_NAME()));
    DECLARE @IsRoleGV BIT = CASE WHEN IS_MEMBER(N'role_GiaoVien') = 1 THEN 1 ELSE 0 END;
    DECLARE @IsRoleSV BIT = CASE WHEN IS_MEMBER(N'role_SinhVien') = 1 THEN 1 ELSE 0 END;

    IF EXISTS (
        SELECT 1 FROM sys.server_principals
        WHERE name = @LoginName COLLATE DATABASE_DEFAULT
          AND type = 'S'
          AND is_disabled = 1
    )
    BEGIN
        RAISERROR(N'Tài khoản SQL Server Login [%s] đã bị thu hồi (vô hiệu hóa)!', 16, 1, @LoginName);
        RETURN;
    END

    IF @DbUserName IS NULL OR @DbUserName = N'guest'
    BEGIN
        RAISERROR(N'Tài khoản SQL Login [%s] chưa được ánh xạ với Database User trong TRAC_NGHIEM!', 16, 1, @LoginName);
        RETURN;
    END

    IF @IsRoleGV = 1
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = UPPER(@LoginName))
        BEGIN
            RAISERROR(N'Tài khoản SQL Login [%s] thuộc role_GiaoVien nhưng không khớp với bất kỳ MAGV nào trong bảng GIAOVIEN!', 16, 1, @LoginName);
            RETURN;
        END

        SELECT
            RTRIM(gv.MAGV) AS TENDANGNHAP,
            @DbUserName AS DB_USERNAME,
            N'GIANGVIEN' AS QUYEN,
            N'role_GiaoVien' AS ROLE_NAME,
            CAST(1 AS BIT) AS TRANGTHAI,
            RTRIM(gv.MAGV) AS MAGV,
            N'' AS MASV,
            (gv.HO + N' ' + gv.TEN) AS HOTEN,
            N'' AS MALOP,
            N'' AS TENLOP,
            CAST(0 AS BIT) AS SV_DAXOA
        FROM dbo.GIAOVIEN gv
        WHERE RTRIM(gv.MAGV) = UPPER(@LoginName);
        RETURN;
    END

    IF @IsRoleSV = 1
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = UPPER(@LoginName))
        BEGIN
            RAISERROR(N'Tài khoản SQL Login [%s] thuộc role_SinhVien nhưng không khớp với bất kỳ MASV nào trong bảng SINHVIEN!', 16, 1, @LoginName);
            RETURN;
        END

        IF EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = UPPER(@LoginName) AND DAXOA = 1)
        BEGIN
            RAISERROR(N'Hồ sơ sinh viên [%s] hiện đã ngừng hoạt động, không được phép đăng nhập!', 16, 1, @LoginName);
            RETURN;
        END

        SELECT
            RTRIM(sv.MASV) AS TENDANGNHAP,
            @DbUserName AS DB_USERNAME,
            N'SINHVIEN' AS QUYEN,
            N'role_SinhVien' AS ROLE_NAME,
            CAST(1 AS BIT) AS TRANGTHAI,
            N'' AS MAGV,
            RTRIM(sv.MASV) AS MASV,
            (sv.HO + N' ' + sv.TEN) AS HOTEN,
            RTRIM(sv.MALOP) AS MALOP,
            ISNULL(l.TENLOP, N'') AS TENLOP,
            sv.DAXOA AS SV_DAXOA
        FROM dbo.SINHVIEN sv
        INNER JOIN dbo.LOP l ON sv.MALOP = l.MALOP
        WHERE RTRIM(sv.MASV) = UPPER(@LoginName);
        RETURN;
    END

    RAISERROR(N'Tài khoản SQL Login [%s] không được phân quyền trong role_GiaoVien hoặc role_SinhVien!', 16, 1, @LoginName);
END;
GO

-- 7.2. Stored Procedure Quản lý Môn Học
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

-- 7.3. Stored Procedure Quản lý Lớp
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

    IF EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MALOP) = RTRIM(@MaLop))
    BEGIN
        RAISERROR(N'Không thể xóa lớp [%s] vì còn hồ sơ sinh viên (kể cả sinh viên chờ phục hồi) thuộc lớp này!', 16, 1, @MaLop);
        RETURN;
    END

    DELETE FROM dbo.LOP WHERE RTRIM(MALOP) = RTRIM(@MaLop);
END;
GO

-- 7.4. Stored Procedure Quản lý Sinh Viên & Cấp SQL Login cho Sinh viên
CREATE PROCEDURE dbo.sp_CapTaiKhoanSinhVien
    @MaSV           NCHAR(8),
    @MatKhauPlain   NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    IF IS_MEMBER(N'role_GiaoVien') <> 1 AND IS_SRVROLEMEMBER(N'sysadmin') <> 1
    BEGIN
        RAISERROR(N'Từ chối truy cập: Chỉ quản trị / giáo viên mới được cấp SQL Login cho sinh viên!', 16, 1);
        RETURN;
    END

    DECLARE @MaSVClean NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaSV, N''))));
    IF LEN(@MaSVClean) = 0 OR LEN(@MaSVClean) > 8 OR @MaSVClean LIKE N'%[^A-Z0-9_-]%'
    BEGIN
        RAISERROR(N'Mã sinh viên (MASV) không hợp lệ!', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = @MaSVClean AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Sinh viên [%s] không tồn tại hoặc đã ngừng hoạt động!', 16, 1, @MaSVClean);
        RETURN;
    END

    IF @MatKhauPlain IS NULL OR LEN(@MatKhauPlain) < 6
    BEGIN
        RAISERROR(N'Mật khẩu sinh viên phải từ 6 ký tự trở lên!', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @MaSVClean COLLATE DATABASE_DEFAULT)
    BEGIN
        DECLARE @SqlLogin NVARCHAR(MAX) = N'CREATE LOGIN ' + QUOTENAME(@MaSVClean)
            + N' WITH PASSWORD = ' + QUOTENAME(@MatKhauPlain, '''')
            + N', DEFAULT_DATABASE = [TRAC_NGHIEM], CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
        EXEC sys.sp_executesql @SqlLogin;
    END
    ELSE
    BEGIN
        DECLARE @SqlEnable NVARCHAR(MAX) = N'ALTER LOGIN ' + QUOTENAME(@MaSVClean) + N' ENABLE;';
        EXEC sys.sp_executesql @SqlEnable;
    END

    IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @MaSVClean COLLATE DATABASE_DEFAULT AND type = 'S')
    BEGIN
        DECLARE @SqlUser NVARCHAR(MAX) = N'CREATE USER ' + QUOTENAME(@MaSVClean) + N' FOR LOGIN ' + QUOTENAME(@MaSVClean) + N';';
        EXEC sys.sp_executesql @SqlUser;
    END

    IF NOT EXISTS (
        SELECT 1
        FROM sys.database_role_members drm
        INNER JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
        INNER JOIN sys.database_principals u ON drm.member_principal_id = u.principal_id
        WHERE r.name = N'role_SinhVien'
          AND u.name = @MaSVClean COLLATE DATABASE_DEFAULT
    )
    BEGIN
        DECLARE @SqlRole NVARCHAR(MAX) = N'ALTER ROLE [role_SinhVien] ADD MEMBER ' + QUOTENAME(@MaSVClean) + N';';
        EXEC sys.sp_executesql @SqlRole;
    END
END;
GO

CREATE PROCEDURE dbo.sp_ThemSinhVien
    @MaSV           NCHAR(8),
    @Ho             NVARCHAR(40),
    @Ten            NVARCHAR(10),
    @NgaySinh       DATETIME = NULL,
    @DiaChi         NVARCHAR(40) = NULL,
    @MaLop          NCHAR(8),
    @MatKhauMacDinh NVARCHAR(100) = N'123456'
AS
BEGIN
    SET NOCOUNT ON;
    SET @MaSV = UPPER(LTRIM(RTRIM(@MaSV)));
    SET @Ho = LTRIM(RTRIM(@Ho));
    SET @Ten = LTRIM(RTRIM(@Ten));
    SET @MaLop = UPPER(LTRIM(RTRIM(@MaLop)));

    DECLARE @MaSVClean NVARCHAR(8) = RTRIM(@MaSV);
    IF LEN(@MaSVClean) = 0 OR @MaSVClean LIKE N'%[^A-Z0-9_-]%'
    BEGIN
        RAISERROR(N'Mã sinh viên [%s] không hợp lệ!', 16, 1, @MaSVClean);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.LOP WHERE RTRIM(MALOP) = RTRIM(@MaLop))
    BEGIN
        RAISERROR(N'Lớp [%s] không tồn tại!', 16, 1, @MaLop);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = @MaSVClean AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Mã sinh viên [%s] đã tồn tại!', 16, 1, @MaSVClean);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = @MaSVClean AND DAXOA = 1)
    BEGIN
        RAISERROR(N'Mã sinh viên [%s] đang nằm trong danh sách đã xóa tạm. Vui lòng sử dụng chức năng Phục hồi!', 16, 1, @MaSVClean);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = @MaSVClean)
    BEGIN
        RAISERROR(N'Mã [%s] trùng với mã giáo viên trong hệ thống!', 16, 1, @MaSVClean);
        RETURN;
    END

    BEGIN TRY
        INSERT INTO dbo.SINHVIEN (MASV, HO, TEN, NGAYSINH, DIACHI, MALOP, DAXOA)
        VALUES (@MaSV, @Ho, @Ten, @NgaySinh, @DiaChi, @MaLop, 0);

        DECLARE @PwdPlain NVARCHAR(128) = CASE
            WHEN @MatKhauMacDinh IS NULL OR LEN(LTRIM(RTRIM(@MatKhauMacDinh))) < 6 OR LEN(@MatKhauMacDinh) = 64
            THEN N'123456'
            ELSE LTRIM(RTRIM(@MatKhauMacDinh))
        END;

        EXEC dbo.sp_CapTaiKhoanSinhVien @MaSV = @MaSV, @MatKhauPlain = @PwdPlain;
    END TRY
    BEGIN CATCH
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        IF EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = @MaSVClean)
           AND NOT EXISTS (SELECT 1 FROM dbo.BANGDIEM WHERE RTRIM(MASV) = @MaSVClean)
        BEGIN
            DELETE FROM dbo.SINHVIEN WHERE RTRIM(MASV) = @MaSVClean;
        END
        RAISERROR(@ErrMsg, 16, 1);
    END CATCH
END;
GO

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

CREATE PROCEDURE dbo.sp_XoaSinhVien
    @MaSV   NCHAR(8)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MaSVClean NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaSV, N''))));

    IF LEN(@MaSVClean) = 0 OR @MaSVClean LIKE N'%[^A-Z0-9_-]%'
    BEGIN
        RAISERROR(N'Mã sinh viên không hợp lệ!', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = @MaSVClean AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Sinh viên [%s] không tồn tại hoặc đã bị xóa!', 16, 1, @MaSVClean);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.BANGDIEM WHERE RTRIM(MASV) = @MaSVClean)
    BEGIN
        RAISERROR(N'Không thể xóa sinh viên [%s] vì sinh viên đã có kết quả điểm thi!', 16, 1, @MaSVClean);
        RETURN;
    END

    UPDATE dbo.SINHVIEN
    SET DAXOA = 1
    WHERE RTRIM(MASV) = @MaSVClean;

    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @MaSVClean COLLATE DATABASE_DEFAULT AND type = 'S' AND is_disabled = 0)
    BEGIN
        DECLARE @SqlDisableSV NVARCHAR(MAX) = N'ALTER LOGIN ' + QUOTENAME(@MaSVClean) + N' DISABLE;';
        EXEC sys.sp_executesql @SqlDisableSV;
    END
END;
GO

CREATE PROCEDURE dbo.sp_PhucHoiSinhVien
    @MaSV   NCHAR(8)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MaSVClean NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaSV, N''))));

    IF LEN(@MaSVClean) = 0 OR @MaSVClean LIKE N'%[^A-Z0-9_-]%'
    BEGIN
        RAISERROR(N'Mã sinh viên không hợp lệ!', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = @MaSVClean AND DAXOA = 1)
    BEGIN
        RAISERROR(N'Không tìm thấy sinh viên [%s] trong danh sách chờ phục hồi!', 16, 1, @MaSVClean);
        RETURN;
    END

    UPDATE dbo.SINHVIEN
    SET DAXOA = 0
    WHERE RTRIM(MASV) = @MaSVClean;

    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @MaSVClean COLLATE DATABASE_DEFAULT AND type = 'S' AND is_disabled = 1)
    BEGIN
        DECLARE @SqlEnableSV NVARCHAR(MAX) = N'ALTER LOGIN ' + QUOTENAME(@MaSVClean) + N' ENABLE;';
        EXEC sys.sp_executesql @SqlEnableSV;
    END
END;
GO

CREATE PROCEDURE dbo.sp_HoanTacThemSinhVien
    @MaSV   NCHAR(8)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MaSVClean NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaSV, N''))));

    IF LEN(@MaSVClean) = 0 OR @MaSVClean LIKE N'%[^A-Z0-9_-]%'
        RETURN;

    IF NOT EXISTS (SELECT 1 FROM dbo.BANGDIEM WHERE RTRIM(MASV) = @MaSVClean)
    BEGIN
        IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @MaSVClean COLLATE DATABASE_DEFAULT AND type = 'S')
        BEGIN
            DECLARE @SqlDropUser NVARCHAR(MAX) = N'DROP USER ' + QUOTENAME(@MaSVClean) + N';';
            BEGIN TRY EXEC sys.sp_executesql @SqlDropUser; END TRY BEGIN CATCH END CATCH
        END

        IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @MaSVClean COLLATE DATABASE_DEFAULT AND type = 'S')
        BEGIN
            DECLARE @SqlDropLogin NVARCHAR(MAX) = N'DROP LOGIN ' + QUOTENAME(@MaSVClean) + N';';
            BEGIN TRY EXEC sys.sp_executesql @SqlDropLogin; END TRY BEGIN CATCH END CATCH
        END

        DELETE FROM dbo.SINHVIEN WHERE RTRIM(MASV) = @MaSVClean;
    END
END;
GO

-- 7.5. Stored Procedure Quản lý Giáo Viên
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
    DECLARE @MaGVClean NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaGV, N''))));

    IF LEN(@MaGVClean) = 0 OR @MaGVClean LIKE N'%[^A-Z0-9_-]%'
    BEGIN
        RAISERROR(N'Mã giáo viên không hợp lệ!', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = @MaGVClean)
    BEGIN
        RAISERROR(N'Không tìm thấy mã giáo viên [%s]!', 16, 1, @MaGVClean);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.BODE WHERE RTRIM(MAGV) = @MaGVClean)
    BEGIN
        RAISERROR(N'Không thể xóa giáo viên [%s] vì giáo viên đã biên soạn câu hỏi trong Bộ đề!', 16, 1, @MaGVClean);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.GIAOVIEN_DANGKY WHERE RTRIM(MAGV) = @MaGVClean)
    BEGIN
        RAISERROR(N'Không thể xóa giáo viên [%s] vì giáo viên đã có lịch đăng ký thi!', 16, 1, @MaGVClean);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @MaGVClean COLLATE DATABASE_DEFAULT AND type = 'S' AND is_disabled = 0)
    BEGIN
        RAISERROR(N'Không thể xóa giáo viên [%s] vì giáo viên đang có tài khoản SQL Server Login đang hoạt động! Vui lòng thu hồi tài khoản trước.', 16, 1, @MaGVClean);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @MaGVClean COLLATE DATABASE_DEFAULT AND type = 'S')
    BEGIN
        DECLARE @SqlDropUser NVARCHAR(MAX) = N'DROP USER ' + QUOTENAME(@MaGVClean) + N';';
        EXEC sys.sp_executesql @SqlDropUser;
    END

    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @MaGVClean COLLATE DATABASE_DEFAULT AND type = 'S' AND is_disabled = 1)
    BEGIN
        DECLARE @SqlDropLogin NVARCHAR(MAX) = N'DROP LOGIN ' + QUOTENAME(@MaGVClean) + N';';
        EXEC sys.sp_executesql @SqlDropLogin;
    END

    DELETE FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = @MaGVClean;
END;
GO

CREATE PROCEDURE dbo.sp_LayDanhSachGiaoVien
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        MAGV,
        HO,
        TEN,
        HOTEN,
        SODTLL,
        DIACHI,
        ISNULL(TENDANGNHAP, N'(Chưa tạo)') AS TENDANGNHAP,
        TRANGTHAI_MOTA,
        CO_TAI_KHOAN,
        TRANGTHAI_TK,
        SO_CAU_DA_SOAN,
        SO_LICH_DANG_KY
    FROM dbo.vw_DanhSachGiaoVien
    ORDER BY MAGV ASC;
END;
GO

CREATE PROCEDURE dbo.sp_TimKiemGiaoVien
    @Keyword NVARCHAR(60)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @KeyPattern NVARCHAR(65) = N'%' + LTRIM(RTRIM(ISNULL(@Keyword, N''))) + N'%';

    SELECT
        MAGV,
        HO,
        TEN,
        HOTEN,
        SODTLL,
        DIACHI,
        ISNULL(TENDANGNHAP, N'(Chưa tạo)') AS TENDANGNHAP,
        TRANGTHAI_MOTA,
        CO_TAI_KHOAN,
        TRANGTHAI_TK,
        SO_CAU_DA_SOAN,
        SO_LICH_DANG_KY
    FROM dbo.vw_DanhSachGiaoVien
    WHERE MAGV LIKE @KeyPattern
       OR HO LIKE @KeyPattern
       OR TEN LIKE @KeyPattern
       OR HOTEN LIKE @KeyPattern
       OR SODTLL LIKE @KeyPattern
    ORDER BY MAGV ASC;
END;
GO

-- 7.6. Stored Procedure Tạo / Thu Hồi / Kích Hoạt Lại SQL Server Login cho Giáo Viên
CREATE PROCEDURE dbo.sp_TaoTaiKhoanGiaoVien
    @MaGV           NCHAR(8),
    @TenDangNhap    NVARCHAR(50),
    @MatKhauPlain   NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    IF IS_MEMBER(N'role_GiaoVien') <> 1 AND IS_SRVROLEMEMBER(N'sysadmin') <> 1
    BEGIN
        RAISERROR(N'Truy cập bị từ chối: Chỉ giáo viên thuộc role_GiaoVien mới có quyền tạo tài khoản giáo viên!', 16, 1);
        RETURN;
    END

    DECLARE @MaGVClean NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaGV, N''))));
    DECLARE @LoginName NVARCHAR(50) = UPPER(LTRIM(RTRIM(ISNULL(@TenDangNhap, N''))));

    IF LEN(@MaGVClean) = 0 OR LEN(@MaGVClean) > 8 OR @MaGVClean LIKE N'%[^A-Z0-9_-]%'
    BEGIN
        RAISERROR(N'Mã giáo viên (MAGV) không hợp lệ!', 16, 1);
        RETURN;
    END

    IF @LoginName <> @MaGVClean
    BEGIN
        RAISERROR(N'Theo quy định bảo mật, Tên đăng nhập (SQL Login) của giáo viên bắt buộc phải trùng khớp với Mã giáo viên [%s]!', 16, 1, @MaGVClean);
        RETURN;
    END

    IF @MatKhauPlain IS NULL OR LEN(@MatKhauPlain) < 6 OR LEN(@MatKhauPlain) > 128
    BEGIN
        RAISERROR(N'Mật khẩu phải có độ dài từ 6 đến 128 ký tự!', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = @MaGVClean)
    BEGIN
        RAISERROR(N'Mã giáo viên [%s] không tồn tại trong bảng GIAOVIEN!', 16, 1, @MaGVClean);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = @LoginName)
    BEGIN
        RAISERROR(N'Mã [%s] đang trùng với mã sinh viên trong hệ thống!', 16, 1, @LoginName);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginName COLLATE DATABASE_DEFAULT)
    BEGIN
        RAISERROR(N'SQL Server Login [%s] đã tồn tại! Mỗi giáo viên chỉ có một tài khoản duy nhất.', 16, 1, @LoginName);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LoginName COLLATE DATABASE_DEFAULT)
    BEGIN
        RAISERROR(N'Database User [%s] đã tồn tại trong cơ sở dữ liệu TRAC_NGHIEM!', 16, 1, @LoginName);
        RETURN;
    END

    DECLARE @LoginCreated BIT = 0;
    DECLARE @UserCreated BIT = 0;
    DECLARE @SqlCreateLogin NVARCHAR(MAX);
    DECLARE @SqlCreateUser NVARCHAR(MAX);
    DECLARE @SqlAddRole NVARCHAR(MAX);

    BEGIN TRY
        SET @SqlCreateLogin = N'CREATE LOGIN ' + QUOTENAME(@LoginName)
            + N' WITH PASSWORD = ' + QUOTENAME(@MatKhauPlain, '''')
            + N', DEFAULT_DATABASE = [TRAC_NGHIEM], CHECK_POLICY = ON, CHECK_EXPIRATION = OFF;';
        EXEC sys.sp_executesql @SqlCreateLogin;
        SET @LoginCreated = 1;

        SET @SqlCreateUser = N'CREATE USER ' + QUOTENAME(@LoginName)
            + N' FOR LOGIN ' + QUOTENAME(@LoginName) + N';';
        EXEC sys.sp_executesql @SqlCreateUser;
        SET @UserCreated = 1;

        SET @SqlAddRole = N'ALTER ROLE [role_GiaoVien] ADD MEMBER ' + QUOTENAME(@LoginName) + N';';
        EXEC sys.sp_executesql @SqlAddRole;
    END TRY
    BEGIN CATCH
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();

        IF @UserCreated = 1 AND EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LoginName COLLATE DATABASE_DEFAULT)
        BEGIN
            DECLARE @SqlDropUser NVARCHAR(MAX) = N'DROP USER ' + QUOTENAME(@LoginName) + N';';
            BEGIN TRY EXEC sys.sp_executesql @SqlDropUser; END TRY BEGIN CATCH END CATCH
        END

        IF @LoginCreated = 1 AND EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginName COLLATE DATABASE_DEFAULT)
        BEGIN
            DECLARE @SqlDropLogin NVARCHAR(MAX) = N'DROP LOGIN ' + QUOTENAME(@LoginName) + N';';
            BEGIN TRY EXEC sys.sp_executesql @SqlDropLogin; END TRY BEGIN CATCH END CATCH
        END

        RAISERROR(N'Không thể tạo tài khoản SQL Login [%s]: %s', 16, 1, @LoginName, @ErrMsg);
    END CATCH
END;
GO

CREATE PROCEDURE dbo.sp_ThuHoiTaiKhoanGiaoVien
    @MaGV   NCHAR(8)
AS
BEGIN
    SET NOCOUNT ON;

    IF IS_MEMBER(N'role_GiaoVien') <> 1 AND IS_SRVROLEMEMBER(N'sysadmin') <> 1
    BEGIN
        RAISERROR(N'Truy cập bị từ chối: Chỉ giáo viên thuộc role_GiaoVien mới có quyền thu hồi tài khoản!', 16, 1);
        RETURN;
    END

    DECLARE @MaGVClean NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaGV, N''))));

    IF LEN(@MaGVClean) = 0 OR LEN(@MaGVClean) > 8 OR @MaGVClean LIKE N'%[^A-Z0-9_-]%'
    BEGIN
        RAISERROR(N'Mã giáo viên (MAGV) không hợp lệ!', 16, 1);
        RETURN;
    END

    IF UPPER(LTRIM(RTRIM(SUSER_SNAME()))) = @MaGVClean
    BEGIN
        RAISERROR(N'Không thể tự thu hồi (vô hiệu hóa) tài khoản đang đăng nhập của chính bạn!', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = @MaGVClean)
    BEGIN
        RAISERROR(N'Mã giáo viên [%s] không tồn tại trong bảng GIAOVIEN!', 16, 1, @MaGVClean);
        RETURN;
    END

    DECLARE @IsDisabled BIT;
    SELECT @IsDisabled = is_disabled
    FROM sys.server_principals
    WHERE name = @MaGVClean COLLATE DATABASE_DEFAULT
      AND type = 'S';

    IF @IsDisabled IS NULL
    BEGIN
        RAISERROR(N'Giáo viên [%s] chưa có SQL Server Login trên hệ thống để thu hồi!', 16, 1, @MaGVClean);
        RETURN;
    END

    IF @IsDisabled = 1
    BEGIN
        RAISERROR(N'Tài khoản SQL Login [%s] đã bị vô hiệu hóa trước đó!', 16, 1, @MaGVClean);
        RETURN;
    END

    BEGIN TRY
        DECLARE @SqlDisable NVARCHAR(MAX) = N'ALTER LOGIN ' + QUOTENAME(@MaGVClean) + N' DISABLE;';
        EXEC sys.sp_executesql @SqlDisable;

        DECLARE @Spid INT;
        DECLARE curSessions CURSOR LOCAL FAST_FORWARD FOR
            SELECT session_id
            FROM sys.dm_exec_sessions
            WHERE login_name = @MaGVClean COLLATE DATABASE_DEFAULT
              AND session_id <> @@SPID;

        OPEN curSessions;
        FETCH NEXT FROM curSessions INTO @Spid;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            DECLARE @SqlKill NVARCHAR(100) = N'KILL ' + CAST(@Spid AS NVARCHAR(20)) + N';';
            BEGIN TRY
                EXEC sys.sp_executesql @SqlKill;
            END TRY
            BEGIN CATCH
            END CATCH
            FETCH NEXT FROM curSessions INTO @Spid;
        END
        CLOSE curSessions;
        DEALLOCATE curSessions;
    END TRY
    BEGIN CATCH
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'Lỗi khi thu hồi SQL Login [%s]: %s', 16, 1, @MaGVClean, @ErrMsg);
    END CATCH
END;
GO

CREATE PROCEDURE dbo.sp_KichHoatLaiTaiKhoanGiaoVien
    @MaGV   NCHAR(8)
AS
BEGIN
    SET NOCOUNT ON;

    IF IS_MEMBER(N'role_GiaoVien') <> 1 AND IS_SRVROLEMEMBER(N'sysadmin') <> 1
    BEGIN
        RAISERROR(N'Truy cập bị từ chối: Chỉ giáo viên thuộc role_GiaoVien mới có quyền kích hoạt lại tài khoản!', 16, 1);
        RETURN;
    END

    DECLARE @MaGVClean NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaGV, N''))));

    IF LEN(@MaGVClean) = 0 OR LEN(@MaGVClean) > 8 OR @MaGVClean LIKE N'%[^A-Z0-9_-]%'
    BEGIN
        RAISERROR(N'Mã giáo viên (MAGV) không hợp lệ!', 16, 1);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = @MaGVClean)
    BEGIN
        RAISERROR(N'Mã giáo viên [%s] không tồn tại trong bảng GIAOVIEN!', 16, 1, @MaGVClean);
        RETURN;
    END

    DECLARE @IsDisabled BIT;
    SELECT @IsDisabled = is_disabled
    FROM sys.server_principals
    WHERE name = @MaGVClean COLLATE DATABASE_DEFAULT
      AND type = 'S';

    IF @IsDisabled IS NULL
    BEGIN
        RAISERROR(N'Giáo viên [%s] chưa có SQL Server Login để kích hoạt lại!', 16, 1, @MaGVClean);
        RETURN;
    END

    IF @IsDisabled = 0
    BEGIN
        RAISERROR(N'Tài khoản SQL Login [%s] hiện đang ở trạng thái hoạt động!', 16, 1, @MaGVClean);
        RETURN;
    END

    BEGIN TRY
        DECLARE @SqlEnable NVARCHAR(MAX) = N'ALTER LOGIN ' + QUOTENAME(@MaGVClean) + N' ENABLE;';
        EXEC sys.sp_executesql @SqlEnable;

        IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @MaGVClean COLLATE DATABASE_DEFAULT AND type = 'S')
        BEGIN
            DECLARE @SqlCreateUser NVARCHAR(MAX) = N'CREATE USER ' + QUOTENAME(@MaGVClean) + N' FOR LOGIN ' + QUOTENAME(@MaGVClean) + N';';
            EXEC sys.sp_executesql @SqlCreateUser;
        END

        IF NOT EXISTS (
            SELECT 1
            FROM sys.database_role_members drm
            INNER JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
            INNER JOIN sys.database_principals u ON drm.member_principal_id = u.principal_id
            WHERE r.name = N'role_GiaoVien'
              AND u.name = @MaGVClean COLLATE DATABASE_DEFAULT
        )
        BEGIN
            DECLARE @SqlEnsureRole NVARCHAR(MAX) = N'ALTER ROLE [role_GiaoVien] ADD MEMBER ' + QUOTENAME(@MaGVClean) + N';';
            EXEC sys.sp_executesql @SqlEnsureRole;
        END
    END TRY
    BEGIN CATCH
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR(N'Lỗi khi kích hoạt lại SQL Login [%s]: %s', 16, 1, @MaGVClean, @ErrMsg);
    END CATCH
END;
GO

-- 7.7. Stored Procedure Đổi Mật Khẩu
CREATE PROCEDURE dbo.sp_DoiMatKhau
    @MatKhauHienTai NVARCHAR(128),
    @MatKhauMoi     NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    IF IS_MEMBER(N'role_GiaoVien') <> 1
    BEGIN
        RAISERROR(N'Từ chối truy cập: Chức năng đổi mật khẩu chỉ dành cho giáo viên thuộc role_GiaoVien!', 16, 1);
        RETURN;
    END

    DECLARE @CurrentLogin NVARCHAR(128) = LTRIM(RTRIM(SUSER_SNAME()));

    IF NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = UPPER(@CurrentLogin))
    BEGIN
        RAISERROR(N'Tài khoản đăng nhập hiện tại [%s] không khớp với hồ sơ giáo viên!', 16, 1, @CurrentLogin);
        RETURN;
    END

    IF @MatKhauHienTai IS NULL OR LEN(@MatKhauHienTai) = 0
    BEGIN
        RAISERROR(N'Vui lòng nhập mật khẩu hiện tại!', 16, 1);
        RETURN;
    END

    IF @MatKhauMoi IS NULL OR LEN(@MatKhauMoi) < 6 OR LEN(@MatKhauMoi) > 128
    BEGIN
        RAISERROR(N'Mật khẩu mới phải có độ dài từ 6 đến 128 ký tự!', 16, 1);
        RETURN;
    END

    IF @MatKhauHienTai = @MatKhauMoi
    BEGIN
        RAISERROR(N'Mật khẩu mới không được trùng với mật khẩu hiện tại!', 16, 1);
        RETURN;
    END

    BEGIN TRY
        DECLARE @SqlChangePwd NVARCHAR(MAX) =
            N'ALTER LOGIN ' + QUOTENAME(@CurrentLogin)
            + N' WITH PASSWORD = ' + QUOTENAME(@MatKhauMoi, '''')
            + N' OLD_PASSWORD = ' + QUOTENAME(@MatKhauHienTai, '''') + N';';

        EXEC sys.sp_executesql @SqlChangePwd;
    END TRY
    BEGIN CATCH
        DECLARE @ErrNum INT = ERROR_NUMBER();
        DECLARE @ErrMsg NVARCHAR(4000) = ERROR_MESSAGE();

        IF @ErrNum IN (15151, 18456)
        BEGIN
            RAISERROR(N'Mật khẩu hiện tại không chính xác!', 16, 1);
            RETURN;
        END

        RAISERROR(N'Không thể đổi mật khẩu: %s', 16, 1, @ErrMsg);
    END CATCH
END;
GO

-- 7.8. Stored Procedure Danh sách tài khoản Giáo viên & Thống kê Tổng quan
CREATE PROCEDURE dbo.sp_LayDanhSachTaiKhoanGiaoVien
AS
BEGIN
    SET NOCOUNT ON;

    IF IS_MEMBER(N'role_GiaoVien') <> 1 AND IS_SRVROLEMEMBER(N'sysadmin') <> 1
    BEGIN
        RAISERROR(N'Từ chối truy cập: Chỉ giáo viên mới có quyền xem danh sách tài khoản!', 16, 1);
        RETURN;
    END

    SELECT
        RTRIM(gv.MAGV) AS MAGV,
        gv.HO,
        gv.TEN,
        (gv.HO + N' ' + gv.TEN) AS HOTEN_GV,
        CASE
            WHEN sp.principal_id IS NOT NULL AND dp.principal_id IS NOT NULL AND role_gv.IS_IN_ROLE = 1
            THEN sp.name
            ELSE N'(Chưa tạo)'
        END AS TENDANGNHAP,
        CASE
            WHEN sp.principal_id IS NULL OR dp.principal_id IS NULL OR ISNULL(role_gv.IS_IN_ROLE, 0) = 0
            THEN 0
            WHEN sp.is_disabled = 1
            THEN 2
            ELSE 1
        END AS TRANGTHAI_CODE,
        CASE
            WHEN sp.principal_id IS NULL OR dp.principal_id IS NULL OR ISNULL(role_gv.IS_IN_ROLE, 0) = 0
            THEN N'Chưa tạo'
            WHEN sp.is_disabled = 1
            THEN N'Đã vô hiệu hóa'
            ELSE N'Đang hoạt động'
        END AS TRANGTHAI_TEXT,
        CASE
            WHEN sp.principal_id IS NULL OR dp.principal_id IS NULL OR ISNULL(role_gv.IS_IN_ROLE, 0) = 0
            THEN N'Tạo tài khoản'
            WHEN sp.is_disabled = 1
            THEN N'Kích hoạt lại tài khoản'
            ELSE N'Thu hồi tài khoản'
        END AS HANHDONG_TEXT,
        CASE WHEN sp.principal_id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS CO_LOGIN,
        CASE WHEN dp.principal_id IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS CO_DB_USER,
        CAST(ISNULL(role_gv.IS_IN_ROLE, 0) AS BIT) AS THUOC_ROLE_GV,
        CAST(ISNULL(sp.is_disabled, 0) AS BIT) AS IS_DISABLED,
        sp.create_date AS NGAYTAO
    FROM dbo.GIAOVIEN gv
    LEFT JOIN sys.server_principals sp
        ON sp.name = RTRIM(gv.MAGV) COLLATE DATABASE_DEFAULT
       AND sp.type = 'S'
    LEFT JOIN sys.database_principals dp
        ON dp.sid = sp.sid
       AND dp.type = 'S'
       AND dp.name = RTRIM(gv.MAGV) COLLATE DATABASE_DEFAULT
    OUTER APPLY (
        SELECT TOP 1 1 AS IS_IN_ROLE
        FROM sys.database_role_members drm
        INNER JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
        WHERE drm.member_principal_id = dp.principal_id
          AND r.name = N'role_GiaoVien'
    ) role_gv
    ORDER BY gv.MAGV ASC;
END;
GO

CREATE PROCEDURE dbo.sp_LayGiaoVienChuaCoTaiKhoan
AS
BEGIN
    SET NOCOUNT ON;

    IF IS_MEMBER(N'role_GiaoVien') <> 1 AND IS_SRVROLEMEMBER(N'sysadmin') <> 1
    BEGIN
        RAISERROR(N'Từ chối truy cập!', 16, 1);
        RETURN;
    END

    SELECT
        RTRIM(gv.MAGV) AS MAGV,
        gv.HO,
        gv.TEN,
        (RTRIM(gv.MAGV) + N' - ' + gv.HO + N' ' + gv.TEN) AS HIENTHI
    FROM dbo.GIAOVIEN gv
    LEFT JOIN sys.server_principals sp
        ON sp.name = RTRIM(gv.MAGV) COLLATE DATABASE_DEFAULT
       AND sp.type = 'S'
    LEFT JOIN sys.database_principals dp
        ON dp.sid = sp.sid
       AND dp.type = 'S'
       AND dp.name = RTRIM(gv.MAGV) COLLATE DATABASE_DEFAULT
    WHERE sp.principal_id IS NULL
       OR dp.principal_id IS NULL
       OR NOT EXISTS (
           SELECT 1
           FROM sys.database_role_members drm
           INNER JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
           WHERE drm.member_principal_id = dp.principal_id
             AND r.name = N'role_GiaoVien'
       )
    ORDER BY gv.MAGV ASC;
END;
GO

CREATE PROCEDURE dbo.sp_LayThongKeTongQuan
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TrangThaiGV TABLE (
        MAGV            NVARCHAR(8) PRIMARY KEY,
        HOTEN           NVARCHAR(60),
        CO_VALID_LOGIN  BIT,
        IS_ACTIVE       BIT,
        IS_DISABLED     BIT
    );

    INSERT INTO @TrangThaiGV (MAGV, HOTEN, CO_VALID_LOGIN, IS_ACTIVE, IS_DISABLED)
    SELECT
        RTRIM(gv.MAGV) AS MAGV,
        (gv.HO + N' ' + gv.TEN) AS HOTEN,
        CASE
            WHEN sp.principal_id IS NOT NULL AND dp.principal_id IS NOT NULL AND ISNULL(role_gv.IS_IN_ROLE, 0) = 1
            THEN 1 ELSE 0
        END AS CO_VALID_LOGIN,
        CASE
            WHEN sp.principal_id IS NOT NULL AND dp.principal_id IS NOT NULL AND ISNULL(role_gv.IS_IN_ROLE, 0) = 1 AND sp.is_disabled = 0
            THEN 1 ELSE 0
        END AS IS_ACTIVE,
        CASE
            WHEN sp.principal_id IS NOT NULL AND dp.principal_id IS NOT NULL AND ISNULL(role_gv.IS_IN_ROLE, 0) = 1 AND sp.is_disabled = 1
            THEN 1 ELSE 0
        END AS IS_DISABLED
    FROM dbo.GIAOVIEN gv
    LEFT JOIN sys.server_principals sp
        ON sp.name = RTRIM(gv.MAGV) COLLATE DATABASE_DEFAULT
       AND sp.type = 'S'
    LEFT JOIN sys.database_principals dp
        ON dp.sid = sp.sid
       AND dp.type = 'S'
       AND dp.name = RTRIM(gv.MAGV) COLLATE DATABASE_DEFAULT
    OUTER APPLY (
        SELECT TOP 1 1 AS IS_IN_ROLE
        FROM sys.database_role_members drm
        INNER JOIN sys.database_principals r ON drm.role_principal_id = r.principal_id
        WHERE drm.member_principal_id = dp.principal_id
          AND r.name = N'role_GiaoVien'
    ) role_gv;

    DECLARE @TongSoGV INT = (SELECT COUNT(*) FROM @TrangThaiGV);
    DECLARE @SoGVCoLogin INT = (SELECT COUNT(*) FROM @TrangThaiGV WHERE CO_VALID_LOGIN = 1);
    DECLARE @SoLoginHoatDong INT = (SELECT COUNT(*) FROM @TrangThaiGV WHERE IS_ACTIVE = 1);
    DECLARE @SoLoginVoHieuHoa INT = (SELECT COUNT(*) FROM @TrangThaiGV WHERE IS_DISABLED = 1);
    DECLARE @SoGVChuaCapLogin INT = (SELECT COUNT(*) FROM @TrangThaiGV WHERE CO_VALID_LOGIN = 0);

    SELECT 1 AS THU_TU, N'1. Tổng số giáo viên (Bảng GIAOVIEN)' AS CHI_TIEU, @TongSoGV AS SO_LUONG
    UNION ALL
    SELECT 2, N'2. Số giáo viên có SQL Login (role_GiaoVien)', @SoGVCoLogin
    UNION ALL
    SELECT 3, N'3. Số Login giáo viên đang hoạt động (ENABLED)', @SoLoginHoatDong
    UNION ALL
    SELECT 4, N'4. Số Login giáo viên đã bị vô hiệu hóa (DISABLED)', @SoLoginVoHieuHoa
    UNION ALL
    SELECT 5, N'5. Số giáo viên chưa được cấp Login', @SoGVChuaCapLogin
    UNION ALL
    SELECT 6, N'6. Tổng số lớp học (Bảng LOP)', (SELECT COUNT(*) FROM dbo.LOP)
    UNION ALL
    SELECT 7, N'7. Tổng số môn học đang sử dụng (Bảng MONHOC)', (SELECT COUNT(*) FROM dbo.MONHOC WHERE DAXOA = 0)
    UNION ALL
    SELECT 8, N'8. Tổng số sinh viên đang theo học (Bảng SINHVIEN)', (SELECT COUNT(*) FROM dbo.SINHVIEN WHERE DAXOA = 0)
    UNION ALL
    SELECT 9, N'9. Tổng số câu hỏi trắc nghiệm (Bảng BODE)', (SELECT COUNT(*) FROM dbo.BODE)
    UNION ALL
    SELECT 10, N'10. Tổng số lịch đăng ký thi (Bảng GIAOVIEN_DANGKY)', (SELECT COUNT(*) FROM dbo.GIAOVIEN_DANGKY)
    UNION ALL
    SELECT 11, N'11. Tổng số bài thi / kết quả điểm (Bảng BANGDIEM)', (SELECT COUNT(*) FROM dbo.BANGDIEM)
    ORDER BY THU_TU ASC;
END;
GO

-- 7.9. Stored Procedure dành cho Sinh viên xem Lịch thi & Kết quả cá nhân
CREATE PROCEDURE dbo.sp_LayLichThiSinhVien
    @MaLop  NCHAR(8)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @MaLopClean NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaLop, N''))));

    SELECT
        RTRIM(dk.MAMH) AS [Mã MH],
        mh.TENMH AS [Tên môn học],
        dk.LAN AS [Lần thi],
        dk.TRINHDO AS [Trình độ],
        dk.SOCAUTHI AS [Số câu],
        dk.THOIGIAN AS [Thời gian (phút)],
        dk.NGAYTHI AS [Ngày thi]
    FROM dbo.GIAOVIEN_DANGKY dk
    INNER JOIN dbo.MONHOC mh ON dk.MAMH = mh.MAMH
    WHERE RTRIM(dk.MALOP) = @MaLopClean
      AND mh.DAXOA = 0
    ORDER BY dk.NGAYTHI DESC;
END;
GO

CREATE PROCEDURE dbo.sp_LayKetQuaThiCaNhan
    @MaSV   NCHAR(8) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CallerLogin NVARCHAR(128) = UPPER(LTRIM(RTRIM(SUSER_SNAME())));
    DECLARE @TargetMaSV NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaSV, @CallerLogin))));

    IF IS_MEMBER(N'role_SinhVien') = 1 AND IS_MEMBER(N'role_GiaoVien') = 0
    BEGIN
        SET @TargetMaSV = @CallerLogin;
    END

    SELECT
        RTRIM(bd.MAMH) AS [Mã MH],
        mh.TENMH AS [Tên môn học],
        bd.LAN AS [Lần thi],
        bd.NGAYTHI AS [Ngày thi],
        bd.DIEM AS [Điểm số],
        bd.BAITHI AS [Chi tiết bài làm]
    FROM dbo.BANGDIEM bd
    INNER JOIN dbo.MONHOC mh ON bd.MAMH = mh.MAMH
    WHERE RTRIM(bd.MASV) = @TargetMaSV
    ORDER BY bd.NGAYTHI DESC;
END;
GO

-- 7.10. Stored Procedure cho Mục 7b (Xem kết quả - In lại các câu đã thi) & Mục 8 (Bảng điểm môn học)
CREATE PROCEDURE dbo.sp_LayDanhSachBaiThiDaCo
    @MaLop  NCHAR(8) = NULL,
    @MaSV   NCHAR(8) = NULL,
    @MaMH   NCHAR(5) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CallerLogin NVARCHAR(128) = UPPER(LTRIM(RTRIM(SUSER_SNAME())));
    DECLARE @FilterMaLop NVARCHAR(8) = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@MaLop, N'')))), N'');
    DECLARE @FilterMaSV  NVARCHAR(8) = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@MaSV, N'')))), N'');
    DECLARE @FilterMaMH  NVARCHAR(5) = NULLIF(UPPER(LTRIM(RTRIM(ISNULL(@MaMH, N'')))), N'');

    IF IS_MEMBER(N'role_SinhVien') = 1
       AND IS_MEMBER(N'role_GiaoVien') = 0
       AND IS_MEMBER(N'db_owner') = 0
       AND IS_SRVROLEMEMBER(N'sysadmin', SUSER_SNAME()) = 0
    BEGIN
        SET @FilterMaSV = @CallerLogin;
    END

    SELECT
        RTRIM(bd.MASV) AS MASV,
        (sv.HO + N' ' + sv.TEN) AS HOTEN,
        RTRIM(sv.MALOP) AS MALOP,
        l.TENLOP,
        RTRIM(bd.MAMH) AS MAMH,
        mh.TENMH,
        bd.LAN,
        ISNULL(RTRIM(dk.TRINHDO), N'A') AS TRINHDO,
        bd.NGAYTHI,
        bd.DIEM,
        ISNULL(dk.SOCAUTHI, 10) AS SOCAUTHI,
        (RTRIM(bd.MASV) + N' - ' + sv.HO + N' ' + sv.TEN + N' | ' + RTRIM(bd.MAMH) + N' (Lần ' + CONVERT(NVARCHAR(5), bd.LAN) + N') - Điểm: ' + CONVERT(NVARCHAR(10), CAST(ROUND(bd.DIEM, 1) AS DECIMAL(4,1)))) AS HIENTHI_PHIEU
    FROM dbo.BANGDIEM bd
    INNER JOIN dbo.SINHVIEN sv ON bd.MASV = sv.MASV
    INNER JOIN dbo.LOP l ON sv.MALOP = l.MALOP
    INNER JOIN dbo.MONHOC mh ON bd.MAMH = mh.MAMH
    LEFT JOIN dbo.GIAOVIEN_DANGKY dk
        ON dk.MALOP = sv.MALOP AND dk.MAMH = bd.MAMH AND dk.LAN = bd.LAN
    WHERE (@FilterMaLop IS NULL OR RTRIM(sv.MALOP) = @FilterMaLop)
      AND (@FilterMaSV IS NULL OR RTRIM(bd.MASV) = @FilterMaSV)
      AND (@FilterMaMH IS NULL OR RTRIM(bd.MAMH) = @FilterMaMH)
    ORDER BY bd.NGAYTHI DESC, bd.MASV ASC, bd.MAMH ASC, bd.LAN ASC;
END;
GO

CREATE PROCEDURE dbo.sp_LayThongTinHeaderKetQua
    @MaSV   NCHAR(8),
    @MaMH   NCHAR(5),
    @Lan    SMALLINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CallerLogin NVARCHAR(128) = UPPER(LTRIM(RTRIM(SUSER_SNAME())));
    DECLARE @CleanMaSV NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaSV, N''))));
    DECLARE @CleanMaMH NVARCHAR(5) = UPPER(LTRIM(RTRIM(ISNULL(@MaMH, N''))));

    IF IS_MEMBER(N'role_SinhVien') = 1
       AND IS_MEMBER(N'role_GiaoVien') = 0
       AND IS_MEMBER(N'db_owner') = 0
       AND IS_SRVROLEMEMBER(N'sysadmin', SUSER_SNAME()) = 0
    BEGIN
        IF @CleanMaSV <> @CallerLogin
        BEGIN
            RAISERROR(N'Từ chối truy cập: Sinh viên [%s] chỉ được phép xem kết quả bài thi của chính mình!', 16, 1, @CallerLogin);
            RETURN;
        END
    END

    SELECT
        RTRIM(l.MALOP) AS MALOP,
        l.TENLOP,
        RTRIM(sv.MASV) AS MASV,
        sv.HO,
        sv.TEN,
        (sv.HO + N' ' + sv.TEN) AS HOTEN,
        sv.NGAYSINH,
        RTRIM(mh.MAMH) AS MAMH,
        mh.TENMH,
        ISNULL(RTRIM(dk.TRINHDO), N'A') AS TRINHDO,
        bd.NGAYTHI,
        bd.LAN,
        ISNULL(dk.SOCAUTHI, 10) AS SOCAUTHI,
        ISNULL(dk.THOIGIAN, 15) AS THOIGIAN,
        bd.DIEM
    FROM dbo.BANGDIEM bd
    INNER JOIN dbo.SINHVIEN sv ON bd.MASV = sv.MASV
    INNER JOIN dbo.LOP l ON sv.MALOP = l.MALOP
    INNER JOIN dbo.MONHOC mh ON bd.MAMH = mh.MAMH
    LEFT JOIN dbo.GIAOVIEN_DANGKY dk
        ON dk.MALOP = sv.MALOP AND dk.MAMH = bd.MAMH AND dk.LAN = bd.LAN
    WHERE RTRIM(bd.MASV) = @CleanMaSV
      AND RTRIM(bd.MAMH) = @CleanMaMH
      AND bd.LAN = @Lan;
END;
GO

CREATE PROCEDURE dbo.sp_XemKetQuaBaiThi
    @MaSV   NCHAR(8),
    @MaMH   NCHAR(5),
    @Lan    SMALLINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CallerLogin NVARCHAR(128) = UPPER(LTRIM(RTRIM(SUSER_SNAME())));
    DECLARE @CleanMaSV NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaSV, N''))));
    DECLARE @CleanMaMH NVARCHAR(5) = UPPER(LTRIM(RTRIM(ISNULL(@MaMH, N''))));

    IF IS_MEMBER(N'role_SinhVien') = 1
       AND IS_MEMBER(N'role_GiaoVien') = 0
       AND IS_MEMBER(N'db_owner') = 0
       AND IS_SRVROLEMEMBER(N'sysadmin', SUSER_SNAME()) = 0
    BEGIN
        IF @CleanMaSV <> @CallerLogin
        BEGIN
            RAISERROR(N'Từ chối truy cập: Sinh viên [%s] chỉ được phép xem chi tiết câu hỏi bài thi của chính mình!', 16, 1, @CallerLogin);
            RETURN;
        END
    END

    DECLARE @BaiThi NVARCHAR(MAX);
    SELECT @BaiThi = BAITHI
    FROM dbo.BANGDIEM
    WHERE RTRIM(MASV) = @CleanMaSV
      AND RTRIM(MAMH) = @CleanMaMH
      AND LAN = @Lan;

    IF @BaiThi IS NULL
    BEGIN
        RAISERROR(N'Không tìm thấy bài thi của sinh viên [%s] - Môn [%s] - Lần [%d]!', 16, 1, @CleanMaSV, @CleanMaMH, @Lan);
        RETURN;
    END

    DECLARE @Parsed TABLE (
        STT     INT IDENTITY(1,1) PRIMARY KEY,
        CAUHOI  INT,
        DA_CHON NVARCHAR(10)
    );

    DECLARE @Work NVARCHAR(MAX) = LTRIM(RTRIM(@BaiThi));
    DECLARE @SemiPos INT;
    DECLARE @ColonPos INT;
    DECLARE @Token NVARCHAR(100);
    DECLARE @CauStr NVARCHAR(20);
    DECLARE @ChonStr NVARCHAR(10);

    WHILE LEN(@Work) > 0
    BEGIN
        SET @SemiPos = CHARINDEX(N';', @Work);
        IF @SemiPos > 0
        BEGIN
            SET @Token = LTRIM(RTRIM(LEFT(@Work, @SemiPos - 1)));
            SET @Work = LTRIM(RTRIM(SUBSTRING(@Work, @SemiPos + 1, LEN(@Work))));
        END
        ELSE
        BEGIN
            SET @Token = LTRIM(RTRIM(@Work));
            SET @Work = N'';
        END

        SET @ColonPos = CHARINDEX(N':', @Token);
        IF @ColonPos > 1
        BEGIN
            SET @CauStr = LTRIM(RTRIM(LEFT(@Token, @ColonPos - 1)));
            SET @ChonStr = UPPER(LTRIM(RTRIM(SUBSTRING(@Token, @ColonPos + 1, 10))));
            IF TRY_CAST(@CauStr AS INT) IS NOT NULL
            BEGIN
                INSERT INTO @Parsed (CAUHOI, DA_CHON)
                VALUES (TRY_CAST(@CauStr AS INT), @ChonStr);
            END
        END
    END

    SELECT
        p.STT AS [STT],
        b.CAUHOI AS [CAUHOI],
        RTRIM(b.TRINHDO) AS [TRINHDO],
        b.NOIDUNG AS [NOIDUNG],
        b.A AS [A],
        b.B AS [B],
        b.C AS [C],
        b.D AS [D],
        (b.NOIDUNG + CHAR(13) + CHAR(10) +
         N'A. ' + b.A + N'   |   B. ' + b.B + N'   |   C. ' + b.C + N'   |   D. ' + b.D) AS [NOIDUNG_VA_LUA_CHON],
        ISNULL(NULLIF(p.DA_CHON, N''), N'(Bỏ trống)') AS [DA_CHON],
        RTRIM(b.DAP_AN) AS [DAP_AN],
        CASE
            WHEN UPPER(RTRIM(p.DA_CHON)) = UPPER(RTRIM(b.DAP_AN)) THEN N'Đúng'
            ELSE N'Sai'
        END AS [KET_QUA]
    FROM @Parsed p
    INNER JOIN dbo.BODE b ON b.CAUHOI = p.CAUHOI
    ORDER BY p.STT ASC;
END;
GO

CREATE PROCEDURE dbo.sp_BaoCaoBangDiemMonHoc
    @MaLop  NCHAR(8),
    @MaMH   NCHAR(5),
    @Lan    SMALLINT
AS
BEGIN
    SET NOCOUNT ON;

    IF IS_MEMBER(N'role_GiaoVien') = 0
       AND IS_MEMBER(N'db_owner') = 0
       AND IS_SRVROLEMEMBER(N'sysadmin', SUSER_SNAME()) = 0
    BEGIN
        DECLARE @Caller NVARCHAR(128) = SUSER_SNAME();
        RAISERROR(N'Từ chối truy cập: Tài khoản [%s] không thuộc role_GiaoVien nên không có quyền xuất Bảng điểm môn học của lớp!', 16, 1, @Caller);
        RETURN;
    END

    DECLARE @CleanMaLop NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaLop, N''))));
    DECLARE @CleanMaMH  NVARCHAR(5) = UPPER(LTRIM(RTRIM(ISNULL(@MaMH, N''))));

    IF NOT EXISTS (SELECT 1 FROM dbo.LOP WHERE RTRIM(MALOP) = @CleanMaLop)
    BEGIN
        RAISERROR(N'Mã lớp [%s] không tồn tại!', 16, 1, @CleanMaLop);
        RETURN;
    END

    IF NOT EXISTS (SELECT 1 FROM dbo.MONHOC WHERE RTRIM(MAMH) = @CleanMaMH AND DAXOA = 0)
    BEGIN
        RAISERROR(N'Mã môn học [%s] không tồn tại!', 16, 1, @CleanMaMH);
        RETURN;
    END

    SELECT
        ROW_NUMBER() OVER (ORDER BY sv.TEN ASC, sv.HO ASC, sv.MASV ASC) AS [STT],
        RTRIM(sv.MASV) AS [MASV],
        sv.HO AS [HO],
        sv.TEN AS [TEN],
        (sv.HO + N' ' + sv.TEN) AS [HOTEN],
        sv.NGAYSINH AS [NGAYSINH],
        ISNULL(bd.NGAYTHI, dk.NGAYTHI) AS [NGAYTHI],
        CASE WHEN bd.DIEM IS NOT NULL THEN 1 ELSE 0 END AS [CO_DIEM],
        bd.DIEM AS [DIEM],
        CASE
            WHEN bd.DIEM IS NOT NULL THEN CONVERT(NVARCHAR(10), CAST(ROUND(bd.DIEM, 1) AS DECIMAL(4,1)))
            ELSE N'Chưa thi'
        END AS [DIEM_SO_TEXT],
        CASE
            WHEN bd.DIEM IS NULL THEN N'Chưa dự thi'
            WHEN ROUND(bd.DIEM, 1) = 10.0 THEN N'Mười điểm'
            WHEN ROUND(bd.DIEM, 1) >= 9.0 THEN N'Chín điểm'
            WHEN ROUND(bd.DIEM, 1) >= 8.0 THEN N'Tám điểm'
            WHEN ROUND(bd.DIEM, 1) >= 7.0 THEN N'Bảy điểm'
            WHEN ROUND(bd.DIEM, 1) >= 6.0 THEN N'Sáu điểm'
            WHEN ROUND(bd.DIEM, 1) >= 5.0 THEN N'Năm điểm'
            WHEN ROUND(bd.DIEM, 1) >= 4.0 THEN N'Bốn điểm'
            WHEN ROUND(bd.DIEM, 1) >= 3.0 THEN N'Ba điểm'
            WHEN ROUND(bd.DIEM, 1) >= 2.0 THEN N'Hai điểm'
            WHEN ROUND(bd.DIEM, 1) >= 1.0 THEN N'Một điểm'
            ELSE N'Không điểm'
        END AS [DIEM_CHU],
        CASE
            WHEN bd.DIEM IS NULL THEN N'Vắng / Chưa thi'
            WHEN bd.DIEM >= 5.0 THEN N'Đạt'
            ELSE N'Không đạt'
        END AS [GHI_CHU],
        l.TENLOP AS [TENLOP],
        mh.TENMH AS [TENMH],
        ISNULL(RTRIM(dk.TRINHDO), N'A') AS [TRINHDO]
    FROM dbo.SINHVIEN sv
    INNER JOIN dbo.LOP l ON sv.MALOP = l.MALOP
    CROSS JOIN dbo.MONHOC mh
    LEFT JOIN dbo.BANGDIEM bd
        ON bd.MASV = sv.MASV AND RTRIM(bd.MAMH) = @CleanMaMH AND bd.LAN = @Lan
    LEFT JOIN dbo.GIAOVIEN_DANGKY dk
        ON dk.MALOP = sv.MALOP AND RTRIM(dk.MAMH) = @CleanMaMH AND dk.LAN = @Lan
    WHERE RTRIM(sv.MALOP) = @CleanMaLop
      AND sv.DAXOA = 0
      AND RTRIM(mh.MAMH) = @CleanMaMH
    ORDER BY sv.TEN ASC, sv.HO ASC, sv.MASV ASC;
END;
GO

/*==============================================================================
  PHẦN 8: THIẾT LẬP CERTIFICATE SIGNING & PHÂN QUYỀN DATABASE ROLE
==============================================================================*/

CREATE CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]
    ENCRYPTION BY PASSWORD = N'Ptit_Cert_TracNghiem#2026!'
    WITH SUBJECT = N'Certificate ky Stored Procedure quan tri SQL Login TRAC_NGHIEM',
         EXPIRY_DATE = '2099-12-31';
GO

CREATE USER [user_Cert_QuanLyTaiKhoan] FROM CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem];
GRANT ALTER ANY USER TO [user_Cert_QuanLyTaiKhoan];
GRANT ALTER ON ROLE::[role_GiaoVien] TO [user_Cert_QuanLyTaiKhoan];
GRANT ALTER ON ROLE::[role_SinhVien] TO [user_Cert_QuanLyTaiKhoan];
GRANT VIEW DEFINITION TO [user_Cert_QuanLyTaiKhoan];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [user_Cert_QuanLyTaiKhoan];
GO

ADD SIGNATURE TO dbo.sp_TaoTaiKhoanGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_ThuHoiTaiKhoanGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_KichHoatLaiTaiKhoanGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_LayDanhSachTaiKhoanGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_LayGiaoVienChuaCoTaiKhoan BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_LayDanhSachGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_TimKiemGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_LayThongKeTongQuan BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_XoaGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_CapTaiKhoanSinhVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_ThemSinhVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_XoaSinhVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_PhucHoiSinhVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_HoanTacThemSinhVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_LayThongTinDangNhapHienTai BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_LayDanhSachBaiThiDaCo BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_LayThongTinHeaderKetQua BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_XemKetQuaBaiThi BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
ADD SIGNATURE TO dbo.sp_BaoCaoBangDiemMonHoc BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem] WITH PASSWORD = N'Ptit_Cert_TracNghiem#2026!';
GO

DECLARE @CertBin VARBINARY(MAX) = CERTENCODED(CERT_ID(N'cert_QuanLyTaiKhoan_TracNghiem'));
DECLARE @SqlMasterCert NVARCHAR(MAX) = N'
USE [master];
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N''login_Cert_QuanLyTaiKhoan'')
    DROP LOGIN [login_Cert_QuanLyTaiKhoan];
IF EXISTS (SELECT 1 FROM sys.certificates WHERE name = N''cert_QuanLyTaiKhoan_TracNghiem'')
    DROP CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem];

CREATE CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]
    FROM BINARY = ' + CONVERT(NVARCHAR(MAX), @CertBin, 1) + N';

CREATE LOGIN [login_Cert_QuanLyTaiKhoan]
    FROM CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem];

GRANT ALTER ANY LOGIN TO [login_Cert_QuanLyTaiKhoan];
GRANT ALTER ANY CONNECTION TO [login_Cert_QuanLyTaiKhoan];
GRANT VIEW ANY DEFINITION TO [login_Cert_QuanLyTaiKhoan];
GRANT VIEW SERVER STATE TO [login_Cert_QuanLyTaiKhoan];
';
EXEC sys.sp_executesql @SqlMasterCert;
GO

USE [TRAC_NGHIEM];
GO

-- Phân quyền role_GiaoVien
GRANT SELECT ON dbo.LOP TO [role_GiaoVien];
GRANT SELECT, DELETE ON dbo.MONHOC TO [role_GiaoVien];
GRANT SELECT, DELETE ON dbo.SINHVIEN TO [role_GiaoVien];
GRANT SELECT ON dbo.GIAOVIEN TO [role_GiaoVien];
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.BODE TO [role_GiaoVien];
GRANT SELECT, INSERT, UPDATE, DELETE ON dbo.GIAOVIEN_DANGKY TO [role_GiaoVien];
GRANT SELECT ON dbo.BANGDIEM TO [role_GiaoVien];

GRANT SELECT ON dbo.vw_DanhSachLop TO [role_GiaoVien];
GRANT SELECT ON dbo.vw_DanhSachMonHoc TO [role_GiaoVien];
GRANT SELECT ON dbo.vw_DanhSachSinhVienTheoLop TO [role_GiaoVien];
GRANT SELECT ON dbo.vw_DanhSachGiaoVien TO [role_GiaoVien];

GRANT EXECUTE ON dbo.sp_LayThongTinDangNhapHienTai TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_ThemMonHoc TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_CapNhatMonHoc TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_XoaMonHoc TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_PhucHoiMonHoc TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_ThemLop TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_CapNhatLop TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_XoaLop TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_ThemSinhVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_CapNhatSinhVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_XoaSinhVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_PhucHoiSinhVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_HoanTacThemSinhVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_CapTaiKhoanSinhVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_ThemGiaoVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_CapNhatGiaoVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_XoaGiaoVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_LayDanhSachGiaoVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_TimKiemGiaoVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_TaoTaiKhoanGiaoVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_ThuHoiTaiKhoanGiaoVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_KichHoatLaiTaiKhoanGiaoVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_LayDanhSachTaiKhoanGiaoVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_LayGiaoVienChuaCoTaiKhoan TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_DoiMatKhau TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_LayThongKeTongQuan TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_LayLichThiSinhVien TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_LayKetQuaThiCaNhan TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_LayDanhSachBaiThiDaCo TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_LayThongTinHeaderKetQua TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_XemKetQuaBaiThi TO [role_GiaoVien];
GRANT EXECUTE ON dbo.sp_BaoCaoBangDiemMonHoc TO [role_GiaoVien];

GRANT BACKUP DATABASE TO [role_GiaoVien];
GRANT VIEW DEFINITION TO [role_GiaoVien];
GO

-- Phân quyền role_SinhVien
GRANT EXECUTE ON dbo.sp_LayThongTinDangNhapHienTai TO [role_SinhVien];
GRANT EXECUTE ON dbo.sp_LayLichThiSinhVien TO [role_SinhVien];
GRANT EXECUTE ON dbo.sp_LayKetQuaThiCaNhan TO [role_SinhVien];
GRANT EXECUTE ON dbo.sp_LayDanhSachBaiThiDaCo TO [role_SinhVien];
GRANT EXECUTE ON dbo.sp_LayThongTinHeaderKetQua TO [role_SinhVien];
GRANT EXECUTE ON dbo.sp_XemKetQuaBaiThi TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_BaoCaoBangDiemMonHoc TO [role_SinhVien];

DENY INSERT, UPDATE, DELETE ON dbo.LOP TO [role_SinhVien];
DENY INSERT, UPDATE, DELETE ON dbo.MONHOC TO [role_SinhVien];
DENY INSERT, UPDATE, DELETE ON dbo.SINHVIEN TO [role_SinhVien];
DENY SELECT, INSERT, UPDATE, DELETE ON dbo.GIAOVIEN TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_ThemMonHoc TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_CapNhatMonHoc TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_XoaMonHoc TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_PhucHoiMonHoc TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_ThemLop TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_CapNhatLop TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_XoaLop TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_ThemSinhVien TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_CapNhatSinhVien TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_XoaSinhVien TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_PhucHoiSinhVien TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_ThemGiaoVien TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_CapNhatGiaoVien TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_XoaGiaoVien TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_TaoTaiKhoanGiaoVien TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_ThuHoiTaiKhoanGiaoVien TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_KichHoatLaiTaiKhoanGiaoVien TO [role_SinhVien];
DENY EXECUTE ON dbo.sp_DoiMatKhau TO [role_SinhVien];
GO

/*==============================================================================
  PHẦN 9: NẠP DỮ LIỆU MẪU & KHỞI TẠO SQL SERVER LOGIN + DATABASE USER
==============================================================================*/

-- 9.1. Dữ liệu bảng LOP (4 lớp)
INSERT INTO dbo.LOP (MALOP, TENLOP) VALUES
(N'CNTT01  ', N'Công nghệ thông tin 01'),
(N'CNTT02  ', N'Công nghệ thông tin 02'),
(N'ATTT01  ', N'An toàn thông tin 01'),
(N'KTPM01  ', N'Kỹ thuật phần mềm 01');

-- 9.2. Dữ liệu bảng MONHOC (5 môn học)
INSERT INTO dbo.MONHOC (MAMH, TENMH, DAXOA) VALUES
(N'CSDL ', N'Cơ sở dữ liệu', 0),
(N'CTDL ', N'Cấu trúc dữ liệu và giải thuật', 0),
(N'MMT  ', N'Mạng máy tính', 0),
(N'LTHDT', N'Lập trình hướng đối tượng', 0),
(N'THDC ', N'Tin học đại cương', 0);

-- 9.3. Dữ liệu bảng GIAOVIEN (5 giảng viên: gồm GV001..GV004 và GV-8-4)
INSERT INTO dbo.GIAOVIEN (MAGV, HO, TEN, SODTLL, DIACHI) VALUES
(N'GV001   ', N'Nguyễn Văn', N'An',    N'0901234567  ', N'Khoa CNTT1 - PTIT Hà Nội'),
(N'GV002   ', N'Trần Thị',   N'Bình',  N'0912345678  ', N'Khoa CNTT2 - PTIT TP.HCM'),
(N'GV003   ', N'Lê Hoàng',   N'Cường', N'0987654321  ', N'Khoa An toàn thông tin - PTIT'),
(N'GV004   ', N'Phạm Minh',  N'Đức',   N'0977889900  ', N'Bộ môn Công nghệ phần mềm - PTIT'),
(N'GV-8-4  ', N'Đặng Quốc',  N'Huy',   N'0934567890  ', N'Khoa Đa phương tiện - PTIT');

-- 9.4. Dữ liệu bảng SINHVIEN (6 sinh viên)
INSERT INTO dbo.SINHVIEN (MASV, HO, TEN, NGAYSINH, DIACHI, MALOP, DAXOA) VALUES
(N'SV001   ', N'Lê Văn',       N'Nam',   '2004-05-12', N'Quận 9, TP. Thủ Đức', N'CNTT01  ', 0),
(N'SV002   ', N'Phạm Thị',     N'Hoa',   '2004-08-20', N'Quận Bình Thạnh, TP.HCM', N'CNTT01  ', 0),
(N'SV003   ', N'Hoàng Minh',   N'Tuấn',  '2004-02-15', N'Quận 1, TP.HCM', N'CNTT01  ', 0),
(N'SV004   ', N'Đỗ Ngọc',      N'Lan',   '2004-11-03', N'TP. Biên Hòa, Đồng Nai', N'CNTT02  ', 0),
(N'SV005   ', N'Vũ Quốc',      N'Khánh', '2004-09-02', N'TP. Dĩ An, Bình Dương', N'CNTT02  ', 0),
(N'SV006   ', N'Ngô Gia',      N'Bảo',   '2004-04-30', N'Quận Tân Bình, TP.HCM', N'ATTT01  ', 0);

-- 9.5. Dữ liệu mẫu bảng BODE
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

-- 9.6. Dữ liệu mẫu bảng GIAOVIEN_DANGKY
INSERT INTO dbo.GIAOVIEN_DANGKY (MAGV, MALOP, MAMH, TRINHDO, NGAYTHI, LAN, SOCAUTHI, THOIGIAN) VALUES
(N'GV001   ', N'CNTT01  ', N'CSDL ', N'A', GETDATE(), 1, 10, 15);

-- 9.7. Dữ liệu mẫu bảng BANGDIEM
INSERT INTO dbo.BANGDIEM (MASV, MAMH, LAN, NGAYTHI, DIEM, BAITHI) VALUES
(N'SV001   ', N'CSDL ', 1, GETDATE(), 9.0, N'1:B;2:A;3:C;4:B;5:C;6:B;7:A;8:C;9:B;10:A'),
(N'SV002   ', N'CSDL ', 1, GETDATE(), 8.0, N'1:B;2:A;3:C;4:B;5:C;6:B;7:A;8:A;9:B;10:B');
GO

-- 9.8. Cấp SQL Server Login + Database User cho Giáo viên (GV001, GV002, GV-8-4) và Sinh viên (SV001..SV006)
-- Nếu SQL Login đã tồn tại từ trước trên Server thì chỉ tạo/ánh xạ Database User và gán Role
DECLARE @TeacherList TABLE (MAGV NVARCHAR(8));
INSERT INTO @TeacherList VALUES (N'GV001'), (N'GV002'), (N'GV-8-4');

DECLARE @CurGV NVARCHAR(8);
DECLARE curInitGV CURSOR LOCAL FAST_FORWARD FOR SELECT MAGV FROM @TeacherList;
OPEN curInitGV;
FETCH NEXT FROM curInitGV INTO @CurGV;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @CurGV COLLATE DATABASE_DEFAULT)
    BEGIN
        EXEC dbo.sp_TaoTaiKhoanGiaoVien @MaGV = @CurGV, @TenDangNhap = @CurGV, @MatKhauPlain = N'123456';
    END
    ELSE
    BEGIN
        DECLARE @SqlEnGV NVARCHAR(MAX) = N'ALTER LOGIN ' + QUOTENAME(@CurGV) + N' ENABLE;';
        EXEC sys.sp_executesql @SqlEnGV;
        IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @CurGV COLLATE DATABASE_DEFAULT)
        BEGIN
            DECLARE @SqlUsrGV NVARCHAR(MAX) = N'CREATE USER ' + QUOTENAME(@CurGV) + N' FOR LOGIN ' + QUOTENAME(@CurGV) + N';';
            EXEC sys.sp_executesql @SqlUsrGV;
        END
        DECLARE @SqlRoleGV NVARCHAR(MAX) = N'ALTER ROLE [role_GiaoVien] ADD MEMBER ' + QUOTENAME(@CurGV) + N';';
        EXEC sys.sp_executesql @SqlRoleGV;
    END
    FETCH NEXT FROM curInitGV INTO @CurGV;
END
CLOSE curInitGV;
DEALLOCATE curInitGV;
GO

DECLARE @CurSV NCHAR(8);
DECLARE curInitSV CURSOR LOCAL FAST_FORWARD FOR SELECT MASV FROM dbo.SINHVIEN WHERE DAXOA = 0;
OPEN curInitSV;
FETCH NEXT FROM curInitSV INTO @CurSV;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.sp_CapTaiKhoanSinhVien @MaSV = @CurSV, @MatKhauPlain = N'123456';
    FETCH NEXT FROM curInitSV INTO @CurSV;
END
CLOSE curInitSV;
DEALLOCATE curInitSV;
GO

PRINT N'=== ĐÃ KHỞI TẠO THÀNH CÔNG CƠ SỞ DỮ LIỆU TRAC_NGHIEM (SQL SERVER LOGIN & DATABASE ROLE) ===';
GO
