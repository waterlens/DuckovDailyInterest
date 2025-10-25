using System;
using Duckov.Weathers;
using MathNet.Numerics.Distributions;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;

namespace DailyInterest
{
  public class Message
  {
    public int ID { get; set; }
    public string Template { get; set; }
    public int Frequency { get; set; }
    public string Trigger { get; set; }
  }

  public static class MessageLocale
  {
    public static Dictionary<string, List<Message>> Translations = new Dictionary<string, List<Message>>();
    public static SystemLanguage Lang => SodaCraft.Localizations.LocalizationManager.CurrentLanguage;
    static MessageLocale()
    { 
      var assembly = Assembly.GetExecutingAssembly();
      var assemblyDirectory = Path.GetDirectoryName(assembly.Location);
      var localeDirectory = Path.Combine(assemblyDirectory, "Localization");

      if (Directory.Exists(localeDirectory))
      {
        foreach (var file in Directory.GetFiles(localeDirectory, "*.tsv"))
        {
          var lang = Path.GetFileNameWithoutExtension(file);

          var messages = new List<Message>();
          var lines = File.ReadAllLines(file);

          // Skip header
          for (int i = 1; i < lines.Length; i++)
          {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = line.Split('\t');
            if (parts.Length >= 4)
            {
              messages.Add(new Message
              {
                ID = int.Parse(parts[0]),
                Template = parts[1].Trim('"'),
                Frequency = int.Parse(parts[2]),
                Trigger = parts[3]
              });
            }
          }
          Translations[lang] = messages;
          Debug.Log($"[DailyInterest] Loaded {messages.Count} messages for language '{lang}'");
        }
      }
    }
  }

  public class MessageInstance
  {

    Dictionary<String, Value> triggers;
    TriggerEvaluator executor;

    public Value safeExecute(string input)
    {
      try
      {
        executor.Eval(input);
        return executor.PeekResult();
      }
      catch (TriggerEvalExn exn)
      {
        Debug.Log($"[Daily Interest] execute `{input}` failed: {exn}");
      }
      return false;
    }
    public double Rate => triggers["RATE"].ToNumber();
    public MessageInstance(TimeSpan diff)
    {
      triggers = new Dictionary<string, Value>
      {
          { "LUCK", MessageSource.TLuckTrigger () },
          { "WEATHER", MessageSource.TWeatherTrigger () },
          { "HOD", MessageSource.THourOfDay () },
          { "HDIFF", diff.TotalHours },
          { "MDIFF", diff.TotalMinutes },
          { "RATE", MessageSource.TRate () },
          { "BQ", MessageSource.TBestQualityOfItemsInInventory () },
      };
      executor = new TriggerEvaluator(triggers);
    }

    public void filterMessages(List<Message> messages)
    {
    }

    public void ShowMessage(Int64 interest)
    {
      triggers.Add("INT", interest);

      string template;
      int speed = 10;

      var langKey = MessageLocale.Lang.ToString();
      var gotTranslation = MessageLocale.Translations.TryGetValue(langKey, out var messages);
      messages = messages.Where(msg => safeExecute(msg.Trigger).ToBoolean()).ToList();

      if (gotTranslation && messages.Any())
      {
        var totalFrequency = messages.Sum(m => m.Frequency);
        if (totalFrequency > 0)
        {
          var randomValue = MessageSource.Rand.Next(totalFrequency);
          Message selectedMessage = null;
          Debug.Log($"[DailyInterest] Total Freq {totalFrequency}, Rand {randomValue}");
          foreach (var message in messages)
          {
            randomValue -= message.Frequency;
            if (randomValue < 0)
            {
              selectedMessage = message;
              break;
            }
          }
          template = selectedMessage.Template;
        }
        else if (messages.Any())
          template = messages[0].Template;
        else
          template = GetDefaultTemplate(langKey);
      }
      else
        template = GetDefaultTemplate(langKey);

      var text = ProcessTemplate(template);
      LevelManager.Instance?.MainCharacter?.PopText(text, speed);
    }

    public string ProcessTemplate(string template)
    {
      return Regex.Replace(template, @"#\{(.+?)\}", match =>
      {
        var key = match.Groups[1].Value;
        var result = safeExecute(key);
        if (result != null)
          return result.ToString();
        return match.Value;
      });
    }

    private static string GetDefaultTemplate(string langKey)
    {
      return langKey switch
      {
        "ChineseSimplified" => "每日利息收入 +$#{INT}",
        "ChineseTraditional" => "每日利息收入 +$#{INT}",
        "English" => "Daily Interest Income +$#{INT}",
        "French" => "Revenu d'intérêts quotidien +$#{INT}",
        "German" => "Tägliche Zinseinnahmen +$#{INT}",
        "Japanese" => "毎日の利息収入 +$#{INT}",
        "Korean" => "일일 이자 수입 +$#{INT}",
        "Portuguese" => "Renda diária de juros +$#{INT}",
        "Russian" => "Ежедневный процентный доход +$#{INT}",
        "Spanish" => "Ingresos por intereses diarios +$#{INT}",
        _ => "Daily Interest Income +$#{INT}"
      };
    }
  }

  public static class MessageSource
  {
    internal static System.Random Rand = new System.Random();
    // https://mathlets.org/mathlets/beta-distribution/
    static BetaScaled LuckDistribution = new BetaScaled(25.0, 20.0, 0, 1.0, Rand);

    static BetaScaled RateDistribution = new BetaScaled(2.0, 7.0, 0.004, 0.004, Rand);

    public static Func<Value> TLuckTrigger = () => LuckDistribution.Sample();
    public static Func<Value> TWeatherTrigger = () => (double)(int)WeatherManager.GetWeather();
    public static Func<Value> TWeatherSixHoursLaterTrigger = () => (double)(int)WeatherManager.GetWeather(GameClock.Now + TimeSpan.FromHours(6));
    public static Func<Value> THourOfDay = () => (double)GameClock.Hour;
    public static Func<Value> TRate = () => RateDistribution.Sample();
    public static Func<Value> TBestQualityOfItemsInInventory = () =>
    {
      var inventory = LevelManager.Instance?.MainCharacter?.CharacterItem?.Inventory;
      var content = inventory?.Content;
      if (content != null && content.Any()) return content.Max(item => item.Quality);
      return 0;
    };
    static MessageSource()
    {
      Debug.Log($"LuckDistribution Mean = {LuckDistribution.Mean} StdDev = {LuckDistribution.StdDev}");
      Debug.Log($"RateDistribution Mean = {RateDistribution.Mean} StdDev = {RateDistribution.StdDev}");
    }
  }
}
