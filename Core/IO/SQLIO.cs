using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using System.Net;
using System.IO;
using GraphicsTestFramework;

namespace GraphicsTestFramework.SQL
{	
	public static class SQLIO {

		// public static SQLIO _Instance = null;//Instance
		// public static SQLIO Instance {
		// 	get {
		// 		if (_Instance == null)
		// 			_Instance = (SQLIO)FindObjectOfType (typeof(SQLIO));
		// 		return _Instance;
		// 	}
		// }

        // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //CONNECTION VARIABLES
		public static string _webservice;// web service address
        public readonly static string _liveWebservice =		"http://ec2-35-176-162-233.eu-west-2.compute.amazonaws.com/UTFFunctions.php";// web service for live
		public readonly static string _stagingWebserver =	"http://ec2-35-176-162-233.eu-west-2.compute.amazonaws.com/UTFFunctions_staging.php";// web service for staging
        private readonly static string _pass = 				"f23-95j-vCt";// Basic security(laughable actually)

        // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //LOCAL VARIABLES
        public static connectionStatus liveConnection;
		private static NetworkReachability netStat = NetworkReachability.NotReachable;
        private static IEnumerator _currentIenumerator;
        private static IEnumerator _currentSubIenumerator;

        //Query retry list - TODO - not hooked up or solved yet
        private static List<QueryBackup> SQLNonQueryBackup = new List<QueryBackup>();

		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		//INFORMATION
		private static SystemData sysData;//local version of systemData
        private static int dumpIndex = 0;

        public static void Init(SystemData _sysData)// Initialization function, this sets up the needed information - TODO - make it work without being called
        {
            sysData = _sysData;

			//setup staging
			if(Master.Instance._sqlMode == SQLmode.Live || Master.Instance._sqlMode == SQLmode.Disabled)
                _webservice = _liveWebservice;
			else if (Master.Instance._sqlMode == SQLmode.Staging || Master.Instance._sqlMode == SQLmode.DisabledStaging)
                _webservice = _stagingWebserver;
        }

		public static void Update()
		{
			if(_currentIenumerator != null)
			{
				if(!_currentIenumerator.MoveNext())
				{
                    _currentIenumerator = null;
                }
			}
        }

		public static void StartCoroutine(IEnumerator f)
		{
            _currentIenumerator = f;
        }

		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Query methods - TODO wip
		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public static IEnumerator SQLNonQuery(string _query, Action<int> callback)
        {
			Console.Instance.Write(DebugLevel.File, MessageLevel.Log, "SQL nonquery:" + _query); // Write to console
            List<IMultipartFormSection> form = new List<IMultipartFormSection>();
            form.Add(new MultipartFormDataSection("type", "nonQuery"));
			form.Add(new MultipartFormDataSection("pass", _pass));
            form.Add(new MultipartFormDataSection("query", _query));
            UnityWebRequest www = UnityWebRequest.Post(_webservice, form); //POST data is sent via the URL

#if !UNITY_2018_1_OR_NEWER
            yield return www.Send();
			while(!www.isDone && www.error == null) yield return null;
#else
            UnityWebRequestAsyncOperation wwwData = www.SendWebRequest();
			while(!wwwData.isDone){
                yield return null;
            }
            www = wwwData.webRequest;
#endif

            if (string.IsNullOrEmpty(www.error))
            {
                Console.Instance.Write(DebugLevel.File, MessageLevel.Log, "SQL response:" + www.downloadHandler.text); // Write to console
                if(Console.Instance._SQLDebugDump)
				{
                    DebugDump(_query, www.downloadHandler.text);
                }
				callback(1);
            }
            else
            {
				if(www.downloadHandler.text.Length < 256)
                	Console.Instance.Write(DebugLevel.File, MessageLevel.LogWarning, "SQL response:" + www.downloadHandler.text); // Write to console
				else
					Console.Instance.Write(DebugLevel.File, MessageLevel.LogWarning, "SQL response:Length too long=" + www.downloadHandler.text.Length); // Write to console
                if (Console.Instance._SQLDebugDump)
                {
                    DebugDump(_query, www.downloadHandler.text);
                }
				callback(-1);
            }
        }
		
