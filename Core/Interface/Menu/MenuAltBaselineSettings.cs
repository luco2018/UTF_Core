using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
    public class MenuAltBaselineSettings : MonoBehaviour
    {

        public Dropdown _platformDropdown;
        public Dropdown _apiDropdown;
        private AltBaselineSettings _altBaseline;
        public AltBaselineSettings _newAltBaseline;
        private NameValueCollection _altBaselineSets;

        // Use this for initialization
        void OnEnable()
        {
            Master.baselinesChanged += UpdateData;
            MenuSettings.saveSettings += ApplyNewBaselineSet;
            MenuSettings.revertSettings += CancelBaselineSet;
        }

        //Desubscribe from event delegates
        void OnDisable()
        {
            Master.baselinesChanged -= UpdateData;
            MenuSettings.saveSettings -= ApplyNewBaselineSet;
            MenuSettings.revertSettings -= CancelBaselineSet;
        }

        void ApplyNewBaselineSet()
        {
            ResultsIO.Instance.PullAltBaselines(_newAltBaseline.Platform, _newAltBaseline.API);
            UpdateData();
        }

        void CancelBaselineSet()
        {
            _newAltBaseline.Platform = _altBaseline.Platform;
            _newAltBaseline.API = _altBaseline.API;
            UpdateData();
        }

        // Update is called once per frame
        void UpdateData()
        {
            _altBaseline = Master.Instance.GetCurrentPlatformAPI();
            _altBaselineSets = Master.GetAltBaselines();
            int platIndex = UpdatePlatformDropdown();
            _platformDropdown.value = platIndex;
            int apiIndex = UpdateAPIDropdown(_altBaseline.Platform);
            _apiDropdown.value = apiIndex;
        }

        public void SetPlatformDropdown(int index)
        {
            _newAltBaseline.Platform = _altBaselineSets.AllKeys[index];
            UpdateAPIDropdown(_altBaselineSets.AllKeys[index]);
            _apiDropdown.value = 0;
            _newAltBaseline.API = _altBaselineSets.GetValues(index)[0];
        }

        public void SetAPIDropdown(int index)
        {
            _apiDropdown.value = index;
            _newAltBaseline.API = _altBaselineSets.GetValues(_platformDropdown.value)[index];
        }

        public int UpdatePlatformDropdown()
        {
            _platformDropdown.ClearOptions();
            List<string> options = new List<string>();
            int keyIndex = 0;
            int curKey = 0;
            foreach(string platform in _altBaselineSets.AllKeys)
            {
                options.Add(platform);
                if(platform == _altBaseline.Platform)
                    curKey = keyIndex;
                keyIndex++;
            }
            _platformDropdown.AddOptions(options);
            return curKey;
            //UpdateAPIDropdown(_altBaseline.Platform);
        }

        int UpdateAPIDropdown(string platform)
        {
            _apiDropdown.ClearOptions();
            List<string> options = new List<string>();
            int keyIndex = 0;
            int curKey = 0;
            foreach (string api in _altBaselineSets.GetValues(platform))
            {
                options.Add(api);
                if (api == _altBaseline.API)
                    curKey = keyIndex;
                keyIndex++;
            }
            _apiDropdown.AddOptions(options);
            return curKey;
        }
    }
}
