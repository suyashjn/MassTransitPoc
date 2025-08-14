using MassTransitPoc.Persistance.Entities;
using Microsoft.EntityFrameworkCore;

namespace MassTransitPoc.Persistance
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        public DbSet<FaultMessage> FaultMessages { get; set; }
    }
}
