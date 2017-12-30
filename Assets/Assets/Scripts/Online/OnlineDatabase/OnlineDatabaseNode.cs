using System.Threading.Tasks;

/// <summary>
/// Represents a node in the database which contains a collection of database entries.
/// </summary>
public abstract class OnlineDatabaseNode
{
    /// <summary>
    /// Reference to the database.
    /// </summary>
    protected readonly OnlineDatabase Database;

    /// <summary>
    /// The full path to the node in the database.
    /// </summary>
    public readonly string Key;

    /// <summary>
    /// Initialises the database node.
    /// </summary>
    public OnlineDatabaseNode(OnlineDatabase database, string key)
    {
        Database = database;
        Key = key;
    }

    /// <summary>
    /// An enumerable collection of database entries.
    /// </summary>
    public abstract OnlineDatabaseEntry[] Entries { get; }

    /// <summary>
    /// Checks if the key exists in the database.
    /// </summary>
    public async Task<bool> Exists()
    {
        return await Database.Exists(Key);
    }

    /// <summary>
    /// Deletes the key from the database.
    /// </summary>
    public async Task<bool> Delete()
    {
        bool success = await Database.Delete(Key);
        if (success)
        {
            foreach (var entry in Entries)
            {
                entry.Value = "";
            }
        }
        return success;
    }

    /// <summary>
    /// Pulls all entries from the database.
    /// </summary>
    public async Task<bool> PullEntries()
    {
        bool success = true;
        foreach (var entry in Entries)
        {
            if (await entry.Pull() == false)
                success = false;
        }
        return success;
    }

    /// <summary>
    /// Pushes all entries to the database.
    /// </summary>
    public async Task<bool> PushEntries()
    {
        bool success = true;
        foreach (var entry in Entries)
        {
            if (await entry.Push() == false)
                success = false;
        }
        return success;
    }

    /// <summary>
    /// Deletes all entries from the database.
    /// </summary>
    public async Task<bool> DeleteEntries()
    {
        bool success = true;
        foreach (var entry in Entries)
        {
            if (await entry.Delete() == false)
                success = false;
        }
        return success;
    }

    /// <summary>
    /// Registers a handler for value changed events.
    /// </summary>
    public void RegisterListeners(OnlineDatabaseEntry.Listener listener)
    {
        foreach (var entry in Entries)
        {
            entry.RegisterListener(listener);
        }
    }

    /// <summary>
    /// Deregisters a handler for value changed events.
    /// </summary>
    public void DeregisterListeners()
    {
        foreach (var entry in Entries)
        {
            entry.DeregisterListener();
        }
    }
}