		public static IEnumerator SQLRequest(string _query, Action<RawData> data)
        {
			Console.Instance.Write(DebugLevel.File, MessageLevel.Log, "SQL query:" + _query); // Write to console
            List<IMultipartFormSection> form = new List<IMultipartFormSection>();
            form.Add(new MultipartFormDataSection("type", "request"));
			form.Add(new MultipartFormDataSection("pass", _pass));
            form.Add(new MultipartFormDataSection("query", _query));
            UnityWebRequest www = UnityWebRequest.Post(_webservice, form); //POST data is sent via the URL

#if !UNITY_2018_1_OR_NEWER
            yield return www.Send();
			while(www.downloadProgress < 1) yield return null;
#else
            UnityWebRequestAsyncOperation wwwData = www.SendWebRequest();
            while (!wwwData.isDone)
            {
                yield return null;
            }
            www = wwwData.webRequest;
#endif

            if (string.IsNullOrEmpty(www.error))
            {
				if(www.downloadHandler.text != "Null" && www.downloadHandler.text != "0")
				{
                    if (www.downloadHandler.text.Length < 256)
                        Console.Instance.Write(DebugLevel.File, MessageLevel.LogWarning, "SQL response:" + www.downloadHandler.text); // Write to console
                    else
                        Console.Instance.Write(DebugLevel.File, MessageLevel.LogWarning, "SQL response:Length too long=" + www.downloadHandler.text.Length); // Write to console
                    if (Console.Instance._SQLDebugDump)
                    {
                        DebugDump(_query, www.downloadHandler.text);
                    }
					data(ConvertRawData(www.downloadHandler.text));
				}
				else
				{
                    Console.Instance.Write(DebugLevel.File, MessageLevel.LogWarning, "SQL response:" + www.downloadHandler.text); // Write to console
					RawData _data = new RawData();
					_data.fields.Add("Null");
                    if (Console.Instance._SQLDebugDump)
                    {
                        DebugDump(_query, www.downloadHandler.text);
                    }
					data(_data);
				}
            }
            else
            {
                Console.Instance.Write(DebugLevel.File, MessageLevel.LogError, www.error);
                if (Console.Instance._SQLDebugDump)
                {
                    DebugDump(_query, www.downloadHandler.text);
                }
				data(null);
            }
        }

		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Query data
		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		//Gets the timestamp from the server, if none return DateTime.MinValue to cause repull regardless
		public static IEnumerator GetbaselineTimestamp(string suiteName, Action<DateTime> outdata){
			DateTime timestamp = DateTime.MinValue;//make date time min, we check this on the other end since it is not nullable
            RawData _rawData = null;//RawData to be filled by the request
            string query = String.Format("SELECT suiteTimestamp FROM SuiteBaselineTimestamps WHERE api='{0}' AND suiteName='{1}' AND platform='{2}';", sysData.API, suiteName, sysData.Platform);//This line sends a query to get timestamps for matching API/platform/suite

			IEnumerator i = SQLRequest(query, (value => { _rawData = value; }));
			while(i.MoveNext()) yield return null;

            if(_rawData.data.Count != 0)
            	timestamp = System.DateTime.Parse(_rawData.data[0][0]);//convert the string to a timestamp

            outdata(timestamp);
        }

		//fetch the server side baselines by providing the suites, pltform and API, later will have to change this to allow more strict baseline matching <TODO
		public static IEnumerator FetchBaselines(Suite[] suites, string platform, string api, Action<ResultsIOData[]> outdata){
			List<ResultsIOData> data = new List<ResultsIOData> ();//ResultsIOData to send back to resultsIO for local processing
			List<string> tables = new List<string>();
			//Get the table names to pull baselines from
			foreach(Suite suite in suites){
                string[] tbls = null;
                IEnumerator i = FetchBaseLineTables(suite.suiteName, (value => { tbls = value; }));
				while (i.MoveNext()) yield return null;
                tables.AddRange(tbls);
            }
			int n = 0;
			foreach(string table in tables){
				string suite = table.Substring (0, table.IndexOf ("_"));//grab the suite from the table name
				string testType = table.Substring (table.IndexOf ("_") + 1, table.LastIndexOf ("_") - (suite.Length + 1));//grab the test type from the table name
                data.Add (new ResultsIOData());
				data [n].suite = suite;
				data [n].testType = testType;
                foreach (Group grp in SuiteManager.GetSuiteByName(suite).groups)
                {
					ProgressScreen.Instance.SetState(true, ProgressType.CloudLoad, "Fetching Baselines\n" + data[n].suite + " | " + grp.groupName);
                    //This line controls how baselines are selected, right now only Platform and API are unique
                    string query = String.Format("SELECT * FROM {0} WHERE platform='{1}' AND api='{2}' AND groupname='{3}'", table, platform, api, grp.groupName);
                    RawData _rawData = new RawData();
                    
                    IEnumerator i = SQLRequest(query, (value => { _rawData = value; }));
                    while (i.MoveNext()) yield return null;
                    
					if(_rawData.fields.Count == 0 || _rawData.fields[0] == "Null")
                        continue;
                    else if (data[n].fieldNames.Count == 0)
                    	data[n].fieldNames.AddRange(_rawData.fields);//Grab the fields from the RawData
                    for (int x = 0; x < _rawData.data.Count; x++)
                    {
                        ResultsIORow row = new ResultsIORow();//create a new row
                        row.resultsColumn.AddRange(_rawData.data[x]);//store the current row of values
                        data[n].resultsRow.Add(row);//add it to the data to send back to resultsIO
                    }
					if (data[n].fieldNames.Count == 0)
                        data.RemoveAt(n);
                }
				n++;
			}
			outdata(data.ToArray ());
		}

