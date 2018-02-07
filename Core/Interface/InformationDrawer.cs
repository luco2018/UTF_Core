using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    public class InformationDrawer : MonoBehaviour
    {
        public List<InformationEntry> informationEntries = new List<InformationEntry>();
        public bool updateOnVisible;
        private ResultsDataCommon commmonData;
        private ResultsDataCommon sysCommonData;

        void OnEnable()
		{
			if(updateOnVisible)
                UpdateInformation();
        }

        // Update is called once per frame
        public void UpdateInformation()
        {
            sysCommonData = Common.GetCommonResultsData();
            foreach(InformationEntry ie in informationEntries)
			{
				switch(ie.source)
				{
					case InformationSource.SystemData:
                    {
                        PopulateInfo(ie.text, FetchInfo(ie.fields, ie.split, ie.splitIndex));
                        break;
					}
					case InformationSource.CommonData:
					{
						PopulateInfo(ie.text, FetchInfo(ie.fields, ie.split, ie.splitIndex));
						break;
					}
					case InformationSource.AlternativeBaseline:
                    {
                        AltBaselineSettings altSettings = Master.Instance.GetCurrentPlatformAPI();
                        if(ie.fields == CommonFields.Platform)
						{
							ie.text.text = altSettings.Platform;
							if(altSettings.Platform != sysCommonData.Platform)
								ie.text.color = Color.yellow;
							else
								ie.text.color = Color.white;
						}
						else if(ie.fields == CommonFields.API)
						{
							ie.text.text = altSettings.API;
							if (altSettings.API != sysCommonData.API)
								ie.text.color = Color.yellow;
							else
								ie.text.color = Color.white;
						}
						else
						{
							ie.text.text = "N/A";
							ie.text.color = Color.red;
						}
						break;
					}
                }
			}
        }

		public void SetAndUpdateInformation(ResultsDataCommon rdc)
		{
            commmonData = rdc;
            UpdateInformation();
        }

		void PopulateInfo(Text text, string info)
		{
			if (info != null)
            {
                text.text = info;
                text.color = Color.white;
            }
            else
            {
            	text.text = "N/A";
                text.color = Color.red;
            }
		}

		string FetchInfo(CommonFields field, bool split, int splitPoint)
		{
            string output;
            if (!split)
            {
                output = (string)sysCommonData.GetType().GetField(field.ToString()).GetValue(sysCommonData);
            }
            else
            {
                output = (string)sysCommonData.GetType().GetField(field.ToString()).GetValue(sysCommonData);
                output = output.Split('|')[splitPoint];
            }
            return output;
        }

		[Serializable]
		public class InformationEntry
		{
            public InformationSource source;
            public CommonFields fields;
            public bool split;
            public int splitIndex;
            public Text text;
        }

        public enum CommonFields
        {
			DateTime,
			UnityVersion,
			AppVersion,
			OS,
			Device,
			Platform,
			API,
			RenderPipe,
			GroupName,
			TestName,
			PassFail,
			Custom
		}

		public enum InformationSource
		{
			SystemData,
			CommonData,
			AlternativeBaseline
		}
}
}
