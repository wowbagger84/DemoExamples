using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ReGenerateMap : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

    private void OnMouseDown()
    {
        FindObjectOfType<MapGenerator>().ToggleControlNode((int)(transform.position.x-0.5f), (int)(transform.position.y - 0.5f));
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        FindObjectOfType<MapGenerator>().ToggleControlNode((int)(transform.position.x - 0.5f), (int)(transform.position.y - 0.5f));
        Destroy(gameObject);
    }


}
