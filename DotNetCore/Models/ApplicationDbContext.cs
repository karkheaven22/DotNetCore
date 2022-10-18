using DotNetCore.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace DotNetCore
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
    {
        public ApplicationDbContext()
        { }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<ApplicationUser>().Ignore(p => p.NormalizedUserName);
            //modelBuilder.Entity<ApplicationUser>().Ignore(p => p.NormalizedEmail);
            modelBuilder.ApplyConfiguration(new SeriesLineEntityConfiguration());

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUser>().ToTable("tbl_Users");
            modelBuilder.Entity<IdentityUserClaim<string>>().ToTable("tbl_UserClaims");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("tbl_UserLogins");
            modelBuilder.Entity<IdentityUserLogin<string>>().ToTable("tbl_UserLogins");
            modelBuilder.Entity<IdentityUserToken<string>>().ToTable("tbl_UserTokens");
            modelBuilder.Entity<ApplicationRole>().ToTable("tbl_Roles");
            modelBuilder.Entity<IdentityRoleClaim<string>>().ToTable("tbl_RoleCliams");
            modelBuilder.Entity<IdentityUserRole<string>>().ToTable("tbl_UserRoles");
        }

        public DbSet<ModelSeriesLine> SeriesLine { get; set; }
        public DbSet<Transaction> Transaction { get; set; }
    }

    public static class ContextSeed
    {
        public static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            var RoleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
            var UserManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            if (!await RoleManager.RoleExistsAsync("Administrator"))
                await RoleManager.CreateAsync(new ApplicationRole("Administrator"));

            if (!await RoleManager.RoleExistsAsync("Member"))
                await RoleManager.CreateAsync(new ApplicationRole("Member"));

            var User = new ApplicationUser
            {
                FirstName = "Chong",
                LastName = "Micheal",
                Email = "karkheaven@hotmail.com",
                UserName = "Admin",
                Password = "999999",
                PhoneNumber = "60197780232"
            };

            var CreateUser = await UserManager.CreateAsync(User, User.Password);
            if (CreateUser.Succeeded)
            {
                await UserManager.AddToRoleAsync(User, "Administrator");
                await UserManager.SetLockoutEnabledAsync(User, false);
            }
        }
    }
}