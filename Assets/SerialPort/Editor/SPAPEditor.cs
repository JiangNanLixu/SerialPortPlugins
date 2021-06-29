using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace SerialPortUtility.Editor
{
    /// <summary>
	/// SPAP Custom Editor
	/// </summary>
	[CustomEditor(typeof(SerialPortUtilityPro))]
    public sealed class SPAPEditor : UnityEditor.Editor
    {
        private SerializedProperty ReadCompleteEventObject = null;
        private SerializedProperty SystemEventObject = null;
        
        private SerializedProperty ExpandConfigProperty;
        private SerializedProperty ExpandEventsProperty;
        private SerializedProperty ExternalConfig;

        private SerializedProperty AvailableParserTypeNames = null;
        private SerializedProperty ParserTypeName = null;

        private string[] m_ParserTypeNames = null;
        private List<string> m_CurrentAvailableParserTypeNames = null;
        private int m_ParserIndex = -1;

        private enum _baudrateSel
        {
            Rate1200bps = 1200,
            Rate2400bps = 2400,
            Rate4800bps = 4800,
            Rate9600bps = 9600,
            Rate19200bps = 19200,
            Rate38400bps = 38400,
            Rate57600bps = 57600,
            Rate115200bps = 115200,
        }

        void OnEnable()
        {
            ReadCompleteEventObject = serializedObject.FindProperty("ReadCompleteEventObject");
            SystemEventObject = serializedObject.FindProperty("SystemEventObject");
            
            ExpandConfigProperty = serializedObject.FindProperty("ExpandConfig");
            ExpandEventsProperty = serializedObject.FindProperty("ExpandEventConfig");

            ExternalConfig = serializedObject.FindProperty("ExternalConfig");

            AvailableParserTypeNames = serializedObject.FindProperty("AvailableParserTypeNames");
            ParserTypeName = serializedObject.FindProperty("ParserTypeName");

            RefreshTypeNames();
        }

        public override void OnInspectorGUI()
        {
            SerialPortUtilityPro obj = target as SerialPortUtilityPro;
            serializedObject.Update();

            GUI.backgroundColor = new Color(0.50f, 0.70f, 1.0f);
            EditorGUILayout.Space();

            //button
            if (GUILayout.Button("SerialPort Configure", EditorStyles.toolbarButton))
                ExpandConfigProperty.boolValue = !ExpandConfigProperty.boolValue;

            GUI.backgroundColor = Color.white;

            if (ExpandConfigProperty.boolValue)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("SerialPort Status", EditorStyles.boldLabel);

                if (EditorApplication.isPlaying)
                {
                    if (obj.IsOpened())
                    {
                        EditorGUILayout.HelpBox("Device Opened.", MessageType.Info, true);
                        GUI.backgroundColor = Color.yellow;
                        if (GUILayout.Button("Close the device."))
                            obj.Close();
                        GUI.backgroundColor = Color.white;
                    }
                    else
                    {
                        if (obj.IsErrorFinished())
                            EditorGUILayout.HelpBox("Device Error Closed.", MessageType.Error, true);
                        else
                            EditorGUILayout.HelpBox("Device Closed.", MessageType.Warning, true);

                        GUI.backgroundColor = Color.yellow;
                        if (GUILayout.Button("Open the device."))
                            obj.Open();
                        GUI.backgroundColor = Color.white;
                    }
                }
                else
                {
                    string infoString = "Device is not running.";
                    EditorGUILayout.HelpBox(infoString, MessageType.Info, true);
                }

                EditorGUILayout.EndVertical();

                if (obj.IsOpened())
                    EditorGUI.BeginDisabledGroup(true);

                EditorGUILayout.BeginVertical(GUI.skin.box);
                string portName = string.Empty;
                try
                {
                    PortUtil.GetPortName(obj.Skip, out portName, obj.PortService);
                }
                catch { }

                EditorGUILayout.TextField("PortName", string.IsNullOrEmpty(portName) ? "NONE" : portName);

                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("Open Configure", EditorStyles.boldLabel);

                obj.PortService = (SerialPortUtility.PortService)EditorGUILayout.EnumPopup("Port Service", obj.PortService);
                obj.IsAutoOpen = EditorGUILayout.Toggle("Auto Open", obj.IsAutoOpen);

                obj.Skip = EditorGUILayout.IntField("Order (Default:0)", obj.Skip);
                if (obj.Skip < 0) obj.Skip = 0;

                GUI.backgroundColor = Color.yellow;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Show the devices connected to this PC.", GUILayout.Width(300)))
                {
                    SPAPTools window = (SPAPTools)EditorWindow.GetWindow(typeof(SPAPTools), true, "Show the devices connected to this PC.", true);
                    window.spapObject = obj;
                    window.Show();
                }
                GUILayout.EndHorizontal();

                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("Communication Structure", EditorStyles.boldLabel);

                if (System.Enum.IsDefined(typeof(_baudrateSel), obj.BaudRate))
                {
                    obj.BaudRate = (int)(_baudrateSel)EditorGUILayout.EnumPopup("BaudRate", (_baudrateSel)obj.BaudRate);
                    obj.BaudRate = EditorGUILayout.IntField(" ", obj.BaudRate);
                }
                else
                {
                    obj.BaudRate = EditorGUILayout.IntField("BaudRate", obj.BaudRate);
                }

                obj.Parity = (Parity)EditorGUILayout.EnumPopup("Parity", obj.Parity);
                obj.StopBit = (StopBits)EditorGUILayout.EnumPopup("Stop Bits", obj.StopBit);
                obj.DataBit = EditorGUILayout.IntField("Data Bit", obj.DataBit);
                EditorGUILayout.EndVertical();
                
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField("Read Data Structure", EditorStyles.boldLabel);
                obj.ReadProtocol = (SerialPortUtilityPro.MethodSystem)EditorGUILayout.EnumPopup("Read Protocol", obj.ReadProtocol);
                EditorGUILayout.EndVertical();

                EditorGUILayout.BeginVertical(GUI.skin.box);

                if (string.IsNullOrEmpty(ParserTypeName.stringValue))
                {
                    EditorGUILayout.HelpBox("Entrance procedure is invalid.", MessageType.Error);
                }
                else if (EditorApplication.isPlaying)
                {
                    EditorGUILayout.LabelField("Current Parser", obj.CurrentParser == null ? "None" : obj.CurrentParser.GetType().ToString());
                }

                EditorGUI.BeginDisabledGroup(EditorApplication.isPlayingOrWillChangePlaymode);
                {
                    GUILayout.Label("Available Procedures", EditorStyles.boldLabel);
                    if (m_ParserTypeNames.Length > 0)
                    {
                        EditorGUILayout.BeginVertical("box");
                        {
                            foreach (string procedureTypeName in m_ParserTypeNames)
                            {
                                bool selected = m_CurrentAvailableParserTypeNames.Contains(procedureTypeName);
                                if (selected != EditorGUILayout.ToggleLeft(procedureTypeName, selected))
                                {
                                    if (!selected)
                                    {
                                        m_CurrentAvailableParserTypeNames.Add(procedureTypeName);
                                        WriteAvailableProcedureTypeNames();
                                    }
                                    else if (procedureTypeName != ParserTypeName.stringValue)
                                    {
                                        m_CurrentAvailableParserTypeNames.Remove(procedureTypeName);
                                        WriteAvailableProcedureTypeNames();
                                    }
                                }
                            }
                        }
                        EditorGUILayout.EndVertical();
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("There is no available procedure.", MessageType.Warning);
                    }

                    if (m_CurrentAvailableParserTypeNames.Count > 0)
                    {
                        EditorGUILayout.Separator();

                        int selectedIndex = EditorGUILayout.Popup("Parser", m_ParserIndex, m_CurrentAvailableParserTypeNames.ToArray());
                        if (selectedIndex != m_ParserIndex)
                        {
                            m_ParserIndex = selectedIndex;
                            ParserTypeName.stringValue = m_CurrentAvailableParserTypeNames[selectedIndex];
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Select available procedures first.", MessageType.Info);
                    }
                }
                EditorGUI.EndDisabledGroup();

                switch (obj.ReadProtocol)
                {
                    default:
                        break;
                }

                EditorGUILayout.EndVertical();
            }
            
            if (obj.IsOpened())
                EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();
            GUI.backgroundColor = new Color(0.50f, 0.70f, 1.0f);
            if (GUILayout.Button("SerialPort Utility Events", EditorStyles.toolbarButton))
                ExpandEventsProperty.boolValue = !ExpandEventsProperty.boolValue;

            GUI.backgroundColor = Color.white;
            GUI.color = Color.white;
            if (ExpandEventsProperty.boolValue)
            {
                EditorGUILayout.LabelField("Event Handler");
                EditorGUILayout.PropertyField(ReadCompleteEventObject);
                EditorGUILayout.PropertyField(SystemEventObject);
            }
            
            EditorGUILayout.Space();

            //changed param
            if (GUI.changed)
            {
                //Todo
                serializedObject.ApplyModifiedProperties();
            }

            //EditorUtility.SetDirty(target);	//editor set
        }

        private void RefreshTypeNames()
        {
            m_ParserTypeNames = Type.GetRuntimeTypeNames(typeof(IParser));

            ReadAvailableProcedureTypeNames();
            int oldCount = m_CurrentAvailableParserTypeNames.Count;
            m_CurrentAvailableParserTypeNames = m_CurrentAvailableParserTypeNames.Where(x => m_ParserTypeNames.Contains(x)).ToList();

            if (m_CurrentAvailableParserTypeNames.Count != oldCount)
            {
                WriteAvailableProcedureTypeNames();
            }
            else if (!string.IsNullOrEmpty(ParserTypeName.stringValue))
            {
                m_ParserIndex = m_CurrentAvailableParserTypeNames.IndexOf(ParserTypeName.stringValue);
                if (m_ParserIndex < 0)
                {
                    ParserTypeName.stringValue = null;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ReadAvailableProcedureTypeNames()
        {
            m_CurrentAvailableParserTypeNames = new List<string>();
            int count = AvailableParserTypeNames.arraySize;
            for (int i = 0; i < count; i++)
            {
                m_CurrentAvailableParserTypeNames.Add(AvailableParserTypeNames.GetArrayElementAtIndex(i).stringValue);
            }
        }

        private void WriteAvailableProcedureTypeNames()
        {
            AvailableParserTypeNames.ClearArray();
            if (m_CurrentAvailableParserTypeNames == null)
            {
                return;
            }

            m_CurrentAvailableParserTypeNames.Sort();
            int count = m_CurrentAvailableParserTypeNames.Count;
            for (int i = 0; i < count; i++)
            {
                AvailableParserTypeNames.InsertArrayElementAtIndex(i);
                AvailableParserTypeNames.GetArrayElementAtIndex(i).stringValue = m_CurrentAvailableParserTypeNames[i];
            }

            if (!string.IsNullOrEmpty(ParserTypeName.stringValue))
            {
                m_ParserIndex = m_CurrentAvailableParserTypeNames.IndexOf(ParserTypeName.stringValue);
                if (m_ParserIndex < 0)
                {
                    ParserTypeName.stringValue = null;
                }
            }
        }
    }
}
