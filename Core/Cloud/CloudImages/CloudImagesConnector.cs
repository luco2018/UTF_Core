using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Text;

public class CloudImagesConnector : MonoBehaviour
{



    // -- Complete the following fields. --
    private string webServiceUrl = "https://script.google.com/macros/s/AKfycbyUY0KjqbXLI0saSp3ZLPQh9wxwmP2bPujStiGTEXQg5VIBOEc/exec";
    private float timeOutLimit = 300f;
    private bool debugMode = true;
    public int jpgQuality = 90;
    // JPG quality to encode with, 1..100.
    // --

    public static bool isWaiting = false;
    public static int responseCount = 0;
    private string currentStatus = "";

    private static CloudImagesConnector _Instance;

    public static CloudImagesConnector Instance
    {
        get
        {
            if (_Instance == null)
            {
                GameObject go = new GameObject("CloudImagesConnector");
                _Instance = go.AddComponent<CloudImagesConnector>();
            }

            return _Instance;
        }
    }

    // Suscribe to this event if want to handle the response as it comes.
    public class CallBackEventRaw : UnityEvent<string, string>
    {

    }

    public CallBackEventRaw responseCallback = new CallBackEventRaw();

    private const string MSG_CONN_ERR = "CONN_ERROR";
    private const string MSG_TIME_OUT = "TIME_OUT";
    private const string MSG_MISS_PARAM = "MISSING_PARAM";
    private const string MSG_IMG_SAVED = "IMAGE_SAVED";
    private const string MSG_TXT_SAVED = "DATA_SAVED";
    private const string MSG_IMG_SENT = "IMAGE_";
    private const string MSG_TXT_SENT = "DATA_";
	private const string MSG_MTXT_SENT = "MDATA_";
    private const string MSG_NAME_SEPARATOR = "_FILE_NAME_";

    private static List<WWWForm> FormsToSend = new List<WWWForm>();
    private bool pending = false;

    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {

        while (FormsToSend.Count > 0 && pending == false)
        {
            pending = true;
            Instance.StartCoroutine(Instance.ExecuteRequest(FormsToSend[0]));
            FormsToSend.RemoveAt(0);
        }

    }

    public static void RequestImage(string imgId)
    {
        isWaiting = true;
        responseCount++;
        WWWForm form = new WWWForm();
        form.AddField("rtype", "imgRequest");
        form.AddField("imgid", imgId);

        Instance.StartCoroutine(Instance.ExecuteRequest(form));
    }

    public static void RequestTxt(string name)
    {
        isWaiting = true;
        responseCount++;
        WWWForm form = new WWWForm();
        form.AddField("rtype", "txtRequest");
        form.AddField("name", name + ".png");

        //FormsToSend.Add(form);
        Instance.StartCoroutine(Instance.ExecuteRequest(form));
    }

	public static void RequestMultiTxt(string[] names)
	{
		Debug.Log ("fetching " + names.Length + " images");
		isWaiting = true;
		responseCount++;
		WWWForm form = new WWWForm();
		form.AddField("rtype", "mtxtRequest");
		form.AddField ("num", names.Length.ToString ());
		int i = 0;

		foreach (string n in names) {
			form.AddField ("name" + i.ToString (), names[i] + ".png");
			i++;
		}

		Instance.StartCoroutine(Instance.ExecuteRequest(form));
	}

    public static void PersistImage(Texture2D texture, string name)
    {
        isWaiting = true;
        responseCount++;
        WWWForm form = new WWWForm();
        form.AddField("rtype", "imgPersist");

        form.AddField("mimetype", "image/png");
        form.AddField("imgdata", System.Convert.ToBase64String(texture.EncodeToPNG()));
        form.AddField("name", name + ".png");

        Instance.StartCoroutine(Instance.ExecuteRequest(form));
    }

