using System.Threading.Tasks;

class Item
{
    public CloudNode Name { get; private set; }
    public CloudNode Description { get; private set; }
    public CloudNode Image { get; private set; }

    public Item(string userId, string id)
    {
        Name = new CloudNode($"users/{userId}/items/{id}/name");
        Description = new CloudNode($"users/{userId}/items/{id}/description");
        Image = new CloudNode($"users/{userId}/items/{id}/image");
    }

    public static async Task<Item> Fetch(string userId, string id) => new Item(userId, id) {
        Name = await CloudNode.Fetch($"users/{userId}/items/{id}/name"),
        Description = await CloudNode.Fetch($"users/{userId}/items/{id}/description"),
        Image = await CloudNode.Fetch($"users/{userId}/items/{id}/image")
    };
}
