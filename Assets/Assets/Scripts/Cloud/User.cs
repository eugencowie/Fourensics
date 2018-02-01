using System.Linq;
using System.Threading.Tasks;

class User
{
    public string Id { get; }

    public CloudNode Name { get; private set; }
    public CloudNode Lobby { get; private set; }
    public CloudNode<long> Scene { get; private set; }
    public CloudNode<bool> Ready { get; private set; }
    public CloudNode Vote { get; private set; }
    public Item[] Items { get; private set; }

    User(string id)
    {
        Id = id;
    }

    public static User Create(string id) => new User(id) {
        Name = CloudNode.Create($"users/{id}/name"),
        Lobby = CloudNode.Create($"users/{id}/lobby"),
        Scene = CloudNode<long>.Create($"users/{id}/scene"),
        Ready = CloudNode<bool>.Create($"users/{id}/ready"),
        Vote = CloudNode.Create($"users/{id}/vote"),
        Items = "012345".Select(n => Item.Create(id, n.ToString())).ToArray()
    };

    public static async Task<User> Fetch(string id) => new User(id) {
        Name = await CloudNode.Fetch($"users/{id}/name"),
        Lobby = await CloudNode.Fetch($"users/{id}/lobby"),
        Scene = await CloudNode<long>.Fetch($"users/{id}/scene"),
        Ready = await CloudNode<bool>.Fetch($"users/{id}/ready"),
        Vote = await CloudNode.Fetch($"users/{id}/vote"),
        Items = await Task.WhenAll("012345".Select(n => Item.Fetch(id, n.ToString())))
    };
}
