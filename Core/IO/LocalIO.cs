using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

namespace GraphicsTestFramework
{
	public class LocalIO : MonoBehaviour
	{
		public static LocalIO _Instance = null;

		public static LocalIO Instance {
			get {
				if (_Instance == null)
					_Instance = (LocalIO)FindObjectOfType (typeof(LocalIO));
				return _Instance;
			}
		}

		//Data path for local files
		private string dataPath;
		//prefix for local baseline files
		private string baselinePrefix = "Baseline";
		//prefix for local results files
		private string resultsCurrentPrefix = "ResultsCurrent";
		//Total disk space used by local files
		public long spaceUsed;

		public void Init ()
		{
			#if UNITY_EDITOR
			dataPath = (Application.dataPath).Substring (0, Application.dataPath.Length - 6) + "EditorResults";
			#else
			dataPath = Application.persistentDataPath;
			#endif

			if (!Directory.Exists (dataPath)) //directory check
				Directory.CreateDirectory (dataPath);
			else {
				spaceUsed = CheckDiskSpace (dataPath, true);
			}
		}

		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		/// Writing data - TODO wip
		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Writes the data files.
		/// </summary>
		/// <returns>The data files.</returns>
		/// <param name="suite">Suite.</param>
		/// <param name="testType">Test type.</param>
		/// <param name="resultIOdata">Result I odata.</param>
		/// <param name="data">Data.</param>
		/// <param name="filetype">Filetype.</param>
		public IEnumerator WriteDataFiles (ResultsIOData resultIOdata, fileType filetype)
		{
            if (resultIOdata.resultsRow.Count != 0)
            {
                string suite = resultIOdata.suite;
                string testType = resultIOdata.testType;
                ResultsDataCommon common = ResultsIO.Instance.GenerateRDC(resultIOdata.resultsRow[0].resultsColumn.ToArray());
                string[] fields = resultIOdata.fieldNames.ToArray();
                Console.Instance.Write(DebugLevel.File, MessageLevel.Log, "Beginning to write data for suite " + suite + " of the testType " + testType + " which contains " + resultIOdata.resultsRow.Count + " files to write"); // Write to console
                string platformAPI = common.Platform + "_" + common.API;
                string filePath = CreateDataDirectory(suite, platformAPI, common.RenderPipe, testType);
                string prefix = "";
                int suiteBaselineDataIndex = -1;
                if (filetype == fileType.Baseline)
                { //if it's a baseline file we need to update latest baseline timesstamp
                    prefix = baselinePrefix;
                    ResultsIO.Instance.UpdateBaselineDictionary(suite, common.Platform, common.API, common.RenderPipe, out suiteBaselineDataIndex);
                }
                else
                {
                    prefix = resultsCurrentPrefix;
                }
                List<string> data = new List<string>();//list to create string for local file
                                                       //iterate through all the results in the current ResultsIOData
                for (int i = 0; i < resultIOdata.resultsRow.Count; i++)
                {
                    common = ResultsIO.Instance.GenerateRDC(resultIOdata.resultsRow[i].resultsColumn.ToArray());
                    data.Clear();//clear data for new file
                    for (int f = 0; f < fields.Length; f++)
                    {//adding the data(values) and fields together
                        data.Add(fields[f]);//add the field name
                        data.Add(resultIOdata.resultsRow[i].resultsColumn[f]);//add the value
                    }
                    if (!Directory.Exists(filePath)) // check to see ig folder exists if not create it
                        Directory.CreateDirectory(filePath);
                    string fileName = prefix + "_" + common.GroupName + "_" + common.TestName + ".txt";//name the file
                    File.WriteAllLines(filePath + "/" + fileName, data.ToArray());//write the contents of data line by line
                    while (!File.Exists(filePath + "/" + fileName))
                    {
                        Console.Instance.Write(DebugLevel.File, MessageLevel.Log, "Writing..."); // Write to console
                        yield return new WaitForEndOfFrame();
                    }
                    //update baseline dictionary(not a dictionary) if baseline
                    if (filetype == fileType.Baseline)
                    {
                        ResultsIO.Instance.BaselineDictionaryEntry(suiteBaselineDataIndex, testType, common.GroupName, common.TestName, common.DateTime);
                    }
                }
                Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Wrote " + resultIOdata.resultsRow.Count + " files to disk"); // Write to console
                                                                                                                                        //Write baseline dictionary for suite and update timestamp TODO might need work/tweaking
                if (filetype == fileType.Baseline)
                {
                    yield return StartCoroutine(UpdateSuiteDataFiles());
                }
                yield return null;
            }
        }

