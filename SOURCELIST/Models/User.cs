using System.ComponentModel.DataAnnotations.Schema;

namespace sourcelist.Models
{
    [Table("M_USER")]
    public class User
    {
        public int UserID { get; set; }
        public string Username { get; set; }
        public string UserPassword { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }


    }
}
