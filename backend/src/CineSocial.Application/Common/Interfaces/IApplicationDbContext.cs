using CineSocial.Domain.Entities.Movie;
using CineSocial.Domain.Entities.Social;
using CineSocial.Domain.Entities.User;
using Microsoft.EntityFrameworkCore;

namespace CineSocial.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Genre> Genres { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
