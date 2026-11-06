using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShopping.Server.Models
{
    [Table("customer")]
    public class Customer
    {
        [Key]
        [Column("cusID")]
        [MaxLength(50)]
        public string CusId { get; set; } = "";

        [Required]
        [Column("cusPW")]
        [MaxLength(50)]
        public string CusPw { get; set; } = "";

        [Required]
        [Column("cusName")]
        [MaxLength(50)]
        public string CusName { get; set; } = "";

        [Column("cusMoney")]
        public int CusMoney { get; set; } = 500000;

        // 결제 이력과의 1:N 관계
        public ICollection<PaymentHistory> PaymentHistories { get; set; }
            = new List<PaymentHistory>();
    }
}