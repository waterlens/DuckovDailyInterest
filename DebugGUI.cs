
using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Reflection;

namespace DailyInterest
{
  public class DebugWindow : MonoBehaviour
  {
    private bool showWindow = false;
    private Rect windowRect = new Rect(20, 120, 400, 300);
    private string expression = "HDIFF > 1";
    private string result = "";
    private static TimeSpan debugTimeSpan = new TimeSpan(9, 12, 34, 56, 78);
    private static Vector2 referenceResolution = new Vector2(1280, 720);
    private static bool? f4Enabled = null;

    void Update()
    {
      if (f4Enabled == null) {
        string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        string debugFlagFile = Path.Combine(assemblyFolder, "NODEBUG_DAILY_INTEREST");
        f4Enabled = !File.Exists(debugFlagFile);
      }
      if (f4Enabled.Value && Input.GetKeyDown(KeyCode.F4))
        showWindow = !showWindow;
    }

    void OnGUI()
    {
      if (!showWindow) return;
      Matrix4x4 originalMatrix = GUI.matrix;

      float scaleX = Screen.width / referenceResolution.x;
      float scaleY = Screen.height / referenceResolution.y;
      float scale = Mathf.Min(scaleX, scaleY);

      Matrix4x4 scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(scale, scale, 1));
      GUI.matrix = scaleMatrix;

      windowRect = GUI.Window(GetHashCode(), windowRect, DrawWindow, "DuckovT Debugger");
      GUI.matrix = originalMatrix;
    }

    void DrawWindow(int windowID)
    {
      GUILayout.BeginVertical();

      GUILayout.Label($"Using TimeSpan: {debugTimeSpan}");

      GUILayout.Label("Expression:");
      expression = GUILayout.TextArea(expression, GUILayout.Height(80));

      if (GUILayout.Button("Evaluate"))
      {
        try
        {
          var messageInstance = new MessageInstance(debugTimeSpan);
          messageInstance.executor.Eval(expression);
          var evalResult = messageInstance.executor.PeekResult();
          result = evalResult != null ? evalResult.ToString() : "null";
        }
        catch (Exception e)
        {
          result = $"Error: {e.Message}";
        }
      }

      GUILayout.Label($"Result: {result}");

      GUILayout.EndVertical();
      GUI.DragWindow();
    }
  }
}

