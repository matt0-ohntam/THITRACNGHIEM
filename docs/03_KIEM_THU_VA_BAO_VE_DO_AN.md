# TÀI LIỆU KIỂM THỬ, BACKUP/RESTORE & HƯỚNG DẪN BẢO VỆ ĐỒ ÁN (`TRAC_NGHIEM`)
*(Giai đoạn 7, 8, 9 & 10: Lý thuyết chuyên sâu, Bộ 22 Test Case chuẩn, Kịch bản Demo & Bộ câu hỏi vấn đáp bảo vệ đồ án)*

---

## 1. GIẢI THÍCH BẢN CHẤT BACKUP & RESTORE TRONG SQL SERVER (GIAI ĐOẠN 7)

### 1.1. Backup là gì? Tại sao cần Backup?
- **Backup (Sao lưu):** Là quá trình trích xuất toàn bộ trang dữ liệu (Data Pages – 8KB/page) từ tập tin `.mdf` và nhật ký giao dịch (Transaction Log – `.ldf`) đang hoạt động của database `TRAC_NGHIEM` ra một tập tin lưu trữ độc lập trên ổ đĩa có đuôi `.bak`.
- **Tại sao cần Backup:** Đảm bảo an toàn dữ liệu (Disaster Recovery) trước các sự cố phần cứng, lỗi thao tác xóa nhầm của người dùng, hoặc hỏng dữ liệu.
- **Cách SQL Server thực hiện Full Backup:**
  ```sql
  BACKUP DATABASE [TRAC_NGHIEM]
  TO DISK = @BackupPath
  WITH INIT,
       NAME = N'TRAC_NGHIEM-Full Database Backup',
       DESCRIPTION = N'Sao lưu từ phần mềm Quản lý Thi Trắc Nghiệm PTIT';
  ```
  - `WITH INIT`: Ghi đè nội dung mới lên file `.bak` nếu file đã tồn tại.
  - `WITH NOINIT`: Ghi nối tiếp (Append) bản sao lưu mới vào cuối file `.bak` hiện có.

### 1.2. Restore là gì? Vì sao Restore thường thất bại nếu không xử lý kết nối?
- **Restore (Phục hồi):** Là quá trình tái tạo lại toàn bộ các trang dữ liệu và trạng thái giao dịch từ file `.bak` vào lại database `TRAC_NGHIEM`.
- **Vì sao Restore dễ thất bại (`Database is in use`)?**
  1. **Đứng ngay trong database `TRAC_NGHIEM` khi gọi lệnh `RESTORE`:** Một kết nối không thể tự ghi đè chính database mà nó đang mở. Do đó trong `DatabaseConnection.GetMasterConnectionString()`, ứng dụng bắt buộc chuyển `InitialCatalog = "master"` trước khi gọi lệnh Restore.
  2. **Còn các kết nối trong Connection Pool của ADO.NET hoặc từ SSMS đang mở vào `TRAC_NGHIEM`:** SQL Server yêu cầu khóa độc quyền (`Exclusive Lock`) trên database khi Restore.
- **Giải pháp an toàn đã triển khai trong `BackupRestoreDAL.cs` & `master.dbo.sp_RestoreDatabase_TracNghiem`:**
  1. Gọi `SqlConnection.ClearAllPools()` ở C# để đóng các kết nối nhàn rỗi trong Pool.
  2. Chạy `RESTORE VERIFYONLY FROM DISK = @BackupPath` trước để chắc chắn file `.bak` không bị hỏng trước khi ngắt kết nối.
  3. Đưa database về chế độ đơn người dùng và ngắt các phiên đang mở:
     ```sql
     ALTER DATABASE [TRAC_NGHIEM] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
     ```
  4. Thực thi khôi phục ghi đè và mở sẵn database (`WITH RECOVERY`):
     ```sql
     RESTORE DATABASE [TRAC_NGHIEM] FROM DISK = @BackupPath WITH REPLACE, RECOVERY;
     ```
  5. Đưa database trở lại chế độ đa người dùng trong cả khối `TRY` và `CATCH`:
     ```sql
     ALTER DATABASE [TRAC_NGHIEM] SET MULTI_USER;
     ```
  6. Tự động đồng bộ lại ánh xạ `Database User <-> SQL Login` (`ALTER USER ... WITH LOGIN`) để không bao giờ xảy ra lỗi *Orphaned User* sau khi Restore.

