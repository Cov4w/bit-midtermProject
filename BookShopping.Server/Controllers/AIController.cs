using BookShopping.Server.Data;
using BookShopping.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Text.Json.Serialization;

namespace BookShopping.Server.Controllers
{
    #region DTO
    public class bookSummaryDTO
    {
        [JsonPropertyName("isbn")]
        public string RawIsbn { get; set; } = "";
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";
        [JsonPropertyName("sale_price")]
        public int SalePrice { get; set; }
        //[JsonPropertyName("url")]
        //public string Url { get; set; } = "";
    }
    public class AiChatRequestDto
    {
        public string CusId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
    #endregion
    [ApiController]
    [Route("api/aiapi")]
    public class AIController : ControllerBase
    {
        private readonly AppDbContext db;
        private aiKey ai = new aiKey();

        public AIController(AppDbContext context)
        {
            db = context;
        }

        //GET api/aiapi/booksummary/{isbn}
        [HttpGet("booksummary/{isbn}")]
        public async Task<IActionResult> bookSummary(string isbn)
        {
            var book = await db.Books.AsNoTracking().FirstOrDefaultAsync(b => b.Isbn == isbn);
            if (book == null)
            {
                return NotFound(new { message = $"상품 을 찾을 수 없습니다." });
            }
            string result = await ai.SendMessage(book.BookName, book.BookPrice, book.BookURL);
            return Ok(result);
        }
        //GET api/aiapi/cussummarry/{cusID}
        [HttpGet("cussummarry/{cusID}")]
        public async Task<IActionResult> cusSummarry(string cusID)
        {
            try
            {
                var histories = await db.PaymentHistories
                .AsNoTracking()
                .Where(h => h.CusId == cusID)
                .ToListAsync();
                if (histories.Count == 0)
                {
                    return Ok("아직 구매하신 도서 내역이 없어 AI 성향 분석을 진행할 수 없습니다.");
                }
                var payIDs = histories.Select(h => h.PayId).ToList();
                int totalSpent = histories.Sum(h => h.PayMoney);

                var purchasedDetails = await db.PaymentItems
                    .AsNoTracking()
                    .Where(pi => payIDs.Contains(pi.PayID))
                    .Join(db.Books, pi => pi.Isbn, b => b.Isbn, (pi, b) => new { pi, b })
                    .Join(db.BookCategories, combined => combined.b.Category, c => c.Category, (combined, c) => new
                    {
                        BookName = combined.b.BookName,
                        CategoryName = c.CategoryName,
                        // ※ 본인의 PaymentItem 모델에 정의된 수량 프로퍼티명(Count 또는 BookCount)에 맞춰주세요.
                        Count = combined.pi.BookCount
                    })
                    .ToListAsync();
                int totalBooks = purchasedDetails.Sum(x => x.Count);

                // AI 프롬프트에 전달할 구매 도서 요약 문자열 생성 (예: [소설] 클린 코드(2권), [컴퓨터] 리팩터링(1권))[cite: 14]
                string summaryText = string.Join(", ", purchasedDetails
                    .GroupBy(x => new { x.BookName, x.CategoryName })
                    .Select(g => $"[{g.Key.CategoryName}] {g.Key.BookName}({g.Sum(x => x.Count)}권)"));

                // 3. aiKey 서비스를 통한 성향 분석 AI 결과 도출[cite: 14]
                var ai = new aiKey();
                string result = await ai.GetCustomerTendency(cusID, summaryText, totalBooks, totalSpent);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"고객 성향 분석 중 오류가 발생했습니다: {ex.Message}");
            }
        }

        //GET api/aiapi/aichat
        [HttpPost("aichat")]
        public async Task<IActionResult> AiChat([FromBody] AiChatRequestDto req)
        {
            // 고객이 그동안 구매했던 도서 제목들 조회
            var payIds = await db.PaymentHistories
                .AsNoTracking()
                .Where(h => h.CusId == req.CusId)
                .Select(h => h.PayId)
                .ToListAsync();

            var purchasedBooks = await db.PaymentItems
                .AsNoTracking()
                .Where(pi => payIds.Contains(pi.PayID))
                .Select(pi => pi.BookName)
                .Distinct()
                .ToListAsync();

            string purchaseContext = string.Join(", ", purchasedBooks);

            // 현재 판매 중인 서점 도서 중 일부(최대 25권)를 AI 참고용 컨텍스트로 제공
            var storeBooks = await db.Books
                .AsNoTracking()
                .Where(b => b.IsForSale)
                .Take(25)
                .Select(b => $"{b.BookName}({b.BookPrice:N0}원)")
                .ToListAsync();

            string storeContext = string.Join(", ", storeBooks);

            var ai = new aiKey();
            string reply = await ai.SendChatMessage(req.CusId, req.Message, purchaseContext, storeContext);
            return Ok(reply);
        }
    }
}        