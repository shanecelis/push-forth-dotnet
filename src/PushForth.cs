using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OneOf;
using Sprache;

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

public class UnaryFunc<X, Y> : Instruction {
  Func<X, Y> func;
  public UnaryFunc(Func<X, Y> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 1)
      return stack;
    object a;
    a = stack.Pop();
    if (! (a is X)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(code);
      return stack;
    }
    stack.Push(func((X) a));
    return stack;
  }
}

public class BinaryFunc<X, Y, Z> : Instruction {
  Func<X, Y, Z> func;
  public BinaryFunc(Func<X, Y, Z> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 2)
      return stack;
    object a, b;
    b = stack.Pop();
    if (! (b is Y)) {
      var code = new Stack();
      code.Push(b);
      code.Push(this);
      stack.Push(code);
      return stack;
    }
    a = stack.Pop();
    if (! (a is X)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(b);
      stack.Push(code);
      return stack;
    }
    stack.Push(func((X) a, (Y) b));
    return stack;
  }
}

public class Symbol : Tuple<string> {
  public Symbol(string s) : base(s) { }

  public string name => Item1;
}

public class Cell : OneOfBase<Symbol, int, string> { }

public class Interpreter {

  public Dictionary<string, Instruction> instructions = new Dictionary<string, Instruction>();

  public Interpreter() {
    instructions["minus"] = new BinaryFunc<int, int, int>((a, b) => a - b);
    instructions["add"] = new BinaryFunc<int, int, int>((a, b) => a + b);
    instructions["+"] = new InstructionFunc(stack => {
        if (stack.Count < 2)
          return stack;
        object a, b;
        a = stack.Pop();
        if (! (a is int)) {
          var code = new Stack();
          code.Push(a);
          code.Push(new Symbol("+"));
          stack.Push(code);
          return stack;
        }
        b = stack.Pop();
        if (! (b is int)) {
          var code = new Stack();
          code.Push(b);
          code.Push(new Symbol("+"));
          stack.Push(a);
          stack.Push(code);
          return stack;
        }
        stack.Push((int) a + (int) b);
        return stack;
      });
  }

  public static Stack Cons(object o, Stack stack) {
    stack.Push(o);
    return stack;
  }

  public static Stack Append(Stack a, Stack b) {
    foreach(var x in a.ToArray().Reverse())
      b.Push(x);
    return b;
  }

  public static Stack ParseString(string s) {
    return StackParser.stackRep.Parse(s);
  }

  public Stack ParseWithResolution(string s) {
    return StackParser.ParseWithResolution(s, instructions);
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
        // Console.WriteLine("Got an instruction!");
        obj = ins;
      }
    }
    if (obj is Instruction) {
      ins = (Instruction) obj;
      var result = ins.Apply(data);
      // Console.WriteLine("result " + string.Join(" ", result.ToArray()));
      if (! result.Any())
        return Cons(code, result);

      object ret = result.Peek();
      if (! (ret is Stack))
        return Cons(code, result);
      else if (((Stack) ret).Any())
        code = Append((Stack)ret, code);
      result.Pop();
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

  public static Parser<object> Resolve<T>(this Parser<Symbol> parser, Dictionary<string, T> dict) {
    return parser.Select(s => {
        T obj;
        if (dict.TryGetValue(s.name, out obj))
          return (object) obj;
        else
          return (object) s;
      });
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
