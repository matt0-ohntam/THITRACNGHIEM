using System;
using System.Text.RegularExpressions;

namespace TRAC_NGHIEM.Helpers
{
    /// <summary>
    /// Lớp kiểm tra tính hợp lệ dữ liệu đầu vào (Input Validation)
    /// </summary>
    public static class ValidationHelper
    {
        private static readonly Regex CodeRegex = new Regex(@"^[A-Z0-9_]+$", RegexOptions.Compiled);
        private static readonly Regex PhoneRegex = new Regex(@"^0[0-9]{8,11}$", RegexOptions.Compiled);
        private static readonly Regex UsernameRegex = new Regex(@"^[a-zA-Z0-9_.\-]+$", RegexOptions.Compiled);

        public static string NormalizeCode(string code)
        {
            return (code ?? string.Empty).Trim().ToUpperInvariant();
        }

        public static string NormalizeText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }
            return Regex.Replace(text.Trim(), @"\s+", " ");
        }

        public static bool IsValidCode(string code, int maxLength, out string errorMessage, string fieldLabel)
        {
            string normalized = NormalizeCode(code);
            if (string.IsNullOrEmpty(normalized))
            {
                errorMessage = string.Format("{0} không được để trống!", fieldLabel);
                return false;
            }
            if (normalized.Length > maxLength)
            {
                errorMessage = string.Format("{0} không được vượt quá {1} ký tự!", fieldLabel, maxLength);
                return false;
            }
            if (!CodeRegex.IsMatch(normalized))
            {
                errorMessage = string.Format("{0} chỉ được chứa chữ cái không dấu (A-Z), chữ số (0-9) và không có khoảng trắng!", fieldLabel);
                return false;
            }
            errorMessage = string.Empty;
            return true;
        }

        public static bool IsValidPhone(string phone, out string errorMessage)
        {
            string trimmed = (phone ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                errorMessage = "Số điện thoại liên lạc không được để trống!";
                return false;
            }
            if (!PhoneRegex.IsMatch(trimmed))
            {
                errorMessage = "Số điện thoại không hợp lệ (phải bắt đầu bằng số 0 và gồm 9 đến 12 chữ số)!";
                return false;
            }
            errorMessage = string.Empty;
            return true;
        }

        public static bool IsValidBirthDate(DateTime birthDate, out string errorMessage)
        {
            DateTime today = DateTime.Today;
            if (birthDate.Date >= today)
            {
                errorMessage = "Ngày sinh phải nhỏ hơn ngày hiện tại!";
                return false;
            }

            int age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
            {
                age--;
            }

            if (age < 15 || age > 80)
            {
                errorMessage = string.Format("Ngày sinh không hợp lệ (Tuổi sinh viên phải từ 15 đến 80 tuổi, hiện tại: {0} tuổi)!", age);
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        public static bool IsValidUsername(string username, out string errorMessage)
        {
            string trimmed = (username ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                errorMessage = "Tên đăng nhập không được để trống!";
                return false;
            }
            if (trimmed.Length < 3 || trimmed.Length > 50)
            {
                errorMessage = "Tên đăng nhập phải có độ dài từ 3 đến 50 ký tự!";
                return false;
            }
            if (!UsernameRegex.IsMatch(trimmed))
            {
                errorMessage = "Tên đăng nhập chỉ được chứa chữ cái không dấu, chữ số, dấu gạch dưới (_) hoặc dấu chấm (.)!";
                return false;
            }
            errorMessage = string.Empty;
            return true;
        }
    }
}

