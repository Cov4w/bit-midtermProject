using System.Text.Json.Serialization;

namespace BookShopping.Client.Models
{
    public class CartItemDto
    {
        public string ISBN { get; set; } = "";
        // 주문할 책 이름
        public string BookName { get; set; } = "";

        // 주문 수량
        [JsonPropertyName("count")]
        public int OrderCount { get; set; }

        // [선택] WPF 장바구니 DataGrid 표시용 속성 (서버 전송 시에는 무시되어도 무방)
        public int BookPrice { get; set; }
        public int TotalPrice => BookPrice * OrderCount;
    }
    public class CartPaymentRequestDto
    {
        public string CusId { get; set; } = "";
        public List<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        //public string Message { get; internal set; } = "";
    }
    public class CheckoutResponseDto
    {
        // 결제 성공 여부
        public bool Success { get; set; }

        // 서버 처리 메시지 (예: "결제가 완료되었습니다.", "재고가 부족합니다.")
        public string Message { get; set; } = string.Empty;

        // 결제 후 차감되고 남은 최종 잔액
        public int RemainBalance { get; set; }
    }
}
