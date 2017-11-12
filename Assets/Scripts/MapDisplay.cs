//this takes a noise map and turns it into a texture and then applies it to a plane in our scene 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour {

	public Renderer textureRender;

	public void DrawTexture(Texture2D texture){


		textureRender.sharedMaterial.mainTexture = texture;
		//textureRender.transform.localScale = new Vector3 (width, 1, height);
		textureRender.transform.localScale = new Vector3(texture.width,1, texture.height);


	}
}
