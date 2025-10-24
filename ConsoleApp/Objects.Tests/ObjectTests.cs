using Microsoft.Extensions.Options;
using NUnit.Framework;
using Objects.Enums;
using Objects.Models;
using Objects.Options;
using StackExchange.Redis;

namespace Objects.Tests;

public static class ObjectTests
{
    [TestFixture]
    public class RedisObjectsServiceTests
    {
        private const string OneSetupMessage = "_redisObjectsService is null — OneTimeSetUp failed";

        private ConnectionMultiplexer? _conn;
        private RedisObjectsService? _redisObjectsService;
        private string _testPrefix = null!; 
        private string _geoKey = null!;
        private string _hashPrefix = null!;
        
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            var connStr = Environment.GetEnvironmentVariable("REDIS_CONNECTION") ?? "localhost:6379";
            
            _testPrefix = $"test:{Guid.NewGuid():N}:";
            _geoKey = _testPrefix + "objects:geo";
            _hashPrefix = _testPrefix + "objects:";
            
            _conn = await Task.Run(() => ConnectionMultiplexer.Connect(connStr));

            var endpoints = _conn.GetEndPoints();
            if (endpoints == null || endpoints.Length == 0)
                throw new InvalidOperationException("No Redis endpoints available");

            var options = new RedisOptions
            {
                GeoKey = _geoKey,
                HashPrefix = _hashPrefix,
            };
            _redisObjectsService = new RedisObjectsService(_conn, new OptionsWrapper<RedisOptions>(options));
        }

        [SetUp]
        public async Task SetUp()
        {
            await DeleteKeysByPrefixAsync(_testPrefix, DefaultTimeout).ConfigureAwait(false);
        }

        [Test]
        public async Task Create_And_Get_By_Zone()
        {
            Assert.That(_redisObjectsService, Is.Not.Null, OneSetupMessage);

            var objectData = new ObjectData { X = 10.1, Y = 20.2, Type = ObjectType.Base };
            var id = await _redisObjectsService?.CreateObjectAsync(objectData).WaitAsync(DefaultTimeout)!;

            var results = await _redisObjectsService.GetObjectsInZoneAsync(10.1, 20.2, radiusMeters: 1000)
                .WaitAsync(DefaultTimeout);
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Any(r => r.Id == id), Is.True);
        }

        [Test]
        public async Task Delete_Removes_Object()
        {
            Assert.That(_redisObjectsService, Is.Not.Null, OneSetupMessage);

            var o = new ObjectData { X = 50.5, Y = 50.6, Type = ObjectType.Mine };
            using var cts = new CancellationTokenSource(DefaultTimeout);
            var id = await _redisObjectsService!.CreateObjectAsync(o, cts.Token);

            var foundBefore = await _redisObjectsService.GetObjectsInZoneAsync(50.5, 50.6, 1000, ct: cts.Token);
            Assert.That(foundBefore.Any(f => f.Id == id), Is.True, "Object must be present before deletion");

            var deleted = await _redisObjectsService.DeleteObjectAsync(id, cts.Token);
            Assert.That(deleted, Is.True, "DeleteObjectAsync should return true when object removed");

            var foundAfter = await _redisObjectsService.GetObjectsInZoneAsync(50.5, 50.6, 1000, ct: cts.Token);
            Assert.That(foundAfter.Any(f => f.Id == id), Is.False, "Object must not be found after deletion");
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            if (_conn != null)
            {
                await DeleteKeysByPrefixAsync(_testPrefix, DefaultTimeout).ConfigureAwait(false);
                await _conn.DisposeAsync();
                _conn = null;
            }
        }

        private async Task DeleteKeysByPrefixAsync(string prefix, TimeSpan timeout)
        {
            if (_conn == null) return;

            var cts = new CancellationTokenSource(timeout);
            var token = cts.Token;

            var endpoints = _conn.GetEndPoints();
            if (endpoints.Length == 0) return;

            var server = _conn.GetServer(endpoints[0]);

            var db = _conn.GetDatabase();
            await foreach (var key in server.KeysAsync(pattern: prefix + "*").WithCancellation(token))
            {
                if (token.IsCancellationRequested) break;
                try
                {
                    await db.KeyDeleteAsync(key).WaitAsync(timeout, token);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}