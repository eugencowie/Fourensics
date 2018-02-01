using UnityEngine;

public class SoundManagerScript : MonoBehaviour
{
    private static bool started;

    private void Awake()
    {
        if (!started)
        {
            DontDestroyOnLoad(gameObject.transform.parent.gameObject);
            started = true;
        }
        else
        {
            DestroyImmediate(gameObject.transform.parent.gameObject);
        }
    }
}
