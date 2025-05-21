using DataBase.Models;
using Microsoft.EntityFrameworkCore;

namespace DataBase
{
    public class PriazovContext : DbContext
    {

        public PriazovContext(DbContextOptions<PriazovContext> options) : base(options)
        {

        }
        //Создание таблиц в бд
        public DbSet<User> Users { get; set; }
        public DbSet<ShortAddressDto> Addresses { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<UserSession> Sessions { get; set; }
        public DbSet<UserPassword> Password { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            //Настройки таблиц

            modelBuilder.Entity<User>()
                .Property<Guid>("Id")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<User>()
               .Property<Guid>("Id").IsRequired();
            modelBuilder.Entity<User>()
                .HasKey(i => i.Id);

            modelBuilder.Entity<Project>()
                .Property<Guid>("Id")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Project>()
               .Property<Guid>("Id").IsRequired();
            modelBuilder.Entity<Project>()
            .HasKey(i => i.Id);

            modelBuilder.Entity<Company>()
                .HasMany(e => e.Projects)
                .WithOne(e => e.Company)
                .HasForeignKey(e => e.CompanyId)
                .IsRequired();

            modelBuilder.Entity<UserSession>()
                .Property<Guid>("Id")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<UserSession>()
              .Property<Guid>("Id").IsRequired();
            modelBuilder.Entity<UserSession>()
                .HasKey(i => i.Id);

            modelBuilder.Entity<UserPassword>()
                .Property<Guid>("Id")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<UserPassword>()
              .Property<Guid>("Id").IsRequired();
            modelBuilder.Entity<UserPassword>()
                .HasKey(i => i.Id);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Session)       // У User есть одна Session
                .WithOne(s => s.User)         // У Session есть один User
                .HasForeignKey<UserSession>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Password)
                .WithOne(p => p.User)
                .HasForeignKey<UserPassword>(p => p.UserId);

            modelBuilder.Entity<User>()
               .HasOne(u => u.PasswordResetToken)
               .WithOne(p => p.User)
               .HasForeignKey<PasswordResetToken>(p => p.UserId);

            //Временная мера, пока адрес одной строкой
            modelBuilder.Entity<User>()
                .HasOne(u => u.Address)
                .WithOne(a => a.User)
                .HasForeignKey<ShortAddressDto>(a => a.UserId);

            modelBuilder.Entity<User>()
                .HasDiscriminator<string>("Role")
                .HasValue<Manager>("Manager")
                .HasValue<Company>("Company")
                .HasValue<Admin>("Admin");
        }
    }
}