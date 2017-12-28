using UnityEngine;

public class SoundManagerScript : MonoBehaviour
{
    private static bool started;

    private void Awake()
    {
        if (!started)
        {
            DontDestroyOnLoad(gameObject);
            started = true;
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
}