		//Fetches a single baseline based of deets
		public static IEnumerator FetchBaseline(ResultsIOData inputData, Action<ResultsIOData> outdata)
        {
            ResultsIOData data = new ResultsIOData();//ResultsIOData to send back to resultsIO for local processing
            TableStrings ts = new TableStrings(inputData.suite, inputData.testType, true);
            string table = TableStringToStrings(ts);

			data.suite = inputData.suite;
			data.testType = inputData.testType;

            string platform = inputData.resultsRow[0].resultsColumn[inputData.fieldNames.FindIndex(x => x =="Platform")];
			string api = inputData.resultsRow[0].resultsColumn[inputData.fieldNames.FindIndex(x => x == "API")];
			string group = inputData.resultsRow[0].resultsColumn[inputData.fieldNames.FindIndex(x => x == "GroupName")];
            string test = inputData.resultsRow[0].resultsColumn[inputData.fieldNames.FindIndex(x => x == "TestName")];
            //This line controls how baselines are selected, right now only Platform and API are unique
            string query = String.Format("SELECT * FROM {0} WHERE platform='{1}' AND api='{2}' AND groupname='{3}' AND testname='{4}'", table, platform, api, group, test);
			RawData _rawData = new RawData();

			IEnumerator i = SQLRequest(query, (value => { _rawData = value; }));
            while (i.MoveNext()) yield return null;
			
			data.fieldNames.AddRange(_rawData.fields);//Grab the fields from the RawData
			for (int x = 0; x < _rawData.data.Count; x++)
			{
				ResultsIORow row = new ResultsIORow();//create a new row
				row.resultsColumn.AddRange(_rawData.data[x]);//store the current row of values
				data.resultsRow.Add(row);//add it to the data to send back to resultsIO
			}
            outdata(data);
        }

		public static IEnumerator FetchSpecificEntry(ResultsIOData inputData, Action<ResultsIOData> outdata)
		{
			//make request based off common
			string baseline = inputData.baseline == true ? "Baseline" : "Results";
			string table = inputData.suite + "_" + inputData.testType + "_" + baseline;

			string values = ConvertToCondition(inputData.resultsRow[0].resultsColumn, inputData.fieldNames);

			string query = String.Format("SELECT * FROM {0} WHERE {1}", table, values);
			RawData _rawData = new RawData();

			IEnumerator i = SQLRequest(query, (value => { _rawData = value; }));
            while (i.MoveNext()) yield return null;

			inputData.fieldNames.Clear();
			inputData.resultsRow.Clear();
			inputData.fieldNames.AddRange(_rawData.fields);//Grab the fields from the RawData
			for (int x = 0; x < _rawData.data.Count; x++)
			{
				ResultsIORow row = new ResultsIORow();//create a new row
				row.resultsColumn.AddRange(_rawData.data[x]);//store the current row of values
				inputData.resultsRow.Add(row);//add it to the data to send back to resultsIO
			}
			outdata(inputData);
		}

