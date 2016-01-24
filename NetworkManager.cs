using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System;
using System.Text;

//Author: Brian Herman
//Last Updated: 11/17/2015
//Summary: Network Manager component of singleton. Handles data transmition to/from game server. Inherits from UnityEngine's MonoBehavior object.

public class NetworkManager : MonoBehaviour
{
    private Socket NetConnection;
    private IPEndPoint currentConnectionEP;
    private UdpClient sock;
    private UdpClient recieve;

    private Queue<Packet> Packet_Queue;
    private Packet CurrentPositionPacket;

    public int myID;

    public bool Active = false;
    public bool NetActive;

    public int FrameUpdate = 20;
    public bool reading;

    public void Awake()
    {
        

        if(Manager.Instance.DEBUG_STATE == DebugState.DEBUG)
        Debug.Log("Network Manager Initialized!");
        Active = true;
        NetActive = true;

        sock = null;

        Packet_Queue = new Queue<Packet>();
        StartCoroutine("NetStream");

        SendLogin("tempAccount","tempPassword");
    }

    private void CreateConnection(NetworkOutboundDestination dest)
    {
        IPAddress ServerIP;
        int ServerPort; //Temporary

        switch (dest)
        {
            case NetworkOutboundDestination.LoginServer:
                //NetConnection = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                ServerIP = IPAddress.Parse(GlobalValues.LOGIN_SERVER_IP);
                ServerPort = GlobalValues.LOGIN_PORT;
                break;
            case NetworkOutboundDestination.MainServer:
                //NetConnection = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                ServerIP = IPAddress.Parse(GlobalValues.MAIN_SERVER_IP);
                ServerPort = GlobalValues.MAIN_PORT;
                break;
            default:
                throw new System.Exception("Attempting to create connection with non-existant server!");
        }

        currentConnectionEP = new IPEndPoint(ServerIP, ServerPort);

        sock = new UdpClient();
        sock.Connect(currentConnectionEP);
    }



    public void SendLogin(string _name, string _pass)
    {
        
        CreateConnection(NetworkOutboundDestination.LoginServer);

        Packet p = new Packet(PacketType.Login);
        p.Push_nData(new NetData(HelperMethods.SHA1_Encrypt(_name)));
        p.Push_nData(new NetData(HelperMethods.SHA1_Encrypt(_pass)));
        byte[] transferData = p.ToStream();


        try
        {
            //NetConnection.SendTo(p.ToStream(), currentConnectionEP);
            sock.Send(transferData,transferData.Length);
            //NetConnection.SendTo(Encoding.ASCII.GetBytes("Hi"),currentConnectionEP);
            //Debug.Log("Login Packet Sent");
        }
        catch
        {
            Debug.LogError("Failed to send message to Login Server");
        }
        //HelperMethods.SHA1_Encrypt("temporaryPassword");
    }

    public void Send(Packet p)
    {
        Packet_Queue.Enqueue(p);
    }
    public void SendPosition(Packet p)
    {
        CurrentPositionPacket = p;
    }

    public IEnumerator NetStream()
    {
        float frameUpdate = 1f / FrameUpdate;

        byte[] msg = new byte[2048];
        while (NetActive)
        {
            reading = false;
            
            //TODO: Handle this if it gets a bit slow...
            while(Packet_Queue.Count > 0)
            {
                Packet pTmp = Packet_Queue.Dequeue();
                byte[] sendData = pTmp.ToStream();4485

                sock.Send(sendData,sendData.Length);
                pTmp = null;
            }
            if (CurrentPositionPacket != null)
            {
                byte[] posData = CurrentPositionPacket.ToStream();
                sock.Send(posData, posData.Length);
            }

            reading = true;

            //Recieve packet;
            if (sock != null)
            {
                if (sock.Available > 0)
                {
                    //Debug.Log(currentConnectionEP.Address.ToString());
                    msg = sock.Receive(ref currentConnectionEP);
                    HandleMessage(msg);

                }
            }

            yield return null;
        }
    }

    private void HandleMessage(byte[] msg)
    {
        //if (msg[0] == 0) return;
        int uID;
        Packet packet = new Packet(msg);
        switch (packet.packet_type)
        {
            case PacketType.ServerMsg:
                break;
            case PacketType.Login:
                //Login Successful
                //Change UI_State
                if (packet.Pop_nData().To_Bool())
                {
                    myID = packet.Pop_nData().To_Int();
                    //Tell UI to display successful login information
                    if (Manager.Instance.DEBUG_STATE == DebugState.DEBUG)
                        Debug.Log("Login Successful!");

                    //CreateConnection(NetworkOutboundDestination.MainServer);
                    //Send request for inventory information
                    //Send request for equipment information

                    //Generate Map
                    //Generate Player
                    Manager.Instance.PlayerManager.PlayerJoinMap(myID, Vector3.zero);
                }
                else
                {
                    //Tell UI to display unsuccessful login information
                }
                break;
            case PacketType.HitboxCreate:
                uID = packet.Pop_nData().To_Int();
                if (uID == myID) return;

                Vector3 hb_pos = new Vector3(packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float());

                if (Manager.Instance.DEBUG_STATE == DebugState.DEBUG)
                Debug.Log("Hitbox Created. Position:" + hb_pos.ToString());

                HelperMethods.SpawnDamageBox(uID, hb_pos, packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float());
                break;
            case PacketType.ProjectileCreate:
                uID = packet.Pop_nData().To_Int();
                if (uID == myID) return;

                Vector3 proj_pos = new Vector3(packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float());
                Vector3 proj_dir = new Vector3(packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float());

                if (Manager.Instance.DEBUG_STATE == DebugState.DEBUG)
                Debug.Log("Projectile Created. Position: " + proj_pos.ToString() + " Direction: " + proj_dir.ToString());

                    HelperMethods.SpawnProjectile(uID,proj_pos, proj_dir,packet.Pop_nData().To_Int(), packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float());
                break;
            case PacketType.ChangeMap:
                break;
            case PacketType.Damage:
                break;
            case PacketType.CrowdControl:
                break;
            case PacketType.Death:
                break;
            case PacketType.ItemAdd:
                break;
            case PacketType.ItemRemove:
                break;
            case PacketType.PositionUpdate:
                int playerID = packet.Pop_nData().To_Int();
                if (playerID == myID) break;

                Vector3 playerPosition = new Vector3(packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float());
                if (Manager.Instance.DEBUG_STATE == DebugState.DEBUG)
                    Debug.Log("Position Update Recieved: Player["+playerID+"]  pos: "+ playerPosition.ToString());

                Manager.Instance.PlayerManager.UpdatePlayerPosition(playerID, playerPosition);
                break;
            default:
                break;
        }

        Debug.Log("Handled packet of type " + packet.packet_type);
    }

}
