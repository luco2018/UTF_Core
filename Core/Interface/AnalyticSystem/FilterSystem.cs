using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

		public TableContainer tableOptions;
		public List<string> tableStrings = new List<string>();
		private List<List<string>> suiteTestTypes = new List<List<string>>();
		public int tableCount = 0;
		// Singleton
		private static FilterSystem _Instance = null;

		public static FilterSystem Instance {
			get {
				if (_Instance == null)
					_Instance = (FilterSystem)FindObjectOfType (typeof(FilterSystem));
				return _Instance;
			}
		}

		public void Start()
		{
			if(Master.Instance.appMode == AppMode.Analytic)
			{
				StartCoroutine(GetTableNames(String.Format("SHOW TABLES LIKE '%{0}'", tableOptions.baselineBool._selection[0])));
			}
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

			suiteTestTypes.Add(new List<string>());
			suiteTestTypes.Add(new List<string>());

			for(int i = 0; i < tableStrings.Count; i++)
			{
				Debug.Log("splitting " + tableStrings[i]);
				string[] split = tableStrings[i].Split(new char[]{'_'}, StringSplitOptions.None);
				Debug.Log(split.Length);
				if(!suiteTestTypes[0].Contains(split[0]))
					suiteTestTypes[0].Add(split[0]);
				if(!suiteTestTypes[1].Contains(split[1]))
					suiteTestTypes[1].Add(split[1]);
			}
			tableOptions.suiteMask.Init(suiteTestTypes[0].ToArray());
			tableOptions.testTypeMask.Init(suiteTestTypes[1].ToArray());
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
