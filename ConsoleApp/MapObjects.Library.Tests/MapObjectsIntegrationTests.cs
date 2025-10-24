using System.Text.Json;
using MapObjects.Library.Events;
using MapObjects.Library.Models;
using MapObjects.Library.Services.MapObjectRepository;
using MapObjects.Library.Transformer;
using NUnit.Framework;
using NUnit.Framework.Legacy;
using StackExchange.Redis;

namespace MapObjects.Library.Tests;

public static class MapObjectsIntegrationTests
{
    [TestFixture]
    public class MapObjectRepositoryTests
    {
        private IConnectionMultiplexer _mux = null!;
        private RedisMapObjectRepository _repo = null!;
        private MapObjectService _service = null!;
        private TileToGeoTransformer _transform = null!;
        private const string MapId = "testmap_nunit";
        private readonly List<string> _createdIds = new();

        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Подключение к Redis на localhost:6379
            _mux = ConnectionMultiplexer.Connect("localhost:6379");

            // Параметры трансформации: карта 100x100 тайлов, произвольная область lon/lat
            _transform = new TileToGeoTransformer(1000, 1000, lon0: 30.0, lat0: 50.0, lonSpan: 0.2, latSpan: 0.2);

            _repo = new RedisMapObjectRepository(_mux, _transform);
            _service = new MapObjectService(_repo, _mux);

            // Очистим ключи тестовой карты перед началом
            try
            {
                var server = _mux.GetServer(_mux.GetEndPoints().First());
                server.FlushDatabase();
            }
            catch
            {
                // ignored
            }
        }

