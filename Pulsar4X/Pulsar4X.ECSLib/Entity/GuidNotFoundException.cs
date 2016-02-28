using System;

namespace Pulsar4X.ECSLib
{
    [PublicAPI]
    public class GuidNotFoundException : Exception
    {
        [PublicAPI]
        public Guid MissingGuid { get; private set; }

        [PublicAPI]
        public GuidNotFoundException(Guid missingGuid)
        {
            MissingGuid = missingGuid;
        }
    }
}