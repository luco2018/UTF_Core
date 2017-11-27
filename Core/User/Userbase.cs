using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Encrypto;
using GraphicsTestFramework;

[System.Serializable]
public class UserInfomation
{
    public object userFile;
    public string masterUsename;
}

public class Userbase : MonoBehaviour
{
    void Start()
    {
        string encrypted = Common.EncryptPassword("Test123!£$");
        Debug.Log(Common.EncryptPassword("Test123!£$"));
        Debug.Log(Common.DecryptPassword(encrypted));
    }
}
