using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using ss1.Models;

namespace ss1.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Photo> Photos { get; set; }

        public DbSet<AppUser> Users { get; set; }

        public DbSet<PhotoSubmission> PhotoSubmissions { get; set; }


    }
}
