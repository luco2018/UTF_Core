using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gurock.TestRail;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using GraphicsTestFramework;

public class SubmitResultsToTestRail : MonoBehaviour {
	private readonly IConfigReader _configReader = new ConfigReader();
	private List<int> listOfPassFail = new List<int>();

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	private APIClient ConnectToTestrail()
	{
		APIClient client = new APIClient("http://qatestrail.hq.unity3d.com");
		client.User = _configReader.TestRailUser;
		client.Password = _configReader.TestRailPass;
		return client;
	}

	// private void AddResultToTestRail(APIClient client, string runID, string caseID, string status_id)
	// {
	// 	var resultObject = CreateResultData(status_id);
	// 	JObject jObj = (JObject)client.SendPost("add_result_for_case/"+runID+"/"+caseID, resultObject);
	// }

	private Dictionary<string,object> CreateResultData(string status_id)
	{
		//objectdata needs to be a json i think
		//http://docs.gurock.com/testrail-api2/reference-results#add_result

		var resultObject = new Dictionary<string,object>
		{
			{"status_id", status_id}
		};
		return resultObject;
	}

	private IEnumerator BuildResults(string runID)
	{
		listOfPassFail.Clear();
		TestStructure.Structure structure = TestStructure.Instance.GetStructure(); // Get structure
		for (int su = 0; su < structure.suites.Count; su++) // Iterate suites 
		{
			string suiteName = structure.suites[su].suiteName; // Get suite name
			for (int ty = 0; ty < structure.suites[su].types.Count; ty++) // Iterate types
			{
				int typeIndex = structure.suites[su].types[ty].typeIndex; // Get type index
				string typeName = structure.suites[su].types[ty].typeName; // Get type name
				for (int gr = 0; gr < structure.suites[su].types[ty].groups.Count; gr++) // Iterate groups
				{
					string groupName = structure.suites[su].types[ty].groups[gr].groupName; // Get group name
					for (int te = 0; te < structure.suites[su].types[ty].groups[gr].tests.Count; te++) // Iterate tests
					{
						string testName = structure.suites[su].types[ty].groups[gr].tests[te].testName; // Get test name
						string caseID = structure.suites[su].types[ty].groups[gr].tests[te].caseID;
						string scenePath = structure.suites[su].types[ty].groups[gr].tests[te].scenePath; // Get scene path
						ResultsDataCommon common = Common.BuildResultsDataCommon(groupName, testName); // Build results data common to retrieve results
						ResultsIOData data = ResultsIO.Instance.RetrieveResult(suiteName, typeName, common); // Retrieve results data

						int passFail = 0; // Set default state (no results)
						if (data != null) // If results data exists
						{ 
							passFail = data.resultsRow[0].resultsColumn[21] == "True" ? 1 : 5; // Set pass fail state
							//listOfPassFail.Add(passFail);
							try
							{
								APIClient client = ConnectToTestrail();
								var resultObject = CreateResultData(passFail.ToString());
								JObject jObj = (JObject)client.SendPost("add_result_for_case/"+runID+"/"+caseID, resultObject);
							}
							catch
							{
								// login failed
							}
						}
						yield return null;
					}
				}
			}
		}
	}
}
