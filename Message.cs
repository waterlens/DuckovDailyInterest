using System;
using Duckov.Weathers;
using MathNet.Numerics.Distributions;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using System.Reflection;
using Duckov.Economy;

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
          Debug.Log($"[Daily Interest] Loaded {messages.Count} messages for language '{lang}'");
        }
      }
    }
  }

  public class MessageInstance
  {

    Dictionary<string, Lazy<Value>> triggers;
    public TriggerEvaluator executor;

    public Value safeExecute(string input)
    {
      try
      {
        executor.Eval(input);
        var result = executor.PeekResult() ?? 0;
        return result;
      }
      catch (TriggerEvalExn exn)
      {
        Debug.Log($"[Daily Interest] execute `{input}` failed: {exn}");
      }
      return false;
    }
    public double Rate => triggers["RATE"].Value.ToNumber();
    public MessageInstance(TimeSpan diff)
    {
      var watch = System.Diagnostics.Stopwatch.StartNew();
      triggers = new Dictionary<string, Lazy<Value>>
      {
          { "LUCK", new Lazy<Value>(MessageSource.TLuckTrigger) },
          { "LUCKM", new Lazy<Value>(MessageSource.TLuckMean) },
          { "WEATHER", new Lazy<Value>(MessageSource.TWeatherTrigger) },
          { "WEATHER6", new Lazy<Value>(() => MessageSource.TWeatherHoursLaterTrigger(6)) },
          { "WEATHER4", new Lazy<Value>(() => MessageSource.TWeatherHoursLaterTrigger(4)) },
          { "HOD", new Lazy<Value>(MessageSource.THourOfDay) },
          { "HDIFF", new Lazy<Value>(() => diff.TotalHours) },
          { "MDIFF", new Lazy<Value>(diff.TotalMinutes) },
          { "RATE", new Lazy<Value>(MessageSource.TRate) },
          { "RATEM", new Lazy<Value>(MessageSource.TRateMean) },
          { "BQ", new Lazy<Value>(MessageSource.TBestQualityOfItemsInInventory) },
          { "BALANCE", new Lazy<Value>(MessageSource.TBalance) },
          { "CASH", new Lazy<Value>(MessageSource.TCash) },
      };
      watch.Stop();
      Debug.Log($"[Daily Interest] Execute triggers in {watch.Elapsed.TotalMilliseconds} ms.");
      executor = new TriggerEvaluator(triggers);
    }

    public void ShowMessage(Int64 interest)
    {
      triggers.Add("INT", new Lazy<Value>(() => interest));

      string template;

      var langKey = MessageLocale.Lang.ToString();
      var gotTranslation = MessageLocale.Translations.TryGetValue(langKey, out var messages);
      int speed = GetDefaultSpeed(langKey);
      if (gotTranslation && messages != null && messages.Any())
      {
        messages = messages.Where(msg =>
        {
          var result = safeExecute(msg.Trigger);
          Debug.Log($"[Daily Interest] eval({msg.Trigger}) = {result}");
          return result.ToBoolean();
        }).ToList();
        var totalFrequency = messages.Sum(m => m == null ? 0 : m.Frequency);
        if (totalFrequency > 0)
        {
          var randomValue = MessageSource.Rand.Next(totalFrequency);
          Message selectedMessage = messages.FirstOrDefault();
          Debug.Log($"[Daily Interest] Total Freq {totalFrequency}, Rand {randomValue}");
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
          return string.Format("{}", result);
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

    private static int GetDefaultSpeed(string langKey)
    {
      return langKey switch
      {
        "ChineseSimplified" => 10,
        "ChineseTraditional" => 10,
        "English" => 20,
        "French" => 20,
        "German" => 20,
        "Japanese" => 12,
        "Korean" => 10,
        "Portuguese" => 18,
        "Russian" => 20,
        "Spanish" => 22,
        _ => 20,
      };
    }
  }

  public static class MessageSource
  {
    internal static System.Random Rand = new System.Random();
    // https://mathlets.org/mathlets/beta-distribution/
    static BetaScaled LuckDistribution = new BetaScaled(25.0, 20.0, 0, 1.0, Rand);

    static BetaScaled RateDistribution = new BetaScaled(2.0, 7.0, 0.0042, 0.004, Rand);

    public static Func<Value> TLuckMean = () => LuckDistribution.Mean;
    public static Func<Value> TLuckTrigger = () => LuckDistribution.Sample();
    // Weather: Sunny,Cloudy,Rainy,Stormy_I,Stormy_II
    public static Func<Value> TWeatherTrigger = () => (double)(int)WeatherManager.GetWeather();
    public static Func<int, Value> TWeatherHoursLaterTrigger = (n) => (double)(int)WeatherManager.GetWeather(GameClock.Now + TimeSpan.FromHours(n));
    public static Func<Value> TBalance = () => (double)EconomyManager.Money;
    public static Func<Value> TCash = () => (double)EconomyManager.Cash;
    public static Func<Value> THourOfDay = () => (double)GameClock.Hour;
    public static Func<Value> TRate = () => RateDistribution.Sample();
    public static Func<Value> TRateMean = () => RateDistribution.Mean;
    public static Func<Value> TBestQualityOfItemsInInventory = () =>
    {
      var inventory = LevelManager.Instance?.MainCharacter?.CharacterItem?.Inventory;
      var content = inventory?.Content;
      if (content != null && content.Any())
        return content.Max(item => item != null ? item.Quality : 0);
      return 0;
    };
    static MessageSource()
    {
      Debug.Log($"[Daily Interest] LuckDistribution Mean = {LuckDistribution.Mean} StdDev = {LuckDistribution.StdDev}");
      Debug.Log($"[Daily Interest] RateDistribution Mean = {RateDistribution.Mean} StdDev = {RateDistribution.StdDev}");
    }
  }
}
