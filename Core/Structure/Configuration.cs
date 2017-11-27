using System;
using UnityEngine;

namespace GraphicsTestFramework
{
    public class Configuration : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables

        // Singleton
        private static Configuration _Instance = null;
        public static Configuration Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (Configuration)FindObjectOfType(typeof(Configuration));
                return _Instance;
            }
        }

        public Settings settings = new Settings();
        private readonly IConfigReader _configReader = new ConfigReader();

        public void SetTestrailUserName(string username)
        {
            _configReader.SetConfigEntry<string>("testrail:user", username);
        }

        public void SetTestrailPassword(string password)
        {
            _configReader.SetConfigEntry<string>("testrail:password", Common.EncryptPassword(password));
        }

        public void SetSlackToken(string token)
        {
            _configReader.SetConfigEntry<string>("slack:token", Common.EncryptPassword(token));
        }

        [Serializable]
        public class Settings
        {
            public bool testviewerOnAutomationTestFail;
        }
    }
}

