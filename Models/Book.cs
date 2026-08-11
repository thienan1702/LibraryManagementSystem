using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.Models
{
    [Index(nameof(ISBN), IsUnique = true)]
    public class Book
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ISBN { get; set; } = string.Empty;

        [Range(1, int.MaxValue,
            ErrorMessage = "Quantity must be greater than 0.")]
        public int Quantity { get; set; }
        public int AvailableQuantity { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        // Foreign Keys
        public int CategoryId { get; set; }

        public int AuthorId { get; set; }

        public int PublisherId { get; set; }

        // Navigation Properties
        public Category? Category { get; set; }

        public Author? Author { get; set; }

        public Publisher? Publisher { get; set; }

        [NotMapped]
        public IFormFile? ImageFile { get; set; }

        public virtual ICollection<BorrowDetail> BorrowDetails { get; set; }
    = new List<BorrowDetail>();

    }
}