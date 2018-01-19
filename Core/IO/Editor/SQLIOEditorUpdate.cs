using System.Collections;
using System.Collections.Generic;
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

		[MenuItem("UTF/Toggle SQLIO update")]
		static void ToggleSQLIOUpdate()
		{
            if (_running)
                EditorApplication.update -= SQL.SQLIO.Update;
            else
                EditorApplication.update += SQL.SQLIO.Update;
            _running = !_running;
			PlayerPrefs.SetInt("SQLIOUpdate", _running == true ? 1 : 0);
        }

		[MenuItem("UTF/Test SQLIO Coroutine")]
		static void Test()
		{
            SQL.SQLIO.StartCoroutine(SQL.SQLIO.TestCoroutine());
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
