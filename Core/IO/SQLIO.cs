using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Text;
using GraphicsTestFramework;

namespace GraphicsTestFramework.SQL
{	
	public class SQLIO : MonoBehaviour {

		public static SQLIO _Instance = null;//Instance

		public static SQLIO Instance {
			get {
				if (_Instance == null)
					_Instance = (SQLIO)FindObjectOfType (typeof(SQLIO));
				return _Instance;
			}
		}

        // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //CONNECTION VARIABLES
        private string _webservice = "http://ec2-35-176-162-233.eu-west-2.compute.amazonaws.com/UTFFunctions.php";//web service
        private string _pass = "f23-95j-vCt";

        // ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
        //LOCAL VARIABLES
        public connectionStatus liveConnection;
		private NetworkReachability netStat = NetworkReachability.NotReachable;

		//Query retry list - TODO - not hooked up or solved yet
		private List<QueryBackup> SQLNonQueryBackup = new List<QueryBackup>();

		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		//INFORMATION
		private SystemData sysData;//local version of systemData

		public void Init(SystemData _sysData)
        {
            sysData = _sysData;

			//setup staging
			if(Master.Instance._sqlMode == SQLmode.Staging)
				_webservice = "http://ec2-35-176-162-233.eu-west-2.compute.amazonaws.com/UTFFunctions_staging.php";//web srvice
        }

		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Query methods - TODO wip
		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		
		public IEnumerator SQLNonQuery(string _input, Action<int> callback)
        {
            List<IMultipartFormSection> form = new List<IMultipartFormSection>();
            form.Add(new MultipartFormDataSection("type", "nonQuery"));
			form.Add(new MultipartFormDataSection("pass", _pass));
            form.Add(new MultipartFormDataSection("query", _input));
            UnityWebRequest www = UnityWebRequest.Post(_webservice, form); //POST data is sent via the URL

#if !UNITY_2018_1_OR_NEWER
            yield return www.Send();
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
                callback(1);
            }
            else
            {
                Console.Instance.Write(DebugLevel.File, MessageLevel.LogWarning, "SQL response:" + www.downloadHandler.text); // Write to console
                callback(-1);
            }
        }
		
