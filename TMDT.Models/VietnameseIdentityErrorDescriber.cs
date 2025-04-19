using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMDT.Models
{
    public class VietnameseIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DefaultError()
        {
            return new IdentityError
            {
                Code = "DefaultError",
                Description = "Đã xảy ra lỗi. Vui lòng thử lại sau."
            };
        }
        public override IdentityError PasswordMismatch()
        {
            return new IdentityError
            {
                Code = "PasswordMismatch",
                Description = "Mật khẩu không đúng."
            };
        }
        public override IdentityError PasswordTooShort(int length)
        {
            return new IdentityError
            {
                Code = "PasswordTooShort",
                Description = $"Mật khẩu phải có ít nhất {length} ký tự."
            };
        }

        public override IdentityError PasswordRequiresDigit()
        {
            return new IdentityError
            {
                Code = "PasswordRequiresDigit",
                Description = "Mật khẩu phải chứa ít nhất một chữ số."
            };
        }

        public override IdentityError PasswordRequiresLower()
        {
            return new IdentityError
            {
                Code = "PasswordRequiresLower",
                Description = "Mật khẩu phải chứa ít nhất một ký tự chữ thường."
            };
        }

        public override IdentityError PasswordRequiresUpper()
        {
            return new IdentityError
            {
                Code = "PasswordRequiresUpper",
                Description = "Mật khẩu phải chứa ít nhất một ký tự chữ hoa."
            };
        }

        public override IdentityError PasswordRequiresNonAlphanumeric()
        {
            return new IdentityError
            {
                Code = "PasswordRequiresNonAlphanumeric",
                Description = "Mật khẩu phải chứa ít nhất một ký tự đặc biệt."
            };
        }

        public override IdentityError DuplicateEmail(string email)
        {
            return new IdentityError
            {
                Code = "DuplicateEmail",
                Description = $"Địa chỉ email {email} đã được sử dụng."
            };
        }

        public override IdentityError InvalidEmail(string email)
        {
            return new IdentityError
            {
                Code = "InvalidEmail",
                Description = $"Địa chỉ email {email} không hợp lệ."
            };
        }

        // Các phương thức khác có thể tiếp tục ghi đè tùy vào nhu cầu của bạn.
    }
}
