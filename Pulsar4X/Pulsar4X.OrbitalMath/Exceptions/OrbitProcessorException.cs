using System;
using System.Collections.Generic;
using System.Text;

namespace Pulsar4X.Orbital
{
    public class OrbitProcessorException : Exception
    {
        public override string Message { get; }
        public KeplerElements Entity { get; }

        public OrbitProcessorException(string message, KeplerElements entity)
        {
            Message = message;
            Entity = entity;
        }
    }
}