		public static IEnumerator RunUUID(Action<string> uuid)
		{
			string _uuid = "";
			bool exists = true;
			RawData rawData = new RawData();//RawData to be filled by the wwwRequest
			do
			{
				_uuid = Common.RandomUUID();

				IEnumerator i1 = SQLRequest(string.Format("SELECT COUNT(*) FROM RunUUIDs WHERE runID='{0}'", _uuid), (value) => { rawData = value; });
                while (i1.MoveNext()) yield return null;
				if(rawData.data[0][0] == "0")
					exists = false;
			}while(exists);
            //Add uuid
			int num = -2;

			IEnumerator i2 = SQLNonQuery(string.Format("INSERT INTO RunUUIDs (runID) VALUES ('{0}')", _uuid), (value) => { num = value; });
            while (i2.MoveNext()) yield return null;

            while(num == -2)//while the request hasnt returned
			{
				yield return null;
			}
			uuid(_uuid);
		}

		public static IEnumerator FetchBaseLineTables(string suite, Action<string[]> outdata)
		{
            List<string> tables = new List<string>();
            RawData rawData = new RawData();//RawData to be filled by the wwwRequest

            IEnumerator i = SQLRequest(String.Format("SHOW TABLES LIKE '{0}%Baseline'", suite), (value => { rawData = value; }));
            while (i.MoveNext()) yield return null;
            i = null;

            for (int t = 0; t < rawData.data.Count; t++)
            {
                tables.Add(rawData.data[t][0]);//add the table name to the list of tables to pull
            }

            outdata(tables.ToArray());
        }

		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Sending data
		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		//Set the suite baseline timestamp based of given SuiteBaselineData
		public static string SetSuiteTimestamp(SuiteBaselineData SBD){
			string tableName = "SuiteBaselineTimestamps";//Hardcoded, this should be ok as it will not change going forwards
			List<string> values = new List<string> (){ SBD.suiteName, SBD.platform, SBD.api, SBD.suiteTimestamp};
			//this next line formats a SQL query, this is called at the end of uploading a new baseline
			return string.Format ("INSERT INTO {0} VALUES ({1}) ON DUPLICATE KEY UPDATE suiteTimestamp = values(suiteTimestamp);\n", tableName, ConvertToValues (values));// update or insert the new timestamp for the baselines for the suite
		}

		//Creates an entry of either result or baseline(replaces UploadData from old system)
		public static IEnumerator AddEntry(ResultsIOData inputData, string tableName, int baseline, Action<int> uploaded){
			Console.Instance.Write (DebugLevel.File, MessageLevel.Log, "Starting SQL query creation"); // Write to console
			StringBuilder outputString = new StringBuilder ();
            outputString.Append ("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;\n");//using isolation to avoid double write issues
            outputString.Append ("START TRANSACTION;\n");//using transaction to do the query in one chunk
			outputString.Append (TableCheck (tableName));//adds a table check/creation

			int rowNum = 0;//Row counter
			if (baseline == 1) {//baseline sorting
				foreach (ResultsIORow row in inputData.resultsRow) {
					rowNum++;
					outputString.AppendFormat (string.Format ("REPLACE INTO {0} VALUES ({1});\n", tableName, ConvertToValues (row.resultsColumn)));//replace the row, this will insert if there are no entries based of the duplicate key, which by default is based on Plateform/API/Group/Test
					yield return null;
				}
				foreach (SuiteBaselineData SBD in ResultsIO.Instance._suiteBaselineData) {
					if(tableName.Contains(SBD.suiteName))
						outputString.Append (SetSuiteTimestamp (SBD));//add the query to update the timestamp
				}
			} else {//result sorting
				outputString.AppendFormat ("INSERT INTO {0} VALUES ", tableName);
				int count = inputData.resultsRow.Count;
				for (int x = 0; x < count; x++) {
					rowNum++;
					outputString.AppendFormat ("({0})", ConvertToValues (inputData.resultsRow [x].resultsColumn));
					if (x < count - 1)
						outputString.Append (",\n");
					else
                        outputString.Append(";");
                    yield return null;
				}
			}

			outputString.Append ("COMMIT;");//close transaction
			int num = -2;//int to check changes were commited

			IEnumerator i = SQLNonQuery(outputString.ToString(), (value) => { num = value; });
            while (i.MoveNext()) yield return null;

			while(num == -2){//while the request hasnt returned
                yield return null;
            }
            //num = SQLNonQuery (outputString.ToString ());//send the query
			if (num == -1) {
                uploaded(0);
                QueryBackup qb = new QueryBackup();//create a new backup
				qb.type = QueryType.NonQuery;//the type is nonquery
				qb.query = outputString.ToString();//store the query
				SQLNonQueryBackup.Add (qb);//store the backup
				Console.Instance.Write (DebugLevel.File, MessageLevel.LogError, "Failed to upload, backing up"); // Write error to console
			} else {
                uploaded(1);
                Console.Instance.Write (DebugLevel.File, MessageLevel.Log, "Uploaded successfully"); // Write to console
			}
		}


