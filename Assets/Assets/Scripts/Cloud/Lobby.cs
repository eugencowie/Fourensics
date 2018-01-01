using System.Linq;
using System.Threading.Tasks;

class Lobby
{
    public string Id { get; }

    public CloudNode State { get; private set; }
    public CloudNode[] Users { get; private set; }

    Lobby(string id)
    {
        Id = id;
    }

    public static Lobby Create(string id) => new Lobby(id) {
        State = CloudNode.Create($"lobbies/{id}/state"),
        Users = "0123".Select(n => CloudNode.Create($"lobbies/{id}/users/{n}")).ToArray()
    };

    public static async Task<Lobby> Fetch(string id) => new Lobby(id) {
        State = await CloudNode.Fetch($"lobbies/{id}/state"),
        Users = await Task.WhenAll("0123".Select(n => CloudNode.Fetch($"lobbies/{id}/users/{n}")))
    };
}
