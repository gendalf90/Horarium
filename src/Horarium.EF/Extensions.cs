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

    public static IServiceProvider CreateHorariumDatabase(
        this IServiceProvider provider, 
        Action<DbContextOptionsBuilder> configuration,
        string scheme = "public")
    {
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        if (string.IsNullOrWhiteSpace(scheme))
        {
            throw new ArgumentNullException(nameof(scheme), "Scheme is empty");
        }
        
        using var context = new HorariumContext(configuration, scheme);

        context.Database.EnsureCreated();

        return provider;
    }

    private class HorariumContext : DbContext
    {
        private readonly Action<DbContextOptionsBuilder> _configuration;
        private readonly string _scheme;

        public HorariumContext(Action<DbContextOptionsBuilder> configuration, string scheme)
        {
            _configuration = configuration;
            _scheme = scheme;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _configuration(optionsBuilder);
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseHorarium().HasDefaultSchema(_scheme);
        }
    }
}
