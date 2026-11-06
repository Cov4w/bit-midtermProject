using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShopping.Client.Models
{
    class paymentHistory
    {
        public int PayId { get; set; }
        public string CusId { get; set; } = "";
        public DateTime PayDate { get; set; }
        public int PayMoney { get; set; }
    }
}
