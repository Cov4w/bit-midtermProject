using BookShopping.Client.Models;
using BookShopping.Client.Services;
using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics; // Process.Start(웹 브라우저 링크 열기)용 네임스페이스 추가
using System.Windows.Media.Imaging;
using System.Windows.Input;

namespace BookShopping.Client.Views
{
    /// <summary>
    /// CustomerWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CustomerWindow : Window
    {
        private ObservableCollection<ChatBubbleItem> chatMessages = new();
        private readonly ApiService api = new();
        protected string cusID = string.Empty;
        private int? cusBalance = null;
        private ObservableCollection<CartItemDto> cartItemDtos = new ObservableCollection<CartItemDto>();
        private readonly Dictionary<string, string?> categoryIdMap = new()
        {
            { "전체", null },
            { "베스트셀러", "1" },
            { "소설", "2" },
            { "컴퓨터", "3" },
            { "인공지능", "4" },
            { "경영", "5" },
            { "인문", "6" },
            { "자기계발", "7" },
            { "역사", "8" }
        };
        public CustomerWindow(string userID)
        {
            InitializeComponent();
            lbCart.ItemsSource = cartItemDtos;
            lbAiChat.ItemsSource = chatMessages;
            cusID = userID;

            cartItemDtos.CollectionChanged += (s, e) =>
            {
                UpdateSummary();
            };
            Loaded += AdminWindow_Loaded;
        }
        private async void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RunSafe(async () =>
            {
                await LoadBooks();
                await LoadBalance(cusID);
                LoadCategories();
            });
        }
        #region 상단 메뉴
        private void cusLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("로그아웃 하시겠습니까?", "로그아웃", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                cusID = string.Empty;
                cusBalance = null;
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }
        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion
        #region  Load
        private async Task RunSafe(Func<Task> action)
        {
            try
            {
                await action();
            }
            catch (HttpRequestException ex)
            {
                MessageBox.Show(
                    "서버 연결에 실패했습니다.\n" + ex.Message);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private async Task LoadBalance(string cusID)
        {
            Customer customer = await api.GetCustomers(cusID);
            cusBalance = customer.cusMoney;
            txtBalance.Text = $"계좌 잔액 : [{customer.cusMoney.ToString()}]원";
        }
        private async Task LoadBooks()
        {
            var list = await api.GetBooks();
            topBookCount.Text = $"도서 수  : [{list.Count}]";
            lbProducts.ItemsSource = list;
        }
        private void LoadCategories()
        {
            cbCategory.ItemsSource = categoryIdMap;
            cbCategory.DisplayMemberPath = "Key";
            cbCategory.SelectedValuePath = "Value";
            cbCategory.SelectedIndex = 0; // 기본값 "전체" 선택

        }
        private void UpdateSummary()
        {
            int totalCount = cartItemDtos.Sum(item => item.OrderCount);
            int totalPrice = cartItemDtos.Sum(item => item.TotalPrice);
            txtTotalCount.Text = $"총 : {totalCount.ToString()}권";
            txtTotalPrice.Text = $"총 가격 : {totalPrice.ToString()}원";
            int afterBalance = (int)(cusBalance! - totalPrice);
            txtAfterBalance.Text = $"예상 잔액 {afterBalance.ToString()}";
            if (afterBalance < 0)
            {
                txtAfterBalance.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                txtAfterBalance.Foreground = System.Windows.Media.Brushes.Black;
            }
        }
        #endregion
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (lbProducts.SelectedItem is Book selectedItem)
            {
                if (!selectedItem.IsForSale)
                {
                    MessageBox.Show("현재 판매가 중단된 도서입니다.");
                    return;
                }
                if (!int.TryParse(txtQuantity.Text.Trim(), out int quantity) || quantity <= 0)
                {
                    MessageBox.Show("올바른 수량을 입력해주세요.");
                    return;
                }
                cartItemDtos.Add( new CartItemDto{
                    ISBN = selectedItem.Isbn,
                    BookName = selectedItem.BookName,
                    OrderCount = quantity,
                    BookPrice = selectedItem.BookPrice
                });
                txtQuantity.Text = "";
            }
        }

        private void lbProducts_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbProducts.SelectedItem is Book selectedBook)
            {
                // 표지 이미지 로드
                if (!string.IsNullOrWhiteSpace(selectedBook.Thumbnail))
                {
                    try
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.UriSource = new Uri(selectedBook.Thumbnail, UriKind.Absolute);
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        imgBookPreview.Source = bitmap;
                    }
                    catch { imgBookPreview.Source = null; }
                }
                else { imgBookPreview.Source = null; }

