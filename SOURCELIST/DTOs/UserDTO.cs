namespace sourcelist.DTOs
{
    public class UserDTO
    {
        public int ID_User { get; set; }

        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string? Password { get; set; }

        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}