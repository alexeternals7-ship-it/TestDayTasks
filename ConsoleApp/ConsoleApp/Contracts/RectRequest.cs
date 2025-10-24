﻿using MemoryPack;

namespace ConsoleApp.Contracts;

[MemoryPackable]
public partial class RectRequest
{
    public int X1 { get; set; }
    public int Y1 { get; set; }
    public int X2 { get; set; }
    public int Y2 { get; set; }
}