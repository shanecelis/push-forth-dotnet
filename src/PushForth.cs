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

  public static Stack Cons(object o, Stack stack) {
    stack.Push(o);
    return stack;
  }

  public static Stack ParseString(string s) {
    return StackParser.stackRep.Parse(s);
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
