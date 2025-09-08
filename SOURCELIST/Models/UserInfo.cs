namespace sourcelist.Models
{
    public class UserInfo
    {
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? IDCard { get; set; }
        public bool isAssigner { get; set; }
        public bool isOpex { get; set; }
        public string? RoleID { get; set; }
        public string? RoleName { get; set; }
        public SupervisorInfo? Supervisor { get; set; } 
    }
}
