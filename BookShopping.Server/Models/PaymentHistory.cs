using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShopping.Server.Models
{
    [Table("paymentHistory")]
    public class PaymentHistory
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("payID")]
        public int PayId { get; set; }

        [Required]
        [Column("cusID")]
        [MaxLength(50)]
        public string CusId { get; set; } = "";

        [Column("payDate")]
        public DateTime PayDate { get; set; } = DateTime.Now;

        [Column("payMoney")]
        public int PayMoney { get; set; }

        // customer 테이블과의 외래키(FK) 관계 매핑
        [ForeignKey("CusId")]
        public Customer? Customer { get; set; }
        public ICollection<PaymentItem> PaymentItems { get; set; } = new List<PaymentItem>();
    }
}