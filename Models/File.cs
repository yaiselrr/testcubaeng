using System.ComponentModel.DataAnnotations;

namespace API.Models
{
    public class File
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Extension { get; set; }

        [Required]
        public double Size { get; set; }

        [Required]
        public string Path { get; set; }
    }
}
