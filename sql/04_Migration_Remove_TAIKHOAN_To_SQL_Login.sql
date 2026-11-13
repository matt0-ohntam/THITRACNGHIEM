/*==============================================================================
  HỆ THỐNG QUẢN LÝ THI TRẮC NGHIỆM PTIT (TRAC_NGHIEM)
  FILE: 04_Migration_Remove_TAIKHOAN_To_SQL_Login.sql
  PHỤ TRÁCH: NGƯỜI 1 – QUẢN TRỊ VÀ DỮ LIỆU NỀN
  MÔ TẢ:
    Script chuyển đổi an toàn cơ sở dữ liệu TRAC_NGHIEM hiện tại:
    1. Giữ nguyên 100% dữ liệu 7 bảng theo đề (LOP, MONHOC, SINHVIEN, GIAOVIEN,
       BODE, GIAOVIEN_DANGKY, BANGDIEM). Không tạo bất kỳ bảng tài khoản/vai trò nào.
    2. Bổ sung giáo viên mẫu GV-8-4 (nếu chưa có) để kiểm thử mã có dấu gạch ngang.
    3. Tạo Database Role: role_GiaoVien, role_SinhVien.
    4. Thiết lập cơ chế Certificate Signing (cert_QuanLyTaiKhoan_TracNghiem) giữa
       TRAC_NGHIEM và master để cấp quyền tối thiểu (ALTER ANY LOGIN, ALTER ANY CONNECTION,
       VIEW ANY DEFINITION) cho các Stored Procedure quản trị mà KHÔNG dùng sa hay sysadmin.
    5. Tạo mới / cập nhật các View, Trigger và Stored Procedure:
       - sp_LayThongTinDangNhapHienTai
       - sp_TaoTaiKhoanGiaoVien (Signed)
       - sp_ThuHoiTaiKhoanGiaoVien (Signed)
       - sp_KichHoatLaiTaiKhoanGiaoVien (Signed)
       - sp_DoiMatKhau
       - sp_LayDanhSachTaiKhoanGiaoVien (Signed)
       - sp_LayGiaoVienChuaCoTaiKhoan (Signed)
       - sp_LayDanhSachGiaoVien (Signed)
       - sp_TimKiemGiaoVien (Signed)
       - sp_LayThongKeTongQuan (Signed)
       - sp_XoaGiaoVien (Signed)
       - sp_CapTaiKhoanSinhVien, sp_ThemSinhVien, sp_XoaSinhVien, sp_PhucHoiSinhVien,
         sp_HoanTacThemSinhVien (Signed)
       - sp_LayLichThiSinhVien, sp_LayKetQuaThiCaNhan
    6. Phân quyền chi tiết cho role_GiaoVien và role_SinhVien.
    7. Khởi tạo SQL Server Login & Database User cho Giáo viên (GV001, GV002, GV-8-4)
       và Sinh viên (SV001..SV006).
    8. Xóa hoàn toàn các phụ thuộc cũ và DROP TABLE dbo.TAIKHOAN.
==============================================================================*/

USE [TRAC_NGHIEM];
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

/*==============================================================================
  PHẦN 1: CẬP NHẬT RÀNG BUỘC MÃ GIÁO VIÊN & BỔ SUNG DỮ LIỆU GV-8-4 (KHÔNG XÓA DỮ LIỆU CŨ)
==============================================================================*/

-- Đảm bảo bảng GIAOVIEN có bản ghi GV-8-4 phục vụ kiểm thử mã có dấu '-' và mục Tổng quan
IF NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = N'GV-8-4')
BEGIN
    INSERT INTO dbo.GIAOVIEN (MAGV, HO, TEN, SODTLL, DIACHI)
    VALUES (N'GV-8-4  ', N'Đặng Quốc', N'Huy', N'0934567890  ', N'Khoa Đa phương tiện - PTIT');
END
GO

-- Khôi phục tên SV006 nếu bị lỗi font trước đó
UPDATE dbo.SINHVIEN
SET TEN = N'Bảo'
WHERE RTRIM(MASV) = N'SV006' AND TEN <> N'Bảo';
GO

