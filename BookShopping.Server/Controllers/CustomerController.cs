using BookShopping.Server.Data;
using BookShopping.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace BookShopping.Server.Controllers
{
    # region 요청/응답용 DTO 정의

    // 회원가입 요청 DTO (클라이언트로부터 받을 데이터만 정의)
    public class SignupRequestDto
    {
        [Required]
        [MaxLength(50)]
        public string CusId { get; set; } = "";
        [Required]
        [MaxLength(50)]
        public string CusPw { get; set; } = "";
        [Required]
        [MaxLength(50)]
        public string CusName { get; set; } = "";
    }

    // 로그인 요청 DTO (JSON Body 바인딩용)
    public class LoginRequestDto
    {
        [Required]
        public string Id { get; set; } = "";

        [Required]
        public string Pw { get; set; } = "";
    }
    // 결제 요청 개별 품목
    public class CartItemDto
    {
        [Required]
        // 앞서 변경된 Book 모델 컬럼명(BookName) 또는 ProductName
        public string BookName { get; set; } = "";

        [Range(1, int.MaxValue, ErrorMessage = "수량은 1개 이상이어야 합니다.")]
        public int Count { get; set; }
    }
    // 결제 요청 Body
    public class CartPaymentRequestDto
    {
        [Required]
        public string CusId { get; set; } = "";

        [Required]
        public List<CartItemDto> Items { get; set; } = new();
    }
    #endregion

    [ApiController]
    [Route("api/auth")]
    public class CustomerController : ControllerBase
    {
        private readonly AppDbContext db;
        public CustomerController(AppDbContext context)
        {
            db = context;
        }

        //POST /api/auth/signup
        [HttpPost("signup")]
        public async Task<IActionResult> Create(Customer dto)
        {
            if(await db.Customers.AnyAsync(c=>c.CusId==dto.CusId))
            {
                return Conflict("이미 존재하는 회원입니다.");
            }  
            var customer = new Customer
            {
                CusId = dto.CusId,
                CusPw = dto.CusPw,
                CusName = dto.CusName
            };

            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(Get), new { id = customer.CusId },
                new
                {
                    message = "회원가입이 완료되었습니다.",
                    cusID = customer.CusId,
                    cusName = customer.CusName,
                    cusMoney = customer.CusMoney
                });
        }
        //POST /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto req)
        {
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.CusId == req.Id);
            if(customer == null)
            {
                return Unauthorized(new { message = "존재하지 않는 아이디입니다." });
            }
            if(customer.CusPw != req.Pw)
            {
                return Unauthorized(new { message = "비밀번호가 틀렸습니다." });
            }

            return Ok(new
            {
                cusID = customer.CusId,
                cusName = customer.CusName,
                cusMoney = customer.CusMoney,
            });
        }

        //GET /api/auth/1
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(string id)
        {
            var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CusId == id);

            if (customer == null) 
            {
                return NotFound();
            }
            return Ok(new
            {
                cusID = customer.CusId,
                cusName = customer.CusName,
                cusMoney = customer.CusMoney
            });
        }

        //POST /api/auth/checkout
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CartPaymentRequestDto req)
        {
            // 0. 빈 장바구니 검증
            if (req.Items == null || req.Items.Count == 0)
            {
                return BadRequest(new { message = "장바구니에 담긴 상품이 없습니다." });
            }

            // [핵심] 여러 테이블의 변경 사항을 하나의 트랜잭션으로 묶음
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                // 1. 회원 정보 조회
                var customer = await db.Customers.FirstOrDefaultAsync(c => c.CusId == req.CusId);
                if (customer == null)
                {
                    return NotFound(new { message = "존재하지 않는 회원입니다." });
                }

                int totalPayMoney = 0;

                // 첫 번째 루프와 두 번째 루프에서 책 정보를 공유하기 위해 임시 저장할 튜플 리스트
                var processedBookItems = new List<(Book Book, int Count)>();

                // 2. 장바구니 품목 순회하며 재고 검증 및 가격 계산
                foreach (var item in req.Items)
                {
                    var book = await db.Books.FirstOrDefaultAsync(b => b.BookName == item.BookName);
                    if (book == null)
                    {
                        await transaction.RollbackAsync();
                        return NotFound(new { message = $"상품 '{item.BookName}'을(를) 찾을 수 없습니다." });
                    }

                    // 재고 부족 여부 체크
                    if (book.BookCount < item.Count)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new
                        {
                            message = $"'{book.BookName}' 상품의 재고가 부족합니다. (현재 재고: {book.BookCount}개, 요청: {item.Count}개)"
                        });
                    }

                    // 재고 차감
                    book.BookCount -= item.Count;

                    // 금액 계산 (책 가격 * 수량)
                    totalPayMoney += (book.BookPrice * item.Count);

                    // 나중에 PaymentItem을 만들기 위해 리스트에 보관
                    processedBookItems.Add((book, item.Count));
                }

                // 3. 회원 잔액(cusMoney) 검증
                if (customer.CusMoney < totalPayMoney)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(new
                    {
                        message = $"잔액이 부족합니다. (필요 금액: {totalPayMoney:N0}원, 보유 잔액: {customer.CusMoney:N0}원)"
                    });
                }

                // 잔액 차감
                customer.CusMoney -= totalPayMoney;

                // 4. 결제 기록(PaymentHistory) 생성 및 저장 (PayID 발급을 위해 먼저 SaveChanges)
                var paymentRecord = new PaymentHistory
                {
                    CusId = customer.CusId,
                    PayDate = DateTime.Now,
                    PayMoney = totalPayMoney
                };

                db.PaymentHistories.Add(paymentRecord);
                await db.SaveChangesAsync(); // 이 시점에 paymentRecord.PayID가 자동 생성됨!

                // 5. 상세 결제 아이템(PaymentItems) 생성 및 일괄 저장
                var paymentItems = new List<PaymentItem>();
                foreach (var pair in processedBookItems)
                {
                    var book = pair.Book;
                    var count = pair.Count;

                    paymentItems.Add(new PaymentItem
                    {
                        PayID = paymentRecord.PayId, // 생성된 결제 기록의 PK 연결
                        Isbn = book.Isbn,
                        BookName = book.BookName,
                        BookCount = count,
                        BookUnitPrice = book.BookPrice
                    });
                }

                // 상세 품목들을 DB에 추가하고 반영
                await db.PaymentItems.AddRangeAsync(paymentItems);
                await db.SaveChangesAsync();

                // 6. 모든 작업 성공 시 트랜잭션 커밋
                await transaction.CommitAsync();

                // 응답용 payList 문자열 재조합 ("책이름(수량), 책이름(수량)")
                string generatedPayList = string.Join(", ", paymentItems.Select(i => $"{i.BookName}({i.BookCount})"));

                // 7. 성공 응답 반환
                return CreatedAtAction(nameof(Checkout), new
                {
                    payID = paymentRecord.PayId,
                    cusID = paymentRecord.CusId,
                    payDate = paymentRecord.PayDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    payList = generatedPayList, // 조합된 문자열 전달
                    payMoney = paymentRecord.PayMoney,
                    remainCusMoney = customer.CusMoney
                });
            }
            catch (Exception ex)
            {
                // 예기치 못한 DB 오류 발생 시 자동 롤백
                await transaction.RollbackAsync();
                var innerMessage = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
                return StatusCode(500, new { message = "결제 처리 중 서버 오류가 발생했습니다.", detail = ex.Message });
            }
        }
        //GET /api/auth/booklist
        [HttpGet("booklist")]
        public async Task<IActionResult> GetBookList()
        {
            var books = await db.Books
                .AsNoTracking()
                .Join(
                    db.BookCategories,
                    b => b.Category,          // Book의 외래키
                    c => c.Category,          // BookCategory의 기본키
                    (b, c) => new
                    {
                        b.Isbn,
                        b.BookName,
                        b.Category,
                        CategoryName = c.CategoryName, // BookCategory 테이블의 이름 컬럼 매핑
                        b.BookPrice,
                        b.BookCount,
                        b.IsForSale,
                        b.BookURL,   // [추가] 상세 링크 전달
                        b.Thumbnail
                    }
                )
                .ToListAsync();

            return Ok(books);
        }
    }
}
