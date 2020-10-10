using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    NetworkMan networkMan;
    // Start is called before the first frame update
    void Start()
    {
        networkMan = GameObject.Find("NetworkMan").GetComponent<NetworkMan>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.name != networkMan.playerAddress)
        {
            return;
        }
        if(Input.GetKeyDown(KeyCode.A))
        {
            gameObject.transform.Translate(new Vector3(-1, 0, 0));
        }
        if (Input.GetKeyDown(KeyCode.W))
        {
            gameObject.transform.Translate(new Vector3(0, 0, 1));
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            gameObject.transform.Translate(new Vector3(0, 0, -1));
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            gameObject.transform.Translate(new Vector3(1, 0, 0));
        }
    }
}
