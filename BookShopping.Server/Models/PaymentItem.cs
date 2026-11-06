using System.ComponentModel.DataAnnotations;

namespace BookShopping.Server.Models
{
    public class PaymentItem
    {
        [Key]
        public int PayItemID { get; set; }

        public int PayID { get; set; }

        public string Isbn { get; set; } = "";

        public string BookName { get; set; } = "";

        public int BookCount { get; set; }

        public int BookUnitPrice { get; set; }

        public PaymentHistory? PaymentHistory { get; set; }

        public Book? Book { get; set; }
    }
}
