using BookShopping.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace BookShopping.Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options): base(options)
        { 

        }
        public DbSet<Book> Books => Set<Book>();
        public DbSet<BookCategory> BookCategories => Set<BookCategory>();
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<PaymentHistory> PaymentHistories => Set<PaymentHistory>();
        public DbSet<PaymentItem> PaymentItems => Set<PaymentItem>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. 책(Book)과 카테고리(BookCategory) 관계 설정 (N:1)
            modelBuilder.Entity<Book>()
                .HasOne(b => b.BookCategoryEntity)     // 책 입장에서 카테고리는 1개
                .WithMany(c => c.Books)                // 카테고리 입장에서 책은 여러 개
                .HasForeignKey(b => b.Category)        // 외래키 컬럼명: b.Category
                .OnDelete(DeleteBehavior.Restrict);    // 카테고리가 삭제될 때 연결된 책이 있으면 삭제 방지 (RESTRICT)

            // 2. 결제 기록(PaymentHistory)과 회원(Customer) 관계 설정 (N:1)
            modelBuilder.Entity<PaymentHistory>()
                 .HasOne(p => p.Customer)
                 .WithMany(c => c.PaymentHistories) // 만약 Customer 모델에 이 컬렉션이 있다면 연결, 없다면 .WithMany()
                 .HasForeignKey(p => p.CusId)      
                 .OnDelete(DeleteBehavior.Cascade);     // 회원이 탈퇴하면 결제 기록도 연쇄 삭제 (CASCADE)

            // 3. 상세 결제 아이템(PaymentItem)과 결제 기록(PaymentHistory) 관계 설정 (N:1)
            modelBuilder.Entity<PaymentItem>()
                .HasOne(pi => pi.PaymentHistory)
                .WithMany(ph => ph.PaymentItems)
                .HasForeignKey(pi => pi.PayID)
                .OnDelete(DeleteBehavior.Cascade);     // 결제 기록이 삭제되면 상세 아이템도 연쇄 삭제 (CASCADE)

            // 4. 상세 결제 아이템(PaymentItem)과 책(Book) 관계 설정 (N:1)
            modelBuilder.Entity<PaymentItem>()
                .HasOne(pi => pi.Book)
                .WithMany(b => b.PaymentItems)
                .HasForeignKey(pi => pi.Isbn)
                .OnDelete(DeleteBehavior.Cascade);     // 책 정보가 삭제되면 상세 아이템도 연쇄 삭제 (CASCADE)
        }
    }
}
