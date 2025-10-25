using System;
using System.Collections.Generic;
using Duckov.CreditsUtility;
using UnityEngine;

namespace DailyInterest
{

    public class TriggerEvaluator
    {

        string input;
        int pos;
        string token;
        TokenKind tokenKind;

        Stack<double> evalStack;

        enum TokenKind
        {
            EOF,
            NAME,
            OPERATOR,
            NUMBER,
        }

        [Serializable]
        public class TriggerEvalExn : Exception
        {
            public TriggerEvalExn()
            { }

            public TriggerEvalExn(string message)
                : base(message)
            { }

            public TriggerEvalExn(string message, Exception innerException)
                : base(message, innerException)
            { }
        }

        public void Parse(string expression)
        {
            input = expression;
            pos = 0;
            token = null;
            evalStack = new Stack<double>();
            ParseWithAff(0);
            if (tokenKind != TokenKind.EOF)
                throw new TriggerEvalExn("Extra characters at end of expression");
        }

        TokenKind NextToken()
        {
            while (pos < input.Length && char.IsWhiteSpace(input[pos]))
                pos++;

            if (pos >= input.Length)
            {
                tokenKind = TokenKind.EOF;
                token = null;
                return tokenKind;
            }

            char ch = input[pos];

            if (char.IsLetter(ch) || ch == '_')
            {
                int start = pos;
                while (pos < input.Length && (char.IsLetterOrDigit(input[pos]) || input[pos] == '_'))
                    pos++;
                token = input.Substring(start, pos - start);
                tokenKind = TokenKind.NAME;
                return tokenKind;
            }

            if (char.IsDigit(ch))
            {
                int start = pos;
                while (pos < input.Length && char.IsDigit(input[pos]))
                    pos++;
                token = input.Substring(start, pos - start);
                tokenKind = TokenKind.NUMBER;
                return tokenKind;
            }

            int startPos = pos;
            pos++;
            if (pos < input.Length)
            {
                string twoCharOp = input.Substring(startPos, 2);
                switch (twoCharOp)
                {
                    case "**":
                    case "==":
                    case "!=":
                    case "<=":
                    case ">=":
                    case "&&":
                    case "||":
                        pos++;
                        token = twoCharOp;
                        tokenKind = TokenKind.OPERATOR;
                        return tokenKind;
                }
            }

            token = ch.ToString();
            tokenKind = TokenKind.OPERATOR;
            return tokenKind;
        }

        (string, TokenKind) PeekToken()
        {
            int originalPos = pos;
            string originalToken = token;
            TokenKind originalTokenKind = tokenKind;

            NextToken();

            string peekedTokenStr = token;
            TokenKind peekedTokenKind = tokenKind;

            pos = originalPos;
            token = originalToken;
            tokenKind = originalTokenKind;

            return (peekedTokenStr, peekedTokenKind);
        }

        int GetPrefixPrecedence(string op)
        {
            switch (op)
            {
                case "+": case "-": return 6;
                default: return 0;
            }
        }

        (int, int) GetInfixPrecedence(string op)
        {
            if (op == null) return (0, 0);
            switch (op)
            {
                // left-associative operators (l, l + 1)
                case "||": return (1, 2);
                case "&&": return (2, 3);
                case "==": case "!=": case "<": case ">": case "<=": case ">=": return (3, 4);
                case "+": case "-": return (4, 5);
                case "*": case "/": return (5, 6);
                // right-associative operator (p + 1, p) per user request
                case "**": return (8, 7);
                default: return (0, 0);
            }
        }

