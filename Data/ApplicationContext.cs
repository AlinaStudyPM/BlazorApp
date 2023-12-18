using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
//using Pomelo.EntityFrameworkCore.MySql;
using Microsoft.EntityFrameworkCore.Sqlite;

namespace MovieBlazor2.Data
{
    public class ApplicationContext : DbContext
    {
        public DbSet<Movie> Movies { get; set; } = null!;
        public DbSet<Tag> Tags { get; set; } = null!;
        public DbSet<Actor> Actors { get; set; } = null!;

        public ApplicationContext(bool ToDelete)
        {
            Movies = Set<Movie>();
            Tags = Set<Tag>();
            Actors = Set<Actor>();
            if (ToDelete)
            {
                Database.EnsureDeleted();
            }
            Database.EnsureCreated();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Actor>().HasKey(a => a.Code);
            modelBuilder.Entity<Tag>().HasKey(t => t.Code);
            modelBuilder.Entity<Movie>().HasKey(m => m.Code);

            modelBuilder.Entity<Movie>().HasMany(ma => ma.Actors).WithMany(a => a.Movies).UsingEntity(j => j.ToTable("MovieActor"));
            modelBuilder.Entity<Movie>().HasMany(ma => ma.Director).WithMany(a => a.DirectedMovies).UsingEntity(j => j.ToTable("Directors"));
            //modelBuilder.Entity<Movie>().HasMany(m => m.Director).WithMany(a => a.Movies).UsingEntity(t => ToTable("Directors"));
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)                                                                       
        {
            optionsBuilder.UseSqlite("Data Source=DataBase1.db");
        }
    }
}
