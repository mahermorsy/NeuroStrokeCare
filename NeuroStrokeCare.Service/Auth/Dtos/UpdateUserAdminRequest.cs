namespace NeuroStrokeCare.Service.Auth.Dtos
{
    public class UpdateUserAdminRequest
    {
        // Admin can update either, both, or neither (send null to leave a field alone;
        // an empty string for EmployeeId explicitly clears it).
        public string? Role { get; set; }
        public string? EmployeeId { get; set; }
    }
}
