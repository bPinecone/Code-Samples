using UnityEngine;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System;

//Author: Brian Herman
//Last Updated: 11/17/2015
//Summary: Storage class for primitives. Has helper methods for serialization.

public class NetData
{
    public NetDataType netDataType;
    public int numBytes;
    public byte[] content;

    public NetData() { }

    public NetData(bool b)
    {
        netDataType = NetDataType.nBool;
        numBytes = sizeof(bool);
        content = BitConverter.GetBytes(b);
    }

    public NetData(char c)
    {
        netDataType = NetDataType.nChar;
        numBytes = sizeof(char);
        content = BitConverter.GetBytes(c);
    }

    public NetData(int i)
    {
        netDataType = NetDataType.nInt;
        numBytes = sizeof(Int32);
        byte[] toAdd = BitConverter.GetBytes(i);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(toAdd);
        content = toAdd;
    }

    public NetData(float f)
    {
        netDataType = NetDataType.nFloat;
        numBytes = sizeof(float);
        byte[] toAdd = BitConverter.GetBytes(f);
        if (!BitConverter.IsLittleEndian)
            Array.Reverse(toAdd);
        content = toAdd;
    }

    public NetData(string s)
    {
        if (s.Length > 252) return;

        netDataType = NetDataType.nString;
        numBytes = s.Length;
        content = new byte[s.Length];

        content = Encoding.ASCII.GetBytes(s);
    }

    public NetData(byte[] data)
    {
        netDataType = (NetDataType)data[0];
        numBytes = (int)data[1];
        content = new byte[numBytes];
        for (int i = 2; i < data.Length; i++)
        {
            content[i - 2] = data[i]; 
        }
    }

    public bool To_Bool()
    {
        if (netDataType != NetDataType.nBool)
            throw new Exception("Attempt to access incorrect Data type.");
        return BitConverter.ToBoolean(content, 0);
    }

    public char To_Char()
    {
        if (netDataType != NetDataType.nChar)
            throw new Exception("Attempt to access incorrect Data type.");
        return BitConverter.ToChar(content, 0);
    }

    public int To_Int()
    {
        if (netDataType != NetDataType.nInt)
            throw new Exception("Attempt to access incorrect Data type.");
        return BitConverter.ToInt32(content, 0);
    }

    public float To_Float()
    {
        if (netDataType != NetDataType.nFloat)
            throw new Exception("Attempt to access incorrect Data type.");
        return BitConverter.ToSingle(content,0);
    }

    public string To_String()
    {
        if (netDataType != NetDataType.nString)
            throw new Exception("Attempt to access incorrect Data type.");

        return Encoding.ASCII.GetString(content);
    }
}
