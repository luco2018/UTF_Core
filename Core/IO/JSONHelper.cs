using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;

namespace GraphicsTestFramework
{
	/// <summary>
	/// JSON helper.
	/// </summary>
	public class JSONHelper : MonoBehaviour
	{
		
		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		/// Converting to JSON
		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Converts a ResultsIO class 
		/// </summary>
		/// <returns>The JSO.</returns>
		/// <param name="inputData">Input data.</param>
		public static string[] ToJSON (ResultsIOData inputData)
		{
			if (inputData.resultsRow.Count <= 1) {
				Debug.LogWarning ("Invalid ResultsIOData sent to Serializer");
				return null;
			} else {
				string[] output = new string[inputData.resultsRow.Count];
				string[] tableLabels = new string[inputData.resultsRow [0].resultsColumn.Count];
				for (int i = 0; i < tableLabels.Length; i++)// column labels in string array
					tableLabels [i] = inputData.resultsRow [0].resultsColumn [i];
				for (int rows = 0; rows < inputData.resultsRow.Count; rows++) {
					output [rows] = "{";
					for (int columns = 0; columns < inputData.resultsRow [rows].resultsColumn.Count; columns++) {
						string value = inputData.resultsRow [rows].resultsColumn [columns];
						string s = "\"" + inputData.resultsRow [0].resultsColumn [columns] + "\":\"" + value + "\"";
						if (columns < inputData.resultsRow [0].resultsColumn.Count - 1)
							s += ",";
						output [rows] += s;
					}
					output [rows] += "}";
				}
				Console.Instance.Write (DebugLevel.Full, MessageLevel.Log, "Converted to JSON"); // Write to console
				return output;
			}
		}

		/// <summary>
		/// Takes a dictionary of key value pairs and returns a JSON formatted string.
		/// </summary>
		/// <returns>A string formatted as JSON</returns>
		/// <param name="inputData">Dictionary of key value pairs to be converted.</param>
		public static string ToJSON (Dictionary<string, string> inputData)
		{
			string output = "{";
			int i = 0;
			foreach (string key in inputData.Keys) {
				string value = inputData [key];
				string s = "\"" + key + "\":\"" + value + "\"";
				if (i < inputData.Count - 1)
					s += ",";
				output += s;
				i++;
			}
			output += "}";
			return output;
		}

		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------
		/// Converting from JSON
		/// ------------------------------------------------------------------------------------------------------------------------------------------------------------------------


		/// <summary>
		/// Converts a JSON string to a dictionary list.
		/// </summary>
		/// <returns>A dictionary of kay value pairs.</returns>
		/// <param name="inputData">Input unformatted JSON string.</param>
		public static Dictionary<string, string> JSON_Dictionary (string inputData)
		{
			string[] splitData = JSONToStringArray (inputData);
			Dictionary<string, string> convertedJSON = new Dictionary<string, string> ();

			for (int i = 0; i < splitData.Length / 2; i++) {
				convertedJSON.Add (splitData [i * 2], splitData [(i * 2) + 1]);
			}
			return convertedJSON;
		}

		/// <summary>
		/// Converts JSON to the ResultsIO class.
		/// </summary>
		/// <returns>A ResultsIO is returned.</returns>
		/// <param name="inputData">Input unformatted JSON string.</param>
		public static ResultsIOData FromJSON (string inputData)
		{
			if (inputData != null) {
				string[] splitData = JSONToStringArray (inputData);
				ResultsIOData data = new ResultsIOData ();//new ResultsIOData
				ResultsIORow row = new ResultsIORow ();
				data.resultsRow.Add (row);

				for (int i = 0; i < splitData.Length; i++) {
					int cur = i;
					string entry = splitData [cur];
					data.resultsRow [0].resultsColumn.Add (entry);
				}
				return data;
			} else
				return null;
		}

		/// <summary>
		/// Converts multiple JSON strings to the ResultsIO class, populating multiple results rows.
		/// </summary>
		/// <returns>A ResultsIO is returned.</returns>
		/// <param name="inputData">Array of unformatted JSON strings.</param>
		public static ResultsIOData FromJSON (string[] inputData)
		{
			if (inputData.Length != 0) {
				ResultsIOData data = new ResultsIOData ();//new ResultsIOData

				for (int a = 0; a < inputData.Length; a++) {
					string[] splitData = JSONToStringArray (inputData [a]);
					for (int i = 0; i < splitData.Length; i++) {
						data.resultsRow.Add (new ResultsIORow ());
						int cur = i;
						string entry = splitData [cur];
						data.resultsRow [a].resultsColumn.Add (entry);
					}
				}
				return data;
			} else
				return null;
		}


		static string[] JSONToStringArray (string JSON)
		{
			string[] separators = new string[]{ "\",\"", "\":\"" };//split by the two JSON separators
            JSON = JSON.Replace(System.Environment.NewLine, "");
            JSON = JSON.TrimStart('{').TrimStart('"');
            int stringEnd = JSON.LastIndexOf("\"");
            JSON = JSON.Remove(stringEnd);
            string[] splitData = JSON.Split(separators, System.StringSplitOptions.None);//remove curly brackets
            return splitData;
		}

		static ResultsDataCommon ArrayToResultsDataCommon (string[] splitData)
		{
			ResultsDataCommon RDC = new ResultsDataCommon ();

			for (int i = 0; i < splitData.Length; i++) {
				switch (i) {
				case 1:
					RDC.DateTime = splitData [i];
					break;
				case 3:
					RDC.UnityVersion = splitData [i];
					break;
				case 5:
					RDC.AppVersion = splitData [i];
					break;
				case 7:
					RDC.OS = splitData [i];
					break;
				case 9:
					RDC.Device = splitData [i];
					break;
				case 11:
					RDC.Platform = splitData [i];
					break;
				case 13:
					RDC.API = splitData [i];
					break;
				case 15:
					RDC.RenderPipe = splitData [i];
					break;
				case 17:
					RDC.GroupName = splitData [i];
					break;
				case 19:
					RDC.TestName = splitData [i];
					break;
				case 21:
					RDC.PassFail = bool.Parse (splitData [i]);
					break;
				case 23:
					RDC.Custom = splitData [i];
					break;
				default:
					break;
				}
			}
			return RDC;
		}

	}
}
