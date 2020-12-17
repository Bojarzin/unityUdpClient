using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    string idToDelete = "non";
    public string thisID;
    int spawnPos = 0;
    public GameObject cubePrefab1;
    public UdpClient udp;
    // Start is called before the first frame update
    void Start()
    {
        udp = new UdpClient();
        //3.17.142.185
        udp.Connect("localhost", 12345);

        //Reply, sending back "connect" in bytes to notify that a connection has been established. Send the length of the bytes as well.
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 1);
        InvokeRepeating("SendPosition", 1, 0.03f);
    }

    void OnDestroy()
    {
        udp.Dispose();
    }

    public enum commands
    {
        NEW_CLIENT,
        UPDATE,
        DROPPED
    };

    [Serializable]
    public class Message
    {
        public commands cmd;
        public Player player;
    }

    [Serializable]
    public class Player
    {
        [Serializable]
        public struct receivedColor
        {
            public float R;
            public float G;
            public float B;
        }

        public string id;
        public receivedColor color;
        public Vector3 position;
        public bool spawned = false;
        public GameObject playerMesh;
    }

    [Serializable]
    public class NewPlayer
    {

    }

    [Serializable]
    public class GameState
    {
        public Player[] players;
    }

    public List<Player> connectedPlayers = new List<Player>();

    public Message latestMessage;
    public GameState lastestGameState;
    void OnReceived(IAsyncResult result)
    {
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;

        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);

        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        Debug.Log("Got this: " + returnData);

        latestMessage = JsonUtility.FromJson<Message>(returnData);

        try
        {
            switch (latestMessage.cmd)
            {
                case commands.NEW_CLIENT:
                    connectedPlayers.Add(latestMessage.player);
                    thisID = latestMessage.player.id;
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.DROPPED:
                    Debug.Log("Dropped: " + latestMessage.player.id);
                    idToDelete = latestMessage.player.id;
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers()
    {
        for (int i = 0; i < connectedPlayers.Count; i++)
        {
            if (connectedPlayers[i].spawned == false)
            {
                Debug.Log("New player: " + connectedPlayers[i].id);
                Vector3 start = new Vector3(0, 0, 0);
                GameObject newMesh = Instantiate(cubePrefab1, start, Quaternion.identity);
                Color myColor = new Color(connectedPlayers[i].color.R, connectedPlayers[i].color.G, connectedPlayers[i].color.B);
                var cubeRenderer = newMesh.GetComponent<Renderer>();
                cubeRenderer.material.SetColor("_Color", myColor);
                connectedPlayers[i].playerMesh = newMesh;
                connectedPlayers[i].playerMesh.GetComponent<PlayerController>().id = connectedPlayers[i].id;
                connectedPlayers[i].spawned = true;
                spawnPos++;
            }
        }
    }

    void UpdatePlayers()
    {
        for (int i = 0; i < lastestGameState.players.Length; i++)
        {

            if (thisID != lastestGameState.players[i].id)
            {
                connectedPlayers[i].playerMesh.GetComponent<PlayerController>().position = lastestGameState.players[i].position;
            }
            connectedPlayers[i].position = connectedPlayers[i].playerMesh.GetComponent<PlayerController>().position;

            connectedPlayers[i].id = lastestGameState.players[i].id;
            connectedPlayers[i].color = lastestGameState.players[i].color;

            Color myColor = new Color(connectedPlayers[i].color.R, connectedPlayers[i].color.G, connectedPlayers[i].color.B);
            var cubeRenderer = connectedPlayers[i].playerMesh.GetComponent<Renderer>();
            cubeRenderer.material.SetColor("_Color", myColor);
        }
    }

    void DestroyPlayers(string id)
    {
        for (int i = 0; i < connectedPlayers.Count; i++)
        {
            if (connectedPlayers[i].id == id)
            {
                Debug.Log("Removed:" + connectedPlayers[i].id);
                Destroy(connectedPlayers[i].playerMesh);
                connectedPlayers.RemoveAt(i);
            }
        }
    }

    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void SendPosition()
    {
        if (connectedPlayers.Count != 0)
        {
            for (int i = 0; i < connectedPlayers.Count; i++)
            {
                if (connectedPlayers[i].id == thisID)
                {
                    string sendMessage = JsonUtility.ToJson(connectedPlayers[i]);
                    Byte[] sendBytes1 = Encoding.ASCII.GetBytes(sendMessage);

                    udp.Send(sendBytes1, sendBytes1.Length);
                }
            }
        }
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers(idToDelete);
    }
}