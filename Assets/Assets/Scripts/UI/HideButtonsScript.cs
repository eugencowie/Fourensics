using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideButtonsScript : MonoBehaviour {

    public GameObject hintUI;
    //public GameObject leaveButton;
    public GameObject[] buttons;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update ()
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