        void ParseWithAff(int minAff)
        {
            NextToken();
            switch (tokenKind)
            {
                case TokenKind.NUMBER:
                    evalStack.Push(int.Parse(token));
                    break;
                case TokenKind.NAME:
                    // call handler
                    break;
                case TokenKind.OPERATOR:
                    if (token == "(")
                    {
                        ParseWithAff(0);
                        var (peekedToken, peekedKind) = PeekToken();
                        if (peekedKind != TokenKind.OPERATOR || peekedToken != ")")
                            throw new TriggerEvalExn("Expected ')'");
                        NextToken(); // Consume ')'
                    }
                    else if (token == "+" || token == "-")
                    {
                        int precedence = GetPrefixPrecedence(token);
                        ParseWithAff(precedence);
                        if (token == "-")
                        {
                            double r;
                            if (!evalStack.TryPop(out r))
                                throw new TriggerEvalExn("Missing operand for unary minus");
                            evalStack.Push(-r);
                        }
                    }
                    else
                        throw new TriggerEvalExn($"Unexpected operator in prefix position: {token}");
                    break;
                default:
                    throw new TriggerEvalExn($"Unexpected token: {token}");
            }

            while (true)
            {
                var (peekedToken, peekedKind) = PeekToken();

                if (peekedKind != TokenKind.OPERATOR || peekedToken == ")")
                {
                    break;
                }

                var (leftPrecedence, rightPrecedence) = GetInfixPrecedence(peekedToken);

                if (leftPrecedence < minAff)
                {
                    break;
                }

                NextToken(); // Consume operator
                string op = token;

                ParseWithAff(rightPrecedence);

                double l, r, ans;
                if (!evalStack.TryPop(out r)) throw new TriggerEvalExn("Lack of right operand");
                if (!evalStack.TryPop(out l)) throw new TriggerEvalExn("Lack of left operand");

                ans = op switch
                {
                    "+" => l + r,
                    "-" => l - r,
                    "*" => l * r,
                    "/" => l / r,
                    "**" => Math.Pow(l, r),
                    _ => throw new TriggerEvalExn($"Unsupported operator {op}")
                };
                evalStack.Push(ans);
            }
        }
    }

    public static class Localization
    {
        static System.Random Rand = new System.Random();
        static SystemLanguage Lang
        {
            get
            {
                return SodaCraft.Localizations.LocalizationManager.CurrentLanguage;
            }
        }
        public static void ShowMessage(Int64 increase)
        {
            if (Lang == SystemLanguage.ChineseSimplified || Lang == SystemLanguage.ChineseTraditional)
            {
                LevelManager.Instance?.MainCharacter?.PopText("", 8);
            }
            else
            {
                var message = $"Daily Interest: +${increase}";
                LevelManager.Instance?.MainCharacter?.PopText(message, 10);
            }
        }

        static Localization()
        {
        }
    }

    public static class ModMain
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
            var currentDay = GameClock.Day;

            if (LastDay == -1L || GameClock.Day < LastDay)
            {
                LastDay = currentDay;
                Debug.Log($"[Interest] Game Clock Stepped - Initial Day Set to {LastDay}");
            }
            else if (GameClock.Day > LastDay)
            {
                var diffDay = GameClock.Day - LastDay;
                LastDay = GameClock.Day;
                Debug.Log($"[Interest] Game Clock Stepped - Day advanced by {diffDay} to {LastDay}");

                if (EconomyReady)
                {
                    var rate = Math.Pow(1.0 + InterestRate, diffDay) - 1.0;
                    var increase = (long)Math.Floor(Duckov.Economy.EconomyManager.Money * rate);

                    var result = Duckov.Economy.EconomyManager.Add(increase);

                    if (result) Localization.ShowMessage(increase);

                    var text = result ? "succeeded" : "failed";
                    Debug.Log($"[Interest] Added {increase} units of currency due to day advancement: {text}");
                }
            }
        }
    }

    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        public void OnEnable()
        {
            Debug.Log("[Interest] Mod Enabled");
            Duckov.Economy.EconomyManager.OnEconomyManagerLoaded += ModMain.NotifyEconomyReady;
            GameClock.OnGameClockStep += ModMain.NotifyGameClockStepped;
        }

        public void OnDisable()
        {
            Debug.Log("[Interest] Mod Disabled");
            Duckov.Economy.EconomyManager.OnEconomyManagerLoaded -= ModMain.NotifyEconomyReady;
            GameClock.OnGameClockStep -= ModMain.NotifyGameClockStepped;
            ModMain.EconomyReady = false;
            ModMain.LastDay = -1L;
        }
    }
}
