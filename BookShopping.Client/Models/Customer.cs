using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShopping.Client.Models
{
    class Customer
    {
        public string cusID { get; set; } = "";
        public string cusPW { get; set; } = "";
        public string cusName { get; set; } = "";
        public int cusMoney { get; set; }
    }
}
