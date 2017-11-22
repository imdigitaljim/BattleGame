using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour {

    public float MOVEMENT_SPEED = 0.2f;

	// Use this for initialization
	void Start () {
		// Will need to implement a find player function so camera focuses on player character

	}
	
	// Update is called once per frame
	void Update () {
		// If arrow keys detected scroll through map
        //Gets keys for continuous movement
        if (Input.GetKey(KeyCode.UpArrow))
        {
            transform.Translate(new Vector3(0, MOVEMENT_SPEED, 0));
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            transform.Translate(new Vector3(MOVEMENT_SPEED, 0, 0));
        }
        else if (Input.GetKey(KeyCode.LeftArrow))
        {
            transform.Translate(new Vector3(-MOVEMENT_SPEED, 0, 0));
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            transform.Translate(new Vector3(0, -MOVEMENT_SPEED, 0));
        }

        //Will need to implement map limit detector to prevent scrolling infinitely off map, or bind find player function to key
	}
}
