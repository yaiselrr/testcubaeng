using System.ComponentModel.DataAnnotations;

namespace API.Models.Dto
{
    public class UserRegisterDto
    {
        [Required]
        [StringLength(50)]
        public string UserName { get; set; }

        [Required]
        [StringLength(255)]
        public string Email { get; set; }

        [Required]
        [StringLength(255)]
        public string Password { get; set; }

        [Required]
        [StringLength(255)]
        public string RepeatPassword { get; set; }
    }
}
