using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PrintDebugLog : MonoBehaviour
{

    private int numLines;
    private int maxLines;

    private void Start()
    {
        Application.logMessageReceived += HandleLog;
        numLines = 0;
        maxLines = 25;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        string newText = this.GetComponent<Text>().text += "\n" + logString;

        numLines++;
        if (numLines > maxLines)
        {
            newText = RemoveFirstLine(newText);
        }

        this.GetComponent<Text>().text = newText;
    }

    private string RemoveFirstLine(string str)
    {
        int index = str.IndexOf("\n");
        if (index == -1)
        {
            return str;
        }

        return str.Substring(index + 1);
    }
}
