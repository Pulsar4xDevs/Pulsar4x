using System;
using System.Diagnostics;
using Pulsar4X.Client;
using Pulsar4X.Client.Host;

#if TRACE
Trace.Listeners.Add(new ConsoleTraceListener());
#endif
// dotnet core doesn't have Debug.Listeners for some reason...
// https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.debug?view=net-10.0
// https://github.com/dotnet/dotnet-api-docs/issues/4866
// Run the game
using (var pulsar = new PulsarMainWindow(args))
{
    pulsar.State.Lifecycle = new GameLifecycle(pulsar.State);
    DevToolWindows.Register(pulsar.State);
    pulsar.Run();
}
