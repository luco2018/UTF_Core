using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[System.Serializable]
public class UserInfomation
{
    public object userFile;
    public string masterUsename;
}

public class Userbase : MonoBehaviour
{
    const string keys = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz";
    private void Start()
    {
        EncryptString("Matt");

    }

    internal Dictionary<char, string> EncryptString(string T)
    {
        Dictionary<char, string> eArray = new Dictionary<char, string>();
        string encrypted = "";
        foreach (char F in T)
        {
            eArray.Add(char.Parse(GetNewKey(eArray)), ((byte)F).ToString());

        }

        foreach (KeyValuePair<char, string> F in eArray)
        {
            encrypted += F.Key.ToString() + F.Value;
        }
        Debug.Log(T + " as encrypted: " +  encrypted);
        Debug.Log(encrypted + "as decrypted: " + DecryptString(encrypted));
        return eArray;
    }

    protected string GetNewKey(Dictionary<char, string> dict)
    {
        string availableKeys = "";
        foreach (char F in keys)
        {
            if (!dict.ContainsKey(F))
            {
                availableKeys += F.ToString();
            }
        }
        string key = availableKeys[Random.Range(0, availableKeys.Length)].ToString();

        return key;
    }

    internal string DecryptString(string T)
    {

        string result = "";
        string buffer = "";

        //foreach (byte b in System.Text.Encoding.UTF8.GetBytes(T.ToCharArray()))
        //    UnityEngine.Debug.Log(b.ToString());

        foreach (char F in T)
        {

            if (char.IsLetter(F))
            {
                if (buffer.Length > 0)
                {
                    int code = int.Parse(buffer);
                    //byte[] by = Encoding.ASCII.GetBytes(buffer);
                    result += System.Convert.ToChar(code).ToString();
                }
                buffer = "";
            }
            else
            {
                buffer += F.ToString();
            }
        }

        if (buffer.Length > 0)
        {
            int code = int.Parse(buffer);
            //byte[] by = Encoding.ASCII.GetBytes(buffer);
            result += System.Convert.ToChar(code).ToString();
        }

        return result;
    }

    internal string ToHexString(byte[] hex)
    {
        if (hex == null) return null;
        if (hex.Length == 0) return string.Empty;

        var s = new StringBuilder();
        foreach (byte b in hex)
        {
            s.Append(b.ToString("x2"));
        }
        return s.ToString();
    }

    internal byte[] ToHexBytes(string hex)
    {
        if (hex == null) return null;
        if (hex.Length == 0) return new byte[0];

        int l = hex.Length / 2;
        var b = new byte[l];
        for (int i = 0; i < l; ++i)
        {
            b[i] = System.Convert.ToByte(hex.Substring(i * 2, 2), 16);
        }
        return b;
    }

    internal bool EqualsTo(byte[] bytes, byte[] bytesToCompare)
    {
        if (bytes == null && bytesToCompare == null) return true; // ?
        if (bytes == null || bytesToCompare == null) return false;
        if (object.ReferenceEquals(bytes, bytesToCompare)) return true;

        if (bytes.Length != bytesToCompare.Length) return false;

        for (int i = 0; i < bytes.Length; ++i)
        {
            if (bytes[i] != bytesToCompare[i]) return false;
        }
        return true;
    }

}
