using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserControl : MonoBehaviour {

    public MeshRenderer _renderer;
    private Texture2D _texture;
	// Use this for initialization
	void Start () {
        _renderer = GameObject.Find("Map").GetComponent<MeshRenderer>();
        _texture = _renderer.material.mainTexture as Texture2D;
    }

    private void Update()
    {
        if (_texture == null)
        {
            _texture = _renderer.material.mainTexture as Texture2D;
        }
        if (Input.GetMouseButtonDown(0))
            Clicked();
    }

    void Clicked()
    {
        var ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit = new RaycastHit();

        if (Physics.Raycast(ray, out hit))
        {
            var color = _texture.GetPixel((int)hit.point.x, (int)hit.point.z);
            Debug.Log(hit.point);
        }
    }
}
