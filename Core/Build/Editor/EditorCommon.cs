using UnityEngine;
using UnityEditor;
using System.IO;

namespace GraphicsTestFramework
{
    // ------------------------------------------------------------------------------------
    // Common (Static)

    public class EditorCommon
    {
        private static string _utfCorePath;
        public static string utfCorePath
        {
            get
            {
                if ( string.IsNullOrEmpty( _utfCorePath ) )
                {
                    string[] guids = AssetDatabase.FindAssets("UTFCOREMARKER");

                    if ( guids != null && guids.Length > 0)
                        _utfCorePath = Path.GetDirectoryName( AssetDatabase.GUIDToAssetPath( guids[0] ) );
                    else
                        _utfCorePath = "";
                }

                return _utfCorePath;
            }
        }

        private static SceneAsset _masterScene;
        public static SceneAsset masterScene
        {
            get
            {
                if (_masterScene == null)
                    _masterScene = AssetDatabase.LoadAssetAtPath<SceneAsset>( Path.Combine( utfCorePath, "Master.unity" ) );
                    
                return _masterScene;
            }
        }

    }
}