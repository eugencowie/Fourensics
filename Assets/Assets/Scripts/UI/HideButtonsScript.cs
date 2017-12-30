using UnityEngine;

public class HideButtonsScript : MonoBehaviour
{
    public GameObject hintUI;
    public GameObject[] buttons;

    void Update()
    {
        if (hintUI.activeSelf == true)
        {
            foreach (var button in buttons)
            {
                button.SetActive(false);
            }
        }
        else
        {
            foreach (var button in buttons)
            {
                button.SetActive(true);
            }
        }
    }
}
