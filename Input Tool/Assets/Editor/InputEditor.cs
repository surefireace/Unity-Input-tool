// By Donovan Colen
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Xml.Serialization;
using System.IO;
using System;


/// <summary>
/// a editor for CustomInput that helps test different inputs for scripts.
/// touch and acceleration not supported. input and function only work at runtime.
/// JoystickButtons, JoystickAxis names, and sequences must be typed into the textfield.
/// </summary>

[CustomEditor(typeof(CustomInput))]
public class InputEditor : Editor
{
    // simple class editor uses to save/load the data for the tool
    [Serializable]
    public class EditorInputEventPair   
    {
        public string m_input = "None";

        public CustomInput.InputType m_type = CustomInput.InputType.kStandard;
        public CustomInput.InputCategory m_category = CustomInput.InputCategory.kButton;
        public CustomInput.AxisDirection m_axis1Dir = CustomInput.AxisDirection.kNeutral;
        public CustomInput.AxisDirection m_axis2Dir = CustomInput.AxisDirection.kNeutral;

        public float m_axis1Tolerance = 0;
        public float m_axis2Tolerance = 0;

        public float m_timeLimit = 0;
    }

    private string m_lastInput = "None";

    private SerializedProperty[] m_eventProp;   // used to serialize the events for the script
    private bool m_foldOut;
    private string m_savePath = "Assets/Saves/";    // the path the save file is saved
    CustomInput m_target;   // the target for the editor
    private int m_maxArraySize = 50;    // the maximum size for the input mapping array

    private void Awake()
    {
        // if the directory doesnt exist create it
        if (!Directory.Exists(m_savePath))
        {
            Directory.CreateDirectory(m_savePath);
        }

        if (m_target != target as CustomInput)
        {
            m_target = target as CustomInput;
        }
    }

    /// <summary>
    /// the main fuction that makes the script look like it does and works like it should in the editor
    /// </summary>
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.LabelField("Custom Input");

        m_target = target as CustomInput;
        int arraySize = 0;

        int previousArraySize;
        if (m_target.m_mappings == null)
        {
            previousArraySize = 0;
        }
        else
        {
            previousArraySize = m_target.m_mappings.Length;
        }
        
        if(previousArraySize != 0)
        {
            arraySize = previousArraySize;
        }        

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Array Size");
        arraySize = Mathf.Max(0, EditorGUILayout.IntField(arraySize));
        EditorGUILayout.EndHorizontal();

        // resize and init the array
        if (arraySize != previousArraySize)
        {
            if(arraySize > m_maxArraySize)
            {
                arraySize = m_maxArraySize;
            }

            CustomInput.InputEvent[] tempMappings = new CustomInput.InputEvent[m_target.m_mappings.Length];

            // copy over the existing array to perserve data after the change
            for(int i = 0; i < tempMappings.Length; ++i)
            {
                tempMappings[i] = new CustomInput.InputEvent();
                tempMappings[i] = m_target.m_mappings[i];
                tempMappings[i].m_axisEvent = m_target.m_mappings[i].m_axisEvent;

                tempMappings[i].m_axis1Dir = m_target.m_mappings[i].m_axis1Dir;
                tempMappings[i].m_axis2Dir = m_target.m_mappings[i].m_axis2Dir;

                tempMappings[i].m_axis1Tolerance = m_target.m_mappings[i].m_axis1Tolerance;
                tempMappings[i].m_axis2Tolerance = m_target.m_mappings[i].m_axis2Tolerance;

            }

            Debug.Log("Resizing array");
            m_target.m_mappings = new CustomInput.InputEvent[arraySize];

            for (int i = 0; i < arraySize; ++i)
            {
                m_target.m_mappings[i] = new CustomInput.InputEvent();

                if (i < tempMappings.Length)
                {
                    m_target.m_mappings[i] = tempMappings[i];
                }
            }
        }

        previousArraySize = arraySize;

        // create the array for the property fields
        m_eventProp = new SerializedProperty[arraySize];

        // create the serialized objects for each event 
        for (int i = 0; i < arraySize; ++i)
        {
            string temp = "";
            if (m_target.m_mappings[i].m_category == CustomInput.InputCategory.kAxis && m_target.m_mappings[i].m_type == CustomInput.InputType.kStandard)
            {
                temp = "m_mappings.Array.data[" + i + "].m_axisEvent";
                m_eventProp[i] = serializedObject.FindProperty(temp);
            }
            else
            {
                temp = "m_mappings.Array.data[" + i + "].m_event";
                m_eventProp[i] = serializedObject.FindProperty(temp);
            }
        }

        m_foldOut = EditorGUILayout.Foldout(m_foldOut, "Array", true);

