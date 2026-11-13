# HỢP ĐỒNG TÍCH HỢP CƠ SỞ DỮ LIỆU (`DATABASE_CONTRACT.md`)
**Dự án:** Hệ thống Quản lý Thi Trắc Nghiệm PTIT (`TRAC_NGHIEM`)  
**Người bàn giao (Người 1):** Quản trị và Dữ liệu nền  
**Người nhận bàn giao (Người 2):** Nghiệp vụ thi (`BODE`, `GIAOVIEN_DANGKY`, `BANGDIEM`)

---

## 1. THÔNG TIN KẾT NỐI, BẢO MẬT SQL SERVER & TÊN DATABASE
* **Tên Database bắt buộc:** `TRAC_NGHIEM` (Không được tự ý đổi tên).
* **Cơ chế xác thực & phân quyền:**
  * **KHÔNG sử dụng bảng `TAIKHOAN` hay `VAITRO`**.
  * Xác thực trực tiếp bằng **SQL Server Login (SQL Server Authentication)** riêng cho từng người dùng:
    * Giáo viên: `SQL Login = User = MAGV` $\rightarrow$ thuộc Database Role `role_GiaoVien`.
    * Sinh viên: `SQL Login = User = MASV` $\rightarrow$ thuộc Database Role `role_SinhVien`.
* **Cấu hình máy chủ cơ sở trong `App.config` (Không lưu cứng tài khoản/mật khẩu người dùng):**
  ```xml
  <connectionStrings>
    <add name="TRAC_NGHIEM_DB"
         connectionString="Server=localhost;Database=TRAC_NGHIEM;TrustServerCertificate=True;Connect Timeout=10;"
         providerName="System.Data.SqlClient" />
  </connectionStrings>
  ```
* **File Script khởi tạo & chuyển đổi CSDL:**
  * Khởi tạo mới hoàn chỉnh: [sql/01_Setup_TRAC_NGHIEM.sql](file:///c:/THITRACNGHIEM/sql/01_Setup_TRAC_NGHIEM.sql)
  * Chuyển đổi từ CSDL cũ (xóa `TAIKHOAN`, giữ nguyên dữ liệu): [sql/04_Migration_Remove_TAIKHOAN_To_SQL_Login.sql](file:///c:/THITRACNGHIEM/sql/04_Migration_Remove_TAIKHOAN_To_SQL_Login.sql)

---

## 2. PHÂN ĐỊNH QUYỀN SỞ HỮU 7 BẢNG CỐT LÕI (THEO ĐÚNG ĐỀ BÀI)

| Tên bảng | Người quản lý | Quy tắc tích hợp bắt buộc |
|---|---|---|
| `dbo.LOP` | **Người 1** | Người 2 chỉ `SELECT` để lấy danh sách lớp khi Chuẩn bị thi / Xem bảng điểm. |
| `dbo.MONHOC` | **Người 1** | Người 2 khi `SELECT` danh sách môn học cần lọc `WHERE DAXOA = 0` (hoặc dùng `dbo.vw_DanhSachMonHoc`). |
| `dbo.SINHVIEN` | **Người 1** | Người 2 khi `SELECT` sinh viên cần lọc `WHERE DAXOA = 0` (hoặc dùng `dbo.vw_DanhSachSinhVienTheoLop`). |
| `dbo.GIAOVIEN` | **Người 1** | Người 2 tham chiếu `MAGV` từ `SessionManager.CurrentUser.MaGV` (tương ứng `SUSER_SNAME()`). |
| `dbo.BODE` | **Người 2** | Đã được tạo sẵn trong script nền kèm khóa ngoại và 12 câu hỏi mẫu. |
| `dbo.GIAOVIEN_DANGKY` | **Người 2** | Đã được tạo sẵn trong script nền kèm khóa chính `(MALOP, MAMH, LAN)` và ràng buộc `CHECK`. |
| `dbo.BANGDIEM` | **Người 2** | Đã được tạo sẵn trong script nền kèm khóa chính `(MASV, MAMH, LAN)` và ràng buộc `CHECK`. |

---

## 3. HỢP ĐỒNG KHÓA NGOẠI (FOREIGN KEY CONTRACT)

Các bảng nghiệp vụ của Người 2 phải tuân thủ đúng 7 khóa ngoại sau:
1. `BODE.MAMH` $\rightarrow$ `MONHOC.MAMH` (`NCHAR(5)`)
2. `BODE.MAGV` $\rightarrow$ `GIAOVIEN.MAGV` (`NCHAR(8)`)
3. `GIAOVIEN_DANGKY.MAGV` $\rightarrow$ `GIAOVIEN.MAGV` (`NCHAR(8)`)
4. `GIAOVIEN_DANGKY.MALOP` $\rightarrow$ `LOP.MALOP` (`NCHAR(8)`)
5. `GIAOVIEN_DANGKY.MAMH` $\rightarrow$ `MONHOC.MAMH` (`NCHAR(5)`)
6. `BANGDIEM.MASV` $\rightarrow$ `SINHVIEN.MASV` (`NCHAR(8)`)
7. `BANGDIEM.MAMH` $\rightarrow$ `MONHOC.MAMH` (`NCHAR(5)`)

---

## 4. TÀI KHOẢN SQL SERVER LOGIN MẪU ĐỂ TEST TÍCH HỢP

* **SQL Login Giảng viên (`role_GiaoVien`):**
  * `GV001` | Password: `123456` | Nguyễn Văn An (`Đang hoạt động`)
  * `GV002` | Password: `123456` | Trần Thị Bình (`Đang hoạt động`)
  * `GV-8-4` | Password: `123456` | Đặng Quốc Huy (`Đang hoạt động`)
  * *(Giảng viên `GV003`, `GV004` chưa có SQL Login để kiểm thử chức năng Tạo tài khoản)*
* **SQL Login Sinh viên (`role_SinhVien`):**
  * `SV001`, `SV002`, `SV003` (Lớp `CNTT01`), `SV004`, `SV005` (Lớp `CNTT02`), `SV006` (Lớp `ATTT01`) | Password: `123456`.