		/// <summary>
		/// Updates the suite data files.
		/// </summary>
		/// <returns>The suite data files.</returns>
		IEnumerator UpdateSuiteDataFiles ()
		{
			Console.Instance.Write (DebugLevel.File, MessageLevel.Log, "Updating Suite baseline data files");
			foreach(SuiteBaselineData SBD in ResultsIO.Instance._suiteBaselineData){
				string filePath = dataPath + "/" + SBD.suiteName + "/" + SBD.platform + "_" + SBD.api;
				if (!Directory.Exists (filePath)) // check to see ig folder exists if not create it
					Directory.CreateDirectory (filePath);
				string fileName = "SuiteData_" + SBD.suiteName + "_" + SBD.platform + "_" + SBD.api + "_" + SBD.pipeline + ".txt";

				string[] newFileContent = new string[SBD._suiteData.Count + 1];
				newFileContent[0] = System.DateTime.UtcNow.ToString (Common.dateTimeFormat);

				for(int i = 1; i <= SBD._suiteData.Count; i++){
					newFileContent[i] = JsonUtility.ToJson (SBD._suiteData[i-1]);//TODO - check if this is how we want it
				}
				File.WriteAllLines (filePath + "/" + fileName, newFileContent);
				//SQL.SQLIO.Instance.SetSuiteTimestamp (SBD);// SQL update
				yield return null;
			}
		}

		/// <summary>
		/// Write large files to EXTERNAL_DATA folder to replicate cloud sorting.
		/// </summary>
		/// <param name="value">Contents to write.</param>
		/// <param name="key">Key for file(also file name).</param>
		public void LargeFileWrite(string value, string key){
			Console.Instance.Write (DebugLevel.File, MessageLevel.Log, "Writing large file to disk named " + key + "");
			string filePath = dataPath + "/EXTERNAL_DATA";
			string fileName = key + ".txt";
			if (!Directory.Exists (filePath))
				Directory.CreateDirectory (filePath);
			File.WriteAllText (filePath + "/" + fileName, value);
		}

		/// <summary>
		/// Creates the data directory string and local directory file for test data.
		/// </summary>
		/// <returns>The data directory as a string.</returns>
		/// <param name="suite">Suite.</param>
		/// <param name="platformAPI">API.</param>
		/// <param name="renderPipe">Render pipe.</param>
		/// <param name="testType">Test type.</param>
		public string CreateDataDirectory(string suite, string platformAPI, string renderPipe, string testType){
			string filePath = dataPath + "/" + suite + "/" + platformAPI + "/" + renderPipe + "/" + testType; // format the folder hierachy
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Checking directory:" + filePath); // Write to console
			if (!Directory.Exists (filePath)) // check to see if folder exists if not create it
				Directory.CreateDirectory (filePath);
			return filePath; // return the 
		}

		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		/// Reading data - TODO wip
		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Populates the local baseline lookup.
		/// </summary>
		public List<SuiteBaselineData> ReadLocalBaselines(){
			//check suites>api>pipelines
			List<SuiteBaselineData> baselineData = new List<SuiteBaselineData>();
			string[] suites = Directory.GetDirectories (dataPath);
			foreach(string s in suites){
				Console.Instance.Write(DebugLevel.File, MessageLevel.Log, "Loading Local Suite:" + s); // Write to console
                string[] platformApis = Directory.GetDirectories (s);
				foreach(string platformApi in platformApis)
                {
					Console.Instance.Write(DebugLevel.File, MessageLevel.Log, "Loading Local Platform API:" + platformApi); // Write to console
                    string[] pipes = Directory.GetDirectories (platformApi);
					foreach(string pipe in pipes){
						string fileName = "SuiteData_" + Path.GetFileName (s) + "_" + Path.GetFileName (platformApi) + "_" + Path.GetFileName (pipe) + ".txt";
						if (File.Exists (platformApi + "/" + fileName)) {
							SuiteBaselineData SBD = new SuiteBaselineData ();
							SBD.suiteName = Path.GetFileName (s);
                            string x = Path.GetFileName(platformApi); // Get platformAPI filename
                            int apiIndex = x.LastIndexOf('_'); // Find start index of API
                            SBD.platform = x.Substring(0, apiIndex); // Get substring of platform
							SBD.api = x.Substring(apiIndex + 1); // Get substring of API
							SBD.pipeline = Path.GetFileName (pipe);
							string[] fileLines = File.ReadAllLines (platformApi + "/" + fileName);
							SBD.suiteTimestamp = fileLines[0];
							List<SuiteData> sData = new List<SuiteData>();
							for(int i = 1; i < fileLines.Length; i++){
								sData.Add (JsonUtility.FromJson<SuiteData> (fileLines[i]));
							}
							SBD._suiteData = sData;
							baselineData.Add (SBD);
						}
					}
				}
			}
			return baselineData;
		}

