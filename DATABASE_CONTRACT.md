# HỢP ĐỒNG TÍCH HỢP CƠ SỞ DỮ LIỆU (`DATABASE_CONTRACT.md`)
**Dự án:** Hệ thống Quản lý Thi Trắc Nghiệm PTIT (`TRAC_NGHIEM`)  
**Người bàn giao (Người 1):** Quản trị và Dữ liệu nền  
**Người nhận bàn giao (Người 2):** Nghiệp vụ thi (`BODE`, `GIAOVIEN_DANGKY`, `BANGDIEM`)

---

## 1. THÔNG TIN KẾT NỐI & TÊN DATABASE
* **Tên Database bắt buộc:** `TRAC_NGHIEM` (Không được tự ý đổi tên).
* **Connection String mẫu (trong `App.config`):**
  ```xml
  <connectionStrings>
    <add name="TRAC_NGHIEM_DB"
         connectionString="Server=localhost;Database=TRAC_NGHIEM;Integrated Security=True;TrustServerCertificate=True;"
         providerName="System.Data.SqlClient" />
  </connectionStrings>
  ```
* **File Script khởi tạo toàn bộ CSDL:** [sql/01_Setup_TRAC_NGHIEM.sql](file:///c:/THITRACNGHIEM/sql/01_Setup_TRAC_NGHIEM.sql)

---

## 2. PHÂN ĐỊNH QUYỀN SỞ HỮU BẢNG

| Tên bảng | Người quản lý | Quy tắc tích hợp bắt buộc |
|---|---|---|
| `dbo.LOP` | **Người 1** | Người 2 chỉ `SELECT` để lấy danh sách lớp khi Chuẩn bị thi / Xem bảng điểm. |
| `dbo.MONHOC` | **Người 1** | Người 2 khi `SELECT` danh sách môn học cần lọc `WHERE DAXOA = 0` (hoặc dùng `dbo.vw_DanhSachMonHoc`). |
| `dbo.SINHVIEN` | **Người 1** | Người 2 khi `SELECT` sinh viên cần lọc `WHERE DAXOA = 0` (hoặc dùng `dbo.vw_DanhSachSinhVienTheoLop`). |
| `dbo.GIAOVIEN` | **Người 1** | Người 2 tham chiếu `MAGV` từ `SessionManager.CurrentUser.MaGV`. |
| `dbo.TAIKHOAN` | **Người 1** | Quản lý đăng nhập, mật khẩu SHA-256 (`8D969EEF...` ứng với `'123456'`), phân quyền `GIANGVIEN` / `SINHVIEN`. |
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

> **Lưu ý kiểu dữ liệu `NCHAR`:** Các cột mã (`MALOP`, `MAMH`, `MASV`, `MAGV`) sử dụng kiểu `NCHAR` cố định độ dài theo đề gốc, nên khi đọc lên C# hoặc truyền tham số so sánh chuỗi cần sử dụng `.Trim()` hoặc `RTRIM()` trong SQL.

---

## 4. TÀI KHOẢN DỮ LIỆU MẪU ĐỂ TEST TÍCH HỢP

Tất cả tài khoản mẫu đều có mật khẩu mặc định là **`123456`** (Lưu trong DB dưới dạng SHA-256: `8D969EEF6ECAD3C29A3A629280E686CF0C3F5D5A86AFF3CA12020C923ADC6C92`):
* **Tài khoản Giảng viên:**
  * Username: `admin` | Password: `123456` | `MAGV`: `GV001` (Nguyễn Văn An)
  * Username: `gv002` | Password: `123456` | `MAGV`: `GV002` (Trần Thị Bình)
  * *(Giảng viên `GV003`, `GV004` chưa có tài khoản để kiểm thử chức năng Tạo tài khoản)*
* **Tài khoản Sinh viên:**
  * Username (`MASV`): `SV001`, `SV002`, `SV003` (Lớp `CNTT01`), `SV004`, `SV005` (Lớp `CNTT02`), `SV006` (Lớp `ATTT01`) | Password: `123456`.

