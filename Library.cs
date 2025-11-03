using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DailyInterest
{
  public static class ModMain
  {
    public static bool EconomyReady = false;
    public static TimeSpan LastDate;

    public static bool Initialized = false;

    public static bool ShowNotifications = true;

    public static void NotifyEconomyReady()
    {
      EconomyReady = true;
      Initialized = false;
      Debug.Log("[Daily Interest] Economy Manager Loaded");
    }

    public static void NotifyGameClockStepped()
    {
      var currentDate = GameClock.Now;

      if (!Initialized || currentDate < LastDate)
      {
        LastDate = currentDate;
        Initialized = true;
        Debug.Log($"[Daily Interest] Game Clock Stepped - Initial Day Set to {LastDate}");
      }
      else if (Initialized && currentDate.Days > LastDate.Days)
      {
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var diffDays = currentDate.Days - LastDate.Days;
        var diff = currentDate - LastDate;
        LastDate = currentDate;
        Debug.Log($"[Daily Interest] Game Clock Stepped - Day advanced by {diff} to {LastDate}");

        if (EconomyReady)
        {
          var mi = new MessageInstance(diff);

          diffDays = diffDays > 2 ? 2 : diffDays;

          var rate = Math.Pow(1.0 + mi.Rate, diffDays) - 1.0;
          var increase = (long)Math.Floor(Duckov.Economy.EconomyManager.Money * rate);

          var result = Duckov.Economy.EconomyManager.Add(increase);

          if (result && ShowNotifications) mi.ShowMessage(increase);

          var text = result ? "succeeded" : "failed";
          Debug.Log($"[Daily Interest] Added {increase} units of currency due to day advancement: {text}");
        }
        watch.Stop();
        Debug.Log($"[Daily Interest] Finished in {watch.Elapsed.TotalMilliseconds} ms");
      }
    }
  }

  public class ModBehaviour : Duckov.Modding.ModBehaviour
  {
    GameObject debugWindowObject;
    private static string configFilePath;

    private static string GetEnableNotificationMessage(string langKey)
    {
      return langKey switch
      {
        "ChineseSimplified" => "每日利息：消息提醒已开启",
        _ => "Daily Interest: Notification Enabled"
      };
    }

    private static string GetDisableNotificationMessage(string langKey)
    {
      return langKey switch
      {
        "ChineseSimplified" => "每日利息：消息提醒已关闭",
        _ => "Daily Interest: Notification Disabled"
      };
    }

    public void Update()
    {
      if (Keyboard.current != null &&
        (Keyboard.current.leftCtrlKey.isPressed ||
         Keyboard.current.rightCtrlKey.isPressed ||
         Keyboard.current.leftCommandKey.isPressed ||
         Keyboard.current.rightCommandKey.isPressed) && Keyboard.current.semicolonKey.wasPressedThisFrame)
      {
        ModMain.ShowNotifications = !ModMain.ShowNotifications;

        if (ModMain.ShowNotifications)
        {
          if (File.Exists(configFilePath))
          {
            File.Delete(configFilePath);
          }
        }
        else
        {
          File.Create(configFilePath).Close();
        }

        var langKey = MessageLocale.Lang.ToString();
        var message = ModMain.ShowNotifications ? GetEnableNotificationMessage(langKey) : GetDisableNotificationMessage(langKey);
        Debug.Log($"[Daily Interest] ShowNotifications was changed to {ModMain.ShowNotifications}");
        LevelManager.Instance?.MainCharacter?.PopText(message);
      }
    }

    public void OnEnable()
    {
      Debug.Log("[Daily Interest] Mod Enabled");

      var assemblyLocation = Assembly.GetExecutingAssembly().Location;
      configFilePath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "NO_NOTIFICATION");
      ModMain.ShowNotifications = !File.Exists(configFilePath);
      Debug.Log($"[Daily Interest] ShowNotifications was set to {ModMain.ShowNotifications}");

      Duckov.Economy.EconomyManager.OnEconomyManagerLoaded += ModMain.NotifyEconomyReady;
      GameClock.OnGameClockStep += ModMain.NotifyGameClockStepped;

      if (debugWindowObject == null)
      {
        debugWindowObject = new GameObject("DailyInterest_DebugWindow");
        debugWindowObject.AddComponent<DebugWindow>();
        DontDestroyOnLoad(debugWindowObject);
      }
    }

    public void OnDisable()
    {
      Debug.Log("[Daily Interest] Mod Disabled");
      Duckov.Economy.EconomyManager.OnEconomyManagerLoaded -= ModMain.NotifyEconomyReady;
      GameClock.OnGameClockStep -= ModMain.NotifyGameClockStepped;
      ModMain.EconomyReady = false;
      ModMain.Initialized = false;

      if (debugWindowObject != null)
      {
        Destroy(debugWindowObject);
        debugWindowObject = null;
      }
    }
  }
}
