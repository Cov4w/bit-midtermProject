using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShopping.Client.Models
{
    internal class PaymentItem
    {
        public int PayItemID { get; set; }
        public int PayID { get; set; }
        public string Isbn { get; set; } = "";
        public string BookName { get; set; } = "";
        public int BookCount { get; set; }
        public int BookUnitPrice { get; set; }
    }
}
