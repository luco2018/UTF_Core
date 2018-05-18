using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GraphicsTestFramework;

public class CommonEditor : MonoBehaviour {

    //Opens the persistent path
    [MenuItem("UTF/Open Persistent Path")]
    public static void OpenPersistentPath()
    {
        EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