        if (m_foldOut)
        {
            // draw everything for the array
            for (int i = 0; i < arraySize; ++i)
            {
                if (m_eventProp[i] == null)  // error checking
                {
                    break;
                }

                CustomInput.InputEvent current = m_target.m_mappings[i];

                // display the rest of the layout for the array
                EditorGUILayout.BeginHorizontal();
                current.m_input = EditorGUILayout.TextField(current.m_input);
                current.m_category = (CustomInput.InputCategory)EditorGUILayout.EnumPopup(current.m_category, GUILayout.MaxWidth(55));
                current.m_type = (CustomInput.InputType)EditorGUILayout.EnumPopup(current.m_type, GUILayout.MaxWidth(70));

                if (current.m_category == CustomInput.InputCategory.kButton)
                {

                    if (current.m_type != CustomInput.InputType.kSequence)
                    {
                        if (GUILayout.Button("Assign Key"))
                        {
                            if (EditorApplication.isPlaying)
                            {
                                // since UnityEditor.GameView is internal this is how I managed to get its type
                                System.Reflection.Assembly assembly = typeof(EditorWindow).Assembly;
                                Type type = assembly.GetType("UnityEditor.GameView");
                                EditorWindow.FocusWindowIfItsOpen(type);
                            }
                            SetInput(ref current);
                            m_target.GetInput(i);
                        }
                        if (GUILayout.Button("Clear Key"))
                        {
                            current.m_input = "None";
                            m_lastInput = "None";
                        }
                    }
                    else
                    {
                        GUIContent content = new GUIContent("Time Limit", "Time Limit is in seconds." + '\n' + "Negative value means no time limit.");
                        EditorGUILayout.LabelField(content, GUILayout.MaxWidth(70));
                        current.m_timeLimit = EditorGUILayout.FloatField(current.m_timeLimit, GUILayout.MaxWidth(50));

                    }
                }
                else
                {
                    if(current.m_type == CustomInput.InputType.kCombo)
                    {

                        if(current.m_input.Contains(", "))  // if there are two axis entered for the combo add the tolerance and direction for both axis
                        {
                            EditorGUILayout.EndHorizontal();
                            EditorGUILayout.BeginHorizontal();
                            current.m_axis1Dir = (CustomInput.AxisDirection)EditorGUILayout.EnumPopup(current.m_axis1Dir, GUILayout.MaxWidth(70));
                            if (current.m_axis1Dir != CustomInput.AxisDirection.kNeutral)
                            {

                                current.m_axis1Tolerance = EditorGUILayout.Slider(current.m_axis1Tolerance, 0, 1, GUILayout.MaxWidth(175));
                                Rect tempRect = GUILayoutUtility.GetLastRect();
                                tempRect.x -= 20;
                                GUI.Label(tempRect, new GUIContent("", "Axis Tolerance is from 0 to 1." + '\n' + "Determines how far the joystick" 
                                    + '\n' + "must be moved to activate."));
                            }
                            else
                            {
                                current.m_axis1Tolerance = 0;
                            }

                            current.m_axis2Dir = (CustomInput.AxisDirection)EditorGUILayout.EnumPopup(current.m_axis2Dir, GUILayout.MaxWidth(70));
                            if (current.m_axis2Dir != CustomInput.AxisDirection.kNeutral)
                            {
                                current.m_axis2Tolerance = EditorGUILayout.Slider(current.m_axis2Tolerance, 0, 1, GUILayout.MaxWidth(175));
                                Rect tempRect = GUILayoutUtility.GetLastRect();
                                tempRect.x -= 20;
                                GUI.Label(tempRect, new GUIContent("", "Axis Tolerance is from 0 to 1." + '\n' + "Determines how far the joystick" 
                                    + '\n' + "must be moved to activate."));
                            }
                            else
                            {
                                current.m_axis2Tolerance = 0;
                            }
                        }
                        else
                        {
                            current.m_axis1Dir = (CustomInput.AxisDirection)EditorGUILayout.EnumPopup(current.m_axis1Dir, GUILayout.MaxWidth(70));
                            if (current.m_axis1Dir != CustomInput.AxisDirection.kNeutral)
                            {
                                current.m_axis1Tolerance = EditorGUILayout.Slider(current.m_axis1Tolerance, 0, 1, GUILayout.MaxWidth(175));
                                Rect tempRect = GUILayoutUtility.GetLastRect();
                                tempRect.x -= 20;
                                GUI.Label(tempRect, new GUIContent("", "Axis Tolerance is from 0 to 1." + '\n' + "Determines how far the joystick" + '\n' + "must be moved to activate."));
                            }
                            else
                            {
                                current.m_axis1Tolerance = 0;
                            }
                        }
                    }
                    else if(current.m_type == CustomInput.InputType.kSequence)
                    {
                        GUIContent content = new GUIContent("Time Limit", "Time Limit is in seconds." + '\n' + "Negative value means no time limit.");
                        EditorGUILayout.LabelField(content, GUILayout.MaxWidth(70));
                        current.m_timeLimit = EditorGUILayout.FloatField(current.m_timeLimit, GUILayout.MaxWidth(50));

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("Axis Tolerance", GUILayout.MaxWidth(100));
                        current.m_axis1Tolerance = EditorGUILayout.Slider(current.m_axis1Tolerance, 0, 1, GUILayout.MaxWidth(175));
                        Rect tempRect = GUILayoutUtility.GetLastRect();
                        tempRect.x -= 20;
                        GUI.Label(tempRect, new GUIContent("", "Axis Tolerance is from 0 to 1." + '\n' + "Determines how far the joystick"
                            + '\n' + "must be moved to activate."));

                        current.m_axis2Tolerance = EditorGUILayout.Slider(current.m_axis2Tolerance, 0, 1, GUILayout.MaxWidth(175));
                        tempRect = GUILayoutUtility.GetLastRect();
                        tempRect.x -= 20;
                        GUI.Label(tempRect, new GUIContent("", "Axis Tolerance is from 0 to 1." + '\n' + "Determines how far the joystick"
                            + '\n' + "must be moved to activate."));

                    }
                }
                EditorGUILayout.EndHorizontal();

                // unity event required stuff for custom editors
                serializedObject.Update();
                EditorGUILayout.PropertyField(m_eventProp[i], true);
                serializedObject.ApplyModifiedProperties();
                
            }
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Save"))
        {
            SaveInput();
        }
        if (GUILayout.Button("Load"))
        {
            LoadInput();
        }

        EditorGUILayout.EndHorizontal();

        // get the input to assign keys at editor time
        Event e = Event.current;
        if(e.keyCode != KeyCode.None || e.shift)
        {
            m_lastInput = e.keyCode.ToString();
        }
    }

