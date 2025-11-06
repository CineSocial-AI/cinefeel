using CineSocial.Application.Common.Security;
using CineSocial.Domain.Entities.User;
using CineSocial.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Infrastructure.Data.Seed;

public static class AppUserSeed
{
    public static void SeedUsers(ModelBuilder modelBuilder)
    {
        var seedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<AppUser>().HasData(
            new AppUser
            {
                Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                Username = "user",
                Email = "user@cinesocial.com",
                PasswordHash = PasswordHasher.HashPassword("User123!"),
                Role = UserRole.User,
                Bio = "Regular user account",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = seedDate,
                CreatedBy = null
            },
            new AppUser
            {
                Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Username = "admin",
                Email = "admin@cinesocial.com",
                PasswordHash = PasswordHasher.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                Bio = "Administrator account",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = seedDate,
                CreatedBy = null
            },
            new AppUser
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Username = "superuser",
                Email = "superuser@cinesocial.com",
                PasswordHash = PasswordHasher.HashPassword("SuperUser123!"),
                Role = UserRole.SuperUser,
                Bio = "Super User account with full privileges",
                IsActive = true,
                IsDeleted = false,
                CreatedAt = seedDate,
                CreatedBy = null
            }
        );
    }
}
