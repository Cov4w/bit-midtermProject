using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShopping.Server.Models
{
    [Table("books")]
    public class Book
    {
        [Key]
        [Column("isbn")]
        [MaxLength(20)]
        public string Isbn { get; set; } = "";

        [Required]
        [Column("bookName")]
        [MaxLength(255)]
        public string BookName { get; set; } = "";

        [Required]
        [Column("category")]
        [MaxLength(50)]
        public string Category { get; set; } = "";

        [Required]
        [Column("bookPrice")]
        public int BookPrice { get; set; }

        [Column("bookCount")]
        public int BookCount { get; set; } = 999;

        [Column("is_for_sale")]
        public bool IsForSale { get; set; } = true;

        [Column("bookURL")]
        public string BookURL { get; set; } = "";
        [Column("thumbnail")]
        public string Thumbnail {  get; set; } = "";

        public BookCategory? BookCategoryEntity { get; set; }
        public ICollection<PaymentItem> PaymentItems { get; set; } = new List<PaymentItem>();
    }
}