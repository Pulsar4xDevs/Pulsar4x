﻿using Newtonsoft.Json;
using System;

namespace Pulsar4X.ECSLib
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class BaseDataBlob : ICloneable
    {
        [NotNull]
        public virtual Entity OwningEntity { get; internal set; }

        public abstract object Clone();
    }
}
