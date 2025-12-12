using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace PuppetFace
{
	[InitializeOnLoad]
	public class InitialPuppetFaceLaunchEditor : MonoBehaviour
	{
        private static string _puppetFacePath;

        static InitialPuppetFaceLaunchEditor()
		{
            _puppetFacePath = "Assets" + RecursivelyFindFolderPath().Substring(Application.dataPath.Length);

            if (AssetDatabase.IsValidFolder(_puppetFacePath +"/Gizmos"))
			{
				if (!AssetDatabase.IsValidFolder("Assets/Gizmos"))
				{
					FileUtil.MoveFileOrDirectory(_puppetFacePath +"/Gizmos", "Assets/Gizmos");
					FileUtil.DeleteFileOrDirectory(_puppetFacePath +"/Gizmos.meta");
				}
				else
				{
					if (AssetDatabase.IsValidFolder(_puppetFacePath +"/Gizmos"))
					{
						if (!AssetDatabase.IsValidFolder("Assets/Gizmos/PuppetFace"))
						{
							FileUtil.MoveFileOrDirectory(_puppetFacePath +"/Gizmos/PuppetFace", "Assets/Gizmos/PuppetFace");
						}
					}

				}
				Debug.Log("Puppet Face is installed.");
			}

		}
        private static string RecursivelyFindFolderPath()
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Application.dataPath);
            DirectoryInfo[] dirInfos = directoryInfo.GetDirectories("*", SearchOption.AllDirectories);
            foreach (DirectoryInfo d in dirInfos)
            {
                if (d.Name == "PuppetFace" && d.Parent.Name != "Gizmos")
                    return d.FullName;
            }
            return "";
        }
    }

}