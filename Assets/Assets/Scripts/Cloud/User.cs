using System;
using System.Linq;
using System.Threading.Tasks;

struct Key
{
    public string Path { get; set; }
    public string Id { get; set; }
    public override string ToString() => $"{Path}/{Id}";
    
    public Key(string path, string id)
    {
        if (path == null) throw new ArgumentNullException("path");
        if (id == null) throw new ArgumentNullException("id");

        Path = path;
        Id = id;
    }
}

class User : ICloudObject
{
    public Key Key { get; private set; }
    public CloudNode Name { get; private set; }
    public CloudNode Lobby { get; private set; }
    public CloudNode<long> Scene { get; private set; }
    public CloudNode<bool> Ready { get; private set; }
    public CloudNode Vote { get; private set; }
    public LobbyUserItem[] Items { get; private set; }

    public string Id => Key.Id;

    void ICloudObject.Create(Key key)
    {
        Key = key;
        Name = CloudNode.Create($"{Key}/name");
        Lobby = CloudNode.Create($"{Key}/lobby");
        Scene = CloudNode<long>.Create($"{Key}/scene");
        Ready = CloudNode<bool>.Create($"{Key}/ready");
        Vote = CloudNode.Create($"{Key}/vote");
        Items = "012345".Select(n => Cloud.Create<LobbyUserItem>($"{Key}/items", n.ToString())).ToArray();
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        Name = await CloudNode.Fetch($"{Key}/name");
        Lobby = await CloudNode.Fetch($"{Key}/lobby");
        Scene = await CloudNode<long>.Fetch($"{Key}/scene");
        Ready = await CloudNode<bool>.Fetch($"{Key}/ready");
        Vote = await CloudNode.Fetch($"{Key}/vote");
        Items = await Task.WhenAll("012345".Select(n => Cloud.Fetch<LobbyUserItem>($"{Key}/items", n.ToString())));
    }
}
