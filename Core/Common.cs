using System;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using Encrypto;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Common (Static)
    // - Common results data structures and returns
    // - Common conversion methods
    // - Common helper functions

    public class Common : MonoBehaviour
    {
        // Framework Information
        public static string applicationVersion = "1.0b4";


        // ------------------------------------------------------------------------------------
        // System

        /// <summary>
        /// Encrypts a user's password. This can only be invoked from the common class.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string EncryptPassword(string password)
        {
            string T = typeof(Common).Name;
            string ePassword = "Failed to encrypt";
            try
            {
                ePassword = Encrypto.Tools.EncryptString(password, T);
            }
            catch (System.Exception exception)
            {
                Debug.LogError(exception.Message);
            }
            return ePassword;
        }

        /// <summary>
        /// Decrypts a user's password. This can only be invoked from the common class.
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string DecryptPassword(string password)
        {
            string T = typeof(Common).Name;
            string ePassword = "Failed to decrypt";
            try
            {
                ePassword = Tools.DecryptString(password, T);
            }
            catch (Exception exception)
            {
                Debug.LogError(exception.Message);
            }
            return ePassword;
        }


        // Get command line argument
        public static string GetArg(string name)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Getting command line arguments"); // Write to console
            if (Application.platform != RuntimePlatform.Android) // Fails on these platforms
            {
                var args = System.Environment.GetCommandLineArgs(); // Get all arguments
                for (int i = 0; i < args.Length; i++) // Iterate
                {
                    if (args[i] == "-" + name && args.Length > i + 1) // If arg matches and has value
                    {
                        return args[i + 1]; // Return value of arg
                    }
                }
            }
            return ""; // Fail
        }

        // Quit application
        public static void QuitApplication()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Quitting application"); // Write to console
            if (!Application.isEditor && Application.platform != RuntimePlatform.IPhonePlayer) // If not editor or iOS
                System.Diagnostics.Process.GetCurrentProcess().Kill(); // Kill process
            else if (Application.platform == RuntimePlatform.IPhonePlayer) // If iOS
                Application.Quit(); // Quit
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // ------------------------------------------------------------------------------------
        // Shared data

        // Resolutions
        public static Dictionary<FrameResolution, Vector2> frameResolutionList = new Dictionary<FrameResolution, Vector2>
        {
            //{#, typeof(ExampleModel) }, // We dont include ExampleModel here as it is only for reference
            {FrameResolution.nHD , new Vector2(640, 360) },
            {FrameResolution.qHD , new Vector2(960, 540) },
            {FrameResolution.HD , new Vector2(1280, 720) },
            {FrameResolution.FullHD , new Vector2(1920, 1080) },
        };

        // Date time format string
        public static string dateTimeFormat = "yyyy-MM-dd\\THH:mm:ss\\Z";

        // Unity Versions
        public static string[] unityVersionList = new string[7]
        {
            "5.6",
            "2017.1",
            "2017.2",
            "2017.3",
            "2018.1",
            "2018.2",
            "2018.3"
        };

        // ------------------------------------------------------------------------------------
        // Get Common Data

        // Get common data about platform and version
        public static ResultsDataCommon GetCommonResultsData()
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Getting common results data"); // Write to console
            ResultsDataCommon output = new ResultsDataCommon(); // Create new class instance
            output.DateTime = Master.Instance.GetSystemTime().ToString(dateTimeFormat); // Get SystemTime from Master
            SystemData systemData = Master.Instance.GetSystemData(); // Get SystemData from Master
            output.UnityVersion = systemData.UnityVersion; // Extract from SystemData
            output.AppVersion = systemData.AppVersion; // Extract from SystemData
            output.OS = systemData.OS; // Extract from SystemData
            output.Device = systemData.Device; // Extract from SystemData
            output.Platform = systemData.Platform; // Extract from SystemData
            output.API = systemData.API; // Extract from SystemData
            output.RenderPipe = GetRenderPipelineName(); // Get the currently assigned pipeline
            output.Custom = ""; // Futureproof
            return output; // Return
        }

        // ------------------------------------------------------------------------------------
        // Common Conversions

        // Convert string to dropdown option data
        public static UnityEngine.UI.Dropdown.OptionData ConvertStringToDropdownOptionData(string input)
        {
            UnityEngine.UI.Dropdown.OptionData newOption = new UnityEngine.UI.Dropdown.OptionData(); // Create new OptionData
            newOption.text = input; // Set text
            return newOption;
        }

        // Convert Array types ready to be serialized for saving
        public static string ConvertStringArrayToString(string[] input)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Converting String Array to String"); // Write to console
            StringBuilder stringBuilder = new StringBuilder(); // Create new StringBuilder
            foreach (string value in input) // Iterate input strings
            {
                stringBuilder.Append(value); // Append current input string
                stringBuilder.Append('|'); // Append character to split
            }
            return stringBuilder.ToString(); // Return
        }

        // Convert a Base64 String to a Texture2D
        // TODO - Remove commented code related to texture resolution, format and filtermode
        public static Texture2D ConvertStringToTexture(string textureName, /*byte[] input*/string input/*, Vector2 resolution, TextureFormat format, FilterMode filterMode*/)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Converting String to Texture2D"); // Write to console
            Texture2D output = new Texture2D(2, 2); // Create output Texture2D
            output.name = textureName; // Set texture name
            byte[] decodedBytes = new byte[input.Length / 2]; // Create byte array to hold data
            for (int i = 0; i < input.Length; i += 2)
            { // Convert input string from Hex to byte array
                decodedBytes[i / 2] = Convert.ToByte(input.Substring(i, 2), 16);
            }
            output.LoadImage(decodedBytes); // Load image (PNG)
            return output; // Return
        }

        // Convert a Texture2D to a HEX string
        public static string ConvertTextureToString(Texture2D texture)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Converting Texture2D to String"); // Write to console
            byte[] bytes = texture.EncodeToPNG(); // Create Byte array
            StringBuilder sb = new StringBuilder();
            foreach (byte b in bytes)
                sb.Append(b.ToString("X2"));//Add bytes as Hex values
            return sb.ToString(); // Return
        }

        // Convert a RenderTexture to a Texture2D
        public static Texture2D ConvertRenderTextureToTexture2D(string textureName, RenderTexture input, Vector2 resolution, TextureFormat format, FilterMode filterMode)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Converting Render Texture to Texture2D"); // Write to console
            RenderTexture.active = input; // Set input as active RenderTexture
            Texture2D output = new Texture2D((int)resolution.x, (int)resolution.y, format, false); // Create new output Texture2D
            output.name = textureName; // Set texture name
            output.filterMode = filterMode; // Set filter mode
            output.ReadPixels(new Rect(0, 0, (int)resolution.x, (int)resolution.y), 0, 0); // Read pixels from active RenderTexture
            output.Apply(); // Apply
            RenderTexture.active = null; // Null active RenderTexture
            return output; // Return
        }

        // ------------------------------------------------------------------------------------
        // Helper functions

