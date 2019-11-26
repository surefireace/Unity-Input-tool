// By Donovan Colen
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///  this is a example script to show some of what can be done with the CustomInput script
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ExampleUse : MonoBehaviour
{
    Rigidbody m_rb;
    Vector2 m_leftStick;

    // Start is called before the first frame update
    void Start()
    {
        m_rb = gameObject.GetComponent<Rigidbody>();
    }

    public void Up()
    {
        m_rb.velocity = new Vector3(0, 0.5f, 0);
        Debug.Log("Moving Up");
    }

    public void Down()
    {
        m_rb.velocity = new Vector3(0, -0.5f, 0);
        Debug.Log("Moving Down");
    }

    public void Right()
    {
        m_rb.velocity = new Vector3(0.5f, 0, 0);
        Debug.Log("Moving Right");
    }

    public void Left()
    {
        m_rb.velocity = new Vector3(-0.5f, 0, 0);
        Debug.Log("Moving Left");
    }

    public void Forward()
    {
        m_rb.velocity = new Vector3(0, 0, 0.5f);
        Debug.Log("Moving Forward");
    }

    public void Backward()
    {
        m_rb.velocity = new Vector3(0, 0, -0.5f);
        Debug.Log("Moving Backward");
    }

    public void LeftRightAxis(float val)
    {
        m_leftStick.x = val;
        if (val != 0)
        {
            Debug.Log("Moving LeftRightAxis");
        }
    }

    public void UpDownAxis(float val)
    {
        m_leftStick.y = val;
        if (val != 0)
        {
            Debug.Log("Moving UpDownAxis");
        }
    }

    public void ForwardBackwardAxis(float val)
    {
        m_rb.velocity = new Vector3(m_rb.velocity.x, m_rb.velocity.y, val);
        if (val != 0)
        {
            Debug.Log("Moving ForwardBackwardAxis");
        }
    }

    public void Stop()
    {
        m_rb.velocity = Vector3.zero;
        Debug.Log("Stopped");
    }

    public void EasterEgg() // prints EASTER EGG in italic bold rainbow
    {
        Debug.LogFormat("<B><I><Color=Red>E</Color><Color=Orange>A</Color><Color=Yellow>S</Color>" +
            "<Color=Green>T</Color><Color=Cyan>E</Color><Color=Blue>R</Color> <Color=Purple>E</Color>" +
            "<Color=Magenta>G</Color><Color=Red>G</Color></I></B>");
    }

    // Update is called once per frame
    void Update()
    {
        m_rb.velocity = m_leftStick;
    }
}
