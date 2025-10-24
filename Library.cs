using System;
using UnityEngine;

namespace DailyInterest
{
    public static class ModState
    {
        public static bool EconomyReady = false;
        public static long LastDay = -1L;

        public const double InterestRate = 0.005; // 0.5% daily interest

        public static void NotifyEconomyReady()
        {
            EconomyReady = true;
            Debug.Log("[Interest] Economy Manager Loaded");
        }

        public static void NotifyGameClockStepped()
        {
            long currentDay = GameClock.Day;

            if (LastDay == -1L)
            {
                LastDay = currentDay;
                Debug.Log($"[Interest] Game Clock Stepped - Initial Day Set to {LastDay}");
            }
            else if (GameClock.Day != LastDay)
            {
                long diffDay = GameClock.Day - LastDay;
                LastDay = GameClock.Day;
                Debug.Log($"[Interest] Game Clock Stepped - Day advanced by {diffDay} to {LastDay}");

                if (EconomyReady)
                {
                    // Equivalent to F# 'pown (1.0 + interestRate) (int diffDay) - 1.0'
                    double rate = Math.Pow(1.0 + InterestRate, (int)diffDay) - 1.0;
                    long increase = (long)Math.Floor(Duckov.Economy.EconomyManager.Money * rate);

                    string status = Duckov.Economy.EconomyManager.Add(increase) ? "succeeded" : "failed";
                    Debug.Log($"[Interest] Added {increase} units of currency due to day advancement: {status}");
                }
            }
        }
    }

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public void OnEnable()
        {
            Debug.Log("[Interest] Mod Enabled");
            Duckov.Economy.EconomyManager.OnEconomyManagerLoaded += ModState.NotifyEconomyReady;
            GameClock.OnGameClockStep += ModState.NotifyGameClockStepped;
        }

        public void OnDisable()
        {
            Debug.Log("[Interest] Mod Disabled");
            Duckov.Economy.EconomyManager.OnEconomyManagerLoaded -= ModState.NotifyEconomyReady;
            GameClock.OnGameClockStep -= ModState.NotifyGameClockStepped;
        }
    }
}