#if UNITY_EDITOR
        // Check if editor build target is standalone
        public static bool IsStandaloneTarget(UnityEditor.BuildTarget target)
        {
            bool output = false; // Create output

            if(target.ToString().Contains("Standalone"))//is it a standalone platform?
                output = true;

            return output; // Return
        }

#endif

        // Check if a test is applicable
        public static bool IsTestApplicable(Test input)
        {
            if (input.run == false) // If set to disabled
                return false; // Return false
            if (!IsCurrentPlatformInBitMask(input.platforms)) // If platform check fails
                return false; // Return false
            if (!IsUnityVersionAboveMinimum(input.minimumUnityVersion)) // If version check fails
                return false; // Return false
            return true; // All passed. Return true
        }

        // Find if current platform is selected within a platform bitmask
        public static bool IsCurrentPlatformInBitMask(int input)
        {
            int[] selectedPlatforms = GetPlatformSelectionFromBitMask(input); // Get selected platform indices from bitmask
            for (int i = 0; i < selectedPlatforms.Length; i++) // Iterate selected platforms
            {
                if (Enum.GetNames(typeof(RuntimePlatform))[selectedPlatforms[i]] == Application.platform.ToString()) // If index in full platform list matches current platform
                    return true; // Set to continue
            }
            return false; // Return false
        }

        // Find if Unity version is above specified index from unityVersionList
        public static bool IsUnityVersionAboveMinimum(int input)
        {
            ProjectSettings projectSettings = SuiteManager.GetProjectSettings(); // Get settings
            int versionIndex = 0; // Create version index
            for (int i = 0; i < unityVersionList.Length; i++) // Iterate version list
            {
                if (projectSettings.unityVersion.Contains(unityVersionList[i])) // If unity version contains current index
                    versionIndex = i; // Set output index
            }
            if (input > versionIndex) // If minimum is higher than current
                return false; // Return false
            else
                return true; // Return true
        }

        // Get a platform selection array from bitmask
        public static int[] GetPlatformSelectionFromBitMask(int bitMask)
        {
            int length = Enum.GetNames(typeof(RuntimePlatform)).Length; // Get length of platform list
            List<int> intList = new List<int>(); // Create int list to track
            for (int i = 0; i < length; i++) // Iterate platform list
            {
                if (bitMask == (bitMask | (1 << i))) // If bit mask returns true
                {
                    intList.Add(i); // Add to list
                }
            }
            return intList.ToArray(); // Return list as array
        }

        public enum TimePeriod { Year, Month, Day, Hour, Minute, Second, Closest };

        // Compare two DateTimes and return the difference
        public static float GetTimeDifference(DateTime start, DateTime end, ref TimePeriod period)
        {
            float output = 0f; // Create output
            switch (period) // Switch on incoming time period
            {
                case TimePeriod.Year:
                    output = (float)(end - start).TotalDays / 365; // Return years
                    break;
                case TimePeriod.Month:
                    output = (float)(end - start).TotalDays / 31; // Return months (approx)
                    break;
                case TimePeriod.Day:
                    output = (float)(end - start).TotalDays; // Return days
                    break;
                case TimePeriod.Hour:
                    output = (float)(end - start).TotalHours; // Return hours
                    break;
                case TimePeriod.Minute:
                    output = (float)(end - start).TotalMinutes; // Return minutes
                    break;
                case TimePeriod.Second:
                    output = (float)(end - start).TotalSeconds; // Return seconds
                    break;
                case TimePeriod.Closest:
                    if ((end - start).TotalDays >= 365) // If over a year ago
                    {
                        output = Mathf.Floor((float)(end - start).TotalDays / 365); // Round years
                        period = TimePeriod.Year; // Set period
                    }
                    else if ((end - start).TotalDays >= 31) // If over a month ago (approx)
                    {
                        output = Mathf.Floor((float)(end - start).TotalDays / 31); // Round months (approx)
                        period = TimePeriod.Month; // Set period
                    }
                    else if ((end - start).TotalDays >= 1) // If over a day ago
                    {
                        output = Mathf.Floor((float)(end - start).TotalDays); // Return days
                        period = TimePeriod.Day; // Set period
                    }
                    else if ((end - start).TotalHours >= 1) // If over an hour ago
                    {
                        output = Mathf.Floor((float)(end - start).TotalHours); // Return hours
                        period = TimePeriod.Hour; // Set period
                    }
                    else if ((end - start).TotalMinutes >= 1) // If over a minute ago
                    {
                        output = Mathf.Floor((float)(end - start).TotalMinutes); // Return minutes
                        period = TimePeriod.Minute; // Set period
                    }
                    else
                    {
                        output = Mathf.Floor((float)(end - start).TotalSeconds); // Return seconds
                        period = TimePeriod.Second; // Set period
                    }
                    break;
            }
            return output; // Return
        }

        // Prevent divide by zero
        static public float SafeDivision(float numerator, float denominator)
        {
            return (denominator == 0) ? 0 : numerator / denominator;
        }

        // Get a comparison value from a texture
        public static float GetTextureComparisonValue(Texture2D baselineInput, Texture2D resultsInput)
        {
            Console.Instance.Write(DebugLevel.Full, MessageLevel.Log, "Getting Texture2D comparison value"); // Write to console
            float value = 0; // Create float for total pixel value
            int i = 0; // Create index for pixel count
            for (int x = 0; x < resultsInput.width; x++) // Iterate pixel columns
            {
                for (int y = 0; y < resultsInput.height; y++) // Iterate pixel rows
                {
                    Color c1 = baselineInput.GetPixel(x, y); // Get pixel from Baseline texture
                    Color c2 = resultsInput.GetPixel(x, y); // Get pixel from Results texture
                    float compR = Mathf.Abs(c2.r - c1.r); // Get comparison value from red channel
                    float compG = Mathf.Abs(c2.g - c1.g); // Get comparison value from green channel
                    float compB = Mathf.Abs(c2.b - c1.b); // Get comparison value from blue channel
                    value += ((compR + compG + compB) / 3); // Add average comparison value to total pixel value
                    i++; // Increment pixel count index
                }
            }
            return (value / i) * 100; // Divide total value by pixel count and multiply by 100 to return average percent
        }

        public static void SetTestSettings(TestSettings testSettings)
        {
            QualitySettings.pixelLightCount = testSettings.pixelLightCount;
            QualitySettings.anisotropicFiltering = testSettings.anisotropicFiltering;
            QualitySettings.antiAliasing = testSettings.antiAliasing;
            QualitySettings.softParticles = testSettings.softParticles;
            QualitySettings.realtimeReflectionProbes = testSettings.realtimeReflectionProbes;
            QualitySettings.billboardsFaceCameraPosition = testSettings.billboardsFacingCameraPosition;
            QualitySettings.shadows = testSettings.shadows;
            QualitySettings.shadowResolution = testSettings.shadowResolution;
            QualitySettings.shadowProjection = testSettings.shadowProjection;
            QualitySettings.shadowDistance = testSettings.shadowDistance;
            QualitySettings.shadowNearPlaneOffset = testSettings.shadowNearPlaneOffset;
            QualitySettings.shadowCascades = testSettings.shadowCascades;
            QualitySettings.shadowCascade2Split = testSettings.shadowCascade2Split;
            QualitySettings.shadowCascade4Split = testSettings.shadowCascade4Split;
            QualitySettings.blendWeights = testSettings.blendWeights;
            QualitySettings.vSyncCount = testSettings.vSyncCount;
            QualitySettings.lodBias = testSettings.lodBias;
            QualitySettings.maximumLODLevel = testSettings.maximumLodLevel;
            QualitySettings.particleRaycastBudget = testSettings.particleRaycastBudget;
            QualitySettings.asyncUploadTimeSlice = testSettings.asyncUploadTimeSlice;
            QualitySettings.asyncUploadBufferSize = testSettings.asyncUploadBufferSize;
        }

        // Get the active Render Pipeline asset
        public static RenderPipelineAsset GetRenderPipeline()
        {
            return GraphicsSettings.renderPipelineAsset;
        }

        // Get the active Render Pipeline name
        public static string GetRenderPipelineName()
        {
            string defaultPipeline = "Standard Legacy"; // If no pipeline is loaded then will be set to this
            if (GraphicsSettings.renderPipelineAsset == null)
                return defaultPipeline; // return the default pipeline string
            else
                return GraphicsSettings.renderPipelineAsset.GetType().ToString() + "|" + GraphicsSettings.renderPipelineAsset.name; // Gets the currently active pieplines name in 5.6
        }

        // Generate random UUID
        public static string RandomUUID()
        {
            //Semi-random UUID
            string uuid = String.Format("{0:ssmmffffyyyyMMddHH}", DateTime.Now) + UnityEngine.Random.Range(1000, 9999);
            string converted = "";
            //Turns the second half into a char string
            for (int i = (uuid.Length / 2) - 1; i < uuid.Length; i += 2)
            {
                int num;
                int.TryParse(uuid.Substring(i, 2), out num);
                num += 32;
                if (num < 48)
                    num += 48;
                if (num > 57 && num < 65)
                    num += 20;
                if (num > 90 && num < 97)
                    num += 15;
                if (num > 122)
                    num = 122;
                converted += (char)num;
            }
            //Turns the first half into a summed interger
            int count = 0;
            for (int i = 0; i < (uuid.Length / 2); i += 2)
            {
                count += Mathf.Abs(uuid[i] - uuid[i + 1]);
            }
            converted = count.ToString() + converted;
            return converted;
        }


        //Addcustom entry
        public static string CustomEntry(string key, string input)
        {
            string output = key + "|" + input + "<>|<>";
            return output;
        }

        public static Texture2D ResizeInto(Texture2D source, int width, int height)
        {
            Texture2D resize = new Texture2D(width, height, source.format, false);
            resize.wrapMode = TextureWrapMode.Clamp;
            Color[] destPix = new Color[width * height];
            int y = 0;
            while (y < height)
            {
                int x = 0;
                while (x < width)
                {
                    float xFrac = x * 1.0F / (width);
                    float yFrac = y * 1.0F / (height);
                    destPix[y * width + x] = source.GetPixelBilinear(xFrac, yFrac);
                    x++;
                }
                y++;
            }
            resize.SetPixels(destPix);
            resize.Apply();
            return resize;
        }

        //Linear to gamma colorspace
        public static Color ConvertToGamma(Color _color)
        {
            _color.r = Mathf.GammaToLinearSpace(_color.r);
            _color.g = Mathf.GammaToLinearSpace(_color.g);
            _color.b = Mathf.GammaToLinearSpace(_color.b);
            return _color;
        }

        // Return an ResultsCommon index from a Field Name input
        public static int FindResultsDataIOFieldIdByName(ResultsIOData results, string name)
        {
            for (int i = 0; i < results.fieldNames.Count; i++) // Iterate field names
            {
                if (results.fieldNames[i] == name) // If matches query
                    return i; // Return
            }
            return -1; // Fail
        }

        // Generates a dummy Test instance from a TestEntry
        public static Test GenerateTestFromTestEntry(TestEntry input)
        {
            Test output = new Test(); // Create output
            output.name = input.testName; // Set name as thats all we need
            return output; // Return
        }
        
    }    
	
	// ------------------------------------------------------------------------------------
    // Global Data Structures

    [System.Serializable]
    public class ResultsDataCommon
    {
        public string DateTime;
        public string UnityVersion;
        public string AppVersion;
        public string OS;
        public string Device;
        public string Platform;
        public string API;
        public string RenderPipe;
        public string GroupName;
        public string TestName;
        public bool PassFail;
        public string Custom;

        public ResultsDataCommon Clone()
        {
            ResultsDataCommon output = new ResultsDataCommon();
            output.DateTime = this.DateTime;
            output.UnityVersion = this.UnityVersion;
            output.AppVersion = this.AppVersion;
            output.OS = this.OS;
            output.Device = this.Device;
            output.Platform = this.Platform;
            output.API = this.API;
            output.RenderPipe = this.RenderPipe;
            output.GroupName = this.GroupName;
            output.TestName = this.TestName;
            output.PassFail = this.PassFail;
            output.Custom = this.Custom;
            return output;
        }

        public ResultsDataCommon SwitchPlatformAPI(string platform, string api)
        {
            ResultsDataCommon output = this.Clone();
            output.Platform = platform;
            output.API = api;
            return output;
        }
    }

    // ------------------------------------------------------------------------------------
    // Resolutions

    public enum FrameResolution
    {
        [Tooltip("640x360")]
        nHD,
        [Tooltip("960x540")]
        qHD,
        [Tooltip("1280x720")]
        HD,
        [Tooltip("1920x1080")]
        FullHD
    }
}
