using System;

namespace TRAC_NGHIEM.Helpers
{
    /// <summary>
    /// Lớp hỗ trợ hằng số bảo mật cho việc khởi tạo tài khoản SQL Server Login mặc định của Sinh viên.
    /// Thông tin xác thực và băm mật khẩu do chính SQL Server quản lý hoàn toàn (SQL Server Authentication).
    /// </summary>
    public static class SecurityHelper
    {
        public const string DefaultStudentPasswordPlain = "123456";
    }
}
