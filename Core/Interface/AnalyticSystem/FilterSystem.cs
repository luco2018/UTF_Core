using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace GraphicsTestFramework
{
	// ------------------------------------------------------------------------------------
	// FilterSystem
	// - The base for the filter system
	// - Collects filtered data or pairs of data from SQL
	// - Sends to the Structure for it do view and compare

	public class FilterSystem : MonoBehaviour
	{
		// ------------------------------------------------------------------------------------
		// Variables
		private static FilterSystem _Instance = null;
        public static FilterSystem Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (FilterSystem)FindObjectOfType(typeof(FilterSystem));
                return _Instance;
            }
        }
		public GameObject canvas;
		public TableContainer tableOptions;
        public Text filterCountText;
        public Button filterFetchButton;
        public List<string> tableStrings = new List<string>();
        private string[] baseTableStrings;
        private List<List<string>> suiteTestTypes = new List<List<string>>();
		public int tableCount = 0;
        public int rowCount = 0;
        static int maxResults = 250;
        public List<ResultsIOData> tempData = new List<ResultsIOData>();
        public List<DataContainer> fetchedData = new List<DataContainer>();
        //Modes
        bool runID;
        bool comparison;
        string currentRunID;
        static string commonFields = "DateTime, UnityVersion, AppVersion, OS, Device, Platform, API, RenderPipe, GroupName, TestName, PassFail, Custom";
		static bool firstRun = false;

		public void BaseFilter()
		{
			if(baseTableStrings == null || baseTableStrings.Length == 0)
            	StartCoroutine(GetTableNames(String.Format("SHOW TABLES LIKE '%{0}'", "Results"))); //tableOptions.baselineBool._selection[0])));//TODO - default is results
		}

		IEnumerator GetTableNames(string query)
		{
			SQL.SQLIO.RawData rawData = new GraphicsTestFramework.SQL.SQLIO.RawData();
			yield return StartCoroutine(SQL.SQLIO.Instance.SQLRequest(query, (value => { rawData = value; })));//Get all tables
			foreach(string[] strArr in rawData.data)
			{
				if(strArr[0].Split(new char[]{'_'}, StringSplitOptions.None).Length == 3)
				{
					tableStrings.Add(strArr[0]);
					tableCount++;
				}
			}

			if(baseTableStrings == null)
                baseTableStrings = tableStrings.ToArray();

            suiteTestTypes.Add(new List<string>());
			suiteTestTypes.Add(new List<string>());

			for(int i = 0; i < tableStrings.Count; i++)
			{
				Debug.Log("splitting " + tableStrings[i]);
				string[] split = tableStrings[i].Split(new char[]{'_'}, StringSplitOptions.None);
				if(!suiteTestTypes[0].Contains(split[0]))
					suiteTestTypes[0].Add(split[0]);
				if(!suiteTestTypes[1].Contains(split[1]))
					suiteTestTypes[1].Add(split[1]);
			}
			if(tableOptions.suiteMask)
				tableOptions.suiteMask.Init(suiteTestTypes[0].ToArray());
			if(tableOptions.testTypeMask)
				tableOptions.testTypeMask.Init(suiteTestTypes[1].ToArray());
				
            yield return StartCoroutine(UpdateResultCount(runID, null, null, (value => { rowCount = value; })));
        }

		public void SetRunID(string id)
		{
			if(id != null && id != "")
			{
				tableStrings.Clear();
				tableStrings.AddRange(baseTableStrings);
				runID = true;
                currentRunID = id;
                StartCoroutine(UpdateResultCount(runID, null, ("'%runID|" + id + "%'"), (value => { rowCount = value; })));
			}
			else
			{
                runID = false;
            }

        }

		public IEnumerator UpdateResultCount(bool _runID, string _filterA, string _filterB, Action<int> countOut)
		{
            int count = 0;

			string rowCountQ = "SELECT x FROM (\n";

            //make the query
            for (int i = 0; i < tableStrings.Count; i++)
            {
                rowCountQ += CreateQueryString(tableStrings[i], _runID, "count(*) x", _filterA, _filterB);//TODO broken since refactor
				if(i != tableStrings.Count - 1)
                    rowCountQ += "\nUNION ALL\n";
            }
            rowCountQ += "\n) AS x;";

			// submit the query
			SQL.SQLIO.RawData rawData = new GraphicsTestFramework.SQL.SQLIO.RawData();
            Debug.LogWarning(rowCountQ);
            yield return StartCoroutine(SQL.SQLIO.Instance.SQLRequest(rowCountQ, (value => { rawData = value; })));//Get all tables

            //get the count and remove empty table results
            for (int i = tableStrings.Count - 1; i >= 0; i--)
            {
                int tableRowCount = 0;
                int.TryParse(rawData.data[i][0], out tableRowCount);//convert the cell form a string to int(or try at least)
                if(tableRowCount == 0)
                    tableStrings.RemoveAt(i);
				else
                    count += tableRowCount;
            }

            //Display the 
            string resultRowString = "Filtered Results : ";
			if(count < maxResults)
            	resultRowString += "<color=#008000ff>" + count + "</color>";
			else
                resultRowString += "<color=#f00000ff>" + count + "</color>";
            filterCountText.text = resultRowString;
            countOut(count);
        }

        public void FetchFilter()
        {
			if(rowCount < maxResults && rowCount > 0)
            	StartCoroutine(FetchFilteredResults());
        }

		IEnumerator FetchFilteredResults()
		{
            ProgressScreen.Instance.SetState(true, ProgressType.CloudLoad, "Fetching Cloud Data"); //Show loading screen
            ResultsIOData[] riodA = new ResultsIOData[0];
            yield return StartCoroutine(FetchFilterData(value => { riodA = value; }));

			ResultsIOData[] riodB = new ResultsIOData[0];
			if(comparison)
            	yield return StartCoroutine(FetchFilterData(value => { riodB = value; }));
			else
                riodB = riodA;
            ProgressScreen.Instance.SetState(false, ProgressType.CloudLoad, "null"); //Show loading screen
			canvas.SetActive(false);//turn off the filter menu
            StartCoroutine(TestStructure.Instance.GenerateAnalyticStructure(riodA));
			//StartCoroutine(TestStructure.Instance.GenerateAnalyticStructure(riodA, riodB));
        }

        IEnumerator FetchFilterData(Action<ResultsIOData[]> outData)
		{
            List<ResultsIOData> riod = new List<ResultsIOData>();
            foreach (string table in tableStrings)
            {
                SQL.SQLIO.TableStrings ts = SQL.SQLIO.TableStringToStrings(table);
                string query = CreateQueryString(table, runID, commonFields, null, ("'%runID|" + currentRunID + "%'"));///hardcoded to run id
                SQL.SQLIO.RawData rawData = new GraphicsTestFramework.SQL.SQLIO.RawData();
                yield return StartCoroutine(SQL.SQLIO.Instance.SQLRequest(query, (value => { rawData = value; })));//Get all tables
                riod.Add(SQL.SQLIO.ConvertRawDataToResultsIOData(ts.suite, ts.testType, rawData, ts.baseline));
            }
            outData(riod.ToArray());
        }

		// IEnumerator FetchFilterBaselinePair(Action<ResultsIOData[]> outData)
		// {

		// }

		string CreateQueryString(string table, bool _runID, string _column, string _filterA, string _filterB)
		{
            string fullQuery = "";
            string filterA = null;
            string filterB = _filterB;

            if(_runID)
                filterA = "Custom";
			else
			{
				if(_filterA != null || _filterB == null)
                    filterA = _filterA;
            }

			if(filterA != null && filterB != null)
				fullQuery += String.Format("SELECT {0} FROM {1} WHERE {2} LIKE {3}", _column, table, filterA, filterB);
			else
				fullQuery += String.Format("SELECT {0} FROM {1}", _column, table);

            return fullQuery;
        }

		[System.Serializable]
		public class DataContainer
		{
            public string suite;
            public string testType;
            public SQL.SQLIO.RawData data;
        }

        [System.Serializable]
		public class TableContainer
		{
			public DropdownEnumMask suiteMask;
			public DropdownEnumMask testTypeMask;
			public DropdownEnumMask baselineBool;
		}

		public class Filter
		{
			string[] tables;
		}
	}
}
