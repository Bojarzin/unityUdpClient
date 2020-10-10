﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using UnityEngine.UIElements;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;                                   // UDP client instance
    public GameObject playerGO;                             // Object to represent player

    public string playerAddress;                            // Address = (IP, PORT)
    public Dictionary<string,GameObject> currentPlayers;    // A list of currently connected players
    public List<string> newPlayers,                         // List of new players
                        droppedPlayers;                     // List of dropped players
    public ListOfPlayers initialSetofPlayers;               // Initial set of players to spawn

    public GameState lastestGameState;                      // Last GameState received from the server
    public MessageType latestMessage;                       // Last message received from the server

    public Vector3 newPosition = new Vector3(1, 0, 0);

    // Start is called before the first frame update
    void Start()
    {
        // Initialize variables
        newPlayers = new List<string>();
        droppedPlayers = new List<string>();
        currentPlayers = new Dictionary<string, GameObject>();
        initialSetofPlayers = new ListOfPlayers();

        // Connect to the client.
        udp = new UdpClient();
        Debug.Log("Connecting...");
        udp.Connect("localhost",12345);
        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
        udp.Send(sendBytes, sendBytes.Length);
        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        // Used to repeatedly call the heartbeat
        InvokeRepeating("HeartBeat", 1, 1);

        InvokeRepeating("UpdatePosition", 1, 0.5f);
    }

    void OnDestroy()
    {
        udp.Dispose();
    }

    [Serializable]
    public struct receivedColor
    {
        public float R;
        public float G;
        public float B;
    }

    [Serializable]
    public struct receivedPosition
    {
        public float X;
        public float Y;
        public float Z;
    }

    [Serializable]
    public class Player
    {
        public string id;
        public receivedColor color;
        public receivedPosition position;
    }


    [Serializable]
    public class ListOfPlayers
    {
        public Player[] players;

        public ListOfPlayers()
        {
            players = new Player[0];
        }
    }

    [Serializable]
    public class ListOfDroppedPlayers
    {
        public string[] droppedPlayers;
    }


    [Serializable]
    public class GameState
    {
        public Player[] players;
    }


    [Serializable]
    public class MessageType
    {
        public commands cmd;
        public receivedPosition position;
    }

    public enum commands
    {
        PLAYER_CONNECTED,       //0
        GAME_UPDATE,            // 1
        PLAYER_DISCONNECTED,    // 2
        CONNECTION_APPROVED,    // 3
        LIST_OF_PLAYERS,        // 4
        POSITION_UPDATE,        // 5
    };
    
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
        // Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<MessageType>(returnData);
        
        Debug.Log(returnData);
        try{
            switch(latestMessage.cmd)
            {
                case commands.PLAYER_CONNECTED:
                    ListOfPlayers latestPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    foreach (Player player in latestPlayer.players)
                    {
                        newPlayers.Add(player.id);
                    }
                    break;
                case commands.GAME_UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.PLAYER_DISCONNECTED:
                    ListOfDroppedPlayers latestDroppedPlayer = JsonUtility.FromJson<ListOfDroppedPlayers>(returnData);
                    foreach (string player in latestDroppedPlayer.droppedPlayers)
                    {
                        droppedPlayers.Add(player);
                    }
                    break;
                case commands.CONNECTION_APPROVED:
                    ListOfPlayers myPlayer = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    foreach (Player player in myPlayer.players)
                    {
                        newPlayers.Add(player.id);
                        playerAddress = player.id;
                    }
                    break;
                case commands.LIST_OF_PLAYERS:
                    initialSetofPlayers = JsonUtility.FromJson<ListOfPlayers>(returnData);
                    break; 
                default:
                    Debug.Log("Error: " + returnData);
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
        if (newPlayers.Count > 0)
        {
            foreach (string playerID in newPlayers)
            {
                currentPlayers.Add(playerID,Instantiate(playerGO, new Vector3(0,0,0), Quaternion.identity));
                currentPlayers[playerID].name = playerID;
            }
            //newPosition = newPosition + new Vector3(20, 30, 30);
            newPlayers.Clear();
        }
        if (initialSetofPlayers.players.Length > 0)
        {
            foreach (Player player in initialSetofPlayers.players)
            {
                if (player.id == playerAddress)
                    continue;
                currentPlayers.Add(player.id, Instantiate(playerGO, new Vector3(0,0,0), Quaternion.identity));
                currentPlayers[player.id].GetComponent<Renderer>().material.color = new Color(player.color.R, player.color.G, player.color.B);
                currentPlayers[player.id].transform.position = new Vector3(player.position.X, player.position.Y, player.position.Z);
                currentPlayers[player.id].name = player.id;
            }
            //newPosition = newPosition + new Vector3(1, 0, 0);
            initialSetofPlayers.players = new Player[0];
        }
    }

    void UpdatePlayers()
    {
        if (lastestGameState.players.Length >0)
        {
            foreach (NetworkMan.Player player in lastestGameState.players)
            {
                string playerID = player.id;
                currentPlayers[player.id].GetComponent<Renderer>().material.color = new Color(player.color.R,player.color.G,player.color.B);
                currentPlayers[player.id].transform.position = new Vector3(player.position.X, player.position.Y, player.position.Z);
            }
            lastestGameState.players = new Player[0];
        }
    }

    void DestroyPlayers(){
        if (droppedPlayers.Count > 0)
        {
            foreach (string playerID in droppedPlayers)
            {
                Destroy(currentPlayers[playerID].gameObject);
                currentPlayers.Remove(playerID);
            }
            droppedPlayers.Clear();
        }
    }
    
    void HeartBeat()
    {
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    void UpdatePosition()
    {
        MessageType message = new MessageType();
        message.cmd = commands.POSITION_UPDATE;
        message.position.X = currentPlayers[playerAddress].transform.position.x;
        message.position.Y = currentPlayers[playerAddress].transform.position.y;
        message.position.Z = currentPlayers[playerAddress].transform.position.z;
        Byte[] sendBytes = Encoding.ASCII.GetBytes(JsonUtility.ToJson(message));
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update()
    {
        SpawnPlayers();
        UpdatePlayers();
        DestroyPlayers();
    }
}