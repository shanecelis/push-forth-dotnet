using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sprache;
using OneOf;

namespace SeawispHunter.PushForth {

public interface Instruction {
  Stack Apply(Stack stack);
}

public class InstructionFunc : Instruction {
  Func<Stack, Stack> func;
  public InstructionFunc(Func<Stack, Stack> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    return func(stack);
  }
}

public class Symbol : Tuple<string> {
  public Symbol(string s) : base(s) { }
}

public class Cell : OneOfBase<Symbol, int, string> { }

public class Interpreter {

  public Dictionary<string, Instruction> instructions = new Dictionary<string, Instruction>();

  public Interpreter() {
    instructions["+"] = new InstructionFunc(stack => {
        stack.Push((int) stack.Pop() + (int) stack.Pop());
        return stack;
      });
  }

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
    from trailingSpaces in Parse.Char(' ').Many()
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
    from num in Parse.Number
    from dot in Parse.Char('.')
    from frac in Parse.Number
    from trailingSpaces in Parse.Char(' ').Many()
    select double.Parse(num + "." + frac) * (op.IsDefined ? -1 : 1);

  // private static readonly Parser<Cell> cell =
  //   // quotedString.Or(bareWord).Or(integer.Select(i => i.ToString()));
  //   quotedString.ToCell().Or(symbol.ToCell());

  private static readonly Parser<object> cell =
    // quotedString.Or(bareWord).Or(integer.Select(i => i.ToString()));
    // quotedString.ToCell().Or(integer.ToCell()).Or(symbol.ToCell());
    quotedString.ToCell().Or(floatRep.ToCell()).Or(doubleRep.ToCell()).Or(integer.ToCell()).Or(symbol.ToCell());

  private static readonly Parser<Stack> stackRep =
    from lbracket in Parse.Char('[')
    from leadingSpaces in Parse.Char(' ').Many()
    from contents in cell.Or(Parse.Ref(() => stackRep)).Many()
    // from contents in cell.Many()
    from rbracket in Parse.Char(']')
    from trailingSpaces in Parse.Char(' ').Many()
    select new Stack(contents.Reverse().ToArray());

  public static Stack Cons(object o, Stack stack) {
    stack.Push(o);
    return stack;
  }

  public static Stack ParseString(string s) {
    return stackRep.Parse(s);
  }

  public Stack Eval(Stack stack) {
    if (! stack.Any())
      return stack; // halt
    object first = stack.Pop();
    Stack code = first as Stack;
    if (! code.Any())
      // Program is halted.
      return Cons(code, stack);
    var data = stack;
    object obj = code.Pop();
    Instruction ins;
    if (obj is Symbol s) {
      if (instructions.TryGetValue(s.Item1, out ins)) {
        Console.WriteLine("Got an instruction!");
        obj = ins;
      }
    }
    if (obj is Instruction) {
      ins = (Instruction) obj;
      var result = ins.Apply(data);
      Console.WriteLine("result " + string.Join(" ", result.ToArray()));
      if (! result.Any())
        return Cons(code, result);

      object ret = result.Peek();
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

  public static Parser<object> ToCell<T>(this Parser<T> parser) {
    return parser.Select(t => (object) t);
  }

  // public static Parser<Cell> ToCell(this Parser<string> parser) {
  //   return parser.Select(t => (Cell) t);
  // }

  // public static Parser<Cell> ToCell(this Parser<int> parser) {
  //   return parser.Select(t => (Cell) t);
  // }

  // public static Parser<Cell> ToCell(this Parser<Symbol> parser) {
  //   return parser.Select(t => (Cell) t);
  // }
}

}
