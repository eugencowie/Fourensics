using Firebase;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

class SignInScene : MonoBehaviour
{
    [Serializable]
    class SignInPanels
    {
        public GameObject Wait = null;
        public GameObject Main = null;
        public GameObject[] All => new GameObject[] { Wait, Main };
    }

    [SerializeField] SignInPanels m_panels = null;

    void Start()
    {
        // Show start panel
        SwitchPanel(m_panels.Main);
    }

    public async void GoogleSignInButtonPressed()
    {
        // Show wait panel
        SwitchPanel(m_panels.Wait);

        // Create google user
        await User.SignInWithGoogle();

        // Load lobby scene
        SceneManager.LoadSceneAsync("Lobby");
    }

    public async void GuestSignInButtonPressed()
    {
        // Show wait panel
        SwitchPanel(m_panels.Wait);

        // Create guest user
        await User.SignInAsGuest();

        // Load lobby scene
        SceneManager.LoadSceneAsync("Lobby");
    }

    void SwitchPanel(GameObject panel)
    {
        // Disable all panels
        foreach (var p in m_panels.All)
            p.SetActive(false);

        // Enable specified panel
        panel.SetActive(true);
    }
}
