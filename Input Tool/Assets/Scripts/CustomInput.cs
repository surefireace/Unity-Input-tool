// By Donovan Colen
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System;

/// <summary>
/// a class that helps test different inputs for scripts. 
/// touch and acceleration not supported. input and function only work at runtime.
/// JoystickAxis names and sequences must be typed into the textfield.
/// </summary>
public class CustomInput : MonoBehaviour
{
    // the three main types of input that the script supports
    [Serializable]
    public enum InputType   
    {
        kStandard = 0,
        kCombo = 1,
        kSequence = 2
    }

    // for the editor script to make the script in editor look nicer
    [Serializable]
    public enum InputCategory   
    {
        kButton = 0,
        kAxis = 1
    }

    // to help the designer choose the desired direction in stick movement
    [Serializable]
    public enum AxisDirection   
    {
        kNeutral = 0,
        kPositive = 1,
        kNegative = 2
    }

    // a class that derives from UnityEvent<T0> to allow dynamic calls for axis input. needed for serialization.
    [Serializable]
    public class CustomEvent : UnityEvent<float>
    {
    }

    // the main class used to hold all the info for the inputs
    [Serializable]
    public class InputEvent
    {
        public string m_input = "None";
        public string m_sequenceProgress = "";

        public UnityEvent m_event;  //  the event that holds the funtion to be invoked
        public CustomEvent m_axisEvent; // the event that holds the funtion to be invoked that allows a float to be passed in for standard axis input

        public InputType m_type = InputType.kStandard;
        public InputCategory m_category = InputCategory.kButton;

        public AxisDirection m_axis1Dir = AxisDirection.kNeutral;
        public AxisDirection m_axis2Dir = AxisDirection.kNeutral;

        public float m_timeLimit = 0;
        public float m_timer = 0;
        public float m_axis1Tolerance = 0;
        public float m_axis2Tolerance = 0;

        public int m_sequenceIndex = 0;
    }

    [HideInInspector]
    public InputEvent[] m_mappings; // the main array that is used for all the different inputs.
    private bool m_setInput = false;    // to check if they are binding keys
    private int[] m_keyCodes = (int[])Enum.GetValues(typeof(KeyCode));  // keycodes for assigning keys to the input
    private int m_index = -1;   // current index in mappings

    // Update is called once per frame
    void Update()
    {
        if (m_setInput && Input.anyKey)
        {
            AssignButton();
            return; // no need to run input calls if tring to bind keys
        }

        for (int i = 0; i < m_mappings.Length; ++i)
        {
            switch (m_mappings[i].m_type)
            {
                case InputType.kStandard:
                {
                    HandleStandardInput(i);
                    break;
                }
                case InputType.kCombo:
                {
                    if(HandleComboInput(i))
                    {
                        m_mappings[i].m_event.Invoke();
                    }
                    break;
                }
                case InputType.kSequence:
                {
                    HandleSequenceInput(i);

                    m_mappings[i].m_timer += Time.deltaTime;
                    if (m_mappings[i].m_timer > m_mappings[i].m_timeLimit && m_mappings[i].m_timeLimit > 0)   // times up reset progress
                    {
                        ResetSequence(i);
                        Debug.Log("Timer reset for sequence: " + m_mappings[i].m_input);
                    }
                    break;
                }
                default:
                {
                    // should never get here
                    Debug.Assert(false);
                    break;
                }
            }
        }

    }

    /// <summary>
    /// handles standard input
    /// </summary>
    /// <param name="index"> the index in the input mappings </param>
    private void HandleStandardInput(int index)
    {
        KeyCode key;
        bool temp = Enum.TryParse(m_mappings[index].m_input, out key);   // try to find the button/key in the KeyCode
        if (temp)   // if the button/key is in the KeyCode
        {
            if (Input.GetKey(key))  // see if it was pressed
            {
                m_mappings[index].m_event.Invoke();
            }
        }
        else if(IsValidAxis(m_mappings[index].m_input)) // check for axis input
        {
            float val = Input.GetAxis(m_mappings[index].m_input);
            m_mappings[index].m_axisEvent.Invoke(val);
        }
        else
        {
            Debug.LogWarning("Button name or Axis name incorrect. For joystick button input use JoystickButton0 - JoystickButton19." +
                " For Axis put the same name as in the Input settings for the project ");
        }
    }