    public static void PersistText(string data, string name)
    {
        isWaiting = true;
        responseCount++;
        WWWForm form = new WWWForm();
        form.AddField("rtype", "txtPersist");

        form.AddField("mimetype", "text/plain");
        form.AddField("data", data);
        form.AddField("name", name + ".png");

        Instance.StartCoroutine(Instance.ExecuteRequest(form));
    }

    IEnumerator ExecuteRequest(WWWForm postData)
    {
        UpdateStatus("Establishing Connection at URL " + webServiceUrl);

        UnityWebRequest www = UnityWebRequest.Post(webServiceUrl, postData);
        www.Send();

        float elapsedTime = 0.0f;

        while (!www.isDone)
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= timeOutLimit)
            {
                ProcessResponse(MSG_TIME_OUT, "");
                break;
            }

            yield return null;
        }

        if (www.isError)
        {
            ProcessResponse(MSG_CONN_ERR + www.error, elapsedTime.ToString());
            yield break;
        }

        ProcessResponse(www.downloadHandler.text, elapsedTime.ToString());
    }

    void ProcessResponse(string response, string time)
    {
        string responseType = MSG_IMG_SAVED;

        if (response.StartsWith(MSG_IMG_SAVED))
        {
            string fileId = response.Substring(MSG_IMG_SAVED.Length);
            fileId = fileId.Remove(fileId.IndexOf(MSG_NAME_SEPARATOR));
            string fileName = response.Substring(response.IndexOf(MSG_NAME_SEPARATOR));
            fileName = fileName.TrimStart(MSG_NAME_SEPARATOR.ToCharArray());

            UpdateStatus("Image successfully persisted as " + fileName + " after " + time + " seconds. Image Id: " + fileId);
            response = fileId;
        }

        if (response.StartsWith(MSG_IMG_SENT))
        {
            UpdateStatus("Image successfully retrieved.");
            responseType = MSG_IMG_SENT;
            response = response.TrimStart(MSG_IMG_SENT.ToCharArray());
        }

        if (response.StartsWith(MSG_TXT_SAVED))
        {
            string fileId = response.Substring(MSG_TXT_SAVED.Length);
            fileId = fileId.Remove(fileId.IndexOf(MSG_NAME_SEPARATOR));
            string fileName = response.Substring(response.IndexOf(MSG_NAME_SEPARATOR));
            fileName = fileName.TrimStart(MSG_NAME_SEPARATOR.ToCharArray());

            UpdateStatus("Txt successfully persisted as " + fileName + " after " + time + " seconds. Txt Id: " + fileId);
            response = fileId;

            responseType = MSG_TXT_SAVED;
        }

        if (response.StartsWith(MSG_TXT_SENT))
        {
            responseType = MSG_TXT_SENT;
            response = response.TrimStart(MSG_TXT_SENT.ToCharArray());
            UpdateStatus("Txt successfully retrieved." + response);
        }

		if (response.StartsWith(MSG_MTXT_SENT))
		{
			responseType = MSG_MTXT_SENT;
			response = response.TrimStart(MSG_MTXT_SENT.ToCharArray());
			UpdateStatus("Txts successfully retrieved." + response);
		}

        if (response.StartsWith(MSG_CONN_ERR))
        {
            UpdateStatus("Connection error after " + time + " seconds: \n" + response.TrimStart(MSG_CONN_ERR.ToCharArray()));
        }

        if (response.StartsWith(MSG_TIME_OUT))
        {
            UpdateStatus("Operation timed out, connection aborted. Check your internet connection and try again.");
        }

        if (response.StartsWith(MSG_MISS_PARAM))
        {
            UpdateStatus("Parsing Error: Missing parameters.");
        }

        responseCallback.Invoke(responseType, response);
        responseCount--;
        pending = false;
        //Debug.Log (responseCount);
        isWaiting = false;
    }

    void UpdateStatus(string status)
    {
        currentStatus = status;

        if (debugMode)
            Debug.Log(currentStatus);
    }

}
