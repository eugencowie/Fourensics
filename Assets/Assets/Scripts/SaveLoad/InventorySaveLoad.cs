using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class InventorySaveLoad : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Load();
    }

    private void OnApplicationPause(bool pause)
    {
        Save();
    }

    private void OnApplicationQuit()
    {
        Save();
    }

    public static void Save()
    {
        using (FileStream file = File.Create(Application.persistentDataPath + "/savedGames.gd"))
        {
            new BinaryFormatter().Serialize(file, StaticInventory.Hints);
        }
    }

    public static void Load()
    {
        if (File.Exists(Application.persistentDataPath + "/savedGames.gd"))
        {
            using (FileStream file = File.Open(Application.persistentDataPath + "/savedGames.gd", FileMode.Open))
            {
                StaticInventory.Hints = (List<ObjectHintData>)new BinaryFormatter().Deserialize(file);
            }
        }
    }
}
