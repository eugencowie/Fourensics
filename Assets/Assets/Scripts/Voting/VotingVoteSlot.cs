using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

public class VotingVoteSlot : MonoBehaviour, IDropHandler
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

    public Button.ButtonClickedEvent OnDropped;

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

            var color = page.Image.color;
            color.a = 1.0f;
            page.Image.color = color;

            OnDropped.Invoke();


            //var prevPage = page.PanelLeft;
            //var nextPage = page.PanelRight;

            //prevPage.GetComponent<VotingPageController>().PanelRight = nextPage;
            //nextPage.GetComponent<VotingPageController>().PanelLeft = prevPage;

            // TODO: don't remove last item

            //page.Right();
            //suspect.gameObject.SetActive(false);

            Destroy(VotingDragHandler.itemBeingDragged);

            //if (!StaticSuspects.DiscardedSuspects.Any(s => s.Name == suspect.Name.text))
            //{
            //    StaticSuspects.DiscardedSuspects.Add(new VotingSuspectData(suspect));
            //}
        }
    }
    #endregion
}
