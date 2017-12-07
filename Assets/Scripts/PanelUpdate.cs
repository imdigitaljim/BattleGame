using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PanelUpdate : MonoBehaviour {

    private List<GameObject> PlayerObjects = new List<GameObject>();
    public GameObject MaxGameObject;
    public GameObject PlayerGameObject;
	// Use this for initialization
	void Start ()
    {
	    foreach (Transform child in transform)
        {
            PlayerObjects.Add(child.gameObject);
        }
        PlayerObjects[2].SetActive(false);
        PlayerObjects[3].SetActive(false);
        PlayerObjects[0].transform.GetChild(0).gameObject.SetActive(false);
    }
	
	// Update is called once per frame
	void Update ()
    {
	    	
	}

    public void UpdatePanel()
    {
        int value = (int)MaxGameObject.GetComponent<Slider>().value;
        foreach (var obj in PlayerObjects)
        {
            obj.SetActive(false);
        }
        for (int i = 0; i < value; i++)
        {
            PlayerObjects[i].SetActive(true);
        }
    }
    public void UpdateUser()
    {
        int value = (int)PlayerGameObject.GetComponent<Slider>().value;
        for (int i = 0; i < 4; i++)
        {
            if (PlayerObjects[i].activeSelf)
            {
                PlayerObjects[i].transform.GetChild(0).gameObject.SetActive(true);
            }      
        }
        PlayerObjects[value - 1].transform.GetChild(0).gameObject.SetActive(false);
    }
}
