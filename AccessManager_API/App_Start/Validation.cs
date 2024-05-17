
using System.Text.RegularExpressions;

namespace MyHub
{
    //17-05-2024 by Periya Samy P CHC1761
    public class Validation
    {
        #region Password Validation Function
        internal static bool PasswordValidation(string password)
        {
            // Check if password length is at least 8 characters
            if (password.Length < 8)
            {
                return false;
            }
            // Check if password contains at least one digit
            if (!Regex.IsMatch(password, @"\d"))
            {
                return false;
            }
            // Check if password contains at least one special character
            if (!Regex.IsMatch(password, @"[^\w\d]"))
            {
                return false;
            }
            // Check if password contains at least one uppercase letter
            if (!Regex.IsMatch(password, @"[A-Z]"))
            {
                return false;
            }
            // Check if password contains at least one lowercase letter
            if (!Regex.IsMatch(password, @"[a-z]"))
            {
                return false;
            }
            return true;
        }
        #endregion

    }
}
