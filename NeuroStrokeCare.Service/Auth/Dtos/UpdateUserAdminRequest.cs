namespace NeuroStrokeCare.Service.Auth.Dtos
{
    public class UpdateUserAdminRequest
    {
        // Admin can update either, both, or neither (send null to leave a field alone;
        // an empty string for EmployeeId explicitly clears it).
        public string? Role { get; set; }
        public string? EmployeeId { get; set; }

        // Same null-leaves-alone / empty-string-clears convention as EmployeeId above.
        // These are privileged employment fields — this request only reaches AuthService
        // through AuthController.UpdateUserAdmin, which stays [Authorize(Roles = "Admin")].
        public string? Profession { get; set; }
        public string? JobTitle { get; set; }
        public string? AcademicDegree { get; set; }
        public string? Department { get; set; }
    }
}
