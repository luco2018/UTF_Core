using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;

namespace GraphicsTestFramework
{
	public class ResultsIO : MonoBehaviour
	{
		private static ResultsIO _Instance = null;
		private List<string> suiteBaselinesPullList = new List<string> ();
		private SystemData sysData;
		public bool isWaiting = false;
		[HideInInspector]
		public bool writeLocal = true;
		[HideInInspector]
		public bool writeCloud = true;

		//List of suiteBaselineData for suites
		public List<SuiteBaselineData> _suiteBaselineData = new List<SuiteBaselineData> ();
		//TODO make private once working

		public static ResultsIO Instance {
			get {
				if (_Instance == null)
					_Instance = (ResultsIO)FindObjectOfType (typeof(ResultsIO));
				return _Instance;
			}
		}

		private void Start ()
		{
			//Grab the system data to share around
			sysData = Master.Instance.GetSystemData ();

			if(Master.Instance._sqlMode == SQLmode.Disabled)
				writeCloud = false;

			//setup local IO
			if (LocalIO.Instance == null)
				gameObject.AddComponent<LocalIO> ();
			LocalIO.Instance.Init ();
			//setup SQL IO
			if (SQL.SQLIO.Instance == null)
				gameObject.AddComponent<SQL.SQLIO> ();
			SQL.SQLIO.Instance.Init (sysData);

			if (Master.Instance.appMode == AppMode.TestRunner)
				StartCoroutine (Init ());
		}

		public void Restart ()
		{
			StartCoroutine (Init ());
		}

		public IEnumerator Init ()
		{
			ProgressScreen.Instance.SetState (true, ProgressType.LocalLoad, "Loading local data"); //Show loading screen
			_suiteBaselineData = LocalIO.Instance.ReadLocalBaselines ();// - TODO this needs to get called again at some point

			//Hardcoded wait for SuiteManager to populate - TODO might be cleaner way to do later
			float timeout = 0f;
			while (SuiteManager.GetSuiteNames ().Length == 0) {
				timeout += Time.deltaTime;
				if (timeout > 5f) {
					break;
				}
				yield return new WaitForEndOfFrame ();
			}

			//wait for the SQL connection to be made, timeout after 10seconds
			timeout = 0f;
			do {
				yield return new WaitForEndOfFrame ();
				timeout += Time.deltaTime;
			} while(SQL.SQLIO.Instance.liveConnection == connectionStatus.Internet && timeout < 10f);

			//if timeout reaches 10f then no network

			//fetch suite names from the suite manager
			string[] suiteNames = SuiteManager.GetSuiteNames ();

			if (suiteNames.Length == 0) {//if there are no suites loaded fail - TODO need to add code path for this
				Console.Instance.Write (DebugLevel.Critical, MessageLevel.LogWarning, "No suites loaded in SuiteManager, unable to continue"); // Write to console
			} else {

				//now we check local timestamps vs server to make sure up to date, then add the outdated ones to be pulled
				foreach (string suiteName in suiteNames) {
					Console.Instance.Write (DebugLevel.File, MessageLevel.Log, "Fetching baseline timestamps from cloud");
                    //Get timestamp for suite via SQL
                    DateTime dt = DateTime.MaxValue;
                    StartCoroutine(SQL.SQLIO.Instance.GetbaselineTimestamp(suiteName, (value => { dt = value; })));
					while (dt == DateTime.MaxValue)
                    {
                        yield return null;
                    }
                    if(dt != DateTime.MinValue)//Min value is null(doesnt exist)
						CompareBaselineTimestamps (suiteName, dt.ToString ());
				}

				if (suiteBaselinesPullList.Count > 0) {
                    ResultsIOData[] data = null;
                    StartCoroutine(SQL.SQLIO.Instance.FetchBaselines(suiteBaselinesPullList.ToArray(), sysData.Platform, sysData.API, (value => { data = value; })));
					while(data == null){
                        yield return null;
                    }
                    //ResultsIOData[] data = SQL.SQLIO.Instance.FetchBaselines (suiteBaselinesPullList.ToArray (), sysData.Platform, sysData.API);
                    Console.Instance.Write (DebugLevel.File, MessageLevel.Log, "Cloud baselines pulled, writing local files");
					foreach(ResultsIOData rd in data){
						StartCoroutine (LocalIO.Instance.WriteDataFiles (rd, fileType.Baseline));
					}
					yield return new WaitForSeconds (0.5f);
					_suiteBaselineData = LocalIO.Instance.ReadLocalBaselines ();
					BroadcastBaselineParsed ();
				} else {
					Console.Instance.Write (DebugLevel.Logic, MessageLevel.Log, "No cloud based baselines to pull"); // Write to console
					BroadcastBaselineParsed ();
				}
			}
		}

		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		/// Initial checks
		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Query if specific baseline exists
		/// </summary>
		/// <returns><c>true</c>, if baseline exists, <c>false</c> if not.</returns>
		/// <param name="suiteName">Suite name.</param>
		/// <param name="pipeline">Pipeline.</param>
		/// <param name="testType">Test type.</param>
		/// <param name="sceneName">Scene name.</param>
		/// <param name="testName">Test name.</param>
		public bool BaselineExists (string suiteName, string pipeline, string testType, string sceneName, string testName)
		{
			//check local file for current platform/api baseline
			foreach (SuiteBaselineData SBD in _suiteBaselineData) {
				if (SBD.suiteName == suiteName) {
                    //Suite exists
                    if (SBD.platform == sysData.Platform)
                    {
                        //Platform exists
                        if (SBD.api == sysData.API)
                        {
                            //API matches
                            if (SBD.pipeline == pipeline)
                            {
                                //pipeline exists
                                foreach (SuiteData SD in SBD._suiteData)
                                {
                                    if (SD.testType == testType && SD.sceneName == sceneName && SD.testName == testName)
                                    {
                                        return true;
                                    }
                                }
                            }
                        }
                    }
				}
			}
			return false;
		}

