using System.Threading.Tasks;

class User
{
    public string Id { get; }

    public CloudNode Name { get; private set; }
    public CloudNode Lobby { get; private set; }
    public CloudNode Scene { get; private set; }
    public CloudNode Ready { get; private set; }
    public CloudNode Vote { get; private set; }
    public Item[] Items { get; private set; }

    public User(string id)
    {
        Id = id;
        Name = new CloudNode($"users/{id}/name");
        Lobby = new CloudNode($"users/{id}/lobby");
        Scene = new CloudNode($"users/{id}/scene");
        Ready = new CloudNode($"users/{id}/ready");
        Vote = new CloudNode($"users/{id}/vote");
        Items = new Item[6] {
            new Item(id, "0"),
            new Item(id, "1"),
            new Item(id, "2"),
            new Item(id, "3"),
            new Item(id, "4"),
            new Item(id, "5"),
        };
    }

    public static async Task<User> Fetch(string id) => new User(id) {
        Name = await CloudNode.Fetch($"users/{id}/name"),
        Lobby = await CloudNode.Fetch($"users/{id}/lobby"),
        Scene = await CloudNode.Fetch($"users/{id}/scene"),
        Ready = await CloudNode.Fetch($"users/{id}/ready"),
        Vote = await CloudNode.Fetch($"users/{id}/vote"),
        Items = new Item[6] {
            await Item.Fetch(id, "0"),
            await Item.Fetch(id, "1"),
            await Item.Fetch(id, "2"),
            await Item.Fetch(id, "3"),
            await Item.Fetch(id, "4"),
            await Item.Fetch(id, "5"),
        }
    };
}
