using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

//Author: Brian Herman
//Last Updated: 11/18/2015
//Summary: Main loop for Server front end. Handles received data from clients.

namespace ServerFrontend
{
    public class Server
    {
        private IPEndPoint clientConnections;
        private UdpClient sock;
        private List<PlayerData> OnlinePlayers;
        private static int numTasks;
        private Queue<SendPacket> PacketQueue;

        //TODO: Client Backend
        private BackEndConnector BackEnd;

        private Packet packet;

        public bool running;

        public Server()
        {
            OnlinePlayers = new List<PlayerData>();
            PacketQueue = new Queue<SendPacket>();
            running = true;
            Console.WriteLine("Front End Server Started...");
            Thread.Sleep(100);
            Console.WriteLine("Establishing TCP Connection with Backend Servers...");
            BackEnd = new BackEndConnector(this);
            if(BackEnd.EstablishConnection())
            {
                //BackEnd.run();
                Thread.Sleep(100);

                Console.WriteLine("Connection Established! Starting data flow...");
                Run();
            }
            else
            {
                Console.WriteLine("Connection failed to be established. Closing...");
                return;
            }
        }
        
        public void Run()
        {
            //clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            sock = new UdpClient(17223);
            clientConnections = new IPEndPoint(IPAddress.Any, 17223);
            byte[] msg = new byte[2048];

            Console.WriteLine("Data flow Started. Waiting for message...");
            try
            {
                while(running)
                {
                    msg = sock.Receive(ref clientConnections);

                    //Console.WriteLine("Received message from {0}", clientConnections.ToString());
                    HandleMessage(msg, clientConnections);
                    UpdatePacketStream();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            finally
            {

            }
        }

        private void UpdatePacketStream()
        {
            while(numTasks < 10 && PacketQueue.Count > 0)
            {
                //Console.WriteLine("Attempting to send Packet...");
                SendPacket package = PacketQueue.Dequeue();
                Task tmp = new Task(() => SEND_TASK(package.IPEP, package.Packet));
                tmp.Start();
                numTasks++;
            }
        }

        private void SEND_TASK(IPEndPoint connection, Packet p)
        {
            byte[] b = p.ToStream();
            sock.Send(b, b.Length, connection);
            numTasks--;
        }

        public void Send_Single(IPEndPoint connection, Packet p)
        {
            PacketQueue.Enqueue(new SendPacket() { IPEP = connection, Packet = p });
        }

        public void Send_FromBackend(string IP, Packet p)
        {
            
            IPEndPoint IPEPtmp = new IPEndPoint(IPAddress.Parse(IP), 17223);
            PacketQueue.Enqueue(new SendPacket() { IPEP = IPEPtmp, Packet = p });
        }

        public void Send_All(int MapID, Packet p)
        {
            foreach(var plyData in OnlinePlayers)
            {
                if(plyData.CurrentMap == MapID)
                {
                    PacketQueue.Enqueue(new SendPacket() { IPEP = plyData.PlayerConnectionIPEP, Packet = p });
                }
            }
        }

        private void HandleMessage(byte[] msg, IPEndPoint connection)
        {
            packet = new Packet(msg);
            int uID;
            switch (packet.packet_type)
            {
                //DEBUG MESSAGE RECIEVED
                case PacketType.ServerMsg:
                    NetData tmp = packet.Pop_nData();
                    StringBuilder sb = new StringBuilder("ServerDebugMsg: ");
                    while (tmp != null)
                    {
                        sb.Append(tmp.To_String());
                        sb.Append(" ");
                        tmp = packet.Pop_nData();
                    }
                    Console.WriteLine(sb);
                    sb = null;
                    tmp = null;
                    break;
                case PacketType.Login:

                    //TODO: CLEANUP
                    string CryptUsername = packet.Pop_nData().To_String();
                    string CryptPassword = packet.Pop_nData().To_String();
                    Console.WriteLine("Login Request Received. <" + connection.Address + ">");
                    //Console.WriteLine("Password: " + CryptPassword);

                    Random r = new Random();
                    PlayerData pd = new PlayerData()
                    {
                        PlayerConnectionIPEP = connection,
                        PlayerPosition = new Vector3(),
                        UID = r.Next(0,100),
                        PlayerFacingDirection = new Vector3(0, -1, 0),
                        CurrentMap = 0
                    };
                    OnlinePlayers.Add(pd);

                    Packet loginAuth = new Packet(PacketType.SERVER_QueryLogin);
                    loginAuth.Push_nData(new NetData(connection.Address.ToString()));
                    loginAuth.Push_nData(new NetData(CryptUsername));
                    loginAuth.Push_nData(new NetData(CryptPassword));

                    //TODO: Pass Encrypted data to login server for authentication.
                    //TODO: Pretend its accepted. Pass default accepted response to client.
                    Packet response = new Packet(PacketType.Login);
                    response.Push_nData(new NetData(true));
                    response.Push_nData(new NetData(pd.UID));

                    Send_Single(connection, response);
                    
                    break;
                case PacketType.PositionUpdate:
                    try
                    {
                        uID = packet.Pop_nData().To_Int();
                        int playerIndex = OnlinePlayers.FindIndex(ply => ply.UID == uID);
                        
                        Vector3 PositionUpdate = new Vector3(packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float());
                        OnlinePlayers[playerIndex].PlayerPosition = PositionUpdate;
                        //Console.WriteLine(string.Format("PosUpdate: uID[{0}]: Pos{1}", OnlinePlayers[playerIndex].UID, OnlinePlayers[playerIndex].PlayerPosition.ToString()));
                        Packet posUp = new Packet(PacketType.PositionUpdate);
                        posUp.Push_nData(new NetData(OnlinePlayers[playerIndex].UID));
                        posUp.Push_nData(new NetData(OnlinePlayers[playerIndex].PlayerPosition.x));
                        posUp.Push_nData(new NetData(OnlinePlayers[playerIndex].PlayerPosition.y));
                        posUp.Push_nData(new NetData(OnlinePlayers[playerIndex].PlayerPosition.z));

                        Send_All(OnlinePlayers[playerIndex].CurrentMap,posUp);
                    }
                    catch (ArgumentNullException e)
                    {
                        Console.WriteLine("Couldn't find player in list!");
                    }
                break;
                case PacketType.ChangeMap:
                    try
                    {
                        uID = packet.Pop_nData().To_Int();
                        int playerIndex = OnlinePlayers.FindIndex(ply => ply.UID == uID);
                        int mapID = packet.Pop_nData().To_Int();
                        OnlinePlayers[playerIndex].CurrentMap = mapID;
                        Console.WriteLine(string.Format("uID[{0}]: Switching to MapID<{1}>", mapID));

                    }
                    catch (ArgumentNullException e)
                    {
                        Console.WriteLine("Couldn't find player in list!");
                    }
                    break;
                case PacketType.HitboxCreate:
                    //Hitbox Spawned
                    uID = packet.Pop_nData().To_Int();
                    int pIndex = OnlinePlayers.FindIndex(ply => ply.UID == uID);
                    Vector3 pos = new Vector3(packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float());
                    //Console.WriteLine(string.Format("HitboxCreated: uID[{0}] at Pos{1}", uID, pos));

                    //Inform rest of players in map
                    Packet hitboxPacket = new Packet(PacketType.HitboxCreate);
                    hitboxPacket.Push_nData(new NetData(uID));
                    hitboxPacket.Push_nData(new NetData(pos.x));
                    hitboxPacket.Push_nData(new NetData(pos.y));
                    hitboxPacket.Push_nData(new NetData(pos.z));
                    hitboxPacket.Push_nData(new NetData(packet.Pop_nData().To_Float()));
                    hitboxPacket.Push_nData(new NetData(packet.Pop_nData().To_Float()));

                    Send_All(OnlinePlayers[pIndex].CurrentMap, hitboxPacket);
                    break;
                case PacketType.ProjectileCreate:
                    //Hitbox Spawned
                    //Inform rest of players in map
                    uID = packet.Pop_nData().To_Int();
                    int pProjIndex = OnlinePlayers.FindIndex(ply => ply.UID == uID);
                    Vector3 proj_pos = new Vector3(packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float());
                    Vector3 proj_dir = new Vector3(packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float(), packet.Pop_nData().To_Float());
                    //Console.WriteLine(string.Format("ProjectileCreated: uID[{0}] at Pos{1} Direction{2}", uID, proj_pos, proj_dir));


                    //Inform rest of players in map
                    Packet projPacket = new Packet(PacketType.ProjectileCreate);
                    projPacket.Push_nData(new NetData(uID));
                    projPacket.Push_nData(new NetData(proj_pos.x));
                    projPacket.Push_nData(new NetData(proj_pos.y));
                    projPacket.Push_nData(new NetData(proj_pos.z));
                    projPacket.Push_nData(new NetData(proj_dir.x));
                    projPacket.Push_nData(new NetData(proj_dir.y));
                    projPacket.Push_nData(new NetData(proj_dir.z));
                    projPacket.Push_nData(new NetData(packet.Pop_nData().To_Int()));
                    projPacket.Push_nData(new NetData(packet.Pop_nData().To_Float()));
                    projPacket.Push_nData(new NetData(packet.Pop_nData().To_Float()));

                    Send_All(OnlinePlayers[pProjIndex].CurrentMap, projPacket);
                    break;
                case PacketType.Damage:
                    //Inform rest of players in map
                    break;
                case PacketType.CrowdControl:
                    //Inform rest of players in map
                    break;
                case PacketType.Death:
                    //Inform rest of players in map
                    break;
                case PacketType.ItemAdd:
                    //Query server database
                    break;
                case PacketType.ItemRemove:
                    //Query server database
                    break;
                case PacketType.GetEquipment:
                    //Query server database
                    uID = packet.Pop_nData().To_Int();
                    Packet equipQuery = new Packet(PacketType.SERVER_QueryEquip);
                    equipQuery.Push_nData(new NetData(connection.Address.ToString()));
                    equipQuery.Push_nData(new NetData(uID));
                    BackEnd.Send_ToBackend(equipQuery);
                    break;
                case PacketType.SetEquipment:
                    break;
                case PacketType.GetInventory:
                    //Query server database
                    break;
                case PacketType.CharacterInfo:
                    uID = packet.Pop_nData().To_Int();
                    Packet charQuery = new Packet(PacketType.SERVER_QueryEquip);
                    charQuery.Push_nData(new NetData(uID));
                    BackEnd.Send_ToBackend(charQuery);
                    break;
            }
        }
    }
}
