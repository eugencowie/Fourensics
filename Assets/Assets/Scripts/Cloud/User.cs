using System;
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

    public string Id => Key.Id;

    void ICloudObject.Create(Key key)
    {
        Key = key;
        Name = CloudNode.Create($"{Key}/name");
        Lobby = CloudNode.Create($"{Key}/lobby");
    }

    async Task ICloudObject.Fetch(Key key)
    {
        Key = key;
        Name = await CloudNode.Fetch($"{Key}/name");
        Lobby = await CloudNode.Fetch($"{Key}/lobby");
    }
}