		void CompareBaselineTimestamps (string suiteName, string dateTime)
		{
			System.DateTime cloudTimestamp = System.DateTime.Parse (dateTime);

			if (_suiteBaselineData.Count == 0) {//TODO - shouldnt add this to pull baselines as has issue with iOS trying to pull baselines for OSX
				Console.Instance.Write (DebugLevel.File, MessageLevel.Log, "Putting " + suiteName + " in the pull list"); // Write to console
				suiteBaselinesPullList.Add (suiteName);
			} else {
				int matches = 0;
				foreach (SuiteBaselineData SBD in _suiteBaselineData) {
					if (SBD.suiteName == suiteName && SBD.platform == sysData.Platform && SBD.api == sysData.API) {
						matches++;
						System.DateTime localTimestamp = System.DateTime.ParseExact (SBD.suiteTimestamp, Common.dateTimeFormat, null);
						Console.Instance.Write (DebugLevel.File, MessageLevel.Log, string.Format ("Comparing cloud time {0} vs local time {1}", cloudTimestamp, localTimestamp));

						int timeDiff = cloudTimestamp.CompareTo (localTimestamp);
						if (timeDiff < 0f) {
							Console.Instance.Write (DebugLevel.File, MessageLevel.Log, "Cloud Timestamp is old"); // Write to console
						} else if (timeDiff > 0f) {
							Console.Instance.Write (DebugLevel.File, MessageLevel.Log, "Cloud Timestamp is newer, adding " + suiteName + " to pull list"); // Write to console
							suiteBaselinesPullList.Add (suiteName);
						} else if (timeDiff == 0f) {
							Console.Instance.Write (DebugLevel.File, MessageLevel.Log, "Cloud Timestamp is the same"); // Write to console
						}
					}
				}

				if (matches == 0)
					suiteBaselinesPullList.Add (suiteName);

			}
		}

		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		/// Processing fresh test data
		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Processes the results, saving local txt files and pushing to cloud.
		/// </summary>
		/// <param name="suiteName">Suite name.</param>
		/// <param name="testType">Test type.</param>
		/// <param name="inputData">Input data.</param>
		/// <param name="baseline">Baseline data?</param>
		public IEnumerator ProcessResults (string suiteName, string testType, ResultsIOData inputData, int baseline)
		{
			ProgressScreen.Instance.SetState (true, ProgressType.CloudSave, "Saving local and cloud data");

			inputData.suite = suiteName;
			inputData.testType = testType;
            int uploaded = -1;

			if(writeCloud)
			{
                string sheetName = "Temp";
                if (baseline == 1) //Cloud upload for baseline
					sheetName = suiteName + "_" + testType + "_Baseline";
				else //cloud upload for results
					sheetName = suiteName + "_" + testType + "_Results";
				StartCoroutine(SQL.SQLIO.Instance.AddEntry(inputData, sheetName, baseline, (value) => { uploaded = value; }));
			}else{
                uploaded = 1;
            }

			while(uploaded == -1){
                yield return null;
            }

			if(writeLocal)
			{
				fileType ft = baseline == 1 ? fileType.Baseline : fileType.Result;
				if (uploaded == 1) {//if the entry uploaded fine
					StartCoroutine (LocalIO.Instance.WriteDataFiles (inputData, ft));
				}
			}

			BroadcastEndResultsSave ();
		}

