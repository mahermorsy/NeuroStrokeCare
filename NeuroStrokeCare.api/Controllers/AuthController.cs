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
        private readonly IWebHostEnvironment _env;
        #endregion

        #region Constructor
        public AuthController(IAuthService authService, IWebHostEnvironment env)
        {
            _authService = authService;
            _env = env;
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

        // POST: api/auth/register-request
        // تسجيل ذاتي مفتوح لأي حد - الحساب بيتعمل بس مش موافق عليه (IsApproved = false) ومفيهوش
        // أي Role، فمش هيقدر يسجل دخول لحد ما الأدمن يوافق عليه من صفحة الموافقات ويحدد دوره الفعلي.
        [AllowAnonymous]
        [HttpPost("register-request")]
        public async Task<ActionResult<AuthResponse>> RegisterRequest([FromBody] SelfRegisterRequest request)
        {
            var result = await _authService.RegisterRequestAsync(request);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/auth/pending-users
        [Authorize(Roles = "Admin")]
        [HttpGet("pending-users")]
        public async Task<ActionResult<List<PendingUserResponse>>> GetPendingUsers()
        {
            var users = await _authService.GetPendingUsersAsync();
            return Ok(users);
        }

        // POST: api/auth/approve/{id}?role=Nurse&employeeId=12345
        [Authorize(Roles = "Admin")]
        [HttpPost("approve/{id:guid}")]
        public async Task<ActionResult<AuthResponse>> ApproveUser(Guid id, [FromQuery] string role, [FromQuery] string? employeeId = null)
        {
            var result = await _authService.ApproveUserAsync(id, role, employeeId);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // GET: api/auth/profile — الحساب بتاع الشخص المسجل دخول نفسه، بتفاصيل أكتر من /me
        // (بما فيها رقم الكارنيه والصورة) عشان كارت الهوية.
        [Authorize]
        [HttpGet("profile")]
        public async Task<ActionResult<UserSummaryResponse>> GetMyProfile()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var profile = await _authService.GetMyProfileAsync(userId.Value);
            if (profile == null)
                return NotFound();

            return Ok(profile);
        }

        // PUT: api/auth/users/{id} — الأدمن بس يعدّل رقم الكارنيه و/أو دور موظف موجود بالفعل
        [Authorize(Roles = "Admin")]
        [HttpPut("users/{id:guid}")]
        public async Task<ActionResult<AuthResponse>> UpdateUserAdmin(Guid id, [FromBody] UpdateUserAdminRequest request)
        {
            var result = await _authService.UpdateUserAdminAsync(id, request.Role, request.EmployeeId);
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        // POST: api/auth/upload-photo — كل مستخدم بيرفع صورته الشخصية بنفسه (مش الأدمن نيابة عنه)،
        // بتتخزن على السيرفر نفسه تحت App_Data/uploads/staff-photos (مش جوه wwwroot عشان تبقى
        // منفصلة ويسهل عمل Docker volume ليها - شايفه في docker-compose.*.yml).
        [Authorize]
        [HttpPost("upload-photo")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<ActionResult<AuthResponse>> UploadPhoto(IFormFile file)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest(new AuthResponse { Success = false, Message = "اختر صورة أولًا" });

            var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType))
                return BadRequest(new AuthResponse { Success = false, Message = "الصورة لازم تكون JPG أو PNG أو WEBP" });

            var ext = file.ContentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };

            var uploadsDir = Path.Combine(_env.ContentRootPath, "App_Data", "uploads", "staff-photos");
            Directory.CreateDirectory(uploadsDir);

            // اسم ثابت بمعرّف المستخدم عشان أي صورة جديدة تستبدل القديمة تلقائيًا بدل ما تتراكم ملفات يتيمة
            var fileName = $"{userId.Value}{ext}";
            var filePath = Path.Combine(uploadsDir, fileName);

            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var photoUrl = $"/uploads/staff-photos/{fileName}";
            var result = await _authService.SetProfilePhotoAsync(userId.Value, photoUrl);
            if (!result.Success)
                return BadRequest(result);

            return Ok(new { result.Success, result.Message, photoUrl });
        }

        // POST: api/auth/reject/{id}
        [Authorize(Roles = "Admin")]
        [HttpPost("reject/{id:guid}")]
        public async Task<ActionResult<AuthResponse>> RejectUser(Guid id)
        {
            var result = await _authService.RejectUserAsync(id);
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
