using System;
using System.Security.Cryptography;
using System.Text;

namespace TRAC_NGHIEM.Helpers
{
    /// <summary>
    /// Lớp hỗ trợ bảo mật và băm mật khẩu (SHA-256)
    /// Tương thích 100% với hàm CONVERT(NVARCHAR(64), HASHBYTES('SHA2_256', ...), 2) trên SQL Server.
    /// </summary>
    public static class SecurityHelper
    {
        public const string DefaultStudentPasswordPlain = "123456";

        /// <summary>
        /// Băm chuỗi mật khẩu bằng thuật toán SHA-256 ra chuỗi 64 ký tự Hex in hoa.
        /// Không bao giờ lưu hoặc ghi log mật khẩu dạng plaintext.
        /// </summary>
        public static string HashPassword(string plainTextPassword)
        {
            if (string.IsNullOrEmpty(plainTextPassword))
            {
                return string.Empty;
            }

            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(plainTextPassword);
                byte[] hashBytes = sha256.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder(hashBytes.Length * 2);
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Kiểm tra mật khẩu người dùng nhập vào có khớp với chuỗi Hash đã lưu không.
        /// </summary>
        public static bool VerifyPassword(string plainTextPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(plainTextPassword) || string.IsNullOrEmpty(storedHash))
            {
                return false;
            }

            string computedHash = HashPassword(plainTextPassword);
            return string.Equals(computedHash, storedHash.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}

