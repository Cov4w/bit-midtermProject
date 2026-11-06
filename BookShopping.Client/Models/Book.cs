using System.Text.Json.Serialization;

namespace BookShopping.Client.Models
{
    public class Book
    {
        [JsonPropertyName("isbn")]
        public string Isbn { get; set; } = string.Empty;

        [JsonPropertyName("bookName")]
        public string BookName { get; set; } = string.Empty;

        [JsonPropertyName("category")]
        public string Category { get; set; } = string.Empty;

        [JsonPropertyName("categoryName")]
        public string CategoryName { get; set; } = string.Empty;

        [JsonPropertyName("bookPrice")]
        public int BookPrice { get; set; }

        [JsonPropertyName("bookCount")]
        public int BookCount { get; set; }

        [JsonPropertyName("isForSale")]
        public bool IsForSale { get; set; } = true;
        public string? BookURL { get; set; }
        public string? Thumbnail { get; set; }
    }
}