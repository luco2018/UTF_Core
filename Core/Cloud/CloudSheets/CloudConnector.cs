using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Text;

//hmmmm
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public class CloudConnector : MonoBehaviour
{
	// -- Complete the following fields. --
	static string webServiceUrl = "https://script.google.com/macros/s/AKfycby0jbK1THQxiUJmq4kExrf0yFg9vU-WYDDpEWHlM-qgUNFwkFc/exec";
	static string spreadsheetId = "1JIZMzcGhBP31rI_PmZvkiuZ6Lt-A2Zbd-KUeQYpRKJI";
    private string servicePassword = "passcode";
	private float timeOutLimit = 300f;
	public bool usePOST = true;
	// --
	
	private static CloudConnector _Instance;
	public static CloudConnector Instance
	{
		get
		{
			if(_Instance == null)
			{
				_Instance = new GameObject("CloudConnector").AddComponent<CloudConnector>();
			}
			return _Instance;
		}
	}
	
	private UnityWebRequest www;

	void Awake(){
		DontDestroyOnLoad (gameObject);
	}

	public void CreateRequest(Dictionary<string, string> form)
	{
		WWWForm newForm = new WWWForm ();

		newForm.AddField("ssid", spreadsheetId);
		newForm.AddField("pass", servicePassword);

		foreach(string key in form.Keys){
			newForm.AddField (key, form[key]);
		}


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
		StartCoroutine(ExecuteRequest(newForm));
	}
	
	IEnumerator ExecuteRequest(WWWForm postData)
	{
		www.Send();
		
		float elapsedTime = 0.0f;
		
		while (!www.isDone)
		{
			elapsedTime += Time.deltaTime;			
			if (elapsedTime >= timeOutLimit)
			{
				CloudConnectorCore.ProcessResponse("TIME_OUT", elapsedTime);
				break;
			}
			
			yield return null;
		}
		
		if (www.isError)
		{
			CloudConnectorCore.ProcessResponse(CloudConnectorCore.MSG_CONN_ERR + "Connection error after " + elapsedTime.ToString() + " seconds: " + www.error, elapsedTime);
			yield break;
		}	
		Debug.Log ("Cloud connector is recieving=" + www.downloadHandler.text);
		CloudConnectorCore.ProcessResponse(www.downloadHandler.text, elapsedTime);
	}
	
}

	