using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

namespace PuppetFace
{
	

	public static class Extensions
	{
		public static System.Type[] GetAllDerivedTypes(this System.AppDomain aAppDomain, System.Type aType)
		{
			var result = new List<System.Type>();
			var assemblies = aAppDomain.GetAssemblies();
			foreach (var assembly in assemblies)
			{
				var types = assembly.GetTypes();
				foreach (var type in types)
				{
					if (type.IsSubclassOf(aType))
						result.Add(type);
				}
			}
			return result.ToArray();
		}

		public static Rect GetEditorMainWindowPos()
		{
			var containerWinType = System.AppDomain.CurrentDomain.GetAllDerivedTypes(typeof(ScriptableObject)).Where(t => t.Name == "ContainerWindow").FirstOrDefault();
			if (containerWinType == null)
				throw new System.MissingMemberException("Can't find internal type ContainerWindow. Maybe something has changed inside Unity");
			var showModeField = containerWinType.GetField("m_ShowMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var positionProperty = containerWinType.GetProperty("position", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (showModeField == null || positionProperty == null)
				throw new System.MissingFieldException("Can't find internal fields 'm_ShowMode' or 'position'. Maybe something has changed inside Unity");
			var windows = Resources.FindObjectsOfTypeAll(containerWinType);
			foreach (var win in windows)
			{
				var showmode = (int)showModeField.GetValue(win);
				if (showmode == 4) // main window
				{
					var pos = (Rect)positionProperty.GetValue(win, null);
					return pos;
				}
			}
			throw new System.NotSupportedException("Can't find internal main window. Maybe something has changed inside Unity");
		}

		public static void CenterOnMainWin(this UnityEditor.EditorWindow aWin)
		{
			var main = GetEditorMainWindowPos();
			var pos = aWin.position;
			float w = (main.width - pos.width) * 0.5f;
			float h = (main.height - pos.height) * 0.5f;
			pos.x = main.x + w;
			pos.y = main.y + h;
			aWin.position = pos;
		}
	}
	public class AboutPopupEditor : EditorWindow
	{
		bool groupEnabled;

		// Add menu named "My Window" to the Window menu
		[MenuItem("Window/Puppet Face/About")]
		static void Init()
		{
			// Get existing open window or if none, make a new one:
			AboutPopupEditor window = (AboutPopupEditor)EditorWindow.GetWindow(typeof(AboutPopupEditor));
			window.position = new Rect(Screen.width, Screen.height , 300, 300);
			Extensions.CenterOnMainWin(window);
			window.titleContent = new GUIContent();
			window.titleContent.text = "About Puppet Face";
			window.name = "About";
			window.Show();
		}

		void OnGUI()
		{
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			GUILayout.BeginVertical();
			GUILayout.FlexibleSpace();
			string popupText = "Puppet Face\n\nCreated by Puppetman\n\nv1.5.1\n";
            popupText += "Fix - moved Afterall postprocessor into editor\n";
            popupText += "Fix - Lip Sync Sets dirty\n";

            popupText += "\nv1.5.0\n";
            popupText += "New - Lipsync Emotions (Can add custom blendshapes to lipync anim)\n";
            popupText += "New - Phonemes can be switched\n";
            popupText += "Fix - Auto reloads Audio after convert\n";
            popupText += "Fix - Phoneme deletion error\n";
            popupText += "Fix - Play controls hiding range slider\n";
            popupText += "Fix - Changing Parameters updates visuals\n";
            popupText += "Fix - Hard coded path\n";
            popupText += "Fix - Converting audio files doesnt duplicate\n";
            popupText += "Fix - Error when wav not streaming \n";
            popupText += "Fix - Timeline resets blendshape on exit \n";
            popupText += "Fix - Plastic Error \n";
                       
            popupText += "\nv1.4.2\n";
            popupText += "Fix - Microphone null error\n";
			popupText += "Fix - optimised blendshapes\n";
			popupText += "\nv1.4.1\n";
			popupText += "Fix - Timeline Multiple Lip Syncs\n";
			popupText += "New - Microphone Device Choice\n";
			popupText += "Fix - Lip Sync Culture Invariant\n";
			popupText += "Fix - Timeline 2020 offset\n";
			popupText += "Fix - Blendshape allows no tangents\n";
			popupText += "Fix - Blendshape sculpt undo bug\n";
			popupText += "Fix - No facebones bug\n";
			popupText += "\nv1.3.1\n";
			popupText += "New - 120K Topology Blendshape limit, with cancel\n";
			popupText += "New - Blendshape select Submesh\n";
			popupText += "New - Blendshape Types; BindPose & Current Pose\n";
			popupText += "New - Edit Bone Pose Separate to Blendshape\n";
			popupText += "\nv1.2.1\n";
			popupText += "New - Lip Sync Timeline Track,\n";
			popupText += "New - Eyebrows performance tracking,\n";
			popupText += "New - Select desired Webcam,\n";
			popupText += "Fix - Added Assembly Definition,\n";
			popupText += "Fix - Gizmo folder back,\n";

			popupText += "\nv1.1\n";
			popupText += "New - Includes Brownie Girl,\n";
			popupText += "Fix - Fixed Demo Blend shapes\n";
			popupText += "Fix -Sets \"Unsafe\" code in playersettings.\n";		

			GUILayout.Label(popupText, EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();

			GUILayout.EndVertical();
			GUILayout.FlexibleSpace();

			GUILayout.EndHorizontal();

		}
	}
}