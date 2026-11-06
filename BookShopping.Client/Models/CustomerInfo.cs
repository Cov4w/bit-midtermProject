using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookShopping.Client.Models
{
    public class CategoryChartItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
        public string DisplayPercent => $"{Percentage:0.#}% ({Count}권)";
    }

    // AI 채팅 말풍선 디자인 바인딩용 클래스 (나: 딥 네이비 / AI: 화이트)
    public class ChatBubbleItem
    {
        public string Sender { get; set; }
        public string Message { get; set; }
        public string BubbleBackground { get; set; }
        public string BubbleBorder { get; set; }
        public string SenderForeground { get; set; }
        public string TextForeground { get; set; }

        public ChatBubbleItem(string sender, string message, bool isUser)
        {
            Sender = sender;
            Message = message;
            if (isUser)
            {
                // 사용자 메시지: 어두운 네이비 배경 + 화이트 텍스트
                BubbleBackground = "#1B2A4A";
                BubbleBorder = "#1B2A4A";
                SenderForeground = "#A5B4FC";
                TextForeground = "#FFFFFF";
            }
            else
            {
                // AI 응답 메시지: 화이트 배경 + 차콜 블랙 텍스트
                BubbleBackground = "#FFFFFF";
                BubbleBorder = "#E2E5EC";
                SenderForeground = "#1B2A4A";
                TextForeground = "#18181B";
            }
        }
    }
}
