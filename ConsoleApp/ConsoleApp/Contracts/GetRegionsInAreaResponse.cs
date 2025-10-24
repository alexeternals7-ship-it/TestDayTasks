using MemoryPack;

namespace ConsoleApp.Contracts;

[MemoryPackable]
public partial class GetRegionsInAreaResponse
{
    public RegionDto[] Regions { get; set; } = [];
}