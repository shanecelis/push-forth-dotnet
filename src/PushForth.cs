using System;
using System.Collections;
using System.Linq;
using Sprache;

namespace SeawispHunter.PushForth {

public interface Instruction {
  Stack Apply(Stack stack);
}

public class Symbol : Tuple<string> {
  public Symbol(string s) : base(s) { }
}

public class Interpreter {

  private static readonly Parser<char> _quotedText =
    Parse.AnyChar.Except(Parse.Char('"'));

  private static readonly Parser<char> escapedChar =
    from _ in Parse.Char('\\')
    from c in Parse.AnyChar
    select c;

  private static readonly Parser<string> quotedString =
    from open in Parse.Char('"')
    from text in escapedChar.Or(_quotedText).Many().Text()
    from close in Parse.Char('"')
    select text;

  public static readonly Parser<string> bareWord = Parse.CharExcept(" []").AtLeastOnce().Text().Token();

  public static readonly Parser<Symbol> symbol = bareWord.Select(s => new Symbol(s));

  // https://stackoverflow.com/questions/21414309/sprache-parse-signed-integer
  private static readonly Parser<int> integer =
    from op in Parse.Optional(Parse.Char('-').Token())
    from num in Parse.Number
    from trailingSpaces in Parse.Char(' ').Many()
    select int.Parse(num) * (op.IsDefined ? -1 : 1);

  private static readonly Parser<float> floatRep =
    from op in Parse.Optional(Parse.Char('-').Token())
    from num in Parse.Decimal
    from f in Parse.Char('f')
    from trailingSpaces in Parse.Char(' ').Many()
    select float.Parse(num) * (op.IsDefined ? -1 : 1);

  private static readonly Parser<double> doubleRep =
    from op in Parse.Optional(Parse.Char('-').Token())
    from num in Parse.Decimal
    from trailingSpaces in Parse.Char(' ').Many()
    select double.Parse(num) * (op.IsDefined ? -1 : 1);

  private static readonly Parser<IOneOf<symbol, string, integer>> cell =
    // quotedString.Or(bareWord).Or(integer.Select(i => i.ToString()));
    quotedString.Or(symbol);

  private static readonly Parser<Stack> stackRep =
    from lbracket in Parse.Char('[')
    // from contents in cell.Or(Parse.Ref(() => stackRep)).Many()
    from contents in cell.Many()
    from rbracket in Parse.Char(']')
    select new Stack(contents.ToArray());

  public static Stack Cons(object o, Stack stack) {
    stack.Push(o);
    return stack;
  }

  public static Stack ParseString(string s) {
    return stackRep.Parse(s);
    // return null;
  }

  public static Stack Eval(Stack stack) {
    if (! stack.Any())
      return stack; // halt
    object first = stack.Pop();
    Stack code = first as Stack;
    if (! code.Any())
      // Program is halted.
      return Cons(code, stack);
    var data = stack;
    object obj = code.Pop();
    if (obj is Instruction) {
      var ins = (Instruction) obj;
      var result = ins.Apply(data);
      if (! result.Any())
        return Cons(code, result);

      object ret = result.Pop();
      if (! (ret is Stack))
        return Cons(code, result);
      else if (((Stack) ret).Any())
        code.Push(ret);
      data = result;
    } else {
      data = Cons(obj, data);
    }
    return Cons(code, data);
  }
}

public static class PushForthExtensions {
  public static bool Any(this Stack s) {
    return s.Count != 0;
  }

}

}