		public IEnumerator SQLRequest(string _query, Action<RawData> data)
        {
            List<IMultipartFormSection> form = new List<IMultipartFormSection>();
            form.Add(new MultipartFormDataSection("type", "request"));
			form.Add(new MultipartFormDataSection("pass", _pass));
            form.Add(new MultipartFormDataSection("query", _query));
            UnityWebRequest www = UnityWebRequest.Post(_webservice, form); //POST data is sent via the URL

#if !UNITY_2018_1_OR_NEWER
            yield return www.Send();
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
				if(www.downloadHandler.text != "Null")
				{
                    Console.Instance.Write(DebugLevel.File, MessageLevel.Log, "SQL response:" + www.downloadHandler.text); // Write to console
                	data(ConvertRawData(www.downloadHandler.text));
				}
				else
				{
                    Console.Instance.Write(DebugLevel.File, MessageLevel.LogWarning, "SQL response:" + www.downloadHandler.text); // Write to console
                    data(new RawData());
				}
            }
            else
            {
                Console.Instance.Write(DebugLevel.File, MessageLevel.LogError, www.error);
                data(null);
            }
        }

		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Query data
		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		//Gets the timestamp from the server, if none return DateTime.MinValue to cause repull regardless
		public IEnumerator GetbaselineTimestamp(string suiteName, Action<DateTime> outdata){
			DateTime timestamp = DateTime.MinValue;//make date time min, we check this on the other end since it is not nullable
            RawData _rawData = null;//RawData to be filled by the request
            string query = String.Format("SELECT suiteTimestamp FROM SuiteBaselineTimestamps WHERE api='{0}' AND suiteName='{1}' AND platform='{2}';", sysData.API, suiteName, sysData.Platform);//This line sends a query to get timestamps for matching API/platform/suite
            StartCoroutine(SQLRequest(query, (value => { _rawData = value; })));//send the request

			while(_rawData == null){//we wait until the data is fill from the wwwrequest
                yield return null;
            }
			if(_rawData.data.Count != 0)
            	timestamp = System.DateTime.Parse(_rawData.data[0][0]);//convert the string to a timestamp

            outdata(timestamp);
        }

		//fetch the server side baselines by providing the suites, pltform and API, later will have to change this to allow more strict baseline matching <TODO
		public IEnumerator FetchBaselines(string[] suiteNames, string platform, string api, Action<ResultsIOData[]> outdata){
			List<ResultsIOData> data = new List<ResultsIOData> ();//ResultsIOData to send back to resultsIO for local processing
			List<string> tables = new List<string>();
			//Get the table names to pull baselines from
			foreach(string suite in suiteNames){
                RawData rawData = new RawData();//RawData to be filled by the wwwRequest
                StartCoroutine(SQLRequest(String.Format("SHOW TABLES LIKE '{0}%Baseline'", suite), (value => { rawData = value; })));//Get all tables with the suite and ending with baseline
				while(rawData.data.Count == 0){
                    yield return null;
                }
                for (int t = 0; t < rawData.data.Count; t++){
					tables.Add(rawData.data[t][0]);//add the table name to the list of tables to pull
				}
            }
			int n = 0;
			foreach(string table in tables){
				string suite = table.Substring (0, table.IndexOf ("_"));//grab the suite from the table name
				string testType = table.Substring (table.IndexOf ("_") + 1, table.LastIndexOf ("_") - (suite.Length + 1));//grab the test type from the table name
				data.Add (new ResultsIOData());
				data [n].suite = suite;
				data [n].testType = testType;
                //This line controls how baselines are selected, right now only Platform and API are unique
                string query = String.Format("SELECT * FROM {0} WHERE platform='{1}' AND api='{2}'", table, platform, api);
                RawData _rawData = new RawData();
                StartCoroutine(SQLRequest(query, (value => { _rawData = value; })));
				while(_rawData.data.Count == 0){
                    yield return null;
                }
                data[n].fieldNames.AddRange(_rawData.fields);//Grab the fields from the RawData
				for (int i = 0; i < _rawData.data.Count; i++)
                {
                    ResultsIORow row = new ResultsIORow();//create a new row
                    row.resultsColumn.AddRange(_rawData.data[i]);//store the current row of values
					data[n].resultsRow.Add(row);//add it to the data to send back to resultsIO
                }
				if (data [n].fieldNames.Count == 0)
					data.RemoveAt (n);
				n++;
			}
			outdata(data.ToArray ());
		}

		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Sending data
		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		//Set the suite baseline timestamp based of given SuiteBaselineData
		public string SetSuiteTimestamp(SuiteBaselineData SBD){
			string tableName = "SuiteBaselineTimestamps";//Hardcoded, this should be ok as it will not change going forwards
			List<string> values = new List<string> (){ SBD.suiteName, SBD.platform, SBD.api, SBD.suiteTimestamp};
			//this next line formats a SQL query, this is called at the end of uploading a new baseline
			return string.Format ("INSERT INTO {0} VALUES ({1}) ON DUPLICATE KEY UPDATE suiteTimestamp = values(suiteTimestamp);\n", tableName, ConvertToValues (values));// update or insert the new timestamp for the baselines for the suite
		}

		//Creates an entry of either result or baseline(replaces UploadData from old system)
		public IEnumerator AddEntry(ResultsIOData inputData, string tableName, int baseline, Action<int> uploaded){
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
				for (int i = 0; i < count; i++) {
					rowNum++;
					outputString.AppendFormat ("({0})", ConvertToValues (inputData.resultsRow [i].resultsColumn));
					if (i < count - 1)
						outputString.Append (",\n");
					else
                        outputString.Append(";");
                    yield return null;
				}
			}

			outputString.Append ("COMMIT;");//close transaction
			int num = -2;//int to check changes were commited
            StartCoroutine(SQLNonQuery(outputString.ToString(), (value) => { num = value; }));//send the query, this will return a number if successful or -1 for a failure
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

		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		// Utilities - TODO wip
		// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		//Method to check for valid connection, Invoked from start
		void CheckConnection(){

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
		public string TableCheck(string tableName){
            string _template = tableName.Substring(tableName.IndexOf('_') + 1, tableName.Length - (tableName.IndexOf('_') + 1));//shave off the suite name to get the template table
			return string.Format ("CREATE TABLE IF NOT EXISTS {0} LIKE {1};\n", tableName, _template);//query to check for table, otherwise create one
		}

		//create column list for table creation, inclued data type
		string CreateColumns(string[] columns){
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
		string ConvertToValues(List<string> values){
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < values.Count; i++) {
				sb.Append ("'" + values[i] + "'");
				if (i != values.Count - 1)
					sb.Append (',');
			}
			return sb.ToString ();
		}

		//create column list for named values
		string ConvertToValues(List<string> values, string[] fields){
			StringBuilder sb = new StringBuilder ();
			for (int i = 0; i < values.Count; i++) {
				sb.Append (fields[i] + "='" + values[i] + "' ");
				if (i != values.Count - 1)
					sb.Append (',');
			}
			return sb.ToString ();
		}

		//Convert raw data form the webserver to table data
		RawData ConvertRawData(string data)
        {
            RawData _output = new RawData();
            string[] baseSplit = data.Split(new string[] { "<<|>>" }, StringSplitOptions.None);
            string[] rows = baseSplit[1].Split(new string[] { "<|>" }, StringSplitOptions.None);
            _output.fields.AddRange(baseSplit[0].Split(new string[] { "|||" }, StringSplitOptions.None));
            for (int i = 0; i < rows.Length; i++)
            {
                _output.data.Add(rows[i].Split(new string[] { "|||" }, StringSplitOptions.None));
            }
            return _output;
        }

		class QueryBackup{
			public QueryType type;
			public string query;
		}

        public class RawData
        {
            public List<string> fields = new List<string>();
            public List<string[]> data = new List<string[]>();
        }

		enum QueryType{ Query, NonQuery, QueryRequest};

		//SQL data types for common
		private string[] dataTypes = new string[]{"DATETIME",//datetime
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
