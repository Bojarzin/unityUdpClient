using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using static NetworkMan;

[Serializable]
public class PlayerController : MonoBehaviour
{
    public NetworkMan NetManager;
    public Vector3 position = new Vector3(0, 0, 0);
    public string id;
    // Start is called before the first frame update

    void Start()
    {
        position = new Vector3(0, 0, 0);
        NetManager = GameObject.Find("NetworkMan").GetComponent<NetworkMan>();
    }

    // Update is called once per frame
    void Update()
    {
        if (NetManager.thisID != id)
        {
            transform.position = position;
            return;
        }

        if (Input.GetKey(KeyCode.W))
        {
            position.y += 1 * Time.deltaTime;
            Debug.Log("W");
        }

        if (Input.GetKey(KeyCode.A))
        {
            position.x -= 1 * Time.deltaTime;
            Debug.Log("A");
        }

        if (Input.GetKey(KeyCode.S))
        {
            position.y -= 1 * Time.deltaTime;
            Debug.Log("S");
        }

        if (Input.GetKey(KeyCode.D))
        {
            position.x += 1 * Time.deltaTime;
            Debug.Log("D");
        }
    }
}