        [TearDown]
        public async Task TearDown()
        {
            var db = _mux.GetDatabase();
            foreach (var id in _createdIds.ToArray())
            {
                var objKey = $"obj:{MapId}:{id}";
                await db.KeyDeleteAsync(objKey);
                await db.SortedSetRemoveAsync($"geo:map:{MapId}:objects", id);
                _createdIds.Remove(id);
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _mux.Dispose();
        }

        private static MapObject Make(string id, int x, int y, int width = 1, int height = 1, string type = "generic")
        {
            return new MapObject
            {
                Id = id,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Type = type,
                MetadataJson = "{}",
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }

        private Task RegisterCreatedAsync(string id)
        {
            _createdIds.Add(id);
            return Task.CompletedTask;
        }

        [Test]
        public async Task Add_Object_IsStoredAndRetrievableById()
        {
            // Assert
            var obj = Make("obj-add-1", x: 10, y: 15, width: 2, height: 3);
            await _repo.AddOrUpdateAsync(MapId, obj);
            
            // Act
            await RegisterCreatedAsync(obj.Id);

            // Assert
            var fetched = await _repo.GetByIdAsync(MapId, obj.Id);
            Assert.That(fetched, Is.Not.Null, "Объект не найден по ID после добавления.");
            Assert.That(obj.Id, Is.EqualTo(fetched!.Id));
            Assert.That(obj.X, Is.EqualTo(fetched.X));
            Assert.That(obj.Y, Is.EqualTo(fetched.Y));
            Assert.That(obj.Width, Is.EqualTo(fetched.Width));
            Assert.That(obj.Height, Is.EqualTo(fetched.Height));
        }

        [Test]
        public async Task Delete_Object_RemovesFromIdAndGeoIndex()
        {
            // Assert
            var obj = Make("obj-del-1", x: 30, y: 40, width: 2, height: 2);
            
            // Act
            await _repo.AddOrUpdateAsync(MapId, obj);
            
            // Assert
            var fetched = await _repo.GetByIdAsync(MapId, obj.Id);
            Assert.That(fetched, Is.Not.Null);
            
            var deleted = await _repo.DeleteAsync(MapId, obj.Id);
            Assert.That(deleted, Is.True,
                "Redis transaction мог вернуть false — проверим отсутствие по ID и по координатам.");
            
            var after = await _repo.GetByIdAsync(MapId, obj.Id);
            Assert.That(after, Is.Null, "Объект всё ещё доступен по ID после удаления.");
            
            var atTile = await _repo.GetObjectAtTileAsync(MapId, obj.X, obj.Y);
            Assert.That(atTile, Is.Null, "Объект всё ещё доступен по координатам после удаления.");
        }

        [Test]
        public async Task GetObjectAtTile_ReturnsObject_WhenPointInside()
        {
            // Assert
            var obj = Make("obj-hit-1", x: 50, y: 50, width: 3, height: 3);
            await _repo.AddOrUpdateAsync(MapId, obj);
            
            // Act
            await RegisterCreatedAsync(obj.Id);

            // Assert
            // test central point
            var centerX = obj.X + 1;
            var centerY = obj.Y + 1;
            var found = await _repo.GetObjectAtTileAsync(MapId, centerX, centerY);
            Assert.That(found, Is.Not.Null, "Не найден объект по координатам внутри его площади.");
            Assert.That(obj.Id, Is.EqualTo(found!.Id));

            // test border points (left, top, right-1, bottom-1)
            Assert.That(await _repo.GetObjectAtTileAsync(MapId, obj.X, obj.Y), Is.Not.Null);
            Assert.That(await _repo.GetObjectAtTileAsync(MapId, obj.X + obj.Width - 1, obj.Y + obj.Height - 1),
                Is.Not.Null);
        }

        [Test]
        public async Task Subscribe_EventPublished_OnAddUpdateDelete()
        {
            // Assert
            var obj = Make("obj-event-1", x: 70, y: 70, width: 2, height: 2);

            var tcsCreated = new TaskCompletionSource<MapObject?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var tcsDeleted = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

            _service.OnObjectChanged += Handler;
            await _service.SubscribeToRedisEventsAsync(MapId);
            await _repo.AddOrUpdateAsync(MapId, obj);
            
            // Act
            await RegisterCreatedAsync(obj.Id);

            // Assert
            await Task.WhenAny(tcsCreated.Task, Task.Delay(TimeSpan.FromSeconds(20)));
            Assert.That(tcsCreated.Task.IsCompletedSuccessfully, Is.True, "Событие создания не пришло.");
            Assert.That(obj.Id, Is.EqualTo(tcsCreated.Task.Result?.Id));
            
            var deleted = await _repo.DeleteAsync(MapId, obj.Id);
            Assert.That(deleted, Is.True);

            await Task.WhenAny(tcsDeleted.Task, Task.Delay(TimeSpan.FromSeconds(20)));
            Assert.That(tcsDeleted.Task.IsCompletedSuccessfully, Is.True, "Событие удаления не пришло.");
            Assert.That(obj.Id, Is.EqualTo(tcsDeleted.Task.Result));

            _service.OnObjectChanged -= Handler;
            return;

            void Handler(object? s, MapObjectChangedEventArgs? e)
            {
                try
                {
                    if (e == null)
                    {
                        Console.Error.WriteLine("Received null MapObjectChangedEventArgs");
                        return;
                    }

                    // лог — для диагностики
                    Console.WriteLine($"Event received: type={e.EventType}, id={e.Object?.Id}");

                    switch (e.EventType)
                    {
                        case "created_or_updated" when e.Object?.Id == obj.Id:
                            tcsCreated.TrySetResult(e.Object);
                            break;
                        case "deleted" when e.Object?.Id == obj.Id:
                            tcsDeleted.TrySetResult(e.Object.Id);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Handler exception: {ex}");
                    tcsCreated.TrySetException(ex);
                    tcsDeleted.TrySetException(ex);
                }
            }
        }

        [Test]
        public async Task IntersectionChecks_FullPartialNone_WorkCorrectly()
        {
            // Assert
            // Полное включение: область полностью содержит объект
            var a = Make("obj-int-A", x: 10, y: 10, width: 3, height: 3);
            // Частичное перекрытие: объект частично перекрывает прямоугольник
            var b = Make("obj-int-B", x: 20, y: 20, width: 5, height: 5);
            // Нет пересечений
            var c = Make("obj-int-C", x: 90, y: 90, width: 2, height: 2);

            // Act
            await _repo.AddOrUpdateAsync(MapId, a);
            await _repo.AddOrUpdateAsync(MapId, b);
            await _repo.AddOrUpdateAsync(MapId, c);
            await RegisterCreatedAsync(a.Id);
            await RegisterCreatedAsync(b.Id);
            await RegisterCreatedAsync(c.Id);

            // Assert
            // Прямоугольник содержит объект
            var res1 = await _repo.GetObjectsInRectangleAsync(MapId, 9, 9, 10, 10);
            Assert.That(res1.Any(o => o.Id == a.Id), Is.True, "Полное вхождение: A должен быть в результате.");
            // В частично пересечен прямоугольником
            var res2 = await _repo.GetObjectsInRectangleAsync(MapId, 22, 22, 2, 2);
            Assert.That(res2.Any(o => o.Id == b.Id), Is.True, "Частичное пересечение: B должен быть найден.");
            // Вне рамок
            var res3 = await _repo.GetObjectsInRectangleAsync(MapId, 0, 0, 10, 10);
            Assert.That(res3.Any(o => o.Id == c.Id), Is.False, "Нет пересечения: C не должен быть найден.");
        }

        [Test]
        public async Task GetObjectsInRectangle_ReturnsAllObjectsWithinArea()
        {
            // Assert
            const int rectX = 40;
            const int rectY = 40;
            const int rectW = 10;
            const int rectH = 10;
            var ids = new[] { "o-cl-1", "o-cl-2", "o-cl-3" };
            var objs = new[]
            {
                Make(ids[0], 41, 41, 1, 1),
                Make(ids[1], 45, 43, 2, 2),
                Make(ids[2], 48, 49, 1, 1),
            };

            // Act
            foreach (var o in objs)
            {
                await _repo.AddOrUpdateAsync(MapId, o);
                await RegisterCreatedAsync(o.Id);
            }
            
            // Assert
            var found = await _repo.GetObjectsInRectangleAsync(MapId, rectX, rectY, rectW, rectH);
            CollectionAssert.IsSupersetOf(found.Select(x => x.Id).ToList(), ids.ToList(),
                "Запрос области не вернул все объекты, лежащие внутри.");
        }

        [Test]
        public async Task BoundaryCases_ObjectOnBorder_IncludedCorrectly()
        {
            // Assert
            // объект с размерами 1x1 на границе прямоугольной области
            var obj = Make("obj-border-1", x: 0, y: 0, width: 1, height: 1);
            await _repo.AddOrUpdateAsync(MapId, obj);
            
            // Act
            await RegisterCreatedAsync(obj.Id);

            // Assert
            // прямоугольник который точно соответствует плитке (0,0)
            var res = await _repo.GetObjectsInRectangleAsync(MapId, 0, 0, 1, 1);
            Assert.That(res.Any(o => o.Id == obj.Id), Is.True,
                "Объект на границе не был найден при точном совпадении.");
            
            var at = await _repo.GetObjectAtTileAsync(MapId, 0, 0);
            Assert.That(at, Is.Not.Null);
            Assert.That(obj.Id, Is.EqualTo(at!.Id));
        }

        [Test]
        public async Task EdgeCase_LargeArea_PerformanceSmoke()
        {
            // Assert
            // дымовой тест: добавляем несколько объектов и выполним запрос по большой области — проверим корректность и приемлемость времени
            var rnd = new Random(123);
            var ids = new List<string>();
            for (var i = 0; i < 50; i++)
            {
                var id = $"o-bench-{i}-{Guid.NewGuid():N}".Substring(0, 24);
                var ox = rnd.Next(0, 200);
                var oy = rnd.Next(0, 200);
                var o = Make(id, ox, oy, width: rnd.Next(1, 4), height: rnd.Next(1, 4));
                await _repo.AddOrUpdateAsync(MapId, o);
                ids.Add(o.Id);
                await RegisterCreatedAsync(o.Id);
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();
            
            // Act
            var res = await _repo.GetObjectsInRectangleAsync(MapId, 0, 0, 300, 300);
            sw.Stop();
            
            // Assert
            // хочу убедиться, что запрос вернул как минимум добавленные объекты
            Assert.That(res.Count >= 1, Is.True, "Ожидается как минимум один объект в большой области.");
            Assert.That(sw.ElapsedMilliseconds, Is.LessThanOrEqualTo(5000),
                "GetObjectsInRectangleAsync должен отработать быстро (smoke).");
        }
    }
}