using Microsoft.AspNetCore.Identity;
using NeuroStrokeCare.Data.Enums;
using NeuroStrokeCare.Data.UserApplication;
using NeuroStrokeCare.Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace NeuroStrokeCare.Service.Implementation
{
    public class UserService : IUserService
    {
        public class UserService : IUserService
        {
            //private readonly SignInManager<ApplicationUser> _signInManager;
            private readonly UserManager<ApplicationUser> _UserManager;
            private readonly IHttpContextAccessor _HttpContextAccessor;
            private readonly IEmailService _EmailService;

            public UserService(
                UserManager<ApplicationUser> UserManager,
                IHttpContextAccessor HttpContextAccesso,
                IEmailService emailService)
            {
                // _signInManager = SinginManager;
                _UserManager = UserManager;
                _HttpContextAccessor = HttpContextAccesso;
                _EmailService = emailService;
            }
            public async Task<RegisterResponse> RegisterAsync(DTOUser UserRegister)
            {
                var errors = new List<string>();
                var email = UserRegister.Email?.Trim() ?? string.Empty;
                var userName = string.IsNullOrWhiteSpace(UserRegister.UserName)
                    ? email
                    : UserRegister.UserName.Trim();

                if (UserRegister.Password != UserRegister.ConfirmationPassword)
                    errors.Add("Passwords do not match");

                if (!string.IsNullOrWhiteSpace(email) &&
                    await _UserManager.FindByEmailAsync(email) != null)
                    errors.Add("Email already exists");

                if (!string.IsNullOrWhiteSpace(userName) &&
                    await _UserManager.FindByNameAsync(userName) != null)
                    errors.Add("Username already exists");

                if (!string.IsNullOrWhiteSpace(UserRegister.PhoneNumber) &&
                    _UserManager.Users.Any(u => u.PhoneNumber == UserRegister.PhoneNumber.Trim()))
                    errors.Add("Phone number already exists");

                if (errors.Any())
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Errors = errors
                    };
                }

                var user = new ApplicationUser
                {
                    UserName = userName,
                    FirstName = UserRegister.FirstName?.Trim(),
                    LastName = UserRegister.LastName?.Trim(),
                    PhoneNumber = UserRegister.PhoneNumber?.Trim(),
                    Email = email
                };

                var result = await _UserManager.CreateAsync(user, UserRegister.Password);

                if (!result.Succeeded)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "User creation failed",
                        Errors = result.Errors.Select(e => e.Description)
                    };
                }

                var role = string.IsNullOrWhiteSpace(UserRegister.Role) ? "User" : UserRegister.Role;

                var roleResult = await _UserManager.AddToRoleAsync(user, role);

                if (!roleResult.Succeeded)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "User created but failed to assign role",
                        Errors = roleResult.Errors.Select(e => e.Description)
                    };
                }

                return new RegisterResponse
                {
                    Success = true,
                    Message = "User registered successfully",
                    Errors = null
                };
            }
            public async Task<DTOUserResult> LoginAsync(DTOLoginUser UserLogin)
            {
                // Get ApplicationUser directly (not DTOUser)
                var user = await _UserManager.FindByNameAsync(UserLogin.UserName)
                           ?? await _UserManager.FindByEmailAsync(UserLogin.UserName);

                if (user == null || user.IsDeleted)
                    return new DTOUserResult
                    {
                        Sucess = false,
                        Errors = new[] { "Username or password not match" }
                    };

                // الحساب مقفول بالفعل بسبب محاولات فاشلة سابقة
                if (await _UserManager.IsLockedOutAsync(user))
                    return new DTOUserResult
                    {
                        Sucess = false,
                        Errors = new[] { "Account locked due to multiple failed attempts. Try again later or reset your password." }
                    };

                var passwordValid = await _UserManager.CheckPasswordAsync(user, UserLogin.Password);

                if (!passwordValid)
                {
                    // سجّل المحاولة الفاشلة — Identity هيقفل تلقائيًا عند الوصول للحد (5)
                    await _UserManager.AccessFailedAsync(user);

                    if (await _UserManager.IsLockedOutAsync(user))
                        return new DTOUserResult
                        {
                            Sucess = false,
                            Errors = new[] { "Account locked due to multiple failed attempts. Try again in 5 minutes or reset your password." }
                        };

                    return new DTOUserResult
                    {
                        Sucess = false,
                        Errors = new[] { "Username or password not match" }
                    };
                }

                // نجاح تسجيل الدخول — صفّر عداد المحاولات الفاشلة
                await _UserManager.ResetAccessFailedCountAsync(user);

                return new DTOUserResult { Sucess = true };
            }
            public async Task LogoutAsync()
            {

            }
            public async Task<DTOUser> GetUserbyIdAsync(Guid Userid)
            {
                var User = await _UserManager.FindByIdAsync(Userid.ToString());

                if (User == null)
                {
                    return null;
                }

                return new DTOUser
                {
                    Id = User.Id,
                    UserName = User.UserName,
                    Email = User.Email,
                    FirstName = User.FirstName,
                    LastName = User.LastName,
                    PhoneNumber = User.PhoneNumber,
                    Role = (await _UserManager.GetRolesAsync(User)).FirstOrDefault()
                };
            }
            public async Task<DTOUserProfile?> GetUserProfileAsync(Guid userId)
            {
                var user = await _UserManager.FindByIdAsync(userId.ToString());

                if (user == null || user.IsDeleted)
                {
                    return null;
                }

                return new DTOUserProfile
                {
                    UserName = user.UserName,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber ?? string.Empty
                };
            }

            public async Task<DTOUser?> GetUserByNameOrEmailAsync(string usernameOrEmail)
            {
                var User = await _UserManager.FindByNameAsync(usernameOrEmail);
                if (User == null)
                {
                    User = await _UserManager.FindByEmailAsync(usernameOrEmail);
                }
                if (User == null)
                {
                    return null;
                }

                return new DTOUser
                {
                    Id = User.Id,
                    UserName = User.UserName,
                    Email = User.Email,
                    FirstName = User.FirstName,
                    LastName = User.LastName,
                    PhoneNumber = User.PhoneNumber,
                    Role = (await _UserManager.GetRolesAsync(User)).FirstOrDefault()
                };

            }
            public async Task<IEnumerable<DTOUser>> GetAllUsersAsync()
            {
                var Users = _UserManager.Users;
                var userList = new List<DTOUser>();

                foreach (var user in Users)
                {
                    var role = (await _UserManager.GetRolesAsync(user)).FirstOrDefault();

                    userList.Add(new DTOUser
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber,
                        Role = role
                    });
                }
                return userList;
            }
            public async Task<IEnumerable<DTOUser>> GetAllActiveUsersAsync(string? role = null, string? search = null)
            {
                role = string.IsNullOrWhiteSpace(role) || role.Equals("All", StringComparison.OrdinalIgnoreCase)
                    ? null
                    : role.Trim();

                var users = _UserManager.Users
                    .Where(u => !u.IsDeleted);

                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim();

                    users = users.Where(u =>
                        u.Id.ToString() == search ||
                        u.UserName!.Contains(search) ||
                        u.Email!.Contains(search) ||
                        u.PhoneNumber!.Contains(search) ||
                        u.FirstName!.Contains(search) ||
                        u.LastName!.Contains(search));
                }

                var userList = new List<DTOUser>();

                foreach (var user in users)
                {
                    var userRole = (await _UserManager.GetRolesAsync(user)).FirstOrDefault();

                    if (!string.IsNullOrWhiteSpace(role) && userRole != role)
                        continue;

                    userList.Add(new DTOUser
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        PhoneNumber = user.PhoneNumber,
                        Role = userRole
                    });
                }

                return userList;
            }
            public async Task<int> GetLoggedInUser()
            {
                // var RefreshToken = _HttpContextAccessor.HttpContext?.Request.Cookies["RefreshToken"];
                var LoggedInUser = _HttpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(LoggedInUser))
                    throw new UnauthorizedAccessException("No User logged in.");

                return int.Parse(LoggedInUser);
            }
            public async Task<Claim[]> GetClaims(string Username)
            {
                var user = await _UserManager.FindByNameAsync(Username);
                if (user == null)
                {
                    return Array.Empty<Claim>();
                }
                var roles = await _UserManager.GetRolesAsync(user);

                var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Email, user.Email ?? "")
            };
                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
                return claims.ToArray();
            }
            public async Task<RegisterResponse> UpdateAsync(DTOUser model)
            {
                if (model == null || model.Id <= 0)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Invalid user data"
                    };
                }

                var user = await _UserManager.FindByIdAsync(model.Id.ToString());

                if (user == null)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                var email = model.Email?.Trim() ?? string.Empty;
                var userName = string.IsNullOrWhiteSpace(model.UserName)
                    ? email
                    : model.UserName.Trim();

                var duplicateUserName =
                    await _UserManager.FindByNameAsync(userName);

                if (duplicateUserName != null &&
                    duplicateUserName.Id != user.Id)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Username already exists",
                        Errors = new[] { "Username already exists" }
                    };
                }

                var duplicateEmail =
                    await _UserManager.FindByEmailAsync(email);

                if (duplicateEmail != null &&
                    duplicateEmail.Id != user.Id)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Email already exists",
                        Errors = new[] { "Email already exists" }
                    };
                }

                // Update profile and Identity fields together.
                user.UserName = userName;
                user.Email = email;
                user.FirstName = model.FirstName?.Trim();
                user.LastName = model.LastName?.Trim();
                user.PhoneNumber = model.PhoneNumber?.Trim();

                var updateResult = await _UserManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "User update failed",
                        Errors = updateResult.Errors
                            .Select(error => error.Description)
                    };
                }

                // Keep the current role when no new role is provided.
                if (!string.IsNullOrWhiteSpace(model.Role))
                {
                    var requestedRole = model.Role.Trim();
                    var currentRoles = await _UserManager.GetRolesAsync(user);

                    if (!currentRoles.Contains(
                            requestedRole,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        var addRoleResult =
                            await _UserManager.AddToRoleAsync(user, requestedRole);

                        if (!addRoleResult.Succeeded)
                        {
                            return new RegisterResponse
                            {
                                Success = false,
                                Message = "Failed to assign the new role",
                                Errors = addRoleResult.Errors
                                    .Select(error => error.Description)
                            };
                        }
                    }

                    var rolesToRemove = currentRoles
                        .Where(role => !role.Equals(
                            requestedRole,
                            StringComparison.OrdinalIgnoreCase))
                        .ToArray();

                    if (rolesToRemove.Length > 0)
                    {
                        var removeRoleResult =
                            await _UserManager.RemoveFromRolesAsync(
                                user,
                                rolesToRemove);

                        if (!removeRoleResult.Succeeded)
                        {
                            return new RegisterResponse
                            {
                                Success = false,
                                Message = "Failed to remove previous roles",
                                Errors = removeRoleResult.Errors
                                    .Select(error => error.Description)
                            };
                        }
                    }
                }

                return new RegisterResponse
                {
                    Success = true,
                    Message = "User updated successfully",
                    Errors = null
                };
            }
            public async Task<RegisterResponse> UpdateMyProfileAsync(int userId, DTOUserProfile model)
            {
                if (model == null || userId <= 0)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Invalid user data"
                    };
                }

                var user = await _UserManager.FindByIdAsync(userId.ToString());

                if (user == null || user.IsDeleted)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                var email = model.Email?.Trim() ?? string.Empty;
                var userName = string.IsNullOrWhiteSpace(model.UserName)
                    ? user.UserName ?? email
                    : model.UserName.Trim();

                if (string.IsNullOrWhiteSpace(email))
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Email is required",
                        Errors = new[] { "Email is required" }
                    };
                }

                var duplicateUserName =
                    await _UserManager.FindByNameAsync(userName);

                if (duplicateUserName != null &&
                    duplicateUserName.Id != user.Id)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Username already exists",
                        Errors = new[] { "Username already exists" }
                    };
                }

                var duplicateEmail =
                    await _UserManager.FindByEmailAsync(email);

                if (duplicateEmail != null &&
                    duplicateEmail.Id != user.Id)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Email already exists",
                        Errors = new[] { "Email already exists" }
                    };
                }

                // Update profile and Identity fields together.
                user.UserName = userName;
                user.Email = email;
                user.FirstName = model.FirstName?.Trim();
                user.LastName = model.LastName?.Trim();
                user.PhoneNumber = model.PhoneNumber?.Trim();

                var updateResult = await _UserManager.UpdateAsync(user);

                if (!updateResult.Succeeded)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "User update failed",
                        Errors = updateResult.Errors
                            .Select(error => error.Description)
                    };
                }

                return new RegisterResponse
                {
                    Success = true,
                    Message = "Profile updated successfully",
                    Errors = null
                };
            }
            public async Task<RegisterResponse> DeleteAsync(Guid userId)
            {
                if (userId == Guid.Empty)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Invalid user ID"
                    };
                }

                var user = await _UserManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                user.CurrentStatus = (int)CurrentStatusType.Inactive;
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
                user.IsDeleted = true;
                user.DeletedAt = DateTime.UtcNow;

                var result = await _UserManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "User delete failed",
                        Errors = result.Errors.Select(e => e.Description)
                    };
                }

                return new RegisterResponse
                {
                    Success = true,
                    Message = "User deleted successfully",
                    Errors = null
                };
            }

            public async Task ForgotPasswordAsync(
                DTOForgotPassword model,
                string resetUrl)
            {
                var user = await _UserManager.FindByEmailAsync(model.Email.Trim());

                // لا نكشف أن البريد غير مسجل.
                if (user == null || user.IsDeleted)
                    return;

                var token = await _UserManager
                    .GeneratePasswordResetTokenAsync(user);

                var encodedToken = WebEncoders.Base64UrlEncode(
                    Encoding.UTF8.GetBytes(token));

                var link =
                    $"{resetUrl}?email={Uri.EscapeDataString(user.Email!)}" +
                    $"&token={Uri.EscapeDataString(encodedToken)}";

                await _EmailService.SendAsync(
                    user.Email!,
                    "Reset your HEALZA password",
                    $"Reset your password: {link}");
            }

            public async Task<RegisterResponse> ResetPasswordAsync(
                DTOResetPassword model)
            {
                var user = await _UserManager.FindByEmailAsync(model.Email.Trim());

                if (user == null || user.IsDeleted)
                    return new() { Success = false, Message = "Invalid reset request" };

                string token;

                try
                {
                    token = Encoding.UTF8.GetString(
                        WebEncoders.Base64UrlDecode(model.Token));
                }
                catch (FormatException)
                {
                    return new RegisterResponse
                    {
                        Success = false,
                        Message = "Invalid or expired reset link"
                    };
                }

                var result = await _UserManager.ResetPasswordAsync(
                    user, token, model.NewPassword);

                // إعادة تعيين الباسورد = استعادة الحساب: فكّ القفل وصفّر العداد
                if (result.Succeeded)
                {
                    await _UserManager.SetLockoutEndDateAsync(user, null);
                    await _UserManager.ResetAccessFailedCountAsync(user);
                }

                return new RegisterResponse
                {
                    Success = result.Succeeded,
                    Message = result.Succeeded
                        ? "Password reset successfully"
                        : "Password reset failed",
                    Errors = result.Errors.Select(e => e.Description)
                };
            }
        }
    }
}
