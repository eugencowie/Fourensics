using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class VotingDiscardSlot : MonoBehaviour, IDropHandler
{
    public GameObject item
    {
        get
        {
            if (transform.childCount > 0)
            {
                return transform.GetChild(0).gameObject;
            }
            return null;
        }
    }

    public bool CanDrop = false;

    //public GameObject Text;

    //public DatabaseController DatabaseController;

    //[Range(1, 6)]
    //public int SlotNumber;

    #region IDropHandler implementation

    public void OnDrop(PointerEventData eventData)
    {
        if (item == null && CanDrop)
        {
            var suspect = VotingDragHandler.itemBeingDragged.GetComponent<VotingDragHandler>().Suspect;
            var page = suspect.GetComponent<VotingPageController>();

            if (page.PanelLeft != page.gameObject || page.PanelRight != page.gameObject)
            {
                var prevPage = page.PanelLeft;
                var nextPage = page.PanelRight;

                prevPage.GetComponent<VotingPageController>().PanelRight = nextPage;
                nextPage.GetComponent<VotingPageController>().PanelLeft = prevPage;

                page.Right();
                suspect.gameObject.SetActive(false);

                Destroy(VotingDragHandler.itemBeingDragged);

                if (!StaticSuspects.DiscardedSuspects.Any(s => s.Name == suspect.Name.text))
                {
                    StaticSuspects.DiscardedSuspects.Add(new VotingSuspectData(suspect));
                }
            }
        }
    }

    #endregion
}
