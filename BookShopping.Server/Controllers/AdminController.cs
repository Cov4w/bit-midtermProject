using BookShopping.Server.Data;
using BookShopping.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BookShopping.Server.Controllers
{
    public class UpdateSaleStatusDto
    {
        public bool IsForSale { get; set; }
    }
    [ApiController]
    [Route("api/admin")]
    public class AdminController:ControllerBase
    {
        private readonly AppDbContext db;
        public AdminController(AppDbContext context)
        {
            db = context;
        }
        //GET api/admin/customers
        [HttpGet("customers")]
        public async Task<IActionResult> GetAll()
        {
            var customers = await db.Customers.AsNoTracking().OrderBy(c=>c.CusId).ToListAsync();

            return Ok(customers);
        }
        //GET /api/admin/{cusID}
        [HttpGet("{cusid}")]
        public async Task<IActionResult> Get(string cusid)
        {
            var payHis = await db.PaymentHistories
                .AsNoTracking()
                .Include(p => p.Customer) // 외래키 관계인 Customer를 JOIN해서 함께 로드
                .Include(p => p.PaymentItems)
                .Where(p => p.CusId == cusid)
                .Select(p => new
                {
                    payId = p.PayId,
                    cusId = p.CusId,
                    cusName = p.Customer != null ? p.Customer.CusName : "", // 외래키를 타고 가서 회원 이름 가져오기
                    payDate = p.PayDate,
                    payMoney = p.PayMoney              
                })
                .ToListAsync();

            if (payHis == null)
            {
                return NotFound(new {msg = $"해당 회원{cusid}이 존재하지 않습니다."});
            }
            else if ( payHis.Count == 0)
            {
                return NotFound(new { message = "구매 내역이 존재하지 않습니다." });
            }
            return Ok(payHis);
        }
        //GET /api/admin/{payID}
        [HttpGet("payment/{payid}")]
        public async Task<IActionResult> payDetail(int payid)
        {
            var payDetail = await db.PaymentItems
                .AsNoTracking()
                .Where(p => p.PayID == payid)
                .Select(p => new
                {
                    Isbn = p.Isbn,
                    BookName = p.BookName,
                    BookCount = p.BookCount,
                    BookUnitPrice = p.BookUnitPrice
                })
                .ToListAsync();

            if(payDetail.Count == 0)
            {
                return NotFound();
            }
            return Ok(payDetail);

        }
        //DELETE /api/admin/customers/{cusID}
        [HttpDelete("customers/{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            if (id.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { message = "기본 관리자 계정은 삭제할 수 없습니다." });
            }
            var customer = await db.Customers.Include(c => c.PaymentHistories).FirstOrDefaultAsync(c => c.CusId == id);

            if(customer == null)
            {
                return NotFound(new {message = "존재하지 않는 회원입니다."});
            }
            
            db.Customers.Remove(customer);
            await db.SaveChangesAsync();
            return Ok(new {message = $"회원{id} 및 관련 결제 기록이 모두 삭제되었습니다."});
        }

        [HttpPut("books/{isbn}/sale-status")]
        public async Task<IActionResult> UpdateSaleStatus(string isbn, [FromBody] UpdateSaleStatusDto dto)
        {
            var book = await db.Books.FirstOrDefaultAsync(b => b.Isbn == isbn);
            if (book == null)
            {
                return NotFound(new { message = "해당 도서를 찾을 수 없습니다." });
            }

            book.IsForSale = dto.IsForSale;
            await db.SaveChangesAsync();

            return Ok(new { message = "판매 여부가 변경되었습니다.", isForSale = book.IsForSale });
        }
    }
}
