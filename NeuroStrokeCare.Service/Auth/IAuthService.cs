using NeuroStrokeCare.Service.Auth.Dtos;

namespace NeuroStrokeCare.Service.Auth
{
    public interface IAuthService
    {
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
        Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);
        Task<List<UserSummaryResponse>> GetAllUsersAsync();

        Task<AuthResponse> RegisterRequestAsync(SelfRegisterRequest request);
        Task<List<PendingUserResponse>> GetPendingUsersAsync();
        Task<AuthResponse> ApproveUserAsync(Guid userId, string role);
        Task<AuthResponse> RejectUserAsync(Guid userId);
    }
}