/*==============================================================================
  PHẦN 2: TẠO DATABASE ROLE (role_GiaoVien, role_SinhVien)
==============================================================================*/

IF DATABASE_PRINCIPAL_ID(N'role_GiaoVien') IS NULL
BEGIN
    CREATE ROLE [role_GiaoVien];
END
GO

IF DATABASE_PRINCIPAL_ID(N'role_SinhVien') IS NULL
BEGIN
    CREATE ROLE [role_SinhVien];
END
GO

/*==============================================================================
  PHẦN 3: CẬP NHẬT VIEW & TRIGGER (LOẠI BỎ THAM CHIẾU TỚI BẢNG TAIKHOAN)
==============================================================================*/

-- 3.1. View Danh sách sinh viên theo lớp (Không còn JOIN với dbo.TAIKHOAN)
CREATE OR ALTER VIEW dbo.vw_DanhSachSinhVienTheoLop
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

-- 3.2. View Danh sách giáo viên kèm trạng thái SQL Server Login (Không còn JOIN với dbo.TAIKHOAN)
CREATE OR ALTER VIEW dbo.vw_DanhSachGiaoVien
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

-- 3.3. Trigger bảo vệ không cho xóa Sinh viên đã có điểm thi (Bỏ phần UPDATE dbo.TAIKHOAN)
CREATE OR ALTER TRIGGER dbo.trg_SinhVien_ProtectAndSyncAccount
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
  PHẦN 4: CÁC STORED PROCEDURE XÁC THỰC, PHÂN QUYỀN, PROVISIONING VÀ NGHIỆP VỤ
==============================================================================*/

