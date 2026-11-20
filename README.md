# THE BOOK ARCHIVE

C#과 WPF를 기반으로 구현한 도서 쇼핑 데스크톱 애플리케이션입니다. 도서 검색과 구매, 회원 관리 기능을 제공하며, AI를 활용해 도서 요약과 사용자 독서 성향을 분석합니다.

## 주요 기능

### 사용자
- 회원가입 및 로그인
- 도서 목록 조회 및 검색
- 키워드·카테고리·가격 조건을 이용한 도서 검색
- 장바구니 및 도서 결제
- 회원정보 및 비밀번호 수정
- AI 도서 요약 및 구매 이력 기반 독서 성향 분석
- AI 채팅을 통한 도서 상담 및 추천

### 관리자
- 회원 목록 및 회원별 결제 내역 조회
- 결제 상세 품목 확인
- 회원 삭제
- 도서 판매 상태 관리
- 카카오 도서 API를 이용한 도서 데이터 동기화

## 기술 스택

| 구분 | 기술 |
|---|---|
| Client | C#, WPF, .NET 8 |
| Server | ASP.NET Core Web API, .NET 8 |
| Database | MySQL |
| ORM | Entity Framework Core, Pomelo |
| External API | Kakao Book Search API |
| AI | OpenAI 호환 Chat API |
| API 문서 | Swagger |

## 프로젝트 구조

```text
3조_중간프로젝트/
├── BookShopping.Client/
│   ├── Models/          # 클라이언트 데이터 모델
│   ├── Services/        # 서버 API 통신
│   └── Views/           # 로그인, 사용자, 관리자 화면
├── BookShopping.Server/
│   ├── Controllers/     # 인증, 도서, 관리자, AI API
│   ├── Data/            # EF Core DbContext
│   ├── Models/          # 데이터베이스 모델
│   └── Services/        # AI 연동 서비스
└── 3조_중간프로젝트.sln
```

## 실행 환경

- Windows
- Visual Studio 2022
- .NET 8 SDK
- MySQL Server

## 실행 방법

1. MySQL에 `shop_db` 데이터베이스를 생성하고 서버의 연결 문자열을 환경에 맞게 설정합니다.
2. 서버 프로젝트의 설정 파일에 데이터베이스 연결 정보를 입력합니다.
3. 서버 프로젝트에서 필요한 API 키와 AI API 주소 등 환경 변수를 설정합니다.
4. 솔루션(`3조_중간프로젝트.sln`)을 Visual Studio에서 엽니다.
5. `BookShopping.Server`와 `BookShopping.Client`를 실행합니다.
6. 클라이언트의 API 기본 주소(`ApiService.cs`)가 실행 중인 서버 주소와 일치하는지 확인합니다.

서버는 개발 환경에서 Swagger UI를 제공합니다.
