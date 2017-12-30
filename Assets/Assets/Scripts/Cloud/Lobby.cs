using System.Threading.Tasks;

class Lobby
{
    public string Id { get; }

    public CloudNode State { get; private set; }
    public CloudNode[] Users { get; private set; }

    public Lobby(string id)
    {
        Id = id;
        State = new CloudNode($"lobbies/{id}/state");
        Users = new CloudNode[4] {
            new CloudNode($"lobbies/{id}/users/0"),
            new CloudNode($"lobbies/{id}/users/1"),
            new CloudNode($"lobbies/{id}/users/2"),
            new CloudNode($"lobbies/{id}/users/3")
        };
    }

    public static async Task<Lobby> Fetch(string id) => new Lobby(id) {
        State = await CloudNode.Fetch($"lobbies/{id}/state"),
        Users = new CloudNode[4] {
            await CloudNode.Fetch($"lobbies/{id}/users/0"),
            await CloudNode.Fetch($"lobbies/{id}/users/1"),
            await CloudNode.Fetch($"lobbies/{id}/users/2"),
            await CloudNode.Fetch($"lobbies/{id}/users/3")
        }
    };
}
