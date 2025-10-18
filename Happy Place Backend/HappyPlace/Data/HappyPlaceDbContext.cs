using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HappyWorld.HappyPlace.Data;

public class HappyPlaceDbContext: DbContext
{
    // Constructors
    private HappyPlaceDbContext(DbContextOptions<HappyPlaceDbContext> options) : base(options)
    {
    }

    // Properties
    public DbSet<PendingUserAccount> PendingUserAccounts { get; set; }

    // Methods
    public static HappyPlaceDbContext Create()
    {
        var optionsBuilder = new DbContextOptionsBuilder<HappyPlaceDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=HappyPlace;Trusted_Connection=True;MultipleActiveResultSets=true;trustservercertificate=yes");
        return new HappyPlaceDbContext(optionsBuilder.Options);
    }
}
