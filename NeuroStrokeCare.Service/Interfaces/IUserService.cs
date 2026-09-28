using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;

namespace NeuroStrokeCare.Service.Interfaces
{
    public interface IUserService
    {
        public interface IUserService
        {
            Task<RegisterResponse> RegisterAsync(DTOUser UserRegister);
            Task<DTOUserResult> LoginAsync(DTOLoginUser UserLogin);
            Task LogoutAsync();
            Task<DTOUser> GetUserbyIdAsync(int Userid);
            Task<DTOUser?> GetUserByNameOrEmailAsync(string usernameOrEmail);
            Task<IEnumerable<DTOUser>> GetAllUsersAsync();
            Task<int> GetLoggedInUser();
            Task<Claim[]> GetClaims(string Username);
            Task<RegisterResponse> UpdateAsync(DTOUser UserRegister);
            Task<RegisterResponse> DeleteAsync(int userId);
            Task<IEnumerable<DTOUser>> GetAllActiveUsersAsync(string? role = null, string? search = null);
            Task<DTOUserProfile?> GetUserProfileAsync(int userId);
            Task<RegisterResponse> UpdateMyProfileAsync(int userId, DTOUserProfile model);
            Task ForgotPasswordAsync(DTOForgotPassword model, string resetUrl);
            Task<RegisterResponse> ResetPasswordAsync(DTOResetPassword model);
        }
    }
}