-- Xóa Stored Procedure sp_DangNhap cũ (vì không còn so sánh mật khẩu qua bảng TAIKHOAN)
IF OBJECT_ID(N'dbo.sp_DangNhap', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_DangNhap;
GO

-- 4.1. Stored Procedure lấy thông tin phiên đăng nhập từ SQL Server Login hiện tại
CREATE OR ALTER PROCEDURE dbo.sp_LayThongTinDangNhapHienTai
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @LoginName NVARCHAR(128) = LTRIM(RTRIM(SUSER_SNAME()));
    DECLARE @DbUserName NVARCHAR(128) = LTRIM(RTRIM(USER_NAME()));
    DECLARE @IsRoleGV BIT = CASE WHEN IS_MEMBER(N'role_GiaoVien') = 1 THEN 1 ELSE 0 END;
    DECLARE @IsRoleSV BIT = CASE WHEN IS_MEMBER(N'role_SinhVien') = 1 THEN 1 ELSE 0 END;

    -- Kiểm tra Login có đang bị vô hiệu hóa ở cấp Server hay không
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

    -- Kiểm tra ánh xạ Database User hợp lệ
    IF @DbUserName IS NULL OR @DbUserName = N'guest'
    BEGIN
        RAISERROR(N'Tài khoản SQL Login [%s] chưa được ánh xạ với Database User trong TRAC_NGHIEM!', 16, 1, @LoginName);
        RETURN;
    END

    -- Trường hợp 1: Thuộc role_GiaoVien -> Đối chiếu với MAGV trong bảng GIAOVIEN
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

    -- Trường hợp 2: Thuộc role_SinhVien -> Đối chiếu với MASV trong bảng SINHVIEN
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

-- 4.2. Stored Procedure Tạo Tài Khoản Giáo Viên bằng SQL Server Login + Database User + role_GiaoVien
CREATE OR ALTER PROCEDURE dbo.sp_TaoTaiKhoanGiaoVien
    @MaGV           NCHAR(8),
    @TenDangNhap    NVARCHAR(50),
    @MatKhauPlain   NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    -- 1. Kiểm tra người gọi phải thuộc role_GiaoVien (hoặc sysadmin khi quản trị khởi tạo)
    IF IS_MEMBER(N'role_GiaoVien') <> 1 AND IS_SRVROLEMEMBER(N'sysadmin') <> 1
    BEGIN
        RAISERROR(N'Truy cập bị từ chối: Chỉ giáo viên thuộc role_GiaoVien mới có quyền tạo tài khoản giáo viên!', 16, 1);
        RETURN;
    END

    DECLARE @MaGVClean NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaGV, N''))));
    DECLARE @LoginName NVARCHAR(50) = UPPER(LTRIM(RTRIM(ISNULL(@TenDangNhap, N''))));

    -- 2. Kiểm tra rỗng và định dạng hợp lệ (chỉ chấp nhận chữ cái A-Z, số 0-9, dấu _ và dấu -)
    IF LEN(@MaGVClean) = 0 OR LEN(@MaGVClean) > 8 OR @MaGVClean LIKE N'%[^A-Z0-9_-]%'
    BEGIN
        RAISERROR(N'Mã giáo viên (MAGV) không hợp lệ!', 16, 1);
        RETURN;
    END

    -- 3. Quy ước bắt buộc: Username của giáo viên phải đúng bằng MAGV
    IF @LoginName <> @MaGVClean
    BEGIN
        RAISERROR(N'Theo quy định bảo mật, Tên đăng nhập (SQL Login) của giáo viên bắt buộc phải trùng khớp với Mã giáo viên [%s]!', 16, 1, @MaGVClean);
        RETURN;
    END

    -- 4. Kiểm tra độ dài mật khẩu
    IF @MatKhauPlain IS NULL OR LEN(@MatKhauPlain) < 6 OR LEN(@MatKhauPlain) > 128
    BEGIN
        RAISERROR(N'Mật khẩu phải có độ dài từ 6 đến 128 ký tự!', 16, 1);
        RETURN;
    END

    -- 5. Kiểm tra MAGV có tồn tại trong bảng GIAOVIEN không
    IF NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = @MaGVClean)
    BEGIN
        RAISERROR(N'Mã giáo viên [%s] không tồn tại trong bảng GIAOVIEN!', 16, 1, @MaGVClean);
        RETURN;
    END

    -- 6. Kiểm tra không trùng với MASV của sinh viên
    IF EXISTS (SELECT 1 FROM dbo.SINHVIEN WHERE RTRIM(MASV) = @LoginName)
    BEGIN
        RAISERROR(N'Mã [%s] đang trùng với mã sinh viên trong hệ thống!', 16, 1, @LoginName);
        RETURN;
    END

    -- 7. Kiểm tra SQL Server Login đã tồn tại chưa
    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @LoginName COLLATE DATABASE_DEFAULT)
    BEGIN
        RAISERROR(N'SQL Server Login [%s] đã tồn tại! Mỗi giáo viên chỉ có một tài khoản duy nhất.', 16, 1, @LoginName);
        RETURN;
    END

    -- 8. Kiểm tra Database User đã tồn tại trong TRAC_NGHIEM chưa
    IF EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @LoginName COLLATE DATABASE_DEFAULT)
    BEGIN
        RAISERROR(N'Database User [%s] đã tồn tại trong cơ sở dữ liệu TRAC_NGHIEM!', 16, 1, @LoginName);
        RETURN;
    END

    -- 9. Thực thi tạo SQL Login, Database User và gán vào role_GiaoVien an toàn (dùng QUOTENAME chống SQL Injection)
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

        -- Dọn dẹp nếu lỗi xảy ra giữa chừng (Compensation)
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

-- 4.3. Stored Procedure Thu Hồi (Vô hiệu hóa) Tài Khoản Giáo Viên
CREATE OR ALTER PROCEDURE dbo.sp_ThuHoiTaiKhoanGiaoVien
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

    -- Không cho phép tự vô hiệu hóa chính Login đang đăng nhập
    IF UPPER(LTRIM(RTRIM(SUSER_SNAME()))) = @MaGVClean
    BEGIN
        RAISERROR(N'Không thể tự thu hồi (vô hiệu hóa) tài khoản đang đăng nhập của chính bạn!', 16, 1);
        RETURN;
    END

    -- Kiểm tra giáo viên tồn tại trong bảng GIAOVIEN
    IF NOT EXISTS (SELECT 1 FROM dbo.GIAOVIEN WHERE RTRIM(MAGV) = @MaGVClean)
    BEGIN
        RAISERROR(N'Mã giáo viên [%s] không tồn tại trong bảng GIAOVIEN!', 16, 1, @MaGVClean);
        RETURN;
    END

    -- Kiểm tra SQL Server Login tồn tại
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
        -- 1. Vô hiệu hóa SQL Login (chặn toàn bộ kết nối mới)
        DECLARE @SqlDisable NVARCHAR(MAX) = N'ALTER LOGIN ' + QUOTENAME(@MaGVClean) + N' DISABLE;';
        EXEC sys.sp_executesql @SqlDisable;

        -- 2. Đóng ngay các session đang mở của Login này (nếu có) để chặn thao tác mới ngay lập tức
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

