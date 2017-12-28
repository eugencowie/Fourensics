using UnityEngine;
using UnityEngine.UI;

public class ModalDialog : MonoBehaviour
{
    public GameObject MainScreen;
    public GameObject ThisScreen;
    public GameObject WaitScreen;

    public Button.ButtonClickedEvent OnConfirm;
    
    public void HideDialog()
    {
        SwitchTo(MainScreen);
    }

    public void ShowDialog()
    {
        SwitchTo(ThisScreen);
    }

    public void ShowWaitDialog()
    {
        SwitchTo(WaitScreen);

        OnConfirm.Invoke();
    }

    private void SwitchTo(GameObject gameObj)
    {
        foreach (var obj in new GameObject[] { MainScreen, ThisScreen, WaitScreen })
        {
            obj.SetActive(false);
        }

        gameObj.SetActive(true);
    }
}
