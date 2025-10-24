using ConsoleApp;
using ConsoleApp.Controllers;
using MapObjects.Library.Services.MapObjectRepository;
using MapObjects.Library.Transformer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Region.Library;
using StackExchange.Redis;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((_, services) =>
    {
        services.AddMagicOnion();
        
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:6379"));
        services.AddSingleton<ITileToGeoTransformer>(_ => 
            new TileToGeoTransformer(1000, 1000, 0, 0));
        services.AddSingleton<IMapObjectRepository, RedisMapObjectRepository>();
        
        services.AddSingleton<MapObjects.Library.MapObjectService>();
        services.AddSingleton<IMapObjectEventSource, MapObjectServiceAdapter>(sp => 
            new MapObjectServiceAdapter(sp.GetRequiredService<MapObjects.Library.MapObjectService>()));
        
        services.AddSingleton(sp => RegionsLayer.Generate(1000, 1000));
        
        services.AddSingleton<IMapBroadcaster, MapHubBroadcaster>();
        services.AddSingleton<MapController>();
    });

var host = builder.Build();
await host.RunAsync();