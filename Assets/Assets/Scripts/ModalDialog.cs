using UnityEngine;
using UnityEngine.UI;

public class ModalDialog : MonoBehaviour
{
    public GameObject MainScreen = null;
    public GameObject ThisScreen = null;
    public GameObject WaitScreen = null;

    public Button.ButtonClickedEvent OnConfirm;

    public void HideDialog()
    {
        if (MainScreen != null)
            SwitchTo(MainScreen);
    }

    public void ShowDialog()
    {
        if (ThisScreen != null)
            SwitchTo(ThisScreen);
    }

    public void ShowWaitDialog()
    {
        if (WaitScreen != null)
            SwitchTo(WaitScreen);
        else if (MainScreen != null)
            SwitchTo(MainScreen);

        OnConfirm.Invoke();
    }

    private void SwitchTo(GameObject gameObj)
    {
        foreach (var obj in new GameObject[] { MainScreen, ThisScreen, WaitScreen })
        {
            if (obj != null)
                obj.SetActive(false);
        }

        gameObj.SetActive(true);
    }
}
