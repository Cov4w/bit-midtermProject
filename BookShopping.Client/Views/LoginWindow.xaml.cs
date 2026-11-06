
using BookShopping.Client.Models;
using BookShopping.Client.Services;
using System.Net.Http;
using System.Windows;
using System.Windows.Input;

namespace BookShopping.Client.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ApiService api = new();
        public LoginWindow()
        {
            InitializeComponent();
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
        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string userId = txtId.Text.Trim();
            string userPw = txtPassword.Password.Trim();
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userPw))
            {
                MessageBox.Show("아이디를 입력하세요.");
                return;
            }
            await loginProgress(userId, userPw);
        }
        private async void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            await RunSafe(async () =>
            {
                Customer customer = new Customer
                {
                    cusID = txtId.Text.Trim(),
                    cusName = txtName.Text.Trim(),
                    cusPW = txtPassword.Password.Trim()
                };
                await api.Signup(customer);

                MessageBox.Show("회원가입 완료");
                txtId.Clear();
                txtName.Clear();
                txtPassword.Clear();

                txtId.Focus();
            });            
        }

        private async void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            string userId = txtId.Text.Trim();
            string userPw = txtPassword.Password.Trim();
            
            if (e.Key == Key.Enter)
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(userPw))
                {
                    MessageBox.Show("아이디를 입력하세요.");
                    return;
                }
                await loginProgress(userId, userPw);
            }
        }
        private async Task loginProgress(string userId, string userPw)
        {
            if (userId.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                await RunSafe(async () =>
                {
                    var loginUser = await api.Login(userId, userPw);

                    AdminWindow adminWindow = new AdminWindow();
                    adminWindow.Show();
                    this.Close();
                });
            }
            else
            {
                await RunSafe(async () =>
                {
                    var loginUser = await api.Login(userId, userPw);
                    CustomerWindow customerWindow = new CustomerWindow(userId);
                    customerWindow.Show();
                    this.Close();
                });
            }
        }
    }
}
