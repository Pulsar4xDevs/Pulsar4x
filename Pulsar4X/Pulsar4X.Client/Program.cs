using System;
using Pulsar4X.Client;

// Run the game
using (var pulsar = new PulsarMainWindow(args))
{
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
    }

}
