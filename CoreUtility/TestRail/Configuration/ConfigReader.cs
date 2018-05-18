using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ConfigReader : IConfigReader
{
    private JObject _currentJObject;
    private readonly string _configLocation;
    private readonly object _lock = new object();
    private const string DEFAULT_LOCATION = @"config.json";
    private const string SLACKAPI_CONFIGVALUE = "slack:apiToken";
    private const string TESTRAILUSER = "testrail:user";
    private const string TESTRAILPASS = "testrail:password";

    public ConfigReader() : this(DEFAULT_LOCATION) { }
    public ConfigReader(string configurationFile)
    {
        _configLocation = configurationFile;
        //SetConfigEntries();
    }

    public string SlackApiKey { get { return SLACKAPI_CONFIGVALUE; } }
    public string TestRailUser { get { return TESTRAILUSER; } }
    public string TestRailPass { get { return TESTRAILPASS; } }

    private static bool configCreated = false;

    //Retrieve and entry from the config file
    public T GetConfigEntry<T>(string entryName)
    {
        if (!configCreated)
            CreateBaseConfigFile();
        return GetJObject().Value<T>(entryName);
    }

    //Write/Update an entry into the config file
    public void SetConfigEntry<T>(string entryName, T value)
    {
        if (!configCreated)
            CreateBaseConfigFile();
        SetJObject<T>(entryName, value);
    }

    public void SetConfigEntries()
    {
        // SlackApiKey = GetConfigEntry<string>(SLACKAPI_CONFIGVALUE);
        // TestRailUser = GetConfigEntry<string>(TESTRAILUSER);
        // TestRailPass = GetConfigEntry<string>(TESTRAILPASS);
    }

    private JObject GetJObject()
    {
        lock (_lock)
        {
            if (_currentJObject == null)
            {
                string fileName = Path.Combine(PersitentLocation(), _configLocation);
                string json = File.ReadAllText(fileName);
                _currentJObject = JObject.Parse(json);
            }
        }

        return _currentJObject;
    }

    private void SetJObject<T>(string name, T value)
    {
        lock (_lock)
        {
            GetJObject();
            _currentJObject[name] = value.ToString();//TODO - this is alway making it a string, probably should not??
            string fileName = Path.Combine(PersitentLocation(), _configLocation);
            File.WriteAllText(fileName, _currentJObject.ToString());
        }
    }

    private string AssemblyLocation()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var codebase = new Uri(assembly.CodeBase);
        var path = Path.GetDirectoryName(codebase.LocalPath);
        return path;
    }

    //Using persistentDataPath, which on OSX is User/Library/Application Support/'CompanyName'/'ProductName'
    private string PersitentLocation()
    {
        var path = Application.persistentDataPath;
        return path;
    }

	//This creates a base json file if it doesnt exist already
	private void CreateBaseConfigFile()
	{
		string fileName = Path.Combine(PersitentLocation(), _configLocation);
        string baseJson = "{\n" +
                        "\"logFile\": \"log.txt\",\n" +
                        "\"adminPin\": 0\n" +
                        "}";
        File.WriteAllText(fileName, baseJson);
        configCreated = true;
    }

}