---

## 2. GIẢI THÍCH CÁC KHÁI NIỆM HỆ QUẢN TRỊ CSDL DÙNG TRONG ĐỒ ÁN

| Khái niệm | Bản chất & Tác dụng | Áp dụng thực tế trong đồ án `TRAC_NGHIEM` |
|---|---|---|
| **Primary Key (Khóa chính)** | Định danh duy nhất mỗi bản ghi trong bảng, không chấp nhận `NULL`, tự động tạo Clustered Index. | Khóa đơn: `LOP(MALOP)`, `MONHOC(MAMH)`, `SINHVIEN(MASV)`, `GIAOVIEN(MAGV)`, `BODE(CAUHOI)`. Khóa phức hợp 3 cột: `GIAOVIEN_DANGKY(MALOP, MAMH, LAN)`, `BANGDIEM(MASV, MAMH, LAN)`. |
| **Foreign Key (Khóa ngoại)** | Bảo toàn tham chiếu (Referential Integrity) giữa bảng con và bảng cha. | 7 khóa ngoại giữa dữ liệu nền Người 1 và dữ liệu thi Người 2 (`ON DELETE NO ACTION` để chặn xóa dữ liệu đang có lịch sử thi). |
| **UNIQUE & CHECK** | `UNIQUE` chống trùng lặp trên cột không phải khóa chính; `CHECK` kiểm tra miền giá trị hợp lệ. | `UQ_Lop_TenLop`, `UQ_Monhoc_TenMH`, `CK_BangDiem_Diem (0..10)`, `CK_GVDK_Lan (1..2)`. |
| **Soft Delete (Xóa mềm)** | Dùng cột cờ `DAXOA BIT DEFAULT 0` thay vì `DELETE` vật lý ngay lập tức. | Áp dụng trên `MONHOC` và `SINHVIEN` để hỗ trợ nút **Phục hồi** ngay cả sau khi đóng và mở lại chương trình. |
| **SQL Login, User & Role** | `Login` xác thực ở cấp Server (`sys.server_principals`); `User` ánh xạ vào Database (`sys.database_principals`); `Role` gom nhóm quyền hạn. | Không dùng bảng `TAIKHOAN`. Giáo viên đăng nhập bằng `MAGV` (`role_GiaoVien`), Sinh viên đăng nhập bằng `MASV` (`role_SinhVien`). |
| **Certificate Signing** | Ký chứng thư số lên Stored Procedure để cấp quyền đặc thù tạm thời chỉ trong phạm vi thực thi SP đó. | Cho phép `role_GiaoVien` gọi `sp_TaoTaiKhoanGiaoVien`, `sp_ThuHoiTaiKhoanGiaoVien`, `sp_RestoreDatabase_TracNghiem` mà không cần quyền `sysadmin` hay mật khẩu `sa`. |
| **View, Trigger, Index** | `View` đóng gói truy vấn phức tạp; `Trigger` tự động kiểm tra ràng buộc liên bảng khi `DELETE`; `Index` tăng tốc tìm kiếm. | 4 View (`vw_DanhSachLop`, `vw_DanhSachMonHoc`, `vw_DanhSachSinhVienTheoLop`, `vw_DanhSachGiaoVien`), 4 Trigger bảo vệ xóa vật lý, và các Nonclustered Index trên cột tìm kiếm/khóa ngoại. |

---

