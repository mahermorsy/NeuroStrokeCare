using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroStrokeCare.Service.Auth;
using NeuroStrokeCare.Service.Auth.Dtos;
using System.Security.Claims;

namespace NeuroStrokeCare.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        #region Fields
        private readonly IAuthService _authService;
        #endregion

        #region Constructor
        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }
        #endregion

        // POST: api/auth/register
        // مقفول على الـ Admin بس - إضافة دكاترة وستاف جداد بتتعمل من صفحة إدارة المستخدمين
        [Authorize(Roles = "Admin")]
        [HttpPost("register")]
        public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // POST: api/auth/login
        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
        {
            var result = await _authService.LoginAsync(request);
            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        // GET: api/auth/me
        // للتأكد إن التوكن شغال - محتاج Authorize Header: Bearer {token}
        [Authorize]
        [HttpGet("me")]
        public IActionResult Me()
        {
            return Ok(new
            {
                UserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                UserName = User.FindFirstValue(ClaimTypes.Name),
                Email = User.FindFirstValue(ClaimTypes.Email),
                Role = User.FindFirstValue(ClaimTypes.Role)
            });
        }

        // GET: api/auth/users
        // ليستة كل المستخدمين وأدوارهم - للـ Admin بس
        [Authorize(Roles = "Admin")]
        [HttpGet("users")]
        public async Task<ActionResult<List<UserSummaryResponse>>> GetUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users);
        }

        // PUT: api/auth/profile
        // تعديل بيانات اليوزر المسجل دخول (مش UserName - بيفضل ثابت)
        [Authorize]
        [HttpPut("profile")]
        public async Task<ActionResult<AuthResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _authService.UpdateProfileAsync(userId.Value, request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // POST: api/auth/change-password
        // تغيير الباسورد وانت عارف الباسورد القديم (لازم تكون مسجل دخول)
        [Authorize]
        [HttpPost("change-password")]
        public async Task<ActionResult<AuthResponse>> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var result = await _authService.ChangePasswordAsync(userId.Value, request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // POST: api/auth/forgot-password
        // نسيت الباسورد خالص ومش عارف تسجل دخول - محتاج بس الإيميل
        [AllowAnonymous]
        [HttpPost("forgot-password")]
        public async Task<ActionResult<AuthResponse>> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var result = await _authService.ForgotPasswordAsync(request);
            return Ok(result);
        }

        // POST: api/auth/reset-password
        // بعد ما تاخد الـ Token من forgot-password، بتحط هنا إيميلك + التوكن + باسورد جديد
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<ActionResult<AuthResponse>> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        #region Helpers
        private Guid? GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
        #endregion
    }
}