		public static IEnumerator SuiteReference(Suite suite)
		{
            string tableName = suite.suiteName + "_Reference";
            Console.Instance.Write(DebugLevel.File, MessageLevel.Log, "Starting SQL query creation"); // Write to console
            StringBuilder outputString = new StringBuilder();
            outputString.Append("SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;\n");//using isolation to avoid double write issues
            outputString.Append("START TRANSACTION;\n");//using transaction to do the query in one chunk
            outputString.Append(TableCheck(tableName));//adds a table check/creation
            outputString.Append("DELETE FROM " + tableName + ";\n");// clears the existing suite reference
            outputString.AppendFormat("REPLACE INTO {0} VALUES ('{1}', {2});\n", "Suites", suite.suiteName, suite.suiteVersion);
            outputString.AppendFormat("INSERT INTO {0} VALUES ", tableName);
			int grpCount = suite.groups.Count;
			for(int grp = 0; grp < grpCount; grp++)
			{
				int testCount = suite.groups[grp].tests.Count;
				for (int test = 0; test < testCount; test++)
				{
					Test t = suite.groups[grp].tests[test];
					outputString.AppendFormat("('{0}', '{1}', {2}, {3}, {4})", suite.groups[grp].groupName, t.scene.name, t.testTypes, t.platforms, t.minimumUnityVersion);
					if (test < testCount - 1 && grp < grpCount - 1)
						outputString.Append(",\n");
					else
						outputString.Append(";\n");
					yield return null;
				}
			}

            outputString.Append("COMMIT;");//close transaction
            int num = -2;//int to check changes were commited

            IEnumerator i = SQLNonQuery(outputString.ToString(), (value) => { num = value; });
            while (i.MoveNext()) yield return null;

            while (num == -2)
            {//while the request hasnt returned
                yield return null;
            }

            Debug.Log("done upload");
        }

