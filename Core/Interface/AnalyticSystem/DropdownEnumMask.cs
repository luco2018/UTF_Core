using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    public class DropdownEnumMask : MonoBehaviour
    {

        public string _selection;
        public string _default;
        public bool predefined;
        public bool allowNone;
        public bool showAllOption;
        public InitialState _initState;
        public List<EnumMask> contents = new List<EnumMask>();
        public GameObject contentPrefab;
        public GameObject[] optionObjects;
        public RectTransform container;
        public Text label;
        [HideInInspector]
        public bool active;

        private static string allLabel = "Everything";
		private static string noneLabel = "Nothing";
		private static string mixLabel = "Mixed..";


        void Start()
        {
			if(_initState == InitialState.None && !allowNone) _initState = InitialState.First;
            if (predefined)
            {
                Init(null);
            }
        }

        void Init(string[] options)
        {
            int initCount = contents.Count;

            if (showAllOption)
            {
                contents.Insert(0, (new EnumMask(1)));
                contents[0].state = _initState == InitialState.All ? true : false;
				if(contents[0].state) label.text = contents[0].label;
            }
            if (allowNone)
            {
                contents.Insert(0, new EnumMask(0));
				contents[0].state = _initState == InitialState.None ? true : false;
            }

			if (_initState == InitialState.None) label.text = noneLabel;
			else if (_initState == InitialState.All) label.text = allLabel;

            int i = contents.Count - initCount;

            if(options != null)
			{
				foreach (string s in options)
                {
                    contents.Add(new EnumMask(s));
					contents[i].state = _initState == InitialState.All ? true : false;
                    if (_initState == InitialState.First && i == 0)
                    {
                        label.text = contents[i].label;
                    	contents[i].state = true;
                    }
                    i++;
                }
			}else
			{
                for (int o = i; o < contents.Count; o++)
				{
					contents[o].state = _initState == InitialState.All ? true : false;
					if (_initState == InitialState.First && o == 0)
                    {
                        label.text = contents[o].label;
                        contents[o].state = true;
                    }
				}
            }
            UpdateList();
        }

        void UpdateList()
        {

			foreach(GameObject go in optionObjects)
			{
                Destroy(go);
            }

            container.sizeDelta = new Vector2(0f, 28f * contents.Count);

            int i = 0;
            foreach(EnumMask content in contents)
			{
                GameObject go = Instantiate(contentPrefab, container, false);
                EnumMaskOption options = go.GetComponent<EnumMaskOption>();
                options.enumController = this;
                options.label.text = content.label;
                content.toggle = options.toggle;
				content.toggle.isOn = content.state;
                RectTransform rt = (RectTransform)go.transform;
                rt.localPosition = new Vector2(0f, -28f * i);
                go.SetActive(true);
                i++;
            }
            active = true;
        }

        public void SelectOption(int index, EnumMaskOption options, bool state)
        {
            active = false;
            if(options.label.text == noneLabel)
			{
				if(!state)
                    options.toggle.isOn = true;
				else
                    SetStates(0, true);
                label.text = noneLabel;
            }
			else if (options.label.text == allLabel)
            {
				if(allowNone)
                	SetStates(0, false);
				else
                    SetStates(-1, false);
                label.text = allLabel;
            }
			else
			{
                CheckStates(state, index);
            }
			active = true;
        }

		void SetStates(int index, bool on)
		{
            for (int i = 0; i < contents.Count; i++)
			{
                if (on)
                {
                    contents[i].state = (i == index);
                    contents[i].toggle.isOn = (i == index);
                }
				else
				{
                    contents[i].state = (i != index);
                    contents[i].toggle.isOn = (i != index);
                }
            }
        }

		void CheckStates(bool on, int index)
		{
			int offset = 0;

			if(allowNone)
                offset++;
			if(showAllOption)
                offset++;

            int count = 0;
            int lastIndex = -1;
            for (int i = offset; i < contents.Count; i++)
			{
                if(contents[i].toggle.isOn)
				{
					count++;
                    lastIndex = i;
                } 
            }

            if (on)
			{
				//check if everything, uncheck none, check everything
				if(count + offset == contents.Count)//everything is on
				{
                    if (showAllOption)//if everything button
                    {
                        int allIndex = allowNone ? 1 : 0;
                        contents[allIndex].state = true;
                        contents[allIndex].toggle.isOn = true;
                    }
                }
				else if(count == 1)
				{
                    label.text = contents[lastIndex].label;
                }
				else
				{
                    label.text = mixLabel;
                }
                if (allowNone)//turning something on, so that means none should be off
                {
                    contents[0].state = false;
                    contents[0].toggle.isOn = false;
                }
			}
			else
			{
                //check if none, check none
                if (count == 0)//everything is off
                {
                    if (allowNone)//if none button
                    {
                        contents[0].state = true;
                        contents[0].toggle.isOn = true;
                    }
					else//no none button, reselect last button
					{
						contents[index].state = true;
                        contents[index].toggle.isOn = true;
						label.text = contents[index].label;
					}
                }
                else if (count == 1)
                {
                    label.text = contents[lastIndex].label;
                }
				else
                {
                    label.text = mixLabel;
                }
                if (showAllOption)//turning something on, so that means none should be off
                {	
					int allIndex = allowNone ? 1 : 0;
                    contents[allIndex].state = false;
                    contents[allIndex].toggle.isOn = false;
                }
			}
        }

        [System.Serializable]
        public class EnumMask
        {
            public string label;
            public bool state;
            public Toggle toggle;

            public EnumMask(int option)
			{
				if(option == 0)
				{
                    label = "Nothing";
                }else if (option == 1)
                {
                    label = "Everything";
                }else
				{
                    label = "Void Entry";
                }
			}

			public EnumMask(string option)
            {
                label = option;
            }
        }


        public enum InitialState { None, All, First };

    }
}