using System;
using System.Diagnostics;
using Pulsar4X.Client;

#if TRACE
Trace.Listeners.Add(new ConsoleTraceListener());
#endif
// dotnet core doesn't have Debug.Listeners for some reason...
// https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.debug?view=net-10.0
// https://github.com/dotnet/dotnet-api-docs/issues/4866
// Run the game
using (var pulsar = new PulsarMainWindow(args))
{
    
    pulsar.Run();
    /*
    try
    {
        pulsar.Run();

    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        //todo create a crashsave here.
        //allow the player to try recover from a crash from the save
        //don't overwrite normal saves incase it's not recoverable
        //add extra logs or other data for debugging (such as the error caught above).
        throw;
    }*/

}
