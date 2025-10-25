
using System.Collections.Generic;
using System;

namespace DailyInterest
{

  public class Value
  {
    readonly double numberValue;

    // Constructor
    public Value(double value)
    {
      numberValue = value;
    }

    // Converters
    public double ToNumber() => numberValue;
    public bool ToBoolean() => numberValue != 0;

    // Implicit/Explicit conversions
    public static implicit operator Value(double d) => new Value(d);
    public static implicit operator Value(bool b) => new Value(b ? 1 : 0);
    public static explicit operator double(Value v) => v.ToNumber();
    public static explicit operator bool(Value v) => v.ToBoolean();

    // Unary operators
    public static Value operator -(Value a) => -a.ToNumber();
    public static Value operator !(Value a) => !a.ToBoolean();

    // Binary arithmetic operators
    public static Value operator +(Value a, Value b) => a.ToNumber() + b.ToNumber();
    public static Value operator -(Value a, Value b) => a.ToNumber() - b.ToNumber();
    public static Value operator *(Value a, Value b) => a.ToNumber() * b.ToNumber();
    public static Value operator /(Value a, Value b) => a.ToNumber() / b.ToNumber();
    public static Value operator %(Value a, Value b) => a.ToNumber() % b.ToNumber();

    // Comparison operators
    public static Value operator ==(Value a, Value b)
    {
      if (ReferenceEquals(a, b)) return true;
      if (a is null || b is null) return false;
      return a.numberValue == b.numberValue;
    }

    public static Value operator !=(Value a, Value b) => !(a == b);

    public static Value operator <(Value a, Value b) => a.ToNumber() < b.ToNumber();
    public static Value operator >(Value a, Value b) => a.ToNumber() > b.ToNumber();
    public static Value operator <=(Value a, Value b) => a.ToNumber() <= b.ToNumber();
    public static Value operator >=(Value a, Value b) => a.ToNumber() >= b.ToNumber();

    // Logical operators for && and ||
    public static bool operator true(Value a) => a.ToBoolean();
    public static bool operator false(Value a) => !a.ToBoolean();

    public static Value operator &(Value a, Value b) => a.ToBoolean() && b.ToBoolean();
    public static Value operator |(Value a, Value b) => a.ToBoolean() || b.ToBoolean();

    // Overrides
    public override bool Equals(object obj)
    {
      if (obj is Value other)
      {
        return (bool)(this == other);
      }
      return false;
    }

    public override int GetHashCode()
    {
      return numberValue.GetHashCode();
    }

    public override string ToString()
    {
      return numberValue.ToString();
    }
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

  public class TriggerEvaluator
  {

    string input;
    int pos;
    string token;
    TokenKind tokenKind;

    Stack<Value> evalStack;
    Dictionary<String, Value> triggers;
    Dictionary<String, Func<Value, Value>> handlers;

    public TriggerEvaluator(Dictionary<String, Value> triggers, Dictionary<String, Func<Value, Value>> handlers = null)
    {
      this.triggers = triggers;
      this.handlers = handlers ?? new Dictionary<string, Func<Value, Value>>();
    }
    public Value PeekResult()
    {
      if (evalStack == null || evalStack.Count != 1)
        return null;
      return evalStack.Peek();
    }

    enum TokenKind
    {
      EOF,
      NAME,
      OPERATOR,
      NUMBER,
    }
    public void Eval(string expression)
    {
      input = expression;
      pos = 0;
      token = null;
      evalStack = new Stack<Value>();
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

      if (char.IsDigit(ch) || (ch == '.' && pos + 1 < input.Length && char.IsDigit(input[pos + 1])))
      {
        int start = pos;
        while (pos < input.Length && (char.IsDigit(input[pos]) || input[pos] == '.'))
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
        case "*": case "/": case "%": return (5, 6);
        case ":": return (9, 10);
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
          evalStack.Push(double.Parse(token, System.Globalization.CultureInfo.InvariantCulture));
          break;
        case TokenKind.NAME:
          if (token == "true")
            evalStack.Push(true);
          else if (token == "false")
            evalStack.Push(false);
          else
          {
            if (!triggers.ContainsKey(token))
              throw new TriggerEvalExn($"Unable to find trigger {token}");

            var value = triggers[token];
            evalStack.Push(value);
          }
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
              Value r;
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
          break;

        var (leftPrecedence, rightPrecedence) = GetInfixPrecedence(peekedToken);

        if (leftPrecedence < minAff)
          break;

        NextToken(); // Consume operator
        string op = token;

        Value l, r;

        if (op == ":")
        {
          NextToken();
          if (tokenKind != TokenKind.NAME)
            throw new TriggerEvalExn("Expected a method name after ':'");
          string methodName = token;

          if (!evalStack.TryPop(out l)) throw new TriggerEvalExn("Lack of left operand for ':'");

          if (handlers == null || !handlers.ContainsKey(methodName))
            throw new TriggerEvalExn($"Unknown method: {methodName}");

          var handler = handlers[methodName];
          var result = handler(l);
          evalStack.Push(result);
          continue;
        }

        ParseWithAff(rightPrecedence);

        if (!evalStack.TryPop(out r)) throw new TriggerEvalExn("Lack of right operand");
        if (!evalStack.TryPop(out l)) throw new TriggerEvalExn("Lack of left operand");

        Value ans = op switch
        {
          "+" => l + r,
          "-" => l - r,
          "*" => l * r,
          "/" => l / r,
          "%" => l % r,
          "**" => Math.Pow((double)l, (double)r),
          "==" => l == r,
          "!=" => l != r,
          "<" => l < r,
          ">" => l > r,
          "<=" => l <= r,
          ">=" => l >= r,
          "&&" => l && r,
          "||" => l || r,
          _ => throw new TriggerEvalExn($"Unsupported operator {op}")
        };
        evalStack.Push(ans);
      }
    }
  }
}