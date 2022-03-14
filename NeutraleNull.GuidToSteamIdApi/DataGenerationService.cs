using NeutraleNull.GuidToSteamIdApi.UseCases;

namespace NeutraleNull.GuidToSteamIdApi
{
    public class DataGenerationService : IHostedService
    {

        public DataGenerationService(IServiceProvider services)
        {
            _services = services;
        }

        private readonly IServiceProvider _services;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _services.CreateScope();
            var seedDatabaseUseCaseawait = scope.ServiceProvider.GetRequiredService<ISeedDatabaseUseCase>();
            await seedDatabaseUseCaseawait.HandleAync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
