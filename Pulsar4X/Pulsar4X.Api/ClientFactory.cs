using Pulsar4X.Api.Clients;

namespace Pulsar4X.Api;

/// <summary>
/// A factory class for creating game clients that facilitate communication with the game servers.
/// </summary>
public static class ClientFactory
{
    /// <summary>
    /// Creates a local client that connects to the given server instance.
    /// 
    /// This is beneficial for single-player games, where the client and server are in the same process.
    /// </summary>
    /// <param name="server">The server instance to connect to.</param>
    /// <returns>A game client that communicates with the given server instance.</returns>
    public static IGameClient CreateLocalClient(IGameServer server)
    {
        return new InProcessAdapter(server);
    }
}