		/// <summary>
		/// Retrieves a file.
		/// </summary>
		/// <returns>The data.</returns>
		/// <param name="suiteName">Suite name.</param>
		/// <param name="testType">Test type.</param>
		/// <param name="inputData">Input data.</param>
		public ResultsIOData RetrieveEntry (string suiteName, string testType, ResultsDataCommon inputData, bool baseline, bool full)
		{
			//string rawJSONdata = LocalIO.Instance.FetchDataFile (suiteName, testType, inputData, true);//fetch string from file
			ResultsIOData data = LocalIO.Instance.FetchDataFile (suiteName, testType, inputData, baseline, full);//JSONHelper.FromJSON (rawJSONdata);//take JSON convert to ResultsIOData //REORG
			return data;
		}

		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		/// In memory suite list
		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		public void ProcessBaselineTimestamp (List<string> objNames, List<string> jsonData)
		{ // TODO- externalize JSON splitting for multiple json arrays from cloudconnector
			if (jsonData [0].Length > 2) {
				jsonData [0] = jsonData [0].Remove (0, 1);
				jsonData [0] = jsonData [0].Remove (jsonData [0].Length - 1, 1);
				string[] separators = new string[] { "},{" };
				string[] splitJson = jsonData [0].Split (separators, System.StringSplitOptions.None);


				for (int i = 0; i < splitJson.Length; i++) {
					string jsonString = splitJson [i];
					if (splitJson.Length > 1) {
						if (i == 0)
							jsonString += "}";
						else if (i == splitJson.Length - 1)
							jsonString = "{" + jsonString;
						else
							jsonString = "{" + jsonString + "}";
					}
					Dictionary<string, string> jsonDic = JSONHelper.JSON_Dictionary (jsonString);//REORG
					if (jsonDic ["api"] == sysData.API && jsonDic ["platform"] == sysData.Platform) {
						CompareBaselineTimestamps (jsonDic ["suiteName"], jsonDic ["suiteTimestamp"]);
					}
				}

			}
			isWaiting = false;
		}

		/// <summary>
		/// Updates the baseline dictionary(base not individual baselines, call this first then BaselineDictionaryEntry).
		/// This has an out of an int for knowing what index the particular suite is located in the list
		/// </summary>
		/// <param name="suite">Suite.</param>
		/// <param name="renderPipe">Render pipe.</param>
		/// <param name="baselineIndex">Baseline index in the list.</param>
		public void UpdateBaselineDictionary (string suite, string renderPipe, out int baselineIndex)
		{
			//Updates suiteListData, we need to find the matching suiteListData or create a new one
			int suiteBaselineDataIndex = -1;
			int suiteIndex = 0;
			foreach (SuiteBaselineData SBD in _suiteBaselineData) {
				if (SBD.platform == sysData.Platform && SBD.suiteName == suite && SBD.api == sysData.API && SBD.pipeline == renderPipe) {
					suiteBaselineDataIndex = suiteIndex;
				}
				suiteIndex++;
			}
			if (suiteBaselineDataIndex == -1) {
				SuiteBaselineData newSBD = new SuiteBaselineData ();
				newSBD.api = sysData.API;
				newSBD.pipeline = renderPipe;
				newSBD.platform = sysData.Platform;
				newSBD.suiteName = suite;
				newSBD.suiteTimestamp = System.DateTime.UtcNow.ToString (Common.dateTimeFormat);
				_suiteBaselineData.Add (newSBD);
				suiteBaselineDataIndex = _suiteBaselineData.Count - 1;
			}
			_suiteBaselineData [suiteBaselineDataIndex].suiteTimestamp = System.DateTime.UtcNow.ToString (Common.dateTimeFormat);
			baselineIndex = suiteBaselineDataIndex;
		}

