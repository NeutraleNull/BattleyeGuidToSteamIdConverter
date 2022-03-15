using NeutraleNull.GuidToSteamIdApi.Database;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace NeutraleNull.GuidToSteamIdApi.UseCases
{
    interface ISeedDatabaseUseCase : IUseCaseAsync
    {
    }

    public class SeedDatabaseUseCase : ISeedDatabaseUseCase
    {
        public SeedDatabaseUseCase(ApplicationDbContext database)
        {
            _database = database;
		}

        public readonly ApplicationDbContext _database;
		private readonly List<BattleyeGuidSteamIdTuple> _tempStorage = new List<BattleyeGuidSteamIdTuple>();
		private readonly object _lock = new object();
		private readonly object _dbLock = new object();
		private int completedItems = 0;

        public async Task HandleAync()
        {
			if (_database.BattleyeGuidSteamIdLookupTable.Count(x => x.SteamId64 > 0) == 0)
			{
				await Parallel.ForEachAsync(GenerateSteamIds(), new ParallelOptions { MaxDegreeOfParallelism = 16 }, HandleForeach);
			}
        }

		private async ValueTask HandleForeach(long steamId, CancellationToken cts)
        {
			var completedCache = Interlocked.Increment(ref completedItems);

			if (completedCache % 1000000 == 0)
            {
                Console.WriteLine($"{completedCache / 1000000}mio steamids processed. {(completedCache / 999999999) * 100}% completed");
				WriteCacheToDb();
            }

			var res = CalculateGuid(steamId);
			lock (_lock)
            {
				_tempStorage.Add(new BattleyeGuidSteamIdTuple { Guid = res, SteamId64 = steamId });
            }
        }

		private void WriteCacheToDb()
        {
			List<BattleyeGuidSteamIdTuple> temp;
			lock (_lock)
            {
				temp = new List<BattleyeGuidSteamIdTuple>(_tempStorage);
				_tempStorage.Clear();

				try
				{
					var stopWatch = new Stopwatch();
					lock (_dbLock)
					{

						stopWatch.Start();
						_database.ChangeTracker.AutoDetectChangesEnabled = false;
						_database.ChangeTracker.QueryTrackingBehavior = Microsoft.EntityFrameworkCore.QueryTrackingBehavior.NoTracking;
						_database.
						_database.BattleyeGuidSteamIdLookupTable.AddRangeAsync(temp);
						_database.SaveChanges();
						temp.Clear();
						GC.Collect();
					}
					stopWatch.Stop();
					TimeSpan ts = stopWatch.Elapsed;
					Console.WriteLine($"Saving into database took: {string.Format("{0:00}:{1:00}:{2:00}.{3:00}", ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10)}");

				}
				catch (Exception ex)
				{
					Console.WriteLine($"Failed to insert blob: {ex.Message}");
				}
				finally
				{
					_tempStorage.Clear();
				}
			}		
        }

		private string CalculateGuid(long steamId)
		{
			byte[] parts = { 0x42, 0x45, 0, 0, 0, 0, 0, 0, 0, 0 };
			byte counter = 2;

			do
			{
				parts[counter++] = (byte)(steamId & 0xFF);
			} while ((steamId >>= 8) > 0);

			using var md5 = MD5.Create();
			byte[] beHash = md5.ComputeHash(parts);

			var sb = new StringBuilder(32);
			for (int i = 0; i < beHash.Length; i++)
			{
				sb.Append(beHash[i].ToString("x2"));
			}
			return sb.ToString();
		}

		private IEnumerable<long> GenerateSteamIds()
		{
			long lower = 76561198000000000;
			long upper = 76561198999999999;

			while (lower < upper)
				yield return lower++;
		}
	}
}