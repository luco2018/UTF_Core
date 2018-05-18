using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GraphicsTestFramework
{
	public class Navigation : MonoBehaviour 
	{

		// Singleton
		private static Navigation _Instance = null;
		public static Navigation Instance
		{
			get
			{
				if (_Instance == null)
					_Instance = (Navigation)FindObjectOfType(typeof(Navigation));
				return _Instance;
			}
		}

		public GameObject titleMenu;
		public Button runTestButton;
		public Button analyticButton;
		public NavMode _navMode;

		void Start()
		{
			runTestButton.interactable = SuiteManager.HasSuites();

			runTestButton.onClick.AddListener(RunTestClick);
			analyticButton.onClick.AddListener(AnalyticClick);

           Menu.Instance.MakeCanvasScaleWithScreenSize(titleMenu);
		}

		void RunTestClick()
		{
			_navMode = NavMode.testing;
			titleMenu.SetActive(false);
			Menu.Instance.menuParent.SetActive(true);
			StartCoroutine(ResultsIO.Instance.Init());//begin the baseline fetch/testsystem
		}

		void AnalyticClick()
		{
			_navMode = NavMode.analytic;
			titleMenu.SetActive(false);
			FilterSystem.Instance.canvas.SetActive(true);
			FilterSystem.Instance.BaseFilter();
		}

		public void ReturnHome()
		{
			switch(_navMode)
			{
				case NavMode.testing:
					if(!Menu.Instance.menuParent.activeSelf)
						Menu.Instance.SetMenuState(true);
					else
					{
						ReturnToTitle();
						Menu.Instance.SetMenuState(false);
					}
					break;
				case NavMode.analytic:
					if(!FilterSystem.Instance.canvas.activeSelf)
					{
						TestStructure.Instance.ClearStructure();
						if(TestRunner.Instance)
							Destroy(TestRunner.Instance);
						FilterSystem.Instance.canvas.SetActive(true);
					}
					else
					{
						ReturnToTitle();
						FilterSystem.Instance.canvas.SetActive(false);
					}
					break;
			}
		}

		void ReturnToTitle()
		{
			TestStructure.Instance.ClearStructure();
			if(TestRunner.Instance)
				Destroy(TestRunner.Instance);
			titleMenu.SetActive(true);
		}

		public enum NavMode
		{
			testing,
			analytic
		}

	}
}
