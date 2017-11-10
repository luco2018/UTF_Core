using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // FilterSystem
    // - The base for the filter system
    // - Collects filtered data or pairs of data from SQL
    // - Sends to the Structure for it do view and compare

    public class FilterSystem : MonoBehaviour
    {
        // ------------------------------------------------------------------------------------
        // Variables



        // Singleton
        private static FilterSystem _Instance = null;
        public static FilterSystem Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = (FilterSystem)FindObjectOfType(typeof(FilterSystem));
                return _Instance;
            }
        }
	}
}
