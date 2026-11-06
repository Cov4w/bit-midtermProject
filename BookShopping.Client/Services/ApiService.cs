
using BookShopping.Client.Models;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace BookShopping.Client.Services
{
    internal class ApiService
    {
        private readonly HttpClient client;

        public ApiService()
        {
            client = new HttpClient
            {
                BaseAddress = new Uri("https://localhost:7247")
            };
        }

        #region Admin
        public async Task<List<Customer>> GetCustomers()
        {
            return await client.GetFromJsonAsync<List<Customer>>("api/admin/customers") ?? new();
        }

        public async Task<List<paymentHistory>> GetPaymentHistories(string cusID)
        {
            var response = await client.GetAsync($"api/admin/{cusID}");

            if (!response.IsSuccessStatusCode)
            {
                int statusCode = (int)response.StatusCode;
                string serverMessage = "요청 처리에 실패했습니다.";

                try
                {
                    var errorJson = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                    if (errorJson.TryGetProperty("message", out var msg))
                    {
                        serverMessage = msg.GetString() ?? serverMessage;
                    }
                }
                catch
                {
                    string rawText = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(rawText))
                    {
                        serverMessage = rawText;
                    }
                }

                // 상태 코드와 메시지를 함께 담아 예외 발생
                throw new Exception($"[오류 코드: {statusCode}]\n{serverMessage}");
            }
            return await client.GetFromJsonAsync<List<paymentHistory>>($"api/admin/{cusID}")??new();            
        }
        public async Task<List<PaymentItem>> GetPaymentItems(int payID)
        {
            return await client.GetFromJsonAsync<List<PaymentItem>>($"api/admin/payment/{payID}") ?? new();
        }
        public async Task DeleteCustomer(string cusID)
        {
            var response = await client.DeleteAsync($"api/admin/customers/{cusID}");
            response.EnsureSuccessStatusCode();
        }
        public async Task UpdateBookSaleStatus(string isbn, bool isForSale)
        {
            var req = new { IsForSale = isForSale };
            var response = await client.PutAsJsonAsync($"api/admin/books/{isbn}/sale-status", req);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"[{(int)response.StatusCode}] {error}");
            }
        }
        #endregion

        #region Login/Signup
        public async Task<Customer> Login(string id, string pw)
        {
            var loginData = new { Id = id, Pw = pw };
            var response = await client.PostAsJsonAsync("api/auth/login", loginData);

            // [가장 중요한 부분] 
            // HttpClient는 401(Unauthorized)이나 404(NotFound)가 와도 자동으로 에러를 내지 않습니다.
            // 따라서 직접 IsSuccessStatusCode(200~299번대인지)를 확인해서 예외를 던져줘야 합니다!
            if (!response.IsSuccessStatusCode)
            {
                // 서버가 보낸 { "message": "비밀번호가 틀렸습니다." } JSON에서 메시지 추출
                var errorJson = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                if (errorJson.TryGetProperty("message", out var msg))
                {
                    // 여기서 예외를 던지면 RunSafe의 catch(Exception ex)가 받아서 MessageBox.Show(ex.Message)를 실행합니다!
                    throw new Exception(msg.GetString());
                }

                throw new Exception("아이디 또는 비밀번호가 일치하지 않습니다.");
            }

            // 성공했을 때만 Customer 객체 반환
            return await response.Content.ReadFromJsonAsync<Customer>()
                   ?? throw new Exception("회원 정보를 불러오지 못했습니다.");
        }
        public async Task Signup(Customer customer)
        {
            var response = await client.PostAsJsonAsync("api/auth/signup", customer);
            response.EnsureSuccessStatusCode();
        }
        #endregion

        #region Book
        public async Task BookRefresh(int? page)
        {
            var response = await client.PostAsync($"api/book/sync?searchPage={page}", null);
            response.EnsureSuccessStatusCode();
        }
        public async Task<List<Book>> BookSearch(string? keyword, string? category, int? minprice, int? maxprice)
        {
            var filter = new { keyword, category, minprice, maxprice };
            var response = await client.PostAsJsonAsync("api/book/search", filter);

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"검색 실패: {error}");
            }

            return await response.Content.ReadFromJsonAsync<List<Book>>() ?? new();
        }
        #endregion

        #region Customer
        public async Task<List<Book>> GetBooks()
        {
            return await client.GetFromJsonAsync<List<Book>>("api/auth/booklist") ?? new();
        }
        public async Task<Customer> GetCustomers(string cusID)
        {
            return await client.GetFromJsonAsync<Customer>($"api/auth/{cusID}") ?? new();
        }
        public async Task<CheckoutResponseDto> Checkout(CartPaymentRequestDto chectoutDto)
        {
            var response = await client.PostAsJsonAsync("api/auth/checkout", chectoutDto);

            if (!response.IsSuccessStatusCode)
            {
                //string errorBody = await response.Content.ReadAsStringAsync();
                //throw new Exception($"[{(int)response.StatusCode} {response.StatusCode}]\n{errorBody}");
                var errorJson = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
                if (errorJson.TryGetProperty("message", out var msg))
                {
                    throw new Exception(msg.GetString());
                }

                throw new Exception("결제 처리에 실패했습니다.");
            }

            // 성공 응답 역직렬화 반환
            return await response.Content.ReadFromJsonAsync<CheckoutResponseDto>()
                   ?? throw new Exception("결제 결과 응답을 읽지 못했습니다.");
        }
        #endregion

        #region AI
        public async Task<string> GetBookAiSummary(string isbn)
        {
            var response =  await client.GetAsync($"api/aiapi/booksummary/{isbn}");
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI 요약 호출 실패: {error}");
            }

            // 서버가 return Ok(result)로 보낸 텍스트를 그대로 읽어옵니다.
            return await response.Content.ReadAsStringAsync();
        }
        public async Task<string> GetCustomerSummary(string cusID)
        {
            var response = await client.GetAsync($"api/aiapi/cussummarry/{cusID}");
            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                throw new Exception($"AI 요약 호출 실패: {error}");
            }

            // 서버가 return Ok(result)로 보낸 텍스트를 그대로 읽어옵니다.
            return await response.Content.ReadAsStringAsync();
        }
        public async Task<string> SendAiChat(string cusID, string message)
        {
            var req = new { CusId = cusID, Message = message };
            var response = await client.PostAsJsonAsync("api/aiapi/aichat", req);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        #endregion
    }
}
