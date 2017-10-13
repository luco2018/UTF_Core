#if UNITY_EDITOR
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine.Networking;

public class CloudConnectorEditor
{
	// -- Complete the following fields. --
	private static string webServiceUrl = "https://script.google.com/macros/s/AKfycby0jbK1THQxiUJmq4kExrf0yFg9vU-WYDDpEWHlM-qgUNFwkFc/exec";
	private static string spreadsheetId = "1JIZMzcGhBP31rI_PmZvkiuZ6Lt-A2Zbd-KUeQYpRKJI"; // If this is a fixed value could also be setup on the webservice to save POST request size.
	private static string servicePassword = "passcode";
	private static float timeOutLimit = 300f;
	private static bool usePOST = true;
	// --

	private static UnityWebRequest www;
	private static double elapsedTime = 0.0f;
	private static double startTime = 0.0f;
	
	public static void CreateRequest(Dictionary<string, string> form)
	{
		WWWForm newForm = new WWWForm ();

		newForm.AddField("ssid", spreadsheetId);
		newForm.AddField("pass", servicePassword);

		foreach(string key in form.Keys){
			newForm.AddField (key, form[key]);
		}

		EditorApplication.update += EditorUpdate;

		if (usePOST)
		{
			CloudConnectorCore.UpdateStatus("Establishing Connection at URL " + webServiceUrl);
			www = UnityWebRequest.Post(webServiceUrl, newForm);
		}
		else // Use GET.
		{
			string urlParams = "?";
			foreach (KeyValuePair<string, string> item in form)
			{
				urlParams += item.Key + "=" + item.Value + "&";
			}
			CloudConnectorCore.UpdateStatus("Establishing Connection at URL " + webServiceUrl + urlParams);
			www = UnityWebRequest.Get(webServiceUrl + urlParams);
		}
		
		startTime = EditorApplication.timeSinceStartup;
		www.Send();
	}

	static void EditorUpdate()
	{
		while (!www.isDone)
		{
			elapsedTime = EditorApplication.timeSinceStartup - startTime;
			if (elapsedTime >= timeOutLimit)
			{
				CloudConnectorCore.ProcessResponse("TIME_OUT", (float)elapsedTime);
				EditorApplication.update -= EditorUpdate;
			}
			return;
		}
		
		if (www.isError)
		{
			CloudConnectorCore.ProcessResponse(CloudConnectorCore.MSG_CONN_ERR + "Connection error after " + elapsedTime.ToString() + " seconds: " + www.error, (float)elapsedTime);
			return;
		}
		//Debug.Log ("Cloud connector is receiving " + www.downloadHandler.text);
		CloudConnectorCore.ProcessResponse(www.downloadHandler.text, (float)elapsedTime);
		
		EditorApplication.update -= EditorUpdate;
	}
}
#endif