		public static IEnumerator BaselineSetCheck(string[] suiteNames, Action<NameValueCollection> fullSet)
		{
			NameValueCollection[] platformAPIfullSets = new NameValueCollection[suiteNames.Length];

            for (int s = 0; s < suiteNames.Length; s++)
            {
                platformAPIfullSets[s] = new NameValueCollection();
                string suiteName = suiteNames[s];
                StringBuilder queryString = new StringBuilder();

                //get baseline tables for suite
                string[] tables = null;
                IEnumerator enumerator = FetchBaseLineTables(suiteName, (value => { tables = value; }));
                while (enumerator.MoveNext()) yield return null;
                
				if(tables.Length == 0)
                    continue;

                //get platform/API sets
                NameValueCollection platformAPIsets = new NameValueCollection();
                List<Type> types = new List<Type>();
                RawData rawData = new RawData();//RawData to be filled by the wwwRequest
                foreach (string tbl in tables)
                {
                    types.Add(TestTypes.GetTypeFromString(tbl.Split('_')[1]));
                    queryString.AppendFormat("SELECT DISTINCT Platform, API FROM {0}\n", tbl);
                    if (tbl != tables[tables.Length - 1])
                        queryString.Append("UNION\n");
                }
                Debug.LogWarning(queryString.ToString());
                enumerator = SQLRequest(queryString.ToString(), (value => { rawData = value; }));
                while (enumerator.MoveNext()) yield return null;

                for (int t = 0; t < rawData.data.Count; t++)
                {
                    platformAPIsets.Add(rawData.data[t][0], rawData.data[t][1]);//add the table name to the list of tables to pull
                }

                //get counts against reference table
                //NameValueCollection platformAPIfullSets = new NameValueCollection();
                foreach (string key in platformAPIsets.AllKeys)
                {
                    foreach (string val in platformAPIsets.GetValues(key))
                    {
                        int count = 0;
                        foreach (Type t in types)
                        {
                            string query = String.Format("SELECT Count(*) FROM {0}_Reference WHERE TestType ={1}\n" +
                                                        "UNION ALL\n" +
                                                        "SELECT Count(*) FROM(SELECT GroupName,TestName FROM {0}_{4}_Baseline WHERE Platform =\"{2}\" AND API =\"{3}\") as Num\n" +
                                                        "JOIN\n" +
                                                        "(SELECT* FROM {0}_Reference WHERE TestType ={1}) as Ref ON Num.GroupName = Ref.GroupName AND Num.TestName = Ref.TestName",
                                                        suiteName,
                                                        TestTypes.GetTypeIndexFromType(t) + 1,
                                                        key,
                                                        val,
                                                        t.Name.Substring(0, t.Name.Length - 5));

                            enumerator = SQLRequest(query, (value => { rawData = value; }));
                            while (enumerator.MoveNext()) yield return null;
                            enumerator = null;

							if(rawData.data.Count == 0)
                                continue;

                            if (rawData.data[0][0] == rawData.data[1][0])
                                count++;
                        }
                        if (count == types.Count)
                            platformAPIfullSets[s].Add(key, val);
                    }
                }
            }

			NameValueCollection suitePlatformAPIfullSets = new NameValueCollection();
			string debugSets = "Full baseline sets of:\n";

            if (platformAPIfullSets.Length > 1)
            {
                foreach (string baseKey in platformAPIfullSets[0].AllKeys)
                {
                    foreach (string baseVal in platformAPIfullSets[0].GetValues(baseKey))
                    {
                        for (int nvc = 1; nvc < platformAPIfullSets.Length; nvc++)
                        {
                            foreach (string key in platformAPIfullSets[nvc].AllKeys)
                            {
                                foreach (string val in platformAPIfullSets[nvc].GetValues(key))
                                {
                                    if (baseKey == key && baseVal == val)
                                    {
                                        debugSets += "Platform: " + key + " API: " + val + "\n";
                                        suitePlatformAPIfullSets.Add(key, val);
                                    }
                                }
                            }
                        }
                    }
                }
				Console.Instance.Write(DebugLevel.File, MessageLevel.Log, debugSets);
				fullSet(suitePlatformAPIfullSets);
            }
			else
			{
				foreach (string key in platformAPIfullSets[0].AllKeys)
                {
                    foreach (string val in platformAPIfullSets[0].GetValues(key))
                    {
                        debugSets += "Platform: " + key + " API: " + val + "\n";
                    }
                }
                //Debug.LogError(debugSets);
                fullSet(platformAPIfullSets[0]);
            }

            
        }

		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Utilities - TODO wip
		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		//Method to check for valid connection, Invoked from start
		static void CheckConnection(){

			if(netStat != Application.internetReachability) {
				netStat = Application.internetReachability; //Get network state

				switch (netStat) {
				case NetworkReachability.NotReachable:
					Console.Instance.Write (DebugLevel.Key, MessageLevel.LogError, "Internet Connection Lost");
					liveConnection = connectionStatus.None; // connection is not available
					break;
				case NetworkReachability.ReachableViaCarrierDataNetwork:
					Console.Instance.Write (DebugLevel.Key, MessageLevel.LogError, "Internet Connection Not Reliable, Please connect to Wi-fi");
					liveConnection = connectionStatus.Mobile; // connection is not really available
					break;
				case NetworkReachability.ReachableViaLocalAreaNetwork:
					Console.Instance.Write (DebugLevel.Key, MessageLevel.Log, "Internet Connection Live");
					liveConnection = connectionStatus.Internet; // connection is available
					break;
				}
			}

		}

		//Check to see if table exists
		public static string TableCheck(string tableName){
            string _template = tableName.Substring(tableName.IndexOf('_') + 1, tableName.Length - (tableName.IndexOf('_') + 1));//shave off the suite name to get the template table
			return string.Format ("CREATE TABLE IF NOT EXISTS {0} LIKE {1};\n", tableName, _template);//query to check for table, otherwise create one
		}
		
		//convert table string to tableStrings, Takes 'Suite_TestType_Baseline' and splits into a TableStringsClass
		public static TableStrings TableStringToStrings(string tableString)
		{
            string[] splitName = tableString.Split('_');
			TableStrings _tableStrings = new TableStrings(splitName[0], splitName[1], (splitName[2] == "Baseline" ? true : false));
            return _tableStrings;
        }

        //convert tableStrings to table string, Takes a tablestrings class and outputs a string formatted like 'Suite_TestType_Baseline'
        public static string TableStringToStrings(TableStrings tableString)
        {
			string baseline = tableString.baseline ? "Baseline" : "Results";
            string _tableString = tableString.suite + "_" + tableString.testType + "_" + baseline;
            return _tableString;
        }