    /// <summary>
    /// sets the input of the InputEvent to the last key input
    /// </summary>
    /// <param name="cur"> the current mappings index that is being used</param>
    public void SetInput(ref CustomInput.InputEvent cur)
    {
        if (m_lastInput != "None")
        {
            cur.m_input = m_lastInput;
        }
        else
        {
            Debug.Log("There was no Key pressed");
        }
    }

    /// <summary>
    /// saves the input information. does not save the UnityEvents that the input invokes those are saved with the scene
    /// </summary>
    public void SaveInput()
    {
        // save the data
        XmlSerializer serializer = new XmlSerializer(typeof(EditorInputEventPair[]));
        FileStream stream = new FileStream(m_savePath + m_target.gameObject.name + "Input" + ".xml", FileMode.Create);

        EditorInputEventPair[] editorMapping = new EditorInputEventPair[m_target.m_mappings.Length];
        for (int i = 0; i < editorMapping.Length; ++i)  // store all the saveable data into the editor's class version to save it
        {
            editorMapping[i] = new EditorInputEventPair();
            editorMapping[i].m_input = m_target.m_mappings[i].m_input;
            editorMapping[i].m_type = m_target.m_mappings[i].m_type;
            editorMapping[i].m_category = m_target.m_mappings[i].m_category;
            editorMapping[i].m_timeLimit = m_target.m_mappings[i].m_timeLimit;
            editorMapping[i].m_axis1Dir = m_target.m_mappings[i].m_axis1Dir;
            editorMapping[i].m_axis2Dir = m_target.m_mappings[i].m_axis2Dir;
            editorMapping[i].m_axis1Tolerance = m_target.m_mappings[i].m_axis1Tolerance;
            editorMapping[i].m_axis2Tolerance = m_target.m_mappings[i].m_axis2Tolerance;

        }

        serializer.Serialize(stream, editorMapping);
        stream.Close();
    }

    /// <summary>
    /// loads the input information. does not load the UnityEvents that the input invokes those are loaded with the scene
    /// </summary>
    public void LoadInput()
    {
        CustomInput.InputEvent[] tempMappings;
        EditorInputEventPair[] editorMappings;

        try
        {
            XmlSerializer serializer = new XmlSerializer(typeof(EditorInputEventPair[]));
            FileStream stream = new FileStream(m_savePath + m_target.gameObject.name + "Input" + ".xml", FileMode.Open);
            editorMappings = serializer.Deserialize(stream) as EditorInputEventPair[];
            stream.Close();

            tempMappings = m_target.m_mappings;

            if (m_target.m_mappings.Length != editorMappings.Length)
            {
                m_target.m_mappings = new CustomInput.InputEvent[editorMappings.Length];

                for (int i = 0; i < m_target.m_mappings.Length; ++i)
                {
                    m_target.m_mappings[i] = new CustomInput.InputEvent();
                }

            }

            for (int i = 0; i < m_target.m_mappings.Length; ++i)    // load all the data into the mappings
            {
                m_target.m_mappings[i].m_input = editorMappings[i].m_input;
                m_target.m_mappings[i].m_type = editorMappings[i].m_type;
                m_target.m_mappings[i].m_category = editorMappings[i].m_category;
                m_target.m_mappings[i].m_timeLimit = editorMappings[i].m_timeLimit;
                m_target.m_mappings[i].m_axis1Dir = editorMappings[i].m_axis1Dir;
                m_target.m_mappings[i].m_axis2Dir = editorMappings[i].m_axis2Dir;
                m_target.m_mappings[i].m_axis2Tolerance = editorMappings[i].m_axis2Tolerance;
                m_target.m_mappings[i].m_axis1Tolerance = editorMappings[i].m_axis1Tolerance;


                if (i < tempMappings.Length)
                {
                    m_target.m_mappings[i].m_event = tempMappings[i].m_event;
                }

            }
        }
        catch (Exception e)
        {
            Debug.LogFormat(e.ToString());
        }

    }
}
