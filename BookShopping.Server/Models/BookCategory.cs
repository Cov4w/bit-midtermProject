using System.ComponentModel.DataAnnotations;

namespace BookShopping.Server.Models
{
    public class BookCategory
    {
        [Key]
        public string Category { get; set; } = "";

        public string CategoryName { get; set; } = "";

        public ICollection<Book> Books { get; set; } = new List<Book>();
    }
}
