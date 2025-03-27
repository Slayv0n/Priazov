using DataBase.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DataBase
{
    public class PriazovContext : DbContext
    {
        //Создание таблиц в бд
        public DbSet<User> Users { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<Industry> Industries { get; set; }
        //
        public PriazovContext(DbContextOptions<PriazovContext> options) : base(options)
        {
            Database.EnsureCreated(); 
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Industry>()
                .Property<uint>("Id")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Industry>()
               .Property<uint>("Id").IsRequired();
            modelBuilder.Entity<Industry>()
                .HasKey(i => i.Id);

            modelBuilder.Entity<Region>()
                .Property<uint>("Id")
                .ValueGeneratedOnAdd();
            modelBuilder.Entity<Region>()
               .Property<uint>("Id").IsRequired();
            modelBuilder.Entity<Region>()
                .HasKey(i => i.Id);

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

            modelBuilder.Entity<UserProject>()
               .Property<Guid>("Id")
               .ValueGeneratedOnAdd();
            modelBuilder.Entity<UserProject>()
              .Property<Guid>("Id").IsRequired();
            modelBuilder.Entity<UserProject>()
                .HasKey(i => i.Id);

            modelBuilder.Entity<User>().HasAlternateKey(u => u.Email);
            modelBuilder.Entity<User>().HasAlternateKey(u => u.Phone);
            modelBuilder.Entity<Industry>().HasData(
                new Industry() { Name = "Образовательное учреждение", Id = 1 },
                new Industry() { Name = "Научно-исследовательский институт", Id = 2 },
                new Industry() { Name = "Научно-образовательный проект", Id = 3 },
                new Industry() { Name = "Государственное учреждение", Id = 4 },
                new Industry() { Name = "Компания, ведущая коммерческую деятельность", Id = 5 },
                new Industry() { Name = "Стартап", Id = 6 },
                new Industry() { Name = "Финансовый инструмент (банк, фонд и другие)", Id = 7 },
                new Industry() { Name = "Акселератор/инкубатор/технопарк", Id = 8 },
                new Industry() { Name = "Ассоциация/объединение", Id = 9 },
                new Industry() { Name = "Инициатива", Id = 10 },
                new Industry() { Name = "Отраслевое событие/научная конференция", Id = 11 },
                new Industry() { Name = "Другое", Id = 12 }
                );
            modelBuilder.Entity<Region>().HasData(
                new Region() { Name = "Краснодарский край", Id = 1 },
                new Region() { Name = "Ростовская область", Id = 2 },
                new Region() { Name = "ЛНР", Id = 3 },
                new Region() { Name = "ДНР", Id = 4 },
                new Region() { Name = "Херсонская область", Id = 5 },
                new Region() { Name = "Запорожская область", Id = 6 }
                );
        }
    }
}