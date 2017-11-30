using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor (typeof(MapGenerator))]
public class MapGeneratorEditor : Editor {

	public override void OnInspectorGUI()
    {
		MapGenerator mapGen = (MapGenerator)target;

		if (DrawDefaultInspector ()) { //if any value was chnange 
			if (mapGen.autoUpdate) {
				mapGen.GenerateMap ();
			}

		}

		//if (GUILayout.Button ("Generate")) { //generates map when we click generate but should do it when game starts 
		//	mapGen.GenerateMap ();
		//}


	}
}
