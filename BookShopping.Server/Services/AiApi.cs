using DotNetEnv;
using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;

namespace BookShopping.Server.Services
{
    internal class aiKey
    {
        private ChatClient client;

        public aiKey()
        {
            // .env 파일에서 환경 변수를 로드합니다.
            Env.Load();

            // 공백이나 줄바꿈 문자가 섞여 들어가는 것을 방지하기 위해 Trim()을 호출합니다.
            string key = Environment.GetEnvironmentVariable("VIRTUAL_KEY")?.Trim()!;
            string baseUrl = Environment.GetEnvironmentVariable("SCHOOL_LLM_BASE_URL")?.Trim().TrimEnd('/')!;

            client = new ChatClient(
                //model: "claude-sonnet-4-5",
                model: "gemini-3.1-flash-lite", // 만약 에러 메시지에 모델 관련 오류가 뜨면 처음 코드의 "gpt-4o" 등으로 변경하세요[cite: 13].
                credential: new ApiKeyCredential(key!),
                options: new OpenAIClientOptions { Endpoint = new Uri($"{baseUrl}/api/v1/") });
        }

        public async Task<string> SendMessage(string title, int price, string book_url)
        {
            try
            {
                string combinedPrompt = $"""
                    # Role
                    당신은 도서의 핵심 가치를 꿰뚫어 보는 '수석 큐레이션 사서'입니다. 정중하고 지적인 어조(~입니다/합니다)를 유지하며, 이모티콘을 사용하지 않고 군더더기 없이 깔끔하고 명확하게 도서를 요약합니다.

                    # Input Data
                    - 책 제목: {title}
                    - 책 가격: {price:N0}원
                    - 책 URL: {book_url}

                    # Task & Output Format
                    제공된 도서 정보를 분석하여 아래 3가지 카테고리로 요약하세요. 

                    1. [도서 성향]: 입문용/실무용, 이론/실습 중심, 문체 등 책의 전반적인 성격
                    2. [핵심 지식]: 독자가 이 책을 완독했을 때 얻게 되는 실질적인 지식과 인사이트
                    3. [가성비 및 추천 대상]: 책 가격 대비 지식의 밀도(가성비) 평가 및 가장 큰 도움을 받을 타겟 독자층

                    # Constraints (엄수 사항)
                    - 공백 포함 **반드시 200자 이내**로 작성할 것.
                    - 인사말이나 서론 없이 즉시 3가지 카테고리 결과만 출력할 것.
                    - 각 항목은 한 줄씩 개조식이나 간결한 문장으로 작성할 것.
                    """;

                ChatMessage[] messages = new ChatMessage[]
                {
                    new UserChatMessage(combinedPrompt)
                };

                ChatCompletion completion = await client.CompleteChatAsync(messages);
                string result = completion.Content[0].Text;

                return result ?? "잘못된 답변";
            }
            catch (ClientResultException ex)
            {
                string rawErrorBody = ex.GetRawResponse()?.Content?.ToString() ?? "응답 본문 없음";
                Console.WriteLine($"[상태 코드]: {ex.Status}\n[서버 상세 응답]: {rawErrorBody}");

                return $"API 호출 오류 발생: {rawErrorBody}";
            }
        }
        public async Task<string> GetCustomerTendency(string cusID, string summaryText, int totalBooks, int totalSpent)
        {
            try
            {
                string combinedPrompt = $"""
                # Role
                당신은 독자의 도서 구매 이력을 바탕으로 지적 관심사와 독서 성향을 꿰뚫어 보는 '수석 북 큐레이터'입니다. 정중하고 품격 있는 어조(~입니다/합니다)를 사용하세요.

                # Input Data
                - 고객 ID: {cusID}
                - 총 구매 권수: {totalBooks}권 (누적 결제액: {totalSpent:N0}원)
                - 구매 도서 및 카테고리 내역: {summaryText}

                # Task & Output Format
                고객의 구매 데이터를 분석하여 아래 3가지 항목으로 진단 리포트를 작성하세요.

                1. [독서 성향 진단]: 주로 관심을 가지는 분야와 지식 탐구 스타일 요약
                2. [소비 패턴 분석]: 구매 권수 및 가격대를 통해 본 도서 선택 기준
                3. [맞춤형 큐레이션]: 현재 성향을 확장하거나 보완할 수 있는 다음 독서 주제/분야 추천

                # Constraints (엄수 사항)
                - 공백 포함 250자 이내로 핵심만 명확하게 작성할 것.
                - 인사말 없이 즉시 1~3번 결과만 출력할 것.
                """;

                ChatMessage[] messages = new ChatMessage[]
                {
                    new UserChatMessage(combinedPrompt)
                };

                ChatCompletion completion = await client.CompleteChatAsync(messages);
                string result = completion.Content[0].Text;

                return result ?? "잘못된 답변";
            }
            catch (ClientResultException ex)
            {
                string rawErrorBody = ex.GetRawResponse()?.Content?.ToString() ?? "응답 본문 없음";
                Console.WriteLine($"[상태 코드]: {ex.Status}\n[서버 상세 응답]: {rawErrorBody}");

                return $"API 호출 오류 발생: {rawErrorBody}";
            }
        }
        public async Task<string> SendChatMessage(string cusId, string userMessage, string purchaseHistoryContext, string storeBooksContext)
        {
            try
            {
                string combinedPrompt = $"""
                # Role
                당신은 'THE BOOK ARCHIVE' 서점의 전담 수석 사서 AI입니다. 고객의 질문에 정중하고 지적인 태도로 답변하며 맞춤형 도서를 추천합니다.

                # Context Data
                - 상담 고객 ID: {cusId}
                - 고객의 기존 구매 도서 이력: {(string.IsNullOrWhiteSpace(purchaseHistoryContext) ? "구매 이력 없음" : purchaseHistoryContext)}
                - 현재 서점에 보유 중인 주요 도서 목록: {storeBooksContext}

                # User Question
                "{userMessage}"

                # Instructions
                - 고객의 질문 의도에 맞춰 친절하고 명확하게 답변하세요.
                - 만약 도서 추천을 요청받았다면, 위 '현재 서점에 보유 중인 주요 도서 목록' 또는 고객의 구매 이력과 어울리는 책을 근거와 함께 추천하세요.
                - 답변은 가독성 좋게 공백 포함 200자 이내로 간결하게 작성하세요.
                """;

                ChatMessage[] messages = new ChatMessage[]
                {
                    new UserChatMessage(combinedPrompt)
                };

                ChatCompletion completion = await client.CompleteChatAsync(messages);
                string result = completion.Content[0].Text;

                return result ?? "잘못된 답변";
            }
            catch (ClientResultException ex)
            {
                string rawErrorBody = ex.GetRawResponse()?.Content?.ToString() ?? "응답 본문 없음";
                Console.WriteLine($"[상태 코드]: {ex.Status}\n[서버 상세 응답]: {rawErrorBody}");

                return $"API 호출 오류 발생: {rawErrorBody}";
            }
        }
    }
}