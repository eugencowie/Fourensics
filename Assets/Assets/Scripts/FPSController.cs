using UnityEngine;

class FPSController : MonoBehaviour
{
    float m_deltaTime = 0;

    void Update()
    {
        m_deltaTime += (Time.deltaTime - m_deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        GUIStyle style = new GUIStyle {
            alignment = TextAnchor.UpperLeft,
            fontSize = Screen.height * 2 / 50,
            normal = new GUIStyleState { textColor = Color.yellow }
        };

        float msec = m_deltaTime * 1000.0f;
        float fps = 1.0f / m_deltaTime;
        string text = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);

        Rect rect = new Rect(0, 0, Screen.width, Screen.height * 2 / 50);
        GUI.Label(rect, text, style);
    }
}
