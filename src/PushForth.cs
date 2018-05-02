using System;
using System.Text;
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

  public InstructionFunc(Action<Stack> action)
    : this(stack => { action(stack); return stack; }) {
  }

  public Stack Apply(Stack stack) {
    return func(stack);
  }
}

public class NoResultException : Exception { }

public class NullaryFunc<X> : Instruction {
  Func<X> func;
  public NullaryFunc(Func<X> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    try {
      stack.Push(func());
    } catch(NoResultException) {
      // No-op.
    }
    return stack;
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
    try {
      stack.Push(func((X) a));
    } catch(NoResultException) {
      // No-op.
    }
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
    a = stack.Pop();
    if (! (a is X)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(new Continuation(code));
      return stack;
    }
    b = stack.Pop();
    if (! (b is Y)) {
      var code = new Stack();
      code.Push(b);
      code.Push(this);
      stack.Push(a);
      stack.Push(new Continuation(code));
      return stack;
    }
    try {
      stack.Push(func((X) a, (Y) b));
    } catch(NoResultException) {
      // No-op.
    }
    return stack;
  }
}

public class TrinaryFunc<X, Y, Z> : Instruction {
  Action<Stack, X, Y, Z> func;
  public static TrinaryFunc<X, Y, Z> Func<W>(Func<X, Y, Z, W> func) {
    return new TrinaryFunc<X, Y, Z>((stack, a, b, c) => {
        try {
          stack.Push(func(a, b, c));
        } catch(NoResultException) {
          // No-op.
        }
      });
  }
  public TrinaryFunc(Action<Stack, X, Y, Z> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 3)
      return stack;
    object a, b, c;
    a = stack.Pop();
    if (! (a is X)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(new Continuation(code));
      return stack;
    }
    b = stack.Pop();
    if (! (b is Y)) {
      var code = new Stack();
      code.Push(b);
      code.Push(this);
      stack.Push(a);
      stack.Push(new Continuation(code));
      return stack;
    }
    c = stack.Pop();
    if (! (c is Z)) {
      var code = new Stack();
      code.Push(c);
      code.Push(this);
      stack.Push(b);
      stack.Push(a);
      stack.Push(new Continuation(code));
      return stack;
    }
    func(stack, (X) a, (Y) b, (Z) c);
    return stack;
  }
}

public class Symbol : Tuple<string> {
  public Symbol(string s) : base(s) { }

  public string name => Item1;
  public override string ToString() => Item1;
}

public class Continuation : Tuple<Stack> {
  public Continuation(Stack s) : base(s) { }

  public Stack stack => Item1;
}

public class Cell : OneOfBase<Symbol, int, string> { }

public class Interpreter {

  public Dictionary<string, Instruction> instructions = new Dictionary<string, Instruction>();

