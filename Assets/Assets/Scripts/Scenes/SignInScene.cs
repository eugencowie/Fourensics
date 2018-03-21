using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
class SignInPanels
{
    public GameObject Main = null;
    public GameObject Wait = null;

    public GameObject[] All => new GameObject[] { Main, Wait };
}

class SignInScene : MonoBehaviour
{
    [SerializeField] SignInPanels m_panels = null;

    void Start()
    {
        // Show wait panel
        SwitchPanel(m_panels.Wait);

        // Show start panel
        SwitchPanel(m_panels.Main);
    }

    /// <summary>
    /// Called when the google sign in button in the main panel is pressed.
    /// </summary>
    public void GoogleSignInButtonPressed()
    {
    }

    /// <summary>
    /// Called when the guest sign in button in the main panel is pressed.
    /// </summary>
    public void GuestSignInButtonPressed()
    {
    }

    void SwitchPanel(GameObject panel)
    {
        // Disable all panels
        foreach (var p in m_panels.All)
        {
            p.SetActive(false);
        }

        // Enable specified panel
        panel.SetActive(true);
    }
}
