using UnityEngine;

class DontDestroyOnLoad : MonoBehaviour
{
    static bool m_started = false;

    void Awake()
    {
        if (!m_started)
        {
            DontDestroyOnLoad(gameObject);
            m_started = true;
        }
        else
        {
            DestroyImmediate(gameObject);
        }
    }
}