## 3. BẢNG KẾT QUẢ KIỂM THỬ TOÀN DIỆN 22 TEST CASE (GIAI ĐOẠN 9)

Có thể chạy tự động toàn bộ 22 Test Case bất kỳ lúc nào bằng lệnh:
```powershell
.\TRAC_NGHIEM\bin\Debug\TRAC_NGHIEM.exe --self-test-all
```

| Mã TC | Chức năng | Dữ liệu đầu vào | Kết quả mong đợi | Kết quả thực tế | Trạng thái |
|---|---|---|---|---|---|
| **TC01** | Xóa bảng `TAIKHOAN` | Kiểm tra `OBJECT_ID('dbo.TAIKHOAN')` | Không còn bảng `TAIKHOAN`, đăng nhập bằng SQL Login hoạt động bình thường | `OBJECT_ID IS NULL`, đăng nhập `GV001` thành công | **PASS** |
| **TC02** | Đăng nhập GV hợp lệ | `GV-8-4` / `123456` | Xác thực SQL Login thành công, nhận diện `role_GiaoVien` | Đăng nhập thành công Đặng Quốc Huy (`role_GiaoVien`) | **PASS** |
| **TC03** | Đăng nhập GV sai mật khẩu | `GV-8-4` / `SaiMatKhau999!` | Từ chối kết nối SQL Server, thông báo lỗi rõ ràng | Báo lỗi xác thực SQL Login | **PASS** |
| **TC04** | Đăng nhập Login bị `DISABLE` | Login ở trạng thái `is_disabled = 1` | SQL Server từ chối đăng nhập, báo tài khoản đã bị thu hồi | Chặn đăng nhập chính xác | **PASS** |
| **TC05** | Đăng nhập SV hợp lệ | `SV001` / `123456` | Vào giao diện Sinh viên (`role_SinhVien`), chỉ xem lịch thi & điểm cá nhân | Hiển thị đúng cổng thông tin SV Lê Văn Nam | **PASS** |
| **TC06** | SV truy cập chức năng GV | `SV001` gọi Thêm/Xóa môn học | Bị chặn ở cả C# BLL và SQL Server Role | Từ chối truy cập ở cả 2 tầng | **PASS** |
| **TC07** | Tạo SQL Login cho GV mới | `MAGV = GV-T01`, Pass `123456` | Tạo SQL Login, Database User và gán `role_GiaoVien` | Tạo thành công, đăng nhập được ngay | **PASS** |
| **TC08** | Tạo tài khoản trùng Login | Gọi tạo lại cho `GV-T01` | Báo lỗi Login đã tồn tại, không tạo trùng | Chặn tạo trùng chính xác | **PASS** |
| **TC09** | Tạo Login cho `MAGV` không tồn tại | `MAGV = GV999` | Báo lỗi `MAGV` không tồn tại trong `GIAOVIEN` | Chặn tạo chính xác | **PASS** |
| **TC10** | Thu hồi tài khoản GV | Thu hồi `GV-T01` | Thực thi `ALTER LOGIN [GV-T01] DISABLE`, trạng thái `Đã vô hiệu hóa` | Thu hồi thành công, giữ nguyên dữ liệu GV | **PASS** |
| **TC11** | Kích hoạt lại tài khoản GV | Kích hoạt lại `GV-T01` | Thực thi `ALTER LOGIN [GV-T01] ENABLE`, đăng nhập lại được | Kích hoạt và đăng nhập lại thành công | **PASS** |
| **TC12** | Tự thu hồi tài khoản đang dùng | Đang đăng nhập `GV-T01`, tự thu hồi `GV-T01` | Bị từ chối để tránh tự khóa phiên làm việc hiện tại | Chặn tự thu hồi chính xác | **PASS** |
| **TC13** | Kiểm tra mục Tổng quan | Đối chiếu 5 chỉ số GV & Login | Tổng GV = `5`, Có Login = `3`, Active = `3`, Disabled = `0`, Chưa cấp = `2` | Khớp 100% giữa `GIAOVIEN` và `sys.server_principals` | **PASS** |
| **TC14** | Kiểm tra mã `GV-8-4` | Xem danh sách GV & Tổng quan | Hiển thị đúng `GV-8-4` (`Đặng Quốc Huy - Đang hoạt động`) | Hiển thị đầy đủ, không bị lọc sót | **PASS** |
| **TC15** | Đổi mật khẩu SQL Login GV | Đổi `123456` $\rightarrow$ `654321` $\rightarrow$ `123456` | Mật khẩu cũ hết hiệu lực, mật khẩu mới đăng nhập thành công | Thực thi `ALTER LOGIN ... OLD_PASSWORD` thành công | **PASS** |
| **TC16** | Chống SQL Injection tại Login | `' OR '1'='1`, `' OR 1=1 --`, `admin'--` | Không vượt qua được xác thực, không lỗi cú pháp SQL | Chặn 100% các payload | **PASS** |
| **TC17** | Chống SQL Injection tại Tìm kiếm | `' OR 1=1 --`, `'; DROP TABLE GIAOVIEN; --` | Trả về 0 dòng, bảng `GIAOVIEN` nguyên vẹn 5 giáo viên | An toàn tuyệt đối nhờ `SqlParameter` | **PASS** |
| **TC18** | Kiểm tra CRUD & 7 Form giao diện | Thêm/Sửa/Xóa/Phục hồi MH, Lớp, SV, GV | Các nút bấm hoạt động đúng, chặn xóa dữ liệu đang có bài thi | Khởi tạo và thực thi thành công cả 7 Form | **PASS** |
| **TC19** | Sao lưu CSDL (`Backup`) | `GV001` sao lưu ra `TRAC_NGHIEM_Full.bak` | Tạo file `.bak` thành công trên ổ đĩa | Sao lưu thành công | **PASS** |
| **TC20** | Chặn Backup/Restore sai đường dẫn | Đường dẫn `Z:\...` hoặc file `.md` giả làm `.bak` | Báo lỗi đường dẫn/file không hợp lệ, không làm treo ứng dụng | Chặn và thông báo rõ ràng | **PASS** |
| **TC21** | Kiểm tra file `.bak` (`VERIFYONLY`) | Kiểm tra `TRAC_NGHIEM_Full.bak` | Đọc đúng Header (`DatabaseName = TRAC_NGHIEM`, Server, Ngày sao lưu) | Xác thực hợp lệ | **PASS** |
| **TC22** | Phục hồi CSDL (`Restore`) | `GV001` thực hiện Restore từ `TRAC_NGHIEM_Full.bak` | Tự động chuyển `SINGLE_USER` $\rightarrow$ `RESTORE` $\rightarrow$ `MULTI_USER`, kết nối lại và đủ 11 chỉ số dữ liệu | Phục hồi và đối chiếu dữ liệu thành công | **PASS** |

