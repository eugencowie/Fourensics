using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

class RetryScene : MonoBehaviour
{
    async void Start()
    {
        // Get database objects
        User m_user; try { m_user = await User.Get(); } catch { SceneManager.LoadScene("SignIn"); return; }
        Lobby m_lobby = await Lobby.Get(m_user);
    }
}
