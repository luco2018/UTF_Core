using System;
using System.Collections.Generic;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // TestTypes
    // - Master dictionary of all test types
    // - Used to get a test type

    public static class TestTypes
    {
        static Dictionary< int, Type> m_TypeList = new Dictionary<int, Type>
        {
            //{#, typeof(ExampleModel) }, // We dont include ExampleModel here as it is only for reference
            {0, typeof(AverageFrameTimeModel) },
            {1, typeof(FrameComparisonModel) }
        };

        public static Dictionary<int, Type> typeList { get { return m_TypeList; } }

        // ------------------------------------------------------------------------------------
        // Get Data

        // Get a type from its index
        public static Type GetTypeFromIndex(int index)
        {
            return m_TypeList[index]; // Return requested type
        }

        // Get a type from its name
        public static Type GetTypeFromString(string name)
        {
            for (int i = 0; i < m_TypeList.Count; i++)
            {
                if(m_TypeList[i].Name == (name + "Model"))
                    return m_TypeList[i]; // Return requested type
            }
            return null;
        }

        // Get an index from type
        public static int GetTypeIndexFromType(Type type)
        {
            for (int i = 0; i < m_TypeList.Count; i++)
            {
                if (m_TypeList[i] == type)
                    return i; // Return requested type
            }
            return -1;
        }

        // Get an array of test types names
        public static string[] GetTypeStringList()
        {
            string[] output = new string[m_TypeList.Count]; // Create array of type list length
            for (int i = 0; i < m_TypeList.Count; i++) // Iterate types
                output[i] = m_TypeList[i].ToString().Replace("GraphicsTestFramework.", "").Replace("Model", ""); // Set entry
            return output; // Return
        }

        // Get a logic type from list index
        public static object GetModelInstance(int index)
        {
            var T = GetTypeFromIndex(index); // Get type at an index
            return Activator.CreateInstance(T); // Create and return an instance
        }
    }

}
