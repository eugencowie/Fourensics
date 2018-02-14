using System;
using System.Linq;
using System.Threading.Tasks;

class Lobby : ICloudObject
{
    public Key Key { get; private set; }
    public CloudNode<long> State { get; private set; }
    public CloudNode[] Users { get; private set; }
    //public LobbyUser[] Users { get; private set; }

    public string Id => Key.Id;

    void ICloudObject.Create(Key key)
    {
        Key = key;
        State = CloudNode<long>.Create($"{Key}/state");
        Users = "0123".Select(n => CloudNode.Create($"{Key}/users/{n}")).ToArray();
        //Users = "0123".Select(n => Cloud.Create<LobbyUser>($"{Key}/users", n.ToString())).ToArray();
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        State = await CloudNode<long>.Fetch($"{Key}/state");
        Users = await Task.WhenAll("0123".Select(n => CloudNode.Fetch($"{Key}/users/{n}")));
        //Users = await Task.WhenAll("0123".Select(n => Cloud.Fetch<LobbyUser>($"{Key}/users", n.ToString())));
    }
}

/*class LobbyUser : ICloudObject
{
    public Key Key { get; private set; }
    public CloudNode<long> Scene { get; private set; }
    public CloudNode<bool> Ready { get; private set; }
    public CloudNode Vote { get; private set; }
    public LobbyUserItem[] Items { get; private set; }

    void ICloudObject.Create(Key key)
    {
        Key = key;
        Scene = CloudNode<long>.Create($"{Key}/scene");
        Ready = CloudNode<bool>.Create($"{Key}/ready");
        Vote = CloudNode.Create($"{Key}/vote");
        Items = "012345".Select(n => Cloud.Create<LobbyUserItem>($"{Key}/items", n.ToString())).ToArray();
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        Scene = await CloudNode<long>.Fetch($"{Key}/scene");
        Ready = await CloudNode<bool>.Fetch($"{Key}/ready");
        Vote = await CloudNode.Fetch($"{Key}/vote");
        Items = await Task.WhenAll("012345".Select(n => Cloud.Fetch<LobbyUserItem>($"{Key}/items", n.ToString())));
    }
}*/

class LobbyUserItem : ICloudObject
{
    public Key Key { get; private set; }
    public CloudNode Name { get; private set; }
    public CloudNode Description { get; private set; }
    public CloudNode Image { get; private set; }
    
    public event Action<CloudNode> ValueChanged
    {
        add
        {
            Name.ValueChanged += value;
            Description.ValueChanged += value;
            Image.ValueChanged += value;
        }
        remove
        {
            Name.ValueChanged -= value;
            Description.ValueChanged -= value;
            Image.ValueChanged -= value;
        }
    }

    void ICloudObject.Create(Key key)
    {
        Key = key;
        Name = CloudNode.Create($"{Key}/name");
        Description = CloudNode.Create($"{Key}/description");
        Image = CloudNode.Create($"{Key}/image");
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        Name = await CloudNode.Fetch($"{Key}/name");
        Description = await CloudNode.Fetch($"{Key}/description");
        Image = await CloudNode.Fetch($"{Key}/image");
    }
}
