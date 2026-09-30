using System;

namespace NeuroStrokeCare.Service.Auth.Dtos
{
    public class PendingUserResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? RequestedRole { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
