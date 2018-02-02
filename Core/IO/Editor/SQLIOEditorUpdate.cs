using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;
using UnityEditor;

namespace GraphicsTestFramework
{
	[InitializeOnLoad]
    public class SQLIOEditorUpdate : Editor
    {
        static bool _running = true;

        // Update is called once per frame
        static SQLIOEditorUpdate()
        {
            if(PlayerPrefs.HasKey("SQLIOUpdate"))
            	_running = PlayerPrefs.GetInt("SQLIOUpdate") == 1 ? true : false;
			if(_running)
            	EditorApplication.update += SQL.SQLIO.Update;
            EditorApplication.playmodeStateChanged += PauseState;
        }

		[MenuItem("UTF/SQL/Toggle SQLIO update")]
		static void ToggleSQLIOUpdate()
		{
            if (_running)
                EditorApplication.update -= SQL.SQLIO.Update;
            else
                EditorApplication.update += SQL.SQLIO.Update;
            _running = !_running;
			PlayerPrefs.SetInt("SQLIOUpdate", _running == true ? 1 : 0);
        }

		[MenuItem("UTF/SQL/Test SQLIO Coroutine")]
		static void Test()
		{
            SQL.SQLIO.StartCoroutine(SQL.SQLIO.TestCoroutine());
        }

        [MenuItem("UTF/SQL/Test SQLIO SuiteBaseline Check")]
        static void BaselineSetChecl()
        {
            NameValueCollection fullSet;
            SQL.SQLIO.StartCoroutine(SQL.SQLIO.BaselineSetCheck(new string[]{"Debug"}, (value => { fullSet = value; })));
        }

        [MenuItem("UTF/SQL/Database Mode/Staging")]
        static void StagingSQL()
        {
            SQL.SQLIO._webservice = SQL.SQLIO._stagingWebserver;
        }

        [MenuItem("UTF/SQL/Database Mode/Live")]
        static void LiveSQL()
        {
            SQL.SQLIO._webservice = SQL.SQLIO._liveWebservice;
        }

		static void PauseState()
		{
			if(EditorApplication.isPlaying)
            {
				if(_running)
					EditorApplication.update -= SQL.SQLIO.Update;
			}
			else
			{
				if (_running)
                    EditorApplication.update += SQL.SQLIO.Update;
			}
        }
    }
}
