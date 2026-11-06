using BookShopping.Client.Models;
using BookShopping.Client.Services;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace BookShopping.Client.Views
{
    /// <summary>
    /// AdminWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AdminWindow : Window
    {
        private readonly ApiService api = new();
        public AdminWindow()
        {
            InitializeComponent();
            Loaded += AdminWindow_Loaded;
        }

        private async void AdminWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await RunSafe(async () =>
            {
                await LoadCustomers();
                await LoadAdminBooks();
            });
        }
        private async void menuBookTab_Click(object sender, RoutedEventArgs e)
        {
            mainTabControl.SelectedIndex = 1; // 탭 이동
            await LoadAdminBooks();           // 목록 새로고침
        }
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

        private async Task LoadCustomers()
        {
            var list = await api.GetCustomers();

            CustomersView.ItemsSource = list;
        }

        private void CustomersView_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(CustomersView.SelectedItem is Customer selectedCustomer)
            {
                txtMemberId.Text = selectedCustomer.cusID;
                txtMemberName.Text = selectedCustomer.cusName;
            }
        }

        private async void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            await RunSafe(async () =>
            {
                if (txtMemberId.Text == null)
                {
                    MessageBox.Show("아이디를 입력해주세요.");
                    return;
                }
                    
                var list = await api.GetPaymentHistories(txtMemberId.Text);
                MessageBox.Show("주문 내역 조회 완료");
                lvOrderHistory.ItemsSource = list;
            });
        }

        private async void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (txtMemberId.Text == null)
            {
                MessageBox.Show("아이디를 입력해주세요.");
                return;
            }
            if (MessageBox.Show("삭제하시겠습니까?", "회원 삭제", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;
            await RunSafe(async () =>
            {
                if (txtMemberId.Text.Equals("admin", StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("관리자는 삭제 불가합니다.");
                    return;
                }
                await api.DeleteCustomer(txtMemberId.Text);
                MessageBox.Show("삭제 완료");
                await LoadCustomers();
            });
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                txtMemberId.Text = "";
                txtMemberName.Text = "";
                lvOrderHistory.ItemsSource = null;
                lvPaymentDetails.ItemsSource = null;
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void lvOrderHistory_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (lvOrderHistory.SelectedItem is paymentHistory selectedOrder)
            {
                await RunSafe(async () =>
                {
                    var items = await api.GetPaymentItems(selectedOrder.PayId);
                    if (items == null)  
                    {
                        MessageBox.Show("상세 주문 내역이 없습니다.");
                        return;
                    }
                    MessageBox.Show("상세 주문 내역을 불러왔습니다.");
                    lvPaymentDetails.ItemsSource = items;
                });
            }
        }

        private void menuLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("로그아웃 하시겠습니까?", "로그아웃", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void menuExit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        

        private void menuCustomerTab_Click(object sender, RoutedEventArgs e)
        {
            mainTabControl.SelectedIndex = 0; // "고객 관리" 탭으로 전환
        }

        // 도서 목록 로드
        private async Task LoadAdminBooks()
        {
            try
            {
                var books = await api.GetBooks();
                lvAdminBooks.ItemsSource = books;
            }
            catch (Exception ex)
            {
                MessageBox.Show("도서 목록을 불러오는 중 오류 발생: " + ex.Message);
            }
        }

        // 체크박스 클릭(해제/체크) 시 실시간 서버 업데이트
        private async void chkIsForSale_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox chk && chk.DataContext is Book book)
            {
                bool newStatus = chk.IsChecked ?? false;
                try
                {
                    await api.UpdateBookSaleStatus(book.Isbn, newStatus);
                    book.IsForSale = newStatus; // 모델 값 갱신
                }
                catch (Exception ex)
                {
                    MessageBox.Show("판매 상태 갱신 실패: " + ex.Message);
                    chk.IsChecked = !newStatus; // 실패 시 체크박스 롤백
                }
            }
        }

        private async void mainTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.Source is TabControl)
            {
                // "책 관리" 탭(두 번째 탭, Index 1)으로 전환되었을 때 목록 새로고침
                if (mainTabControl.SelectedIndex == 1)
                {
                    await RunSafe(async () =>
                    {
                        await LoadAdminBooks();
                    });
                }
                // "고객 관리" 탭(첫 번째 탭, Index 0)으로 돌아왔을 때 고객 목록 갱신
                else if (mainTabControl.SelectedIndex == 0)
                {
                    await RunSafe(async () =>
                    {
                        await LoadCustomers();
                    });
                }
            }
        }
    }
}
