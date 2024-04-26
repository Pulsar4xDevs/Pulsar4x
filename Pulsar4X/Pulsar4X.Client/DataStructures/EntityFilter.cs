using System;

namespace Pulsar4X.SDL2UI;

[Flags]
public enum EntityFilter
{
    None = 0,
    Friendly = 1,
    Neutral = 2,
    Hostile = 4
}