---

## 4. KỊCH BẢN DEMO TRƯỚC GIẢNG VIÊN KHI BẢO VỆ ĐỒ ÁN (GIAI ĐOẠN 10)

1. **Bước 1 – Chứng minh kiến trúc CSDL & Bảo mật SQL Server trong SSMS (2 phút):**
   - Mở SSMS $\rightarrow$ `Databases` $\rightarrow$ `TRAC_NGHIEM` $\rightarrow$ `Tables`: Chỉ rõ chỉ có đúng **7 bảng theo đề bài** (`LOP`, `MONHOC`, `SINHVIEN`, `GIAOVIEN`, `BODE`, `GIAOVIEN_DANGKY`, `BANGDIEM`), hoàn toàn không dùng bảng `TAIKHOAN`.
   - Mở `Security` $\rightarrow$ `Roles` $\rightarrow$ `Database Roles`: Chỉ rõ 2 Role `role_GiaoVien` và `role_SinhVien`.
   - Mở `Security` $\rightarrow$ `Certificates`: Chỉ rõ `cert_QuanLyTaiKhoan_TracNghiem` dùng để ký các Stored Procedure quản trị.
2. **Bước 2 – Demo Đăng nhập & Mục Tổng quan (`GV-8-4`) (2 phút):**
   - Chạy ứng dụng WinForms, thử nhập sai mật khẩu hoặc payload `' OR '1'='1` $\rightarrow$ bị từ chối.
   - Đăng nhập bằng tài khoản giáo viên **`GV-8-4`** / mật khẩu **`123456`**.
   - Tại màn hình **Trang chủ (Tổng quan)**: Chỉ rõ 5 thẻ số liệu khớp 100% thực tế (`Tổng GV = 5`, `Có Login = 3`, `Đang hoạt động = 3`, `Đã vô hiệu hóa = 0`, `Chưa cấp Login = 2`) và dòng `GV-8-4` đang hoạt động.
