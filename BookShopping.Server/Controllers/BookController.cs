using BookShopping.Server.Data;
using BookShopping.Server.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetEnv;

namespace BookShopping.Server.Controllers
{
    #region dto
    public class KakaoBookSearchResponse
    {
        [JsonPropertyName("meta")]
        public KakaoMeta Meta { get; set; } = new();

        [JsonPropertyName("documents")]
        public List<KakaoBookDocument> Documents { get; set; } = new();
    }

    public class KakaoMeta
    {
        // 마지막 페이지 여부
        [JsonPropertyName("is_end")]
        public bool IsEnd { get; set; }

        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }
    }

    public class KakaoBookDocument
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("isbn")]
        public string RawIsbn { get; set; } = "";

        [JsonPropertyName("price")]
        public int Price { get; set; }

        [JsonPropertyName("sale_price")]
        public int SalePrice { get; set; }
        // 카카오 도서 상세 링크
        [JsonPropertyName("url")]
        public string Url { get; set; } = "";

        // 표지 이미지 링크
        [JsonPropertyName("thumbnail")]
        public string Thumbnail { get; set; } = "";
    }
    public class BookSearchFilterDto
    {
        // 도서명 검색 키워드 (null 허용)
        public string? Keyword { get; set; }

        // 카테고리 필터 (예: "컴퓨터/IT", "소설", "경제경영" 등, null 허용)
        public string? Category { get; set; }

        // 최소 가격 필터 (null 허용)
        public int? MinPrice { get; set; }

        // 최대 가격 필터 (null 허용)
        public int? MaxPrice { get; set; }
    }
    #endregion
    [ApiController]
    [Route("api/book")]
    public class BookController : ControllerBase
    {        
        private readonly AppDbContext db;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string KAKAO_REST_API_KEY;
        public BookController(AppDbContext context, IHttpClientFactory httpClientFactory)
        {
            db = context;
            _httpClientFactory = httpClientFactory;
            Env.Load();
            this.KAKAO_REST_API_KEY = Environment.GetEnvironmentVariable("VIRTUAL_KEY")?.Trim() ?? string.Empty;
        }
        //POST /api/book/sync
        [HttpPost("sync")]
        public async Task<IActionResult> SyncAllBooks([FromQuery] int? searchPage)
        {
            // 사용자가 입력한 페이지 수가 없으면 기본 5페이지, 카카오 API 최대 한도인 50페이지로 상한선 제한
            int maxPage = (searchPage.HasValue && searchPage.Value > 0)
                          ? Math.Min(searchPage.Value, 50)
                          : 5;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("KakaoAK", KAKAO_REST_API_KEY);

            var seedKeywords = new[] { "베스트셀러", "소설", "컴퓨터", "인공지능", "경영", "인문", "자기계발", "역사" };

            var categoryIdMap = new Dictionary<string, string>
    {
        { "베스트셀러", "1" },
        { "소설", "2" },
        { "컴퓨터", "3" },
        { "인공지능", "4" },
        { "경영", "5" },
        { "인문", "6" },
        { "자기계발", "7" },
        { "역사", "8" }
    };

            var collectedBooksMap = new Dictionary<string, Book>();

            foreach (var keyword in seedKeywords)
            {
                // 1페이지부터 사용자가 입력한 maxPage까지 조회
                for (int page = 1; page <= maxPage; page++)
                {
                    var targetUrl = $"https://dapi.kakao.com/v3/search/book?query={Uri.EscapeDataString(keyword)}&sort=accuracy&page={page}&size=50";
                    var response = await client.GetAsync(targetUrl);

                    if (!response.IsSuccessStatusCode)
                        break;

                    var jsonStream = await response.Content.ReadAsStreamAsync();
                    var searchResult = await JsonSerializer.DeserializeAsync<KakaoBookSearchResponse>(jsonStream);

                    if (searchResult?.Documents == null || searchResult.Documents.Count == 0)
                        break;

                    foreach (var doc in searchResult.Documents)
                    {
                        if (string.IsNullOrWhiteSpace(doc.RawIsbn))
                            continue;

                        var tokens = doc.RawIsbn.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        var cleanIsbn = tokens.Length > 0 ? tokens[^1] : doc.RawIsbn.Trim();

                        int finalPrice = doc.SalePrice > 0 ? doc.SalePrice : (doc.Price > 0 ? doc.Price : 15000);

                        if (!collectedBooksMap.ContainsKey(cleanIsbn))
                        {
                            collectedBooksMap[cleanIsbn] = new Book
                            {
                                Isbn = cleanIsbn,
                                BookName = doc.Title.Trim(),
                                Category = categoryIdMap[keyword],
                                BookPrice = finalPrice,
                                BookURL = doc.Url,
                                Thumbnail = doc.Thumbnail,
                                BookCount = Random.Shared.Next(1, 1000),
                                IsForSale = true
                            };
                        }
                    }

                    if (searchResult.Meta.IsEnd) break;
                }
            }

            var allFetchedIsbns = collectedBooksMap.Keys.ToList();
             
            var existingIsbns = (await db.Books
                .Where(b => allFetchedIsbns.Contains(b.Isbn))
                .Select(b => b.Isbn)
                .ToListAsync())
                .ToHashSet();

            var newBooksToInsert = collectedBooksMap.Values
                .Where(b => !existingIsbns.Contains(b.Isbn))
                .ToList();

            if (newBooksToInsert.Count > 0)
            {
                await db.Books.AddRangeAsync(newBooksToInsert);
                await db.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "외부 도서 데이터 동기화 완료",
                requestedPage = maxPage,
                totalFetchedFromApi = collectedBooksMap.Count,
                alreadyInDbCount = existingIsbns.Count,
                newlyInsertedCount = newBooksToInsert.Count
            });
        }

        //Get /api/book/search/
        //[HttpGet("search")]
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] BookSearchFilterDto filter)
        {
            var query = db.Books.AsNoTracking().AsQueryable();

            // 카테고리가 선택된 경우에만 조건 추가
            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                var trimmedCategory = filter.Category.Trim();
                query = query.Where(b => b.Category == trimmedCategory);
            }

            // 검색어가 입력된 경우에만 조건 추가
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var trimmedKeyword = filter.Keyword.Trim();
                query = query.Where(b => b.BookName.Contains(trimmedKeyword));
            }

            if (filter.MinPrice.HasValue)
            {
                query = query.Where(b => b.BookPrice >= filter.MinPrice.Value);
            }

            if (filter.MaxPrice.HasValue)
            {
                query = query.Where(b => b.BookPrice <= filter.MaxPrice.Value);
            }

            var books = await query
                .Join(
                    db.BookCategories,
                    b => b.Category,
                    c => c.Category,
                    (b, c) => new
                    {
                        b.Isbn,
                        b.BookName,
                        b.Category,
                        CategoryName = c.CategoryName,
                        b.BookPrice,
                        b.BookCount,
                        b.IsForSale
                    }
                )
                .OrderBy(b => b.BookName)
                .ToListAsync();

            return Ok(books);
        }
    }
}
