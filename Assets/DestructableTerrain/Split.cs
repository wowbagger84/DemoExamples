using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Split : MonoBehaviour {

    public float minSize = 0.01f;
    public float rotateSize = 0.1f;

    public bool destructable = true;

    public Transform holder;
    SpriteRenderer sr;

	// Use this for initialization
	void Start () {
        sr = GetComponent<SpriteRenderer>();
        //if (transform.localEulerAngles.z != 0)
        //{
        //    transform.localScale *= 1.5f;
        //    destructable = false;
        //    GetComponent<BoxCollider2D>().enabled = false;
        //    Invoke("Test", 0.5f);
        //}
	}

    void Test()
    {
        GetComponent<BoxCollider2D>().enabled = true;
    }

    private void OnMouseOver()
    {
        Fracture(Vector3.zero);
    }

    public void Fracture(Vector3 position)
    {
        if (destructable && position != Vector3.zero)
        {
            float x = sr.bounds.size.x / 4;
            float y = sr.bounds.size.y / 4;
            transform.localScale *= 0.5f;

            //if we are to small or to close to the explosion, do not spawn new smaller fragments.
            if (transform.localScale.x < minSize || Vector3.Distance(transform.position, position) < sr.bounds.size.magnitude/2)
            {
                return;
            }

            //if (position != Vector3.zero && transform.localScale.x < rotateSize)
            //{
            //    transform.localScale *= 1.5f;
            //    float angle = Mathf.Atan2(transform.position.y - position.y, transform.position.x - position.x) * Mathf.Rad2Deg;
            //    transform.Rotate(Vector3.forward, angle);
            //    //transform.Translate(transform.up.normalized * 0.1f);
            //}

            Instantiate(gameObject, transform.position - new Vector3(x, y, 0), transform.rotation, holder);
            Instantiate(gameObject, transform.position - new Vector3(-x, y, 0), transform.rotation, holder);
            Instantiate(gameObject, transform.position - new Vector3(x, -y, 0), transform.rotation, holder);
            Instantiate(gameObject, transform.position - new Vector3(-x, -y, 0), transform.rotation, holder);
        }

    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (destructable)
        {
            Fracture(other.gameObject.transform.position);
        }
        Destroy(gameObject);
    }


}
