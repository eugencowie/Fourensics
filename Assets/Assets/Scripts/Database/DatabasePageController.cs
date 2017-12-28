using UnityEngine;

public class DatabasePageController : MonoBehaviour
{
    public DatabaseController Database;
    public GameObject PanelAbove;
    public GameObject PanelBelow;

    public void Up()
    {
        if (PanelAbove != null)
        {
            gameObject.SetActive(false);
            PanelAbove.SetActive(true);

            if (Database != null)
            {
                Database.PageChanged(gameObject, PanelAbove);
            }
        }
    }

    public void Down()
    {
        if (PanelBelow != null)
        {
            gameObject.SetActive(false);
            PanelBelow.SetActive(true);

            if (Database != null)
            {
                Database.PageChanged(gameObject, PanelBelow);
            }
        }
    }
}
