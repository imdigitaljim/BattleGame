using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextUpdate : MonoBehaviour {


    public GameObject SliderObject; 
	// Use this for initialization
	void Start ()
    {
        if (SliderObject == null)
        {
            Debug.LogError("Missing GameObject in Component Window");
        }
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}
    public void UpdateText()
    {
        gameObject.GetComponent<Text>().text = SliderObject.GetComponent<Slider>().value.ToString();
    }
}
