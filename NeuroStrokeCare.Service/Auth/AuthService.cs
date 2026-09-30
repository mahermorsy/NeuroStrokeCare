using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NeuroStrokeCare.Data.UserApplication;
using NeuroStrokeCare.Service.Auth.Dtos;
using NeuroStrokeCare.Service.Email;
using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace NeuroStrokeCare.Service.Auth
{
    public class AuthService : IAuthService
    {
        #region Fields
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        #endregion

        #region Constructor
        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration configuration,
            IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _emailService = emailService;
        }
        #endregion

        #region Methods
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            if (await _userManager.FindByEmailAsync(request.Email) != null)
                return new AuthResponse { Success = false, Message = "البريد الإلكتروني مستخدم بالفعل" };

            if (await _userManager.FindByNameAsync(request.UserName) != null)
                return new AuthResponse { Success = false, Message = "اسم المستخدم مستخدم بالفعل" };

            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                IsApproved = true
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "فشل إنشاء الحساب",
                    Errors = createResult.Errors.Select(e => e.Description)
                };
            }

            var role = string.IsNullOrWhiteSpace(request.Role) ? "Staff" : request.Role.Trim();

            if (!await _roleManager.RoleExistsAsync(role))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));

            await _userManager.AddToRoleAsync(user, role);

            return await BuildSuccessResponseAsync(user, "تم إنشاء الحساب بنجاح");
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByNameAsync(request.UserNameOrEmail)
                       ?? await _userManager.FindByEmailAsync(request.UserNameOrEmail);

            if (user == null || user.IsDeleted)
                return new AuthResponse { Success = false, Message = "اسم المستخدم أو كلمة المرور غير صحيحة" };

            if (!user.IsApproved)
                return new AuthResponse { Success = false, Message = "الحساب لسه في انتظار موافقة الأدمن قبل ما تقدر تسجل دخول" };

            if (await _userManager.IsLockedOutAsync(user))
                return new AuthResponse { Success = false, Message = "الحساب مقفول مؤقتًا بسبب محاولات دخول فاشلة متكررة، حاول لاحقًا" };

            var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!passwordValid)
            {
                await _userManager.AccessFailedAsync(user);
                return new AuthResponse { Success = false, Message = "اسم المستخدم أو كلمة المرور غير صحيحة" };
            }

            await _userManager.ResetAccessFailedCountAsync(user);

            return await BuildSuccessResponseAsync(user, "تم تسجيل الدخول بنجاح");
        }

        public async Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.IsDeleted)
                return new AuthResponse { Success = false, Message = "المستخدم غير موجود" };

            var email = request.Email.Trim();
            var duplicateEmail = await _userManager.FindByEmailAsync(email);
            if (duplicateEmail != null && duplicateEmail.Id != user.Id)
                return new AuthResponse { Success = false, Message = "البريد الإلكتروني مستخدم بالفعل" };

            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Email = email;
            user.PhoneNumber = request.PhoneNumber?.Trim();

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "فشل تحديث البيانات",
                    Errors = updateResult.Errors.Select(e => e.Description)
                };
            }

            return await BuildSuccessResponseAsync(user, "تم تحديث البيانات بنجاح");
        }

        public async Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.IsDeleted)
                return new AuthResponse { Success = false, Message = "المستخدم غير موجود" };

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "فشل تغيير كلمة المرور - تأكد من كلمة المرور الحالية",
                    Errors = result.Errors.Select(e => e.Description)
                };
            }

            return new AuthResponse { Success = true, Message = "تم تغيير كلمة المرور بنجاح" };
        }

        public async Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email.Trim());

            // ما بنكشفش إن الإيميل مش مسجل، عشان محدش يستخدمها يجرب إيميلات موجودة ولا لأ
            if (user == null || user.IsDeleted)
                return new AuthResponse { Success = true, Message = "لو الإيميل ده مسجل عندنا، هيوصله رابط إعادة تعيين كلمة المرور" };

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // رابط إعادة التعيين بيتبعت بإيميل فعلي دلوقتي (مش بيرجع في الـ Response تاني -
            // ده كان مؤقت بس لحد ما نضيف IEmailService). Email:FrontendResetUrl مطلوب يتظبط
            // في appsettings.json (أو Environment Variable) على رابط صفحة reset-password
            // بتاعة الفرونت إند الفعلي بعد النشر.
            var resetUrlBase = _configuration["Email:FrontendResetUrl"] ?? "http://localhost:5173/reset-password";
            var resetLink = $"{resetUrlBase}?email={WebUtility.UrlEncode(user.Email)}&token={WebUtility.UrlEncode(token)}";

            var body = $@"
                <p>مرحبًا {WebUtility.HtmlEncode(user.FirstName)},</p>
                <p>طلب إعادة تعيين كلمة المرور الخاصة بحسابك في NeuroStrokeCare. اضغط الرابط ده لتحديد كلمة مرور جديدة (صالح لفترة محدودة):</p>
                <p><a href=""{resetLink}"">{resetLink}</a></p>
                <p>لو أنت مطلبتش الطلب ده، تجاهل الإيميل ده ببساطة.</p>";

            await _emailService.SendAsync(user.Email!, "إعادة تعيين كلمة المرور - NeuroStrokeCare", body);

            return new AuthResponse
            {
                Success = true,
                Message = "لو الإيميل ده مسجل عندنا، هيوصله رابط إعادة تعيين كلمة المرور"
            };
        }

        public async Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email.Trim());
            if (user == null || user.IsDeleted)
                return new AuthResponse { Success = false, Message = "طلب غير صحيح" };

            var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

            if (!result.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "فشل إعادة تعيين كلمة المرور - التوكن غير صحيح أو منتهي",
                    Errors = result.Errors.Select(e => e.Description)
                };
            }

            // إعادة تعيين الباسورد بنجاح = فرصة كويسة إننا نفك أي قفل سابق ونصفر عداد المحاولات الفاشلة
            await _userManager.SetLockoutEndDateAsync(user, null);
            await _userManager.ResetAccessFailedCountAsync(user);

            return new AuthResponse { Success = true, Message = "تم إعادة تعيين كلمة المرور بنجاح" };
        }
        public async Task<List<UserSummaryResponse>> GetAllUsersAsync()
        {
            var users = await _userManager.Users
                .Where(u => !u.IsDeleted)
                .OrderBy(u => u.FirstName)
                .ToListAsync();

            var result = new List<UserSummaryResponse>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserSummaryResponse
                {
                    Id = user.Id,
                    UserName = user.UserName ?? string.Empty,
                    Email = user.Email ?? string.Empty,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    PhoneNumber = user.PhoneNumber,
                    Role = roles.FirstOrDefault() ?? "Staff",
                    IsRootSuperAdmin = user.IsRootSuperAdmin,
                    CreatedAt = user.CreatedAt
                });
            }

            return result;
        }

        public async Task<AuthResponse> RegisterRequestAsync(SelfRegisterRequest request)
        {
            if (await _userManager.FindByEmailAsync(request.Email) != null)
                return new AuthResponse { Success = false, Message = "البريد الإلكتروني مستخدم بالفعل" };

            if (await _userManager.FindByNameAsync(request.UserName) != null)
                return new AuthResponse { Success = false, Message = "اسم المستخدم مستخدم بالفعل" };

            var user = new ApplicationUser
            {
                UserName = request.UserName,
                Email = request.Email,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                IsApproved = false,
                RequestedRole = string.IsNullOrWhiteSpace(request.RequestedRole) ? null : request.RequestedRole.Trim()
            };

            var createResult = await _userManager.CreateAsync(user, request.Password);
            if (!createResult.Succeeded)
            {
                return new AuthResponse
                {
                    Success = false,
                    Message = "فشل إنشاء الطلب",
                    Errors = createResult.Errors.Select(e => e.Description)
                };
            }

            // من غير أي Role دلوقتي - هتتحدد لما الأدمن يوافق. من غير Token ولا SignIn -
            // الحساب مش نشط لحد ما يتوافق عليه.
            return new AuthResponse
            {
                Success = true,
                Message = "تم إرسال طلب إنشاء الحساب - هيتفعّل بعد موافقة الأدمن"
            };
        }

        public async Task<List<PendingUserResponse>> GetPendingUsersAsync()
        {
            var users = await _userManager.Users
                .Where(u => !u.IsDeleted && !u.IsApproved)
                .OrderBy(u => u.CreatedAt)
                .ToListAsync();

            return users.Select(u => new PendingUserResponse
            {
                Id = u.Id,
                UserName = u.UserName ?? string.Empty,
                Email = u.Email ?? string.Empty,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                RequestedRole = u.RequestedRole,
                CreatedAt = u.CreatedAt
            }).ToList();
        }

        public async Task<AuthResponse> ApproveUserAsync(Guid userId, string role)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.IsDeleted)
                return new AuthResponse { Success = false, Message = "المستخدم غير موجود" };

            if (string.IsNullOrWhiteSpace(role))
                return new AuthResponse { Success = false, Message = "الدور مطلوب عشان توافق على الحساب" };

            var trimmedRole = role.Trim();
            if (!await _roleManager.RoleExistsAsync(trimmedRole))
                await _roleManager.CreateAsync(new IdentityRole<Guid>(trimmedRole));

            var existingRoles = await _userManager.GetRolesAsync(user);
            if (existingRoles.Count > 0)
                await _userManager.RemoveFromRolesAsync(user, existingRoles);

            await _userManager.AddToRoleAsync(user, trimmedRole);

            user.IsApproved = true;
            await _userManager.UpdateAsync(user);

            return new AuthResponse { Success = true, Message = "تم قبول الحساب وتفعيله" };
        }

        public async Task<AuthResponse> RejectUserAsync(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null || user.IsDeleted)
                return new AuthResponse { Success = false, Message = "المستخدم غير موجود" };

            // مش بنحذف الحساب فعليًا (متسق مع سياسة الـ Soft Delete) - بس بنعطّله ونمنعه يدخل تاني
            user.IsDeleted = true;
            user.DeletedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return new AuthResponse { Success = true, Message = "تم رفض الطلب" };
        }

        #endregion

        #region Helpers
        private async Task<AuthResponse> BuildSuccessResponseAsync(ApplicationUser user, string message)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "Staff";

            var jwtSection = _configuration.GetSection("Jwt");
            var secretKey = jwtSection["Key"]
                ?? throw new InvalidOperationException("Jwt:Key غير موجود في appsettings.json");

            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, role)
            };

            var durationMinutes = double.TryParse(jwtSection["DurationInMinutes"], out var minutes)
                ? minutes
                : 60;
            var expiresOn = DateTime.UtcNow.AddMinutes(durationMinutes);

            var token = new JwtSecurityToken(
                issuer: jwtSection["Issuer"],
                audience: jwtSection["Audience"],
                claims: claims,
                expires: expiresOn,
                signingCredentials: credentials);

            return new AuthResponse
            {
                Success = true,
                Message = message,
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = role,
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                ExpiresOn = expiresOn
            };
        }
        #endregion
    }
}