-- 4.4. Stored Procedure Kích Hoạt Lại Tài Khoản Giáo Viên
CREATE OR ALTER PROCEDURE dbo.sp_KichHoatLaiTaiKhoanGiaoVien
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
        -- 1. Kích hoạt lại SQL Server Login
        DECLARE @SqlEnable NVARCHAR(MAX) = N'ALTER LOGIN ' + QUOTENAME(@MaGVClean) + N' ENABLE;';
        EXEC sys.sp_executesql @SqlEnable;

        -- 2. Đảm bảo Database User tồn tại trong TRAC_NGHIEM
        IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = @MaGVClean COLLATE DATABASE_DEFAULT AND type = 'S')
        BEGIN
            DECLARE @SqlCreateUser NVARCHAR(MAX) = N'CREATE USER ' + QUOTENAME(@MaGVClean) + N' FOR LOGIN ' + QUOTENAME(@MaGVClean) + N';';
            EXEC sys.sp_executesql @SqlCreateUser;
        END

        -- 3. Đảm bảo Database User thuộc role_GiaoVien
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

-- 4.5. Stored Procedure Đổi Mật Khẩu của chính Giáo Viên đang đăng nhập (Bằng ALTER LOGIN ... WITH PASSWORD ... OLD_PASSWORD ...)
CREATE OR ALTER PROCEDURE dbo.sp_DoiMatKhau
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

    -- Đảm bảo tài khoản hiện tại tương ứng với MAGV trong GIAOVIEN
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
        -- Sử dụng cơ chế gốc của SQL Server: chỉ đổi mật khẩu cho chính SUSER_SNAME() và bắt buộc xác thực OLD_PASSWORD
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

-- 4.6. Stored Procedure lấy danh sách toàn bộ Giáo viên kèm trạng thái SQL Server Login (Phục vụ FrmTaoTaiKhoan / Thu hồi)
CREATE OR ALTER PROCEDURE dbo.sp_LayDanhSachTaiKhoanGiaoVien
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
            THEN 0 -- Chưa tạo
            WHEN sp.is_disabled = 1
            THEN 2 -- Đã vô hiệu hóa
            ELSE 1 -- Đang hoạt động
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

-- 4.7. Stored Procedure lấy danh sách Giáo viên chưa có SQL Server Login
CREATE OR ALTER PROCEDURE dbo.sp_LayGiaoVienChuaCoTaiKhoan
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

-- 4.8. Stored Procedure lấy danh sách Giáo viên và Tìm kiếm Giáo viên (cho FrmGiaoVien)
CREATE OR ALTER PROCEDURE dbo.sp_LayDanhSachGiaoVien
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

CREATE OR ALTER PROCEDURE dbo.sp_TimKiemGiaoVien
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

-- 4.9. Cập nhật sp_XoaGiaoVien (Kiểm tra SQL Login & Database User thay vì bảng TAIKHOAN)
CREATE OR ALTER PROCEDURE dbo.sp_XoaGiaoVien
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

    -- Chặn xóa nếu SQL Login đang hoạt động (is_disabled = 0)
    IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = @MaGVClean COLLATE DATABASE_DEFAULT AND type = 'S' AND is_disabled = 0)
    BEGIN
        RAISERROR(N'Không thể xóa giáo viên [%s] vì giáo viên đang có tài khoản SQL Server Login đang hoạt động! Vui lòng thu hồi tài khoản trước.', 16, 1, @MaGVClean);
        RETURN;
    END

    -- Nếu Login đã thu hồi (is_disabled = 1) và không có dữ liệu phụ thuộc, dọn Database User & Login trước khi xóa hồ sơ
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