		/// <summary>
		/// Fetchs the local data file.
		/// </summary>
		/// <returns>The data file.</returns>
		/// <param name="suite">Suite.</param>
		/// <param name="testType">Test type.</param>
		/// <param name="resultsDataCommon">Results data common.</param>
		/// <param name="baseline">If set to <c>true</c> baseline.</param>
		public ResultsIOData FetchDataFile (string suite, string testType, ResultsDataCommon resultsDataCommon, bool baseline, bool full)
		{
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Beginning fetch process"); // Write to console
			string filePath = dataPath + "/" + suite + "/" + resultsDataCommon.Platform + "_" + resultsDataCommon.API + "/" + resultsDataCommon.RenderPipe + "/" + testType;
			string fileName = "_" + resultsDataCommon.GroupName + "_" + resultsDataCommon.TestName + ".txt";

			if (baseline)
				fileName = baselinePrefix + fileName;
			else
				fileName = resultsCurrentPrefix + fileName;

			if (!Directory.Exists (filePath)) {
                Console.Instance.Write(DebugLevel.Critical, MessageLevel.Log, "Directory for baseline does not exist:" + filePath); // Write to console
				return null;
			} else {
                Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Directory for baseline exists, attempting to fetch requested baseline"); // Write to console
				if (!File.Exists (filePath + "/" + fileName)) {
                    Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Baseline file does not exist for the requested test:" + filePath + "/" + fileName); // Write to console
					return null;
				} else {
					string[] fileLines;
					if(!full)
					{
						fileLines = File.ReadAllLines (filePath + "/" + fileName);
						List<string> lines = new List<string>();
						int commonEnd = 0;
						for(int i = 0; i < fileLines.Length; i++)
						{
							if(commonEnd == 0)
								lines.Add(fileLines[i]);
							if(fileLines[i] == "Custom" && i % 2 == 0)
								commonEnd = 1;
						}
						fileLines = lines.ToArray();
					}
					else
					{
						fileLines = File.ReadAllLines (filePath + "/" + fileName);
					}
					return ResultsIO.Instance.GenerateRIOD (fileLines, suite, testType);
				}
			}
		}

		/// <summary>
		/// Read large file in EXTERNAL_DATA folder.
		/// </summary>
		/// <param name="key">Key for file(also file name).</param>
		public string LargeFileRead(string key){
			string filePath = dataPath + "/EXTERNAL_DATA";
			string fileName = key + ".txt";
			if (!File.Exists (filePath + "/" + fileName))
				return null;
			else{
				return File.ReadAllText (filePath + "/" + fileName);
			}
		}

		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		/// Utilities - TODO wip
		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Checks the used disk space of specified path.
		/// </summary>
		/// <returns>The disk space.</returns>
		/// <param name="dir">Directory to check.</param>
		/// <param name="recursive">If set to <c>true</c> include full subfolder hierachy.</param>
		public long CheckDiskSpace (string dir, bool recursive)
		{
			long bytes = new long ();
			DirectoryInfo dirInfo = new DirectoryInfo (dir);
			FileInfo[] fileInfo = dirInfo.GetFiles ();
			DirectoryInfo[] dirInfos = dirInfo.GetDirectories ();
			foreach (FileInfo fi in fileInfo) {
				bytes += fi.Length;
			}
			if(recursive) {
				foreach (DirectoryInfo di in dirInfos) {
					bytes += CheckDiskSpace (di.FullName, true);
				}
			}
			return bytes;
		}

		public void ClearLocalData(){
            Console.Instance.Write(DebugLevel.File, MessageLevel.LogWarning, "Clearing local data files, this is irreversable.");
            Directory.Delete (dataPath, true);
			Directory.CreateDirectory (dataPath);
            Button[] buttons = new Button[2]; // Create button array
            bool openDialogue = Dialogue.Instance.TryDialogue(true, 2, out buttons); // Try for dialogue window and out buttons
            if (openDialogue) // If dialogue opens
            {
                buttons[0].onClick.AddListener(delegate { ResultsIO.Instance.Restart(); }); // Add listeners
                buttons[0].onClick.AddListener(delegate { Dialogue.Instance.SetState(false, 2); });
                buttons[1].onClick.AddListener(delegate { Master.Instance.ExitApplication(); }); // Add listeners
                buttons[1].onClick.AddListener(delegate { Dialogue.Instance.SetState(false, 2); });
            }
            else
                ResultsIO.Instance.Restart(); // Save baseline
        }
		public void ClearLocalResults()
        {
            Directory.Delete(dataPath);
        }

        
	}
}
