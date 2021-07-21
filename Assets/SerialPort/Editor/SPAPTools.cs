using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace SerialPortUtility
{
	public class SPAPTools : EditorWindow
	{
		public SerialPortUtilityPro spapObject = null;
		private int deviceNum;
		private System.Text.StringBuilder[] deviceString;
		private int[] deviceKind;

        private Dictionary<PortService, List<PortDevice>> devices;

		SPAPTools()
		{
			this.minSize = new Vector2(300, 300);
			this.maxSize = new Vector2(600, 1000);
		}

		void OnGUI()
		{
			if (spapObject == null)
			{
				EditorGUILayout.LabelField("Error!", EditorStyles.boldLabel);
				return;
			}

            devices = PortUtil.GetDeviceListAvailable();

            EditorGUILayout.HelpBox("When button selected, information is set in this inspector.", MessageType.Info, true);
			EditorGUILayout.Space();
            foreach (var item in devices)
            {
                EditorGUILayout.LabelField(string.Format("Service: {0}", item.Key.ToString()), EditorStyles.boldLabel);
                EditorGUILayout.BeginVertical(GUI.skin.box);

                foreach (var dev in item.Value)
                {
                    if (GUILayout.Button(string.Format("{0} ({1})",dev.portName, dev.skip)))
                    {
                        spapObject.portService = dev.devType;
                        spapObject.Skip = dev.skip;
                    }
                }
                EditorGUILayout.LabelField(string.Format("Device Found: {0}",item.Value.Count));
                EditorGUILayout.EndVertical();
            }
		}

		/*
		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
		{

		}
		*/
	}
}