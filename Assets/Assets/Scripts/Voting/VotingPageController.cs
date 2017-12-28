using UnityEngine;
using UnityEngine.UI;

public class VotingPageController : MonoBehaviour
{
    public GameObject PanelLeft;
    public GameObject PanelRight;
    public Image Image;

    public void Left()
    {
        if (PanelLeft != null)
        {
            gameObject.SetActive(false);
            PanelLeft.SetActive(true);
        }
    }

    public void Right()
    {
        if (PanelRight != null)
        {
            gameObject.SetActive(false);
            PanelRight.SetActive(true);
        }
    }
}