    /// <summary>
    ///  handles the combination input
    /// </summary>
    /// <param name="index"> the index in the input mappings </param>
    /// <param name="onDown"> to seperate Input.GetKey() and Input.GetKeyDown(). Input.GetKey() is default </param>
    /// <returns> true if the combo is pressed </returns>
    private bool HandleComboInput(int index, bool onDown = false)
    {
        KeyCode key1;
        KeyCode key2;
        string rawInputString = m_mappings[index].m_input;
        int indexOfSplit = rawInputString.IndexOf('+');

        if(indexOfSplit == -1)
        {
            Debug.LogWarning("Invalid combination string. example: W+E");    
            return false;
        }

        string button1 = rawInputString.Substring(0, indexOfSplit);
        string button2 = rawInputString.Substring(indexOfSplit + 1);

        int indexOfAxisSplit = button1.IndexOf(", ");

        bool temp1 = Enum.TryParse(button1, out key1);   // try to find the button/key in the KeyCode
        bool temp2 = Enum.TryParse(button2, out key2);   // try to find the button/key in the KeyCode

        if (temp1 && temp2)   // if the buttons/keys are in the KeyCode
        {
            if(onDown) 
            {
                if (Input.GetKeyDown(key1) && Input.GetKeyDown(key2))
                {
                    return true;
                }
            }
            else if (Input.GetKey(key1) && Input.GetKey(key2))  // see if it was pressed
            {
                return true;
            }
        }
        else if (indexOfAxisSplit != -1 && temp2)  // see if is a valid two axis button combo
        {
            if (onDown)
            {
                if (!Input.GetKeyDown(key2))
                {
                    return false; //if the button isnt down no need to continue
                }
            }
            else if(!Input.GetKey(key2))
            {
                return false; //if the button isnt pressed no need to continue
            }

            string axis1 = button1.Substring(0, indexOfAxisSplit);
            string axis2 = button1.Substring(indexOfAxisSplit + 2);

            if (IsValidAxis(axis1) && IsValidAxis(axis2))
            {
                float axis1Val = Input.GetAxis(axis1);
                float axis2Val = Input.GetAxis(axis2);

                bool axis1Active = false;
                bool axis2Active = false;

                if (m_mappings[index].m_axis1Dir == AxisDirection.kNeutral)
                {
                    if (axis1Val == 0)
                    {
                        axis1Active = true;
                    }
                }

                if (m_mappings[index].m_axis2Dir == AxisDirection.kNeutral)
                {
                    if (axis2Val == 0)
                    {
                        axis2Active = true;
                    }
                }


                if (Math.Abs(axis1Val) >= m_mappings[index].m_axis1Tolerance &&
                    Math.Abs(axis2Val) >= m_mappings[index].m_axis2Tolerance)  // check if both axis has moved far enough
                {
                    // check first axis
                    if (m_mappings[index].m_axis1Dir == AxisDirection.kPositive && axis1Val > 0)
                    {
                        axis1Active = true;
                    }
                    else if (m_mappings[index].m_axis1Dir == AxisDirection.kNegative && axis1Val < 0)
                    {
                        axis1Active = true;
                    }

                    // check second axis
                    if (m_mappings[index].m_axis2Dir == AxisDirection.kPositive && axis2Val > 0)
                    {
                        axis2Active = true;
                    }
                    else if (m_mappings[index].m_axis2Dir == AxisDirection.kNegative && axis2Val < 0)
                    {
                        axis2Active = true;
                    }
                }

                if (axis1Active && axis2Active)
                {
                    return true;
                }

            }
        }
        else if (IsValidAxis(button1) && temp2)  // see if is a valid axis button combo
        {
            if (onDown)
            {
                if (!Input.GetKeyDown(key2))
                {
                    return false; //if the button isnt down no need to continue
                }
            }
            else if (!Input.GetKey(key2))
            {
                return false; //if the button isnt pressed no need to continue
            }

            float axisVal = Input.GetAxis(button1);
            if (m_mappings[index].m_axis1Dir == AxisDirection.kNeutral)
            {
                if (axisVal == 0)
                {
                    return true;
                }
            }
            else
            {
                if (Math.Abs(axisVal) >= m_mappings[index].m_axis1Tolerance)  // check if axis has moved far enough
                {
                    // check if the direction is the correct one
                    if (m_mappings[index].m_axis1Dir == AxisDirection.kPositive && axisVal > 0)
                    {
                        return true;
                    }
                    else if (m_mappings[index].m_axis1Dir == AxisDirection.kNegative && axisVal < 0)
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            Debug.LogWarning("Button name or Axis name incorrect. For joystick button input use JoystickButton0 - JoystickButton19." +
                " For Axis put the same name as in the Input settings for the project. Axis+Axis is not a valid combination of input.");
            return false;
        }

        return false;
    }

    /// <summary>
    /// handles the sequence input
    /// </summary>
    /// <param name="index"> the index in the input mappings </param>
    private void HandleSequenceInput(int index)
    {
        KeyCode nextKey;
        string rawInputString = m_mappings[index].m_input;
        int keyIndexEnd = rawInputString.IndexOf(", ", m_mappings[index].m_sequenceIndex);
        string keyStr = "";

        if (keyIndexEnd == -1) // the key is the last in the sequence most likely
        {
            keyIndexEnd = rawInputString.LastIndexOf(", ");
            keyStr = rawInputString.Substring(keyIndexEnd + 2);
        }
        else
        {
            keyStr = rawInputString.Substring(m_mappings[index].m_sequenceIndex, keyIndexEnd - m_mappings[index].m_sequenceIndex);
        }

        if (keyStr.Contains("(")) // check if it has a double axis
        {
            keyIndexEnd = rawInputString.IndexOf(")", m_mappings[index].m_sequenceIndex);
            keyStr = rawInputString.Substring(m_mappings[index].m_sequenceIndex + 1, keyIndexEnd - m_mappings[index].m_sequenceIndex - 1);

            char dir1 = keyStr[keyStr.Length - 2];
            char dir2 = keyStr[keyStr.Length - 1];

            keyStr = keyStr.TrimEnd('<', '>', '='); // trim the direction characters so HandleComboInput() can properly read the string

            SetAxisDirection(dir1, index);
            SetAxisDirection(dir2, index, false);

            if (keyStr.Contains("+"))   // check if it has a double axis combo
            {
                m_mappings[index].m_input = keyStr; // change the string so HandleComboInput() can be used to save code

                if (HandleComboInput(index, true))
                {
                    m_mappings[index].m_sequenceProgress += '(' + keyStr.ToString() + dir1 + dir2 + "), ";    // need to re-add the direction chars and () to match the orginal string
                    m_mappings[index].m_sequenceIndex = keyIndexEnd + 3;   // skip over the "), "
                }

                m_mappings[index].m_input = rawInputString; // reset the string

            }
            else // it is a diagonal axis input
            {
                int indexOfAxisSplit = keyStr.IndexOf(", ");

                if (indexOfAxisSplit == -1)
                {
                    Debug.LogWarning("diagonal Axis name incorrect in sequence or set up wrong. For joystick button input use JoystickButton0 - JoystickButton19." +
                    " For Axis put the same name as in the Input settings for the project.");
                    return;
                }

                string axis1 = keyStr.Substring(0, indexOfAxisSplit);
                string axis2 = keyStr.Substring(indexOfAxisSplit + 2);

                if (IsValidAxis(axis1) && IsValidAxis(axis2))
                {
                    float axis1Val = Input.GetAxis(axis1);
                    float axis2Val = Input.GetAxis(axis2);

                    bool axis1Active = false;
                    bool axis2Active = false;

                    if (m_mappings[index].m_axis1Dir == AxisDirection.kNeutral)
                    {
                        if (axis1Val == 0)
                        {
                            axis1Active = true;
                        }
                    }

                    if (m_mappings[index].m_axis2Dir == AxisDirection.kNeutral)
                    {
                        if (axis2Val == 0)
                        {
                            axis2Active = true;
                        }
                    }


                    if (Math.Abs(axis1Val) >= m_mappings[index].m_axis1Tolerance &&
                        Math.Abs(axis2Val) >= m_mappings[index].m_axis2Tolerance)  // check if both axis has moved far enough
                    {
                        // check first axis
                        if (m_mappings[index].m_axis1Dir == AxisDirection.kPositive && axis1Val > 0)
                        {
                            axis1Active = true;
                        }
                        else if (m_mappings[index].m_axis1Dir == AxisDirection.kNegative && axis1Val < 0)
                        {
                            axis1Active = true;
                        }

                        // check second axis
                        if (m_mappings[index].m_axis2Dir == AxisDirection.kPositive && axis2Val > 0)
                        {
                            axis2Active = true;
                        }
                        else if (m_mappings[index].m_axis2Dir == AxisDirection.kNegative && axis2Val < 0)
                        {
                            axis2Active = true;
                        }
                    }

                    if (axis1Active && axis2Active)
                    {
                        m_mappings[index].m_sequenceProgress += '(' + keyStr.ToString() + dir1 + dir2 + "), ";    // need to re-add the direction chars and () to match the orginal string
                        m_mappings[index].m_sequenceIndex = keyIndexEnd + 3;   // skip over the "), "
                    }

                }
            }

            // reset the directions
            m_mappings[index].m_axis1Dir = AxisDirection.kNeutral;
            m_mappings[index].m_axis2Dir = AxisDirection.kNeutral;
            return;

        }

        if (keyStr.Contains("+")) // check if it is a combo
        {
            if (keyStr.Contains("<") || keyStr.Contains(">") || keyStr.Contains("=")) // check if it is a single axis combo
            {
                char dir = keyStr[keyStr.Length - 1];
                keyStr = keyStr.TrimEnd('<', '>', '='); // trim the direction characters so HandleComboInput() can properly read the string
                SetAxisDirection(dir, index);

                m_mappings[index].m_input = keyStr; // change the string so HandleComboInput() can be used to save code
                
                if (HandleComboInput(index, true))
                {
                    m_mappings[index].m_sequenceProgress += keyStr.ToString() + dir + ", "; // need to re-add the direction char to match the orginal string
                    m_mappings[index].m_sequenceIndex = keyIndexEnd + 2;   // skip over the ", "
                }

                m_mappings[index].m_input = rawInputString; // reset the string
                m_mappings[index].m_axis1Dir = AxisDirection.kNeutral; // reset the direction
            }
            else // standard key+key combo
            {
                m_mappings[index].m_input = keyStr; // change the string so HandleComboInput() can be used to save code
                if (HandleComboInput(index, true))
                {
                    m_mappings[index].m_sequenceProgress += keyStr.ToString() + ", ";
                    m_mappings[index].m_sequenceIndex = keyIndexEnd + 2;   // skip over the ", "
                }
                m_mappings[index].m_input = rawInputString; // reset the string
            }
        }
        else if (keyStr.Contains("<") || keyStr.Contains(">") || keyStr.Contains("=")) // check if it is a single axis
        {
            char dir = keyStr[keyStr.Length - 1];
            keyStr = keyStr.TrimEnd('<', '>', '='); // trim the direction characters so IsValidAxis can properly read the string
            SetAxisDirection(dir, index);

            if (IsValidAxis(keyStr))
            {
                float val = Input.GetAxis(keyStr);

                // check to see if the stick is moved far enough and isnt a nuetral direction
                if (Math.Abs(val) < m_mappings[index].m_axis1Tolerance && m_mappings[index].m_axis1Dir != AxisDirection.kNeutral) 
                {
                    return;
                }

                // check that the input and direction of the joystick match
                if ((m_mappings[index].m_axis1Dir == AxisDirection.kPositive && val > 0) ||
                    (m_mappings[index].m_axis1Dir == AxisDirection.kNegative && val < 0) || 
                    (m_mappings[index].m_axis1Dir == AxisDirection.kNeutral && val == 0))
                {
                    m_mappings[index].m_sequenceProgress += keyStr.ToString() + dir + ", "; // need to re-add the direction char to match the orginal string
                    m_mappings[index].m_sequenceIndex = keyIndexEnd + 2;   // skip over the ", "
                }
            }

            m_mappings[index].m_input = rawInputString; // reset the string
            m_mappings[index].m_axis1Dir = AxisDirection.kNeutral; // reset the direction
        }
        else
        {
            bool temp1 = Enum.TryParse(keyStr, out nextKey);   // try to find the button/key in the KeyCode

            if (!temp1)
            {
                Debug.LogWarningFormat("<Color=Yellow> Error in sequence string!. example: W, W, S, S, A, D, A, D, B, A, Return </Color>");
            }

            if (Input.GetKeyDown(nextKey))
            {
                m_mappings[index].m_sequenceProgress += nextKey.ToString() + ", ";
                m_mappings[index].m_sequenceIndex = keyIndexEnd + 2;   // skip over the ", "
            }
            else if (Input.anyKeyDown && !Input.GetKeyDown(nextKey))    // check if a wrong key is pressed
            {
                Debug.Log("<Color=Yellow> sequence interupted </Color>");
                ResetSequence(index);
            }
        }

        //Debug.Log(m_mappings[index].m_sequenceProgress);    // uncomment to see sequence progress

        if (m_mappings[index].m_sequenceProgress.Contains(m_mappings[index].m_input))   // check if the sequence progress contains the sequence aka the sequence was done correctly
        {
            m_mappings[index].m_event.Invoke();
            Debug.Log("<Color=Green> sequence Invoked </Color>");
            ResetSequence(index);
        }

    }

    /// <summary>
    /// resets the sequence progress
    /// </summary>
    /// <param name="index"> index that needs to be reset </param>
    private void ResetSequence(int index)
    {
        m_mappings[index].m_timer = 0;
        m_mappings[index].m_sequenceProgress = "";
        m_mappings[index].m_sequenceIndex = 0;
        Debug.Log("Reset sequence: " + m_mappings[index].m_input);
    }

    /// <summary>
    /// for setting the desired direction for axis input
    /// </summary>
    /// <param name="dir"> the char the represents a direction for the axis </param>
    /// <param name="index"> the index for the mapping </param>
    /// <param name="axis1"> bool to set the first or second axis. first is default </param>
    private void SetAxisDirection(char dir, int index, bool axis1 = true)
    {
        switch (dir)
        {
            case '<':
                {
                    if (axis1)
                    {
                        m_mappings[index].m_axis1Dir = AxisDirection.kNegative;
                    }
                    else
                    {
                        m_mappings[index].m_axis2Dir = AxisDirection.kNegative;
                    }
                    break;
                }
            case '>':
                {
                    if (axis1)
                    {
                        m_mappings[index].m_axis1Dir = AxisDirection.kPositive;
                    }
                    else
                    {
                        m_mappings[index].m_axis2Dir = AxisDirection.kPositive;
                    }
                    break;
                }
            case '=':
                {
                    if (axis1)
                    {
                        m_mappings[index].m_axis1Dir = AxisDirection.kNeutral;
                    }
                    else
                    {
                        m_mappings[index].m_axis2Dir = AxisDirection.kNeutral;
                    }
                    break;
                }
            default:
                {
                    // should not get here unless being used with other chars
                    Debug.LogError("incorrect char");
                    break;
                }
        }
    }

    /// <summary>
    /// gets the button pressed and assigns it to the apropriate index
    /// </summary>
    /// <param name="index"> the index that is needing the button assigned </param>
    public void GetInput(int index)
    {
        m_setInput = true;
        m_index = index;
        m_mappings[m_index].m_input = "";
    }

    /// <summary>
    /// assigns the next input key pressed to the selected index of mappings.
    /// only works for keyboard and joystick buttons
    /// </summary>
    private void AssignButton()
    {
        foreach(int key in m_keyCodes)
        {
            if(Input.GetKey((KeyCode)key))
            {
                switch (m_mappings[m_index].m_type)
                {
                    case InputType.kStandard:
                        {
                            m_mappings[m_index].m_input = ((KeyCode)key).ToString();
                            m_index = -1;   // reset the index
                            m_setInput = false;
                            return;
                        }
                    case InputType.kCombo:
                        {
                            if (m_mappings[m_index].m_input != string.Empty)
                            {
                                KeyCode key1;
                                string tempString = m_mappings[m_index].m_input;
                                int indexOfSplit = tempString.IndexOf('+');
                                string button1 = tempString.Substring(0, indexOfSplit);
                                bool temp1 = Enum.TryParse(button1, out key1);   // try to find the button/key in the KeyCode

                                if (temp1 && key1 != (KeyCode)key)   // the second key in the combo cant be the same as the first
                                {
                                    m_mappings[m_index].m_input += ((KeyCode)key).ToString();
                                    m_index = -1;   // reset the index
                                    m_setInput = false;
                                    return;
                                }
                            }
                            else
                            {
                                m_mappings[m_index].m_input += ((KeyCode)key).ToString() + "+";
                            }
                            break;
                        }
                    case InputType.kSequence:
                        {
                            // will never get here if using editor script that goes with this class
                            Debug.LogWarning("Sequence must be typed in manually. example: W, W, S, S, A, D, A, D, B, A, Return");
                            break;
                        }
                    default:
                        {
                            // should never get here
                            Debug.Assert(false);
                            break;
                        }
                }
                Debug.Log((KeyCode)key + " Pressed");
            }
        }        
    }

    /// <summary>
    /// simple check to see if the axis string is setup or valid
    /// </summary>
    /// <param name="axisName"> the name of the axis from the projects Input manager settings</param>
    /// <returns> true is the axis name exists. </returns>
    public bool IsValidAxis(string axisName)
    {
        try
        {
            Input.GetAxis(axisName);
            return true;
        }
        catch
        {
            return false;
        }
    }

}
