using System;
using UnityEngine;

namespace DailyInterest
{
  public static class ModMain
  {
    public static bool EconomyReady = false;
    public static TimeSpan LastDate;

    public static bool Initialized = false;

    public const double InterestRate = 0.005; // 0.5% daily interest

    public static void NotifyEconomyReady()
    {
      EconomyReady = true;
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

          var rate = Math.Pow(1.0 + mi.Rate, diffDays) - 1.0;
          var increase = (long)Math.Floor(Duckov.Economy.EconomyManager.Money * rate);

          var result = Duckov.Economy.EconomyManager.Add(increase);

          if (result) mi.ShowMessage(increase);

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

    public void OnEnable()
    {
      Debug.Log("[Daily Interest] Mod Enabled");
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