  public Interpreter() {
    instructions["i"] = new InstructionFunc(stack => {
        if (stack.Any()) {
          var x = stack.Pop();
          Stack code;
          if (x is Stack s) {
            code = s;
          } else {
            // Bad argument.
            code = new Stack();
            code.Push(x);
            code.Push(instructions["i"]);
          }
          stack.Push(new Continuation(code));
        }
      });
    // instructions["i"] = new InstructionFunc(stack => {
    //     if (stack.Any()) {
    //       var code = new Stack();
    //       code.Push(stack.Pop());
    //       stack.Push(new Continuation(code));
    //     }
    //   });
    instructions["car"] = new UnaryFunc<Stack, object>(stack => {
        if (stack.Any())
          return stack.Pop();
        else
          throw new NoResultException();
      });
    instructions["eval"]= new UnaryFunc<Stack, Stack>(stack => {
        return Eval(stack);
      });

    // instructions["cdr"] = new InstructionFunc(stack => {
    //     if (stack.Count < 1)
    //       return stack;
    //     object a;
    //     a = stack.Pop();
    //     Stack code;
    //     if (! (a is Stack)) {
    //       code = new Stack();
    //       code.Push(a);
    //       code.Push(this);
    //       stack.Push(code);
    //       return stack;
    //     }
    //     var s = (Stack) a;
    //     s.Pop();
    //     // Add a dummy code stack.
    //     code = new Stack();
    //     stack.Push(s);
    //     stack.Push(code);
    //     return stack;
    //   });

    // This looks like it's returning a code continuation.
    instructions["cdr"] = new UnaryFunc<Stack, object>(stack => {
        if (stack.Any())
          stack.Pop();
        return stack;
      });
    instructions["pop"] = new InstructionFunc(stack => {
        if (stack.Any())
          stack.Pop();
      });
    instructions["dup"] = new InstructionFunc(stack => {
        if (stack.Any())
          stack.Push(stack.Peek());
      });
    instructions["swap"] = new InstructionFunc(stack =>
        {
          if (stack.Count >= 2) {
            var a = stack.Pop();
            var b = stack.Pop();
            stack.Push(a);
            stack.Push(b);
          }
        });
    instructions["cons"] = new BinaryFunc<object, Stack, Stack>((a, b) => Cons(a, b));
    instructions["cat"] = new BinaryFunc<object, object, Stack>((a, b) =>
        {
          var s = new Stack();
          s.Push(b);
          s.Push(a);
          return s;
        });
    instructions["split"] = new InstructionFunc(stack =>
        {
          if (stack.Any()) {
            object o = stack.Pop();
            if (o is Stack s) {
              return Append(s, stack);
            } else {
              var code = new Stack();
              code.Push(o);
              code.Push(instructions["split"]);
              stack.Push(new Continuation(code));
            }
          }
          return stack;
        });
    instructions["unit"] = new UnaryFunc<object, Stack>(a => {
        var s = new Stack();
        s.Push(a);
        return s;
      });

    instructions["minus"] = new BinaryFunc<int, int, int>((a, b) => a - b);
    instructions["add"] = new BinaryFunc<int, int, int>((a, b) => a + b);
    // InstructionFunc you have to do all your own error handling.
    instructions["+"] = new InstructionFunc(stack => {
        if (stack.Count < 2)
          return stack;
        object a, b;
        a = stack.Pop();
        if (! (a is int)) {
          var code = new Stack();
          code.Push(a);
          code.Push(new Symbol("+"));
          stack.Push(new Continuation(code));
          return stack;
        }
        b = stack.Pop();
        if (! (b is int)) {
          var code = new Stack();
          code.Push(b);
          code.Push(new Symbol("+"));
          stack.Push(a);
          stack.Push(new Continuation(code));
          return stack;
        }
        stack.Push((int) a + (int) b);
        return stack;
      });
    // instructions["while"] = new InstructionFunc(stack => {
    //     if (stack.Count >= 3) {
    //       object x = stack.Pop();
    //       object y = stack.Pop();
    //       object z = stack.Pop();
    //       if (z is Stack Z && ! Z.Any()) {
    //         stack.Push(y);
    //       } else {
    //         var code = new Stack();
    //         // code.Push(instructions["i"]);
    //         code.Push(new Symbol("i"));
    //         var subcode = new Stack();
    //         // subcode.Push(instructions["while"]);
    //         subcode.Push(new Symbol("while"));
    //         subcode.Push(x);
    //         code.Push(subcode);
    //         code.Push(x);
    //         stack.Push(new Continuation(code));
    //       }
    //     }
    //     return stack;
    //   });
    instructions["while"] = new TrinaryFunc<Stack, Stack, object>((stack, x, z, y) => {
        if (! z.Any()) {
          stack.Push(y);
        } else {
          var code = new Stack();
          code.Push(instructions["i"]);
          // code.Push(new Symbol("i"));
          var subcode = new Stack();
          subcode.Push(instructions["while"]);
          // subcode.Push(new Symbol("while"));
          subcode.Push(x);
          code.Push(subcode);
          code = Append(x, code);
          // code.Push(x);
          stack.Push(y);
          stack.Push(new Continuation(code));
        }
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
    if (first is Stack code) {
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
        if (! (ret is Continuation))
          return Cons(code, result);
        else
          code = Append(((Continuation) ret).stack, code);
        result.Pop();
        data = result;
      } else {
        data = Cons(obj, data);
      }
      return Cons(code, data);
    } else {
      return Cons(first, stack);
    }
  }

  public string StackToString(Stack s) {
    var sb = new StringBuilder();
    ToStringHelper(s, sb);
    return sb.ToString();
  }

  void ToStringHelper(Stack s, StringBuilder sb) {
    sb.Append("[");
    var a = s.ToArray();
    Array.Reverse(a);
    s = new Stack(a);
    while (s.Any()) {
      object x = s.Pop();
      if (x is Stack substack)
        ToStringHelper(substack, sb);
      else if (x is Instruction i)
        sb.Append(instructions.First(kv => kv.Value == i).Key);
      else
        sb.Append(x.ToString());
      if (s.Any())
        sb.Append(" ");
    }
    sb.Append("]");
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
