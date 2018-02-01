using System;
using System.Threading.Tasks;

class Item
{
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

    Item() { }

    public static Item Create(string userId, string id) => new Item {
        Name = CloudNode.Create($"users/{userId}/items/{id}/name"),
        Description = CloudNode.Create($"users/{userId}/items/{id}/description"),
        Image = CloudNode.Create($"users/{userId}/items/{id}/image")
    };

    public static async Task<Item> Fetch(string userId, string id) => new Item {
        Name = await CloudNode.Fetch($"users/{userId}/items/{id}/name"),
        Description = await CloudNode.Fetch($"users/{userId}/items/{id}/description"),
        Image = await CloudNode.Fetch($"users/{userId}/items/{id}/image")
    };
}
