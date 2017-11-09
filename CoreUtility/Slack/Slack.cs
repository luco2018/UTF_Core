using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MiniJSON;

namespace GraphicsTestFramework
{
    public class Slack : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables

        // Singleton
        private static Slack _Instance = null;
        public static Slack Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (Slack)FindObjectOfType(typeof(Slack));
                return _Instance;
            }
        }

        Message currentMessage;

        public void SetMessage(Message input)
        {
            currentMessage = input;
        }

        public bool enableSlackIntegration;

        string _webhookUrl = "https://hooks.slack.com/services/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";
        Dictionary<string, object> _defaultOption;

        float _lastSend;

        public IEnumerator SendSlack(Message message, bool isChannel)
        {
            if(enableSlackIntegration)
            {
                string option = "{\"username\":\"Test Bud\",\"icon_emoji\":\":chicken:\",\"attachments\":[";
                for (int i = 0; i < message.attachments.Length; i++)
                    option += CreateAttachement(message.attachments[i]);
                option += "]}";
                var slack = new Slack(option);
                yield return slack.Post((isChannel ? "<!channel>" : "") + message.message);
                currentMessage = null;
            }
        }

        public IEnumerator SendSlackResults()
        {
            if (enableSlackIntegration)
            {
                GetSlackResultsMessage();
                while (currentMessage == null)
                    yield return null;
                yield return SendSlack(currentMessage, false);
            }
        }

        [Serializable]
        public class Message
        {
            public string message;
            public Attachment[] attachments;

            public Message(string inputMessage, Attachment[] inputAttachments)
            {
                message = inputMessage;
                attachments = inputAttachments;
            }
        }

        [Serializable]
        public class Attachment
        {
            public string text;
            public bool passFail;

            public Attachment (string inputText, bool inputPassFail)
            {
                text = inputText;
                passFail = inputPassFail;
            }
        }

        public string CreateAttachement(Attachment input)
        {
            return "{\"color\":\"" + (input.passFail ? "#00793c" : "#ff0000") + "\"\"text\":\"" + input.text + "\"}";
        }

        public Slack(string optionJson = null)
        {
            if (optionJson == null)
                _defaultOption = new Dictionary<string, object>();
            else
                _defaultOption = Json.Deserialize(optionJson) as Dictionary<string, object>;
        }

        public IEnumerator Post(string message)
        {
            //_lastSend = Time.realtimeSinceStartup;
            var headers = new Dictionary<string, string>();
            headers.Add("Content-Type", "application/x-www-form-urlencoded");
            var form = new WWWForm();

            if (_defaultOption.ContainsKey("text"))
            {
                _defaultOption["text"] = message;
            }
            else
            {
                _defaultOption.Add("text", message);
            }
            form.AddField("payload", Json.Serialize(_defaultOption));

            var www = new WWW(_webhookUrl, form.data, headers);
            yield return www;
            Debug.LogFormat("slack result {0}", www.text);
        }

        public float lastSend
        {
            get { return _lastSend; }
        }

        public void GetSlackResultsMessage()
        {
            List<Attachment> attachments = new List<Attachment>();
            Attachment attachment = new Attachment("", false);
            string previousSuiteName = "";
            int localTestCount = 0;
            int localFailCount = 0;
            int globalTestCount = 0;
            int globalFailCount = 0;
            bool resultsFound = false;
            for (int i = 0; i < TestRunner.Instance.runner.tests.Count; i++)
            {
                TestEntry currentEntry = TestRunner.Instance.runner.tests[i];
                if (currentEntry.suiteName != previousSuiteName)
                {
                    previousSuiteName = currentEntry.suiteName;
                    if (resultsFound == true)
                    {
                        attachment.text += "Ran " + localTestCount.ToString() + ", Passed " + (localTestCount - localFailCount).ToString() + ", Failed " + localFailCount.ToString();
                        attachments.Add(attachment);
                        globalTestCount += localTestCount;
                        globalFailCount += localFailCount;
                        localTestCount = 0;
                        localFailCount = 0;
                    }
                    attachment = new Attachment(currentEntry.suiteName + ": ", true);
                }
                ResultsDataCommon common = BuildResultsDataCommon(currentEntry.groupName, currentEntry.testName); // Build results data common to retrieve results
                ResultsIOData data = ResultsIO.Instance.RetrieveEntry(currentEntry.suiteName, currentEntry.typeName, common, false, false); // Retrieve results data
                if (data != null)
                {
                    resultsFound = true;
                    localTestCount++;
					//search for PassFail field to avoid hardcoding
					int passFailIndex = -1;
					for(int f = 0; f < data.fieldNames.Count; f++){
						if (data.fieldNames [f] == "PassFail")
							passFailIndex = f;
					}
					if (data.resultsRow[0].resultsColumn[passFailIndex] == "False")
                    {
                        localFailCount++;
                        attachment.passFail = false;
                    }
                }
            }
            if(TestRunner.Instance.runner.tests.Count > 0)
            {
                attachment.text += "Ran " + localTestCount.ToString() + ", Passed " + (localTestCount - localFailCount).ToString() + ", Failed " + localFailCount.ToString();
                attachments.Add(attachment);
                globalTestCount += localTestCount;
                globalFailCount += localFailCount;
            }
            SystemData sysData = Master.Instance.GetSystemData();
            string message = "*UTF completed run. Ran " + globalTestCount.ToString() + ", Passed " + (globalTestCount - globalFailCount).ToString() + ", Failed " + globalFailCount.ToString() +"*" + Environment.NewLine + "_" + sysData.Device + "_" + Environment.NewLine + "_" + sysData.Platform + " - " + sysData.API + "_" + Environment.NewLine + "_" + sysData.UnityVersion + "_";
            Attachment[] attachmentArray = attachments.ToArray();
            currentMessage = new Message(message, attachmentArray);
        }

        ResultsDataCommon BuildResultsDataCommon(string sceneName, string testName)
        {
            ResultsDataCommon common = new ResultsDataCommon();
            SystemData systemData = Master.Instance.GetSystemData();
            common.Platform = systemData.Platform;
            common.API = systemData.API;
            common.RenderPipe = "Standard Legacy"; // TODO - Implement SRP support
            common.GroupName = sceneName;
            common.TestName = testName;
            return common;
        }
    }

}