		//create column list for table creation, inclued data type
		static string CreateColumns(string[] columns){
			StringBuilder sb = new StringBuilder ();
			for(int i = 0; i < columns.Length; i++){
				string dataType = "varchar(255)";
				if(i < 12)
					dataType = dataTypes[i];
				else
					dataType = "TEXT";
				
				sb.Append (columns[i] + " " + dataType);
				if (i != columns.Length - 1)
					sb.Append (',');
			}
			return sb.ToString ();
		}

		//create column list for un-named values
		static string ConvertToValues(List<string> values){
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < values.Count; i++) {
				sb.Append ("'" + values[i] + "'");
				if (i != values.Count - 1)
					sb.Append (',');
			}
			return sb.ToString ();
		}

		//create column list for named values
		static string ConvertToValues(List<string> values, string[] fields){
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < values.Count; i++) {
				sb.Append (fields[i] + "='" + values[i] + "' ");
				if (i != values.Count - 1)
					sb.Append (',');
			}
			return sb.ToString ();
		}

		//create column list for named values for condition use
		static string ConvertToCondition(List<string> values, List<string> fields){
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < values.Count; i++) {
				sb.Append (fields[i] + "='" + values[i] + "' ");
				if (i != values.Count - 1)
					sb.Append (" AND ");
			}
			return sb.ToString ();
		}

		//Convert raw string data form the webserver to table data
		static RawData ConvertRawData(string data)
        {
            RawData _output = new RawData();
            string[] baseSplit = data.Split(new string[] { "<<|>>" }, StringSplitOptions.None);
            string[] rows = baseSplit[2].Split(new string[] { "<|>" }, StringSplitOptions.None);
            _output.fields.AddRange(baseSplit[1].Split(new string[] { "|||" }, StringSplitOptions.None));
            for (int i = 0; i < rows.Length; i++)
            {
                _output.data.Add(rows[i].Split(new string[] { "|||" }, StringSplitOptions.None));
            }
            return _output;
        }

		//Convert RawData class to ResultsIOData Class
		public static ResultsIOData ConvertRawDataToResultsIOData(string suite, string testType, RawData data, bool baseline)
		{
            ResultsIOData outData = new ResultsIOData();
            outData.suite = suite;
            outData.testType = testType;
            outData.baseline = baseline;
            outData.fieldNames = data.fields;
            for (int i = 0; i < data.data.Count; i++)
			{
                ResultsIORow row = new ResultsIORow();
                row.resultsColumn.AddRange(data.data[i]);
                outData.resultsRow.Add(row);
            }
            return outData;
        }

		static void DebugDump(string debug1, string debug2)
		{
#if UNITY_EDITOR
			string dataPath = (Application.dataPath).Substring(0, Application.dataPath.Length - 6) + "EditorResults/SQLDump";
            if(!Directory.Exists(dataPath))
			{
                Directory.CreateDirectory(dataPath);
            }
			string[] lines = new string[4];
            lines[0] = debug1;
            lines[3] = debug2;
            File.WriteAllLines(dataPath + "/dump" + dumpIndex + ".txt", lines);
            dumpIndex++;
#endif
        }

		static IEnumerator Wait(float time)
		{
			float t = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t < time) yield return true;
		}
		
		class QueryBackup{
			public QueryType type;
			public string query;
		}

		[System.Serializable]
        public class RawData
        {
            public List<string> fields = new List<string>();
            public List<string[]> data = new List<string[]>();
        }

		public class TableStrings
		{
            public string suite;
            public string testType;
            public bool baseline;

			public TableStrings(string _suite, string _testType, bool b)
			{
                suite = _suite;
                testType = _testType;
                baseline = b;
            }

        }

		enum QueryType{ Query, NonQuery, QueryRequest};

		//SQL data types for common
		private static string[] dataTypes = new string[]{"DATETIME",//datetime
												"varchar(255)",//UnityVersion
												"varchar(10)",//AppVersion
												"varchar(255)",//OS
												"varchar(255)",//Device
												"varchar(255)",//Platform
												"varchar(50)",//API
												"varchar(128)",//RenderPipe
												"varchar(128)",//GroupName
												"varchar(128)",//TestName
												"varchar(16)",//PassFail
												"TEXT",//Custom
		};
	}
}
