using System.Collections;
using UnityEngine;

namespace CodeZash.Debugger {
    public class TextDebug : MonoBehaviour {

        uint qsize = 30;  // number of messages to keep
        Queue myLogQueue = new Queue();

        void Start() {
            Debug.Log("Started up logging.");
        }

        void OnEnable() {
            Application.logMessageReceived += HandleLog;
        }

        void OnDisable() {
            Application.logMessageReceived -= HandleLog;
        }

        void HandleLog(string logString, string stackTrace, LogType type) {
            // Give color each log type
            switch (type) {
                case LogType.Error:
                    logString = "<color=red>" + logString + "</color>";
                    break;
                case LogType.Assert:
                    logString = "<color=red>" + logString + "</color>";
                    break;
                case LogType.Warning:
                    logString = "<color=yellow>" + logString + "</color>";
                    break;
                case LogType.Log:
                    logString = "<color=white>" + logString + "</color>";
                    break;
                case LogType.Exception:
                    logString = "<color=red>" + logString + "</color>";
                    break;
            }

            myLogQueue.Enqueue("[" + type + "] : " + logString);
            if (type == LogType.Exception)
                myLogQueue.Enqueue(stackTrace);
            while (myLogQueue.Count > qsize)
                myLogQueue.Dequeue();
        }

        void OnGUI() {
            GUILayout.BeginArea(new Rect(10, 0, 400, Screen.height/4));
            // add background color
            GUI.Box(new Rect(10, 0, 400, Screen.height/4), "");
            GUILayout.Label("\n" + string.Join("\n", myLogQueue.ToArray()));
            GUILayout.EndArea();
        }

    }
}