-- 4.10. Stored Procedure Thống kê Tổng quan (Sửa triệt để lỗi thống kê GV & kiểm tra GV-8-4)
CREATE OR ALTER PROCEDURE dbo.sp_LayThongKeTongQuan
AS
BEGIN
    SET NOCOUNT ON;

    -- Bảng tạm phân tích trạng thái Login của từng Giáo viên (1 dòng / 1 MAGV, không trùng lặp)
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

-- 4.11. Stored Procedure Quản trị Cấp/Đồng bộ SQL Login cho Sinh viên & Quản lý Sinh viên
CREATE OR ALTER PROCEDURE dbo.sp_CapTaiKhoanSinhVien
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

CREATE OR ALTER PROCEDURE dbo.sp_ThemSinhVien
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

CREATE OR ALTER PROCEDURE dbo.sp_XoaSinhVien
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

CREATE OR ALTER PROCEDURE dbo.sp_PhucHoiSinhVien
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

CREATE OR ALTER PROCEDURE dbo.sp_HoanTacThemSinhVien
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

-- 4.12. Stored Procedure dành cho Sinh viên xem Lịch thi của lớp và Kết quả cá nhân (Giới hạn chặt theo SUSER_SNAME())
CREATE OR ALTER PROCEDURE dbo.sp_LayLichThiSinhVien
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

CREATE OR ALTER PROCEDURE dbo.sp_LayKetQuaThiCaNhan
    @MaSV   NCHAR(8) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CallerLogin NVARCHAR(128) = UPPER(LTRIM(RTRIM(SUSER_SNAME())));
    DECLARE @TargetMaSV NVARCHAR(8) = UPPER(LTRIM(RTRIM(ISNULL(@MaSV, @CallerLogin))));

    -- Nếu người gọi thuộc role_SinhVien thì chỉ được xem điểm của chính SUSER_SNAME()
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

/*==============================================================================
  PHẦN 5: THIẾT LẬP CERTIFICATE SIGNING CHO CÁC STORED PROCEDURE QUẢN TRỊ
==============================================================================*/

-- Dọn chữ ký và chứng thư cũ (nếu chạy lại script)
IF EXISTS (SELECT 1 FROM sys.certificates WHERE name = N'cert_QuanLyTaiKhoan_TracNghiem')
BEGIN
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_TaoTaiKhoanGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_ThuHoiTaiKhoanGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_KichHoatLaiTaiKhoanGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_LayDanhSachTaiKhoanGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_LayGiaoVienChuaCoTaiKhoan BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_LayDanhSachGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_TimKiemGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_LayThongKeTongQuan BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_XoaGiaoVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_CapTaiKhoanSinhVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_ThemSinhVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_XoaSinhVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_PhucHoiSinhVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_HoanTacThemSinhVien BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH
    BEGIN TRY DROP SIGNATURE FROM dbo.sp_LayThongTinDangNhapHienTai BY CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]; END TRY BEGIN CATCH END CATCH

    IF DATABASE_PRINCIPAL_ID(N'user_Cert_QuanLyTaiKhoan') IS NOT NULL
        DROP USER [user_Cert_QuanLyTaiKhoan];

    DROP CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem];
END
GO

-- 5.1. Tạo Certificate trong TRAC_NGHIEM
CREATE CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem]
    ENCRYPTION BY PASSWORD = N'Ptit_Cert_TracNghiem#2026!'
    WITH SUBJECT = N'Certificate ky Stored Procedure quan tri SQL Login TRAC_NGHIEM',
         EXPIRY_DATE = '2099-12-31';
GO

-- 5.2. Tạo Database User từ Certificate trong TRAC_NGHIEM và cấp quyền quản lý User/Role trong DB
CREATE USER [user_Cert_QuanLyTaiKhoan] FROM CERTIFICATE [cert_QuanLyTaiKhoan_TracNghiem];
GRANT ALTER ANY USER TO [user_Cert_QuanLyTaiKhoan];
GRANT ALTER ON ROLE::[role_GiaoVien] TO [user_Cert_QuanLyTaiKhoan];
GRANT ALTER ON ROLE::[role_SinhVien] TO [user_Cert_QuanLyTaiKhoan];
GRANT VIEW DEFINITION TO [user_Cert_QuanLyTaiKhoan];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[dbo] TO [user_Cert_QuanLyTaiKhoan];
GO

