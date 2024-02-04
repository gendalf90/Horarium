using System;
using Microsoft.EntityFrameworkCore;

namespace Horarium.EF;

public static class Extensions
{
    public static ModelBuilder UseHorarium(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobModel>().ToTable("horarium.jobs");
        
        return modelBuilder;
    }

    public static IServiceProvider CreateHorariumDatabase(this IServiceProvider provider, Action<DbContextOptionsBuilder> configuration)
    {
        using var context = new HorariumContext(configuration);

        context.Database.EnsureCreated();

        return provider;
    }

    private class HorariumContext : DbContext
    {
        private readonly Action<DbContextOptionsBuilder> _configuration;

        public HorariumContext(Action<DbContextOptionsBuilder> configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _configuration(optionsBuilder);
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseHorarium();
        }
    }
}
