using System.Collections;
using System.Collections.Generic;
using System;

//Author: Brian Herman
//Last Updated: 11/17/2015
//Summary: Data storage for NetData objects. Has helper methods for data storage and serialization.

public class Packet
{
    public PacketType packet_type;
    private List<NetData> dataObjs;

    private Packet() { packet_type = PacketType.NULL; }

    public Packet(PacketType p)
    {
        packet_type = p;
        dataObjs = new List<NetData>();
    }

    #region Add/Remove data
    public void Push_nData(NetData data)
    {
        dataObjs.Add(data);
    }

    public NetData Pop_nData()
    {
        NetData tmp;
        if (dataObjs.Count > 0)
        {
            tmp = dataObjs[0];
            dataObjs.RemoveAt(0);
        }
        else tmp = null;
        return tmp;
    }

    //Optional : Add NetData Objects to Packet
    public static Packet operator +(Packet p1, NetData n)
    {
        p1.Push_nData(n);
        return p1;
    }
    #endregion

    #region Serialize/Deserialize
    public Packet(byte[] data)
    {
        dataObjs = new List<NetData>();

        packet_type = (PacketType)data[0];
        int iterator = 1;
        while (iterator < data.Length)
        {
            //NetDataType d = (NetDataType)data[iterator];
            int dLen = (int)data[iterator + 1];

            byte[] nObjData = new byte[dLen + 2];

            //from iterator to dLen+2
            for (int i = iterator; i < iterator + dLen + 2; i++)
            {
                nObjData[i - (iterator)] = data[i];
            }

            NetData nObj = new NetData(nObjData);
            dataObjs.Add(nObj);

            iterator += dLen + 2;
        }
    }


    public byte[] ToStream()
    {
        int packetSize = 1;
        foreach (var obj in dataObjs)
        {
            packetSize += 2;
            packetSize += obj.numBytes;
        }
        byte[] serializedPacket = new byte[packetSize];
        serializedPacket[0] = (byte)packet_type;
        int iterator = 1;
        foreach (var obj in dataObjs)
        {
            serializedPacket[iterator] = (byte)obj.netDataType;
            iterator++;
            serializedPacket[iterator] = (byte)obj.numBytes;
            iterator++;
            for (int i = 0; i < obj.numBytes; i++)
            {
                serializedPacket[iterator] = obj.content[i];
                iterator++;
            }
        }

        return serializedPacket;
    }
    #endregion

}