                // AI 요약 버튼을 누르기 전 기본 안내 표시
                txtBookSummary.Text = $"선택된 도서: [{selectedBook.BookName}]\n우측 상단의 [AI 요약 생성] 버튼을 클릭하면 AI 요약이 이곳에 표시됩니다.";
            }
        }
        private void btnOpenBookUrl_Click(object sender, RoutedEventArgs e)
        {
            if (lbProducts.SelectedItem is not Book selectedBook)
            {
                MessageBox.Show("먼저 좌측 목록에서 도서를 선택해주세요.");
                return;
            }

            if (string.IsNullOrWhiteSpace(selectedBook.BookURL))
            {
                MessageBox.Show("해당 도서의 상세 링크 정보가 없습니다.");
                return;
            }

            try
            {
                // 기본 웹 브라우저로 도서 상세 URL(카카오 도서 페이지) 열기
                Process.Start(new ProcessStartInfo
                {
                    FileName = selectedBook.BookURL,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("링크를 여는 중 오류가 발생했습니다.\n" + ex.Message);
            }
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            await RunSafe(async () =>
            {
                string? keyword = txtSearch.Text.Trim();
                int? min = int.TryParse(txtMinPrice.Text.Trim(), out int minVal) ? minVal : null;
                int? max = int.TryParse(txtMaxPrice.Text.Trim(), out int maxVal) ? maxVal : null;
                string? category = cbCategory.SelectedValue as string;
                
                var list = await api.BookSearch(keyword, category, min, max);
                MessageBox.Show($"검색된 도서 수: {list.Count}건");
                lbProducts.ItemsSource = list;
            });
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RunSafe(async () =>
            {
                int page = int.TryParse(txtPage.Text.Trim(), out int p) && p > 0 ? p : 3;
                await api.BookRefresh(page);
                MessageBox.Show("동기화 성공");
                txtPage.Text = "";
                await LoadBooks();
            });
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if(lbCart.SelectedItem is CartItemDto selectedItem)
            {
                cartItemDtos.Remove(selectedItem);
                UpdateSummary();
            }
            else
            {
                MessageBox.Show("삭제할 상품을 목록에서 선택해주세요.");
            }
        }
        private async void btnPay_Click(object sender, RoutedEventArgs e)
        {
            // 1. 장바구니 항목 유효성 검사
            if (cartItemDtos.Count == 0)
            {
                MessageBox.Show("장바구니가 비어 있습니다.");
                return;
            }

            // 2. 총 결제 금액 계산
            int totalPrice = cartItemDtos.Sum(item => item.TotalPrice);

            // 3. 계좌 잔액 확인 (현재 잔액 부족 시 차단)
            if (cusBalance < totalPrice)
            {
                MessageBox.Show($"잔액이 부족합니다.\n부족 금액: {totalPrice - cusBalance:N0}원");
                return;
            }

            // 4. 결제 의사 확인
            var confirm = MessageBox.Show(
                $"총 {cartItemDtos.Sum(i => i.OrderCount)}권 / {totalPrice:N0}원을 결제하시겠습니까?",
                "결제 확인",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            // 5. 서버 통신 및 안전 실행
            await RunSafe(async () =>
            {
                var req = new CartPaymentRequestDto
                {
                    CusId = cusID,
                    Items = cartItemDtos.ToList()
                };

                // API 결제 호출
                var result = await api.Checkout(req);

                MessageBox.Show(result.Message.Length > 0 ? result.Message : "결제가 정상 완료되었습니다.");

                // 6. 후처리
                // 장바구니 비우기 (CollectionChanged에 의해 총액/총수량/결제후잔액 자동 갱신됨)
                cartItemDtos.Clear();

                // 차감된 최신 잔액으로 UI 및 멤버 변수 갱신
                await LoadBalance(cusID);

                // 구매로 인해 책 재고 수량이 줄었으므로 책 목록 새로고침
                await LoadBooks();
            });
        }
        private async void btnAiSummary_Click(object sender, RoutedEventArgs e)
        {
            if (lbProducts.SelectedItem is not Book selectedBook)
            {
                MessageBox.Show("먼저 좌측 목록에서 요약할 도서를 선택해주세요.");
                return;
            }

            await RunSafe(async () =>
            {
                btnAiSummary.IsEnabled = false;
                txtBookSummary.Text = "AI가 도서를 분석하고 있습니다...";

                string aiResponse = await api.GetBookAiSummary(
                    selectedBook.Isbn);

                txtBookSummary.Text = aiResponse;
            });
            btnAiSummary.IsEnabled = true;
        }
        private async void cusTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl && cusTabControl.SelectedIndex == 2)
            {
                await LoadMyPaymentHistories();
            }
        }
        private async void btnRefreshHistory_Click(object sender, RoutedEventArgs e)
        {
            await LoadMyPaymentHistories();
        }
        private async Task LoadMyPaymentHistories()
        {
            await RunSafe(async () =>
            {
                var histories = await api.GetPaymentHistories(cusID);
                
                lvMyOrderHistory.ItemsSource = histories;
                lvMyPaymentDetails.ItemsSource = null;

                int totalSpent = histories.Sum(h => h.PayMoney);
                txtStatTotalSpent.Text = $"{totalSpent:N0} 원";

                int totalBookCount = 0;
                foreach (var h in histories)
                {
                    var items = await api.GetPaymentItems(h.PayId);
                    if (items != null)
                    {
                        totalBookCount += items.Sum(item => item.BookCount);
                    }
                }
                var allPurchasedItems = new List<PaymentItem>();
                foreach (var h in histories)
                {
                    var items = await api.GetPaymentItems(h.PayId);
                    if (items != null)
                    {
                        allPurchasedItems.AddRange(items);
                    }
                }

                if (allPurchasedItems.Count == 0)
                {
                    txtTendencyReport.Text = "아직 구매하신 도서 내역이 없습니다.";
                    btnAnalyzeTendency.IsEnabled = true;
                    return;
                }
                // 전체 도서 목록을 가져와 ISBN 기준으로 카테고리 이름을 매칭
                var allBooks = await api.GetBooks();
                var bookCategoryLookup = allBooks.ToDictionary(
                    b => b.Isbn,
                    b => string.IsNullOrWhiteSpace(b.CategoryName) ? "기타" : b.CategoryName);
                // 카테고리별로 구매 수량을 합산하여 차트 데이터 생성
                var categoryStats = allPurchasedItems
                    .GroupBy(i => bookCategoryLookup.TryGetValue(i.Isbn, out var catName) ? catName : "기타")
                    .Select(g => new CategoryChartItem
                    {
                        CategoryName = g.Key,
                        Count = g.Sum(x => x.BookCount),
                        Percentage = totalBookCount > 0
                                     ? Math.Round((double)g.Sum(x => x.BookCount) / totalBookCount * 100, 1)
                                     : 0
                    })
                    .OrderByDescending(c => c.Count)
                    .ToList();

                txtStatTopCategory.Text = categoryStats.FirstOrDefault()?.CategoryName ?? "-";
                icCategoryChart.ItemsSource = categoryStats;
                txtStatTotalBooks.Text = $"{totalBookCount:N0} 권";
            });
        }
        private async void lvMyOrderHistory_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvMyOrderHistory.SelectedItem is paymentHistory selectedOrder)
            {
                await RunSafe(async () =>
                {
                    var details = await api.GetPaymentItems(selectedOrder.PayId);
                    lvMyPaymentDetails.ItemsSource = details;
                });
            }
        }
        private async void btnAnalyzeTendency_Click(object sender, RoutedEventArgs e)
        {
            await RunSafe(async () =>
            {
                btnAnalyzeTendency.IsEnabled = false;
                txtTendencyReport.Text = "고객님의 결제 내역과 상세 구매 도서를 취합하여 성향을 분석 중입니다...";

                // 1. 고객의 전체 결제 내역 조회
                var histories = await api.GetPaymentHistories(cusID);

                // 2. 모든 PayId를 순회하며 각각의 PaymentItem들을 하나의 리스트로 합침
                var allPurchasedItems = new List<PaymentItem>();
                foreach (var h in histories)
                {
                    var items = await api.GetPaymentItems(h.PayId);
                    if (items != null)
                    {
                        allPurchasedItems.AddRange(items);
                    }
                }

                if (allPurchasedItems.Count == 0)
                {
                    txtTendencyReport.Text = "아직 구매하신 도서 내역이 없어 성향 분석을 진행할 수 없습니다.";
                    btnAnalyzeTendency.IsEnabled = true;
                    return;
                }
                 string aiReport = await api.GetCustomerSummary(cusID);
                txtTendencyReport.Text = aiReport;
            });
            btnAnalyzeTendency.IsEnabled = true;
        }
        private async void btnSendChat_Click(object sender, RoutedEventArgs e)
        {
            await SendChatMessageAsync();
        }
        private async void txtChatInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await SendChatMessageAsync();
            }
        }
        private async Task SendChatMessageAsync()
        {
            string userMsg = txtChatInput.Text.Trim();
            if (string.IsNullOrEmpty(userMsg)) return;

            txtChatInput.Clear();
            // 내 질문 말풍선 추가 (어두운 네이비 스타일)
            chatMessages.Add(new ChatBubbleItem($"나 ({cusID})", userMsg, true));

            await RunSafe(async () =>
            {
                btnSendChat.IsEnabled = false;

                string aiReply = await api.SendAiChat(cusID, userMsg);

                // AI 답변 말풍선 추가 (화이트 카드 스타일)
                chatMessages.Add(new ChatBubbleItem("수석 사서 AI", aiReply, false));

                // 스크롤을 맨 아래로 이동
                if (lbAiChat.Items.Count > 0)
                {
                    lbAiChat.ScrollIntoView(lbAiChat.Items[lbAiChat.Items.Count - 1]);
                }
            });
            btnSendChat.IsEnabled = true;
        }
    }
}
