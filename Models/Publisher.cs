using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Models
{
    public class Publisher
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Address { get; set; }

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}