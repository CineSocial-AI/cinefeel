namespace CineSocial.Infrastructure.Caching;

public static class FusionCacheConfiguration
{
    /// <summary>
    /// Cache durations for different entity types
    /// </summary>
    public static class CacheDurations
    {
        /// <summary>
        /// Reference data like Genres, Countries, Languages (rarely changes)
        /// </summary>
        public static readonly TimeSpan ReferenceData = TimeSpan.FromHours(24);

        /// <summary>
        /// Movies and People data from TMDB (relatively static)
        /// </summary>
        public static readonly TimeSpan MoviesAndPeople = TimeSpan.FromHours(1);

        /// <summary>
        /// User profiles and lists (moderately dynamic)
        /// </summary>
        public static readonly TimeSpan UserData = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Comments and Rates (more dynamic, needs fresher data)
        /// </summary>
        public static readonly TimeSpan CommentsAndRates = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Search results (temporary cache)
        /// </summary>
        public static readonly TimeSpan SearchResults = TimeSpan.FromMinutes(15);

        /// <summary>
        /// Movie statistics (ratings summary, counts)
        /// </summary>
        public static readonly TimeSpan Statistics = TimeSpan.FromMinutes(10);
    }

    /// <summary>
    /// Cache key prefixes for different entity types
    /// </summary>
    public static class CacheKeys
    {
        // Movies
        public const string MovieDetail = "movie:detail:";
        public const string MovieList = "movie:list";
        public const string MovieSearch = "movie:search:";
        public const string MovieCast = "movie:cast:";
        public const string MovieCrew = "movie:crew:";

        // People
        public const string PersonDetail = "person:detail:";
        public const string PersonCredits = "person:credits:";

        // Reference Data
        public const string Genres = "ref:genres";
        public const string Countries = "ref:countries";
        public const string Languages = "ref:languages";

        // User Data
        public const string UserProfile = "user:profile:";
        public const string UserLists = "user:lists:";
        public const string UserFavorites = "user:favorites:";

        // Social Data
        public const string Comments = "comments:movie:";
        public const string Rates = "rates:movie:";
        public const string RatesSummary = "rates:summary:";

        // Search
        public const string SearchResults = "search:";
    }

    /// <summary>
    /// Cache tags for invalidation
    /// </summary>
    public static class CacheTags
    {
        public static string Movie(Guid movieId) => $"movie:{movieId}";
        public static string Person(Guid personId) => $"person:{personId}";
        public static string User(Guid userId) => $"user:{userId}";
        public static string MovieComments(Guid movieId) => $"comments:movie:{movieId}";
        public static string MovieRates(Guid movieId) => $"rates:movie:{movieId}";
        public static string UserLists(Guid userId) => $"lists:user:{userId}";

        public const string AllMovies = "tag:movies";
        public const string AllPeople = "tag:people";
        public const string AllUsers = "tag:users";
        public const string ReferenceData = "tag:reference";
    }
}