		/// <summary>
		/// Creates a baseline dictionary entry.
		/// </summary>
		/// <param name="suiteIndex">Suite index.</param>
		/// <param name="testType">Test type.</param>
		/// <param name="sceneName">Scene name.</param>
		/// <param name="testName">Test name.</param>
		/// <param name="dateTime">Date time.</param>
		public void BaselineDictionaryEntry (int suiteIndex, string testType, string sceneName, string testName, string dateTime)
		{

			SuiteData sData = new SuiteData (testType, sceneName, testName, dateTime);
			int check = -1;
			for (int si = 0; si < _suiteBaselineData [suiteIndex]._suiteData.Count; si++) {
				SuiteData SD = _suiteBaselineData [suiteIndex]._suiteData [si];
				if (SD.testType == testType && SD.sceneName == sceneName && SD.testName == testName) {
					_suiteBaselineData [suiteIndex]._suiteData [si] = sData;
					check++;
				}
			}
			if (check == -1) {
				_suiteBaselineData [suiteIndex]._suiteData.Add (sData);
			}
			_suiteBaselineData [suiteIndex].suiteTimestamp = System.DateTime.UtcNow.ToString (Common.dateTimeFormat);
		}

		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		/// LISTENERS
		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------


		public static event Broadcast.EndResultsSave endResultsSave;

		public void BroadcastEndResultsSave ()
		{
			if (endResultsSave != null)
				endResultsSave ();
		}

		public static event Broadcast.LocalBaselineParsed baselinesParsed;

		public void BroadcastBaselineParsed ()
		{
			if (baselinesParsed != null)
				baselinesParsed ();
		}

		//Convert list of strings to ResultsDataCommon
		public ResultsDataCommon GenerateRDC(string[] inputData){
			var common = new ResultsDataCommon(); //blank common data
			BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
			FieldInfo[] commonFields = typeof(ResultsDataCommon).GetFields(bindingFlags);
			for (int cf = 0; cf < commonFields.Length; cf++)
			{
				string value = inputData[cf];
				FieldInfo fieldInfo = common.GetType().GetField(commonFields[cf].Name);
				fieldInfo.SetValue(common, Convert.ChangeType(value, fieldInfo.FieldType));
			}
			return common;
		}

		//Generates a single result from a string array to a ResultsIOData
		public ResultsIOData GenerateRIOD(string[] inputData, string suite, string testType){
			ResultsIOData RD = new ResultsIOData ();
			ResultsIORow row = new ResultsIORow ();
			row.resultsColumn = new List<string> ();
			RD.resultsRow.Add (row);
			RD.suite = suite;
			RD.testType = testType;

			for(int i = 0; i < inputData.Length / 2; i++){
				RD.fieldNames.Add (inputData[i * 2]);
				RD.resultsRow [0].resultsColumn.Add (inputData [(i * 2) + 1]);
			}
			return RD;
		}

	}

	/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	/// Data classes to recieve results from tests
	/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
	[System.Serializable]
	public class ResultsIOData
	{
		public string suite;
		public string testType;
		public bool baseline;
		public List<string> fieldNames = new List<string> ();//string list of fields
		public List<ResultsIORow> resultsRow = new List<ResultsIORow> ();//list of row data
	}

	[System.Serializable]
	public class ResultsIORow
	{
		public List<string> resultsColumn = new List<string> ();//list of column values
	}

	[System.Serializable]
	public class SuiteBaselineData
	{
		public string suiteName;
		public string platform;
		public string api;
		public string pipeline;
		public string suiteTimestamp;
		public List<SuiteData> _suiteData = new List<SuiteData> ();
		//Suite information about baselines
	}

	[System.Serializable]
	public class SuiteData
	{
		public string testType;
		public string sceneName;
		public string testName;
		public string timeStamp;

		public SuiteData (string type, string scene, string name, string time)
		{
			testType = type;
			sceneName = scene;
			testName = name;
			timeStamp = time;
		}
	}

	[System.Serializable]
	public enum fileType
	{
		Result,
		Baseline,
		SuiteData,
		BaseData}
	;

	[System.Serializable]
	public enum connectionStatus
	{
		Server,
		Internet,
		Mobile,
		None}
	;

}