-- 5.3. Ký (ADD SIGNATURE) các Stored Procedure cần tương tác với Login/User/Catalog
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
GO

-- 5.4. Sao chép Public Key của Certificate sang master và tạo Server Login cấp quyền tối thiểu
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

/*==============================================================================
  PHẦN 6: PHÂN QUYỀN DATABASE ROLE (NGUYÊN TẮC QUYỀN TỐI THIỂU - LEAST PRIVILEGE)
==============================================================================*/

-- 6.1. Quyền của role_GiaoVien:
--   - Đọc dữ liệu trên các bảng/View phục vụ quản lý & hiển thị
--   - Thực thi (EXECUTE) các Stored Procedure nghiệp vụ Giáo viên
--   - Quyền BACKUP DATABASE trên TRAC_NGHIEM
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

GRANT BACKUP DATABASE TO [role_GiaoVien];
GRANT VIEW DEFINITION TO [role_GiaoVien];
GO

-- 6.2. Quyền của role_SinhVien (Giới hạn nghiêm ngặt: chỉ thi & xem kết quả cá nhân):
GRANT EXECUTE ON dbo.sp_LayThongTinDangNhapHienTai TO [role_SinhVien];
GRANT EXECUTE ON dbo.sp_LayLichThiSinhVien TO [role_SinhVien];
GRANT EXECUTE ON dbo.sp_LayKetQuaThiCaNhan TO [role_SinhVien];

-- Chặn (DENY) tuyệt đối quyền truy cập các bảng và Stored Procedure quản trị đối với role_SinhVien
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
  PHẦN 7: KHỞI TẠO SQL SERVER LOGIN & DATABASE USER CHO GIÁO VIÊN VÀ SINH VIÊN
==============================================================================*/

-- 7.1. Cấp SQL Server Login cho Giáo viên: GV001, GV002, GV-8-4 (GV003, GV004 để trống chưa có Login để kiểm thử Tạo tài khoản)
DECLARE @InitPass NVARCHAR(128) = N'123456';

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'GV001')
    EXEC dbo.sp_TaoTaiKhoanGiaoVien @MaGV = N'GV001', @TenDangNhap = N'GV001', @MatKhauPlain = @InitPass;

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'GV002')
    EXEC dbo.sp_TaoTaiKhoanGiaoVien @MaGV = N'GV002', @TenDangNhap = N'GV002', @MatKhauPlain = @InitPass;

IF NOT EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'GV-8-4')
    EXEC dbo.sp_TaoTaiKhoanGiaoVien @MaGV = N'GV-8-4', @TenDangNhap = N'GV-8-4', @MatKhauPlain = @InitPass;
GO

-- 7.2. Cấp SQL Server Login cho tất cả Sinh viên hiện có (SV001..SV006)
DECLARE @SvMa NCHAR(8);
DECLARE curSV CURSOR LOCAL FAST_FORWARD FOR
    SELECT MASV FROM dbo.SINHVIEN WHERE DAXOA = 0;

OPEN curSV;
FETCH NEXT FROM curSV INTO @SvMa;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC dbo.sp_CapTaiKhoanSinhVien @MaSV = @SvMa, @MatKhauPlain = N'123456';
    FETCH NEXT FROM curSV INTO @SvMa;
END
CLOSE curSV;
DEALLOCATE curSV;
GO

/*==============================================================================
  PHẦN 8: XÓA BẢNG dbo.TAIKHOAN (SAU KHI ĐÃ CHUYỂN ĐỔI HOÀN TẤT SANG SQL LOGIN)
==============================================================================*/

IF OBJECT_ID(N'dbo.TAIKHOAN', N'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.TAIKHOAN;
    PRINT N'=== ĐÃ XÓA BẢNG dbo.TAIKHOAN THÀNH CÔNG ===';
END
GO

PRINT N'=== HOÀN TẤT CHUYỂN ĐỔI SANG CƠ CHẾ SQL SERVER LOGIN & DATABASE ROLE ===';
GO
