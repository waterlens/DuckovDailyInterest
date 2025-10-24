namespace DailyInterest

open UnityEngine
open Duckov

module ModState =
    let mutable economyReady = false
    let mutable lastDay = -1L

    let interestRate = 0.0005 // 0.5% daily interest

    let notifyEconomyReady _ =
        economyReady <- true
        Debug.Log("[Interest] Economy Manager Loaded")

    let notifyGameClockStepped _ =
        let currentDay = GameClock.Day

        if lastDay = -1L then
            lastDay <- currentDay
            Debug.Log($"[Interest] Game Clock Stepped - Initial Day Set to {string lastDay}")
        else if GameClock.Day <> lastDay then
            let diffDay = GameClock.Day - lastDay
            lastDay <- GameClock.Day
            Debug.Log($"[Interest] Game Clock Stepped - Day advanced by {string diffDay} to {string lastDay}")
            if economyReady then
                let rate: float = pown (1.0 + interestRate) (int diffDay) - 1.0
                let increase = float Economy.EconomyManager.Money * rate |> floor |> int64
                let status =
                    if Economy.EconomyManager.Add increase then
                        "succeeded"
                    else
                        "failed"
                Debug.Log($"[Interest] Added {string increase} units of currency due to day advancement: {status}")

type ModBehaviour() =
    inherit Modding.ModBehaviour()


    member public this.OnEanble() =
        Debug.Log("[Interest] Mod Enabled")
        Economy.EconomyManager.add_OnEconomyManagerLoaded (ModState.notifyEconomyReady)
        GameClock.add_OnGameClockStep (ModState.notifyGameClockStepped)

    member public this.OnDisable() =
        Debug.Log("[Interest] Mod Disabled")
        Economy.EconomyManager.remove_OnEconomyManagerLoaded (ModState.notifyEconomyReady)
        GameClock.remove_OnGameClockStep (ModState.notifyEconomyReady)

