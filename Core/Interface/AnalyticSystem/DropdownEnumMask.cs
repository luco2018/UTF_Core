using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicTestFramework
{
	public class DropdownEnumMask : MonoBehaviour {

		public string _selection;
		public string _default;
		public bool predefined;
		public List<EnumMaskOption> contents = new List<EnumMaskOption>();
		public GameObject contentPrefab;
		private ToggleGroup _toggleGrp;

		void Start()
		{
			if(predefined)
			{
				UpdateList();
			}
		}

		void Init (string[] options) 
		{
			foreach (string s in options)
			{
				EnumMaskOption enumMask = new EnumMaskOption();
				enumMask.label = s;
				contents.Add(enumMask);
			}
			UpdateList();
			
		}

		void UpdateList () 
		{
			
		}

		void SelectOption()
		{

		}

		[System.Serializable]
		public class EnumMaskOption
		{
			public string label;
			public bool state;
		}

	}
}