3. **Bước 3 – Demo Quản lý Môn học, Lớp – Subform Sinh viên, Giáo viên (3 phút):**
   - Vào **Quản lý Môn học**: Thêm môn mới $\rightarrow$ Sửa $\rightarrow$ Xóa mềm $\rightarrow$ Nhấn **Phục hồi** $\rightarrow$ Thử xóa môn `CSDL` (đang có câu hỏi trong `BODE`) để chứng minh hệ thống chặn xóa bảo toàn khóa ngoại.
   - Vào **Quản lý Lớp & Sinh viên (Subform)**: Chọn lớp `CNTT01` ở bảng trên $\rightarrow$ bảng dưới tự động lọc danh sách sinh viên của lớp `CNTT01`. Thêm 1 sinh viên mới (tự động được cấp SQL Login + `role_SinhVien`) $\rightarrow$ Xóa mềm $\rightarrow$ Phục hồi.
4. **Bước 4 – Demo Tạo tài khoản, Thu hồi & Kích hoạt lại SQL Login Giáo viên (3 phút):**
   - Vào **Quản lý TK Giảng viên (`FrmTaoTaiKhoan`)**:
     - Chọn giáo viên `GV003` (`Chưa tạo`) $\rightarrow$ nút ngữ cảnh hiện **`Tạo tài khoản`** $\rightarrow$ nhập mật khẩu `123456` $\rightarrow$ Tạo thành công.
     - Chọn dòng `GV003` (`Đang hoạt động`) $\rightarrow$ nút ngữ cảnh đổi thành **`Thu hồi tài khoản`** $\rightarrow$ nhấn Thu hồi (`ALTER LOGIN [GV003] DISABLE`).
     - Chọn lại dòng `GV003` (`Đã vô hiệu hóa`) $\rightarrow$ nút ngữ cảnh đổi thành **`Kích hoạt lại tài khoản`** $\rightarrow$ nhấn Kích hoạt lại (`ALTER LOGIN [GV003] ENABLE`).
5. **Bước 5 – Demo Backup & Restore CSDL và Phân quyền Sinh viên (2 phút):**
   - Vào **Backup & Restore (`FrmBackupRestore`)**: Nhấn **Thực hiện Backup (.bak)** $\rightarrow$ Chọn file vừa tạo $\rightarrow$ Nhấn **1. Kiểm tra file .bak** $\rightarrow$ Nhấn **2. Thực hiện Restore** $\rightarrow$ CSDL được phục hồi an toàn và kết nối lại ngay lập tức.
   - Nhấn **Đăng xuất** $\rightarrow$ Đăng nhập bằng tài khoản Sinh viên **`SV001`** / **`123456`** $\rightarrow$ Chỉ thấy lịch thi của lớp `CNTT01` và kết quả điểm của riêng `SV001`, toàn bộ menu quản trị đã bị khóa.

