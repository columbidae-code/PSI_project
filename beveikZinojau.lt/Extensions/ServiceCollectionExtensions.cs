using beveikZinojau.lt.Scoring;

namespace beveikZinojau.lt.Extensions
{

    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApplicationServices(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddScoped<StatsTracker>();

            var scoringConfig = configuration
                .GetSection("Scoring")
                .Get<ScoringConfig>() ?? new ScoringConfig();

            services.AddSingleton(scoringConfig);
            services.AddSingleton<ScoringCalculator>();

            services.AddSingleton<GameRoomManager>();

            return services;
        }
    }
}