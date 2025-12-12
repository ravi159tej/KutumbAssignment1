using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace PuppetFace
{
    class AfterAllPostprocessor : AssetPostprocessor
    {
        static private List<string> _registeredAsset = new List<string>();
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets)
            {
                bool hasBeenInit = false;
                if (_registeredAsset.Contains(str))
                {
                    hasBeenInit = true;
                    _registeredAsset.Remove(str);
                }
                
                GameObject sel = Selection.activeGameObject;
                if(sel!=null)
                {
                    LipSync lipsync = sel.GetComponent<LipSync>();
                    if(lipsync!=null)
                    {
                        if (hasBeenInit)
                        {
                            lipsync.NewAudioAdded = true;
                        }
                        else
                            _registeredAsset.Add(str);

                    }
                }
            }
            /*foreach (string str in deletedAssets)
            {
                Debug.Log("Deleted Asset: " + str);
            }

            for (int i = 0; i < movedAssets.Length; i++)
            {
                Debug.Log("Moved Asset: " + movedAssets[i] + " from: " + movedFromAssetPaths[i]);
            }*/
        }
    }
}