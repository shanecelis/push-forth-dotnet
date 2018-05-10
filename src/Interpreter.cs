using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeawispHunter.PushForth {

public class Symbol : Tuple<string> {
  public Symbol(string s) : base(s) { }

  public string name => Item1;
  public override string ToString() => Item1;
}

public class Continuation : Tuple<Stack> {
  public Continuation(Stack s) : base(s) { }

  public Stack code => Item1;
  public override string ToString() {
    return "C" + code.ToRepr();
  }
}

// public class Cell : OneOfBase<Symbol, int, string> { }

public class Interpreter {

  public Dictionary<string, Instruction> instructions
    = new Dictionary<string, Instruction>();

  public Dictionary<string, Instruction> reorderInstructions
    = new Dictionary<string, Instruction>();
  public readonly bool isStrict;

  public Interpreter(bool isStrict = false) {
    this.isStrict = isStrict;
    AddInstruction("i", (Stack stack, Stack code) => {
        stack.Push(new Continuation(code));
      });
    AddInstruction("car", (Stack stack, Stack s) => {
        if (isStrict || s.Any())
          stack.Push(s.Pop());
      });
    AddInstruction("eval", (Stack stack) => {
        return Eval(stack);
      });
    AddInstruction("reorder-pre", (Stack program) => {
        return ReorderPre(program);
      });
    AddInstruction("reorder-post", (Stack program) => {
        return ReorderPost(program);
      });
    AddInstruction("!", (Stack stack, Symbol s, object x) => {
        AddInstruction(s.name, () => x);
      });
    // Needed this to track down a bug.
    AddInstruction("!int", (Stack stack, Symbol s, int x) => {
        AddInstruction(s.name, () => x);
      });
    AddInstruction("cdr",(Stack stack) => {
        if (isStrict || stack.Any())
          stack.Pop();
        return stack;
      });
    instructions["pop"] = new InstructionFunc(stack => {
        if (isStrict || stack.Any())
          stack.Pop();
      });
    // Damn, the reorder is more complicated than the thing in itself.
    reorderInstructions["pop"] = new InstructionFunc(stack => {
          var o = stack.Pop();
          var t = stack.Any() ? stack.Peek().GetType() : null;
          var s = new Stack();
          s.Push(new Symbol("pop"));
          s.Push(o);
          stack.Push(new Reorder(s, t));
      });
    instructions["dup"] = new InstructionFunc(stack => {
        if (isStrict || stack.Any())
          stack.Push(Duplicate(stack.Peek()));
      });
    instructions["swap"] = new InstructionFunc(stack => {
        if (isStrict || stack.Count >= 2) {
          var a = stack.Pop();
          var b = stack.Pop();
          stack.Push(a);
          stack.Push(b);
        }
      });
    AddInstruction("cons", (object a, Stack b) => Cons(a, b));
    AddInstruction("cat", (object a, object b) => {
        var s = new Stack();
        s.Push(b);
        s.Push(a);
        return s;
      });
    AddInstruction("split", (Stack stack, Stack s) => {
        stack = Append(s, stack);
      });
    AddInstruction("unit", (object a) => {
        var s = new Stack();
        s.Push(a);
        return s;
      });

    AddInstruction("minus", (int a, int b) => a - b);
    AddInstruction("-", (int a, int b) => a - b);
    AddInstruction("+", (int a, int b) => a + b);
    AddInstruction("negate", (int a) => -a);
    // With InstructionFunc you have to do all your own error handling.
    instructions["add"] = new InstructionFunc(stack => {
        if (stack.Count < 2)
          return stack;
        object a, b;
        a = stack.Pop();
        if (! (a is int)) {
          var code = new Stack();
          code.Push(a);
          code.Push(new Symbol("add"));
          stack.Push(new Continuation(code));
          return stack;
        }
        b = stack.Pop();
        if (! (b is int)) {
          var code = new Stack();
          code.Push(b);
          code.Push(new Symbol("add"));
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
    instructions["while"] = new TrinaryInstruction<Stack, Stack, object>((stack, x, z, y) => {
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

    AddInstruction("==", (int a, int b) => a == b);
    AddInstruction("<", (int a, int b) => a < b);
    AddInstruction(">", (int a, int b) => a > b);
    instructions["while2"] = new TrinaryInstruction<Stack, bool, object>((stack, x, z, y) => {
        if (! z) {
          stack.Push(y);
        } else {
          var code = new Stack();
          code.Push(instructions["i"]);
          // code.Push(new Symbol("i"));
          var subcode = new Stack();
          subcode.Push(instructions["while2"]);
          // subcode.Push(new Symbol("while"));
          subcode.Push(x);
          code.Push(subcode);
          code = Append(x, code);
          // code.Push(x);
          stack.Push(y);
          stack.Push(new Continuation(code));
        }
      });

    AddInstruction("while3", (Stack stack, Stack x, bool z) => {
        // Let's do it again but with no code re-writing to make it compilable.
        while (z) {
          // Must make a copy of the code x, as the Stack is destroyed when
          // it is run.

          // stack.Push(x);
          stack.Push(Append(x, new Stack()));
          stack = Run(stack);
          object code = stack.Pop(); // drop empty code stack.
          if (code is Stack s) {
            if (s.Any()) {
              if (isStrict)
                throw new Exception("Code stack not empty.");
              Console.WriteLine("Code stack had stuff in it.");
              break;
            }
          } else {
            Console.WriteLine("Got non-stack for code");
            break;
          }
          object Z = stack.Pop();
          if (Z is bool zb)
            z = zb;
          else {
            if (isStrict)
              throw new Exception("No boolean on top of stack for while.");
            Console.WriteLine("Got non-bool for z " + Z);
            z = false;
          }
          // z = (bool) stack.Pop();
        }
      });

    AddInstruction("while4", (Stack stack, Stack x) => {
        // Let's do it again but with no code re-writing to make it compilable.
        bool z;
        do {
          // Must make a copy of the code x, as the Stack is destroyed when
          // it is run.

          // stack.Push(x);
          // This is a shallow copy.
          stack.Push(Append(x, new Stack()));
          stack = Run(stack);
          object code = stack.Pop(); // drop empty code stack.
          if (code is Stack s) {
            if (s.Any()) {
              if (isStrict)
                throw new Exception("Code stack not empty.");
              Console.WriteLine("Code stack had stuff in it.");
              break;
            }
          } else {
            Console.WriteLine("Got non-stack for code");
            break;
          }
          object Z = stack.Pop();
          if (Z is bool zb)
            z = zb;
          else {
            if (isStrict)
              throw new Exception("No boolean on top of stack for while.");
            Console.WriteLine("Got non-bool for z " + Z);
            z = false;
          }
        } while (z);
      });
  }

  public void AddInstruction(string name, Instruction i) {
    instructions[name] = i;
  }

  public void AddInstruction(string name, Action action) {
    // These actions don't even take a stack.
    if (! isStrict)
      instructions[name] = new NullaryInstruction(_ => action());
    else
      instructions[name] = new StrictNullaryInstruction(_ => action());
  }

  public void AddInstruction(string name, Action<Stack> func) {
    if (! isStrict)
      instructions[name] = new NullaryInstruction(func);
    else
      instructions[name] = new StrictNullaryInstruction(func);
  }

  public void AddInstruction<X>(string name, Func<X> func) {
    if (! isStrict)
      instructions[name] = NullaryInstruction.WithResult(func);
    else
      instructions[name] = StrictNullaryInstruction.WithResult(func);
    // reorderInstructions[name] = NullaryInstruction.Reorder<X>(name);

    reorderInstructions[name] = new ReorderInstruction(name,
                                                       new Type[] { },
                                                       new [] { typeof(X) });
  }

  public void AddInstruction<X>(string name, Action<Stack,X> func) {
    if (! isStrict)
      instructions[name] = new UnaryInstruction<X>(func);
    else
      instructions[name] = new StrictUnaryInstruction<X>(func);
  }

  public void AddInstruction<X,Y>(string name, Func<X,Y> func) {
    if (! isStrict)
      instructions[name] = UnaryInstruction<X>.WithResult(func);
    else
      instructions[name] = StrictUnaryInstruction<X>.WithResult(func);
    reorderInstructions[name] = new ReorderInstruction(name,
                                                       new [] { typeof(X) },
                                                       new [] { typeof(Y) });
  }

  public void AddInstruction<X,Y,Z>(string name, Func<X,Y,Z> func) {
    if (! isStrict)
      instructions[name] = BinaryInstruction<X,Y>.WithResult(func);
    else
      instructions[name] = StrictBinaryInstruction<X,Y>.WithResult(func);

    reorderInstructions[name] = new ReorderInstruction(name,
                                                       new [] { typeof(X), typeof(Y) },
                                                       new [] { typeof(Z) });
    // reorderInstructions[name] = BinaryInstruction<X,Y>.Reorder(name, typeof(Z));
  }

  public void AddInstruction<X,Y>(string name, Action<Stack,X,Y> func) {
    if (! isStrict)
      instructions[name] = new BinaryInstruction<X,Y>(func);
    else
      instructions[name] = new StrictBinaryInstruction<X,Y>(func);

    reorderInstructions[name] = new ReorderInstruction(name,
                                                       new [] { typeof(X), typeof(Y) },
                                                       new Type[] { });
    // reorderInstructions[name] = BinaryInstruction<X,Y>.Reorder(name, null);
  }

  public void AddInstruction<X,Y,Z,W>(string name, Func<X,Y,Z,W> func) {
    if (! isStrict)
      instructions[name] = TrinaryInstruction<X,Y,Z>.WithResult(func);
    else
      instructions[name] = StrictTrinaryInstruction<X,Y,Z>.WithResult(func);

    reorderInstructions[name] = new ReorderInstruction(name,
                                                       new [] { typeof(X), typeof(Y), typeof(Z) },
                                                       new [] { typeof(W) });
  }

  public void AddInstruction<X,Y,Z>(string name, Action<Stack,X,Y,Z> func) {
    if (! isStrict)
      instructions[name] = new TrinaryInstruction<X,Y,Z>(func);
    else
      instructions[name] = new StrictTrinaryInstruction<X,Y,Z>(func);

    reorderInstructions[name] = new ReorderInstruction(name,
                                                       new [] { typeof(X), typeof(Y), typeof(Z) },
                                                       new Type[] { });
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

  public static Stack Append(ICollection a, Stack b) {
    var e = a.GetEnumerator();
    while (e.MoveNext())
      b.Push(e.Current);
    // foreach(var x in a.GetEnumerator().ToEnumerable().Cast<object>().Reverse())
    //   b.Push(x);
    return b;
  }

  public static Stack ShallowCopy(Stack a) {
    return Append(a, new Stack());
    // How's this versus a.Clone()?
  }

  public static Stack ParseString(string s) {
    return StackParser.ParseStack(s);
  }

  public Stack ParseWithResolution(string s) {
    return StackParser.ParseWithResolution(s, instructions);
  }

  public Stack Reorder(Stack stack) {
    return RunReorderPost(RunReorderPre(stack));
  }

  public Stack ReorderPre(Stack stack) {
    return Eval(stack, new [] { reorderInstructions });
  }

  public bool IsReorderPostDone(Stack stack) {
    var code = (Stack) stack.Pop();
    var data = stack;
    bool done = ! data.Any();
    stack.Push(code);
    return done;
  }

  public Stack RunReorderPre(Stack stack) {
    return Run(stack, IsHalted, ReorderPre);
  }

  public Stack RunReorderPost(Stack stack) {
    return Run(stack, IsReorderPostDone, ReorderPost);
  }

  public Stack ReorderPost(Stack stack) {
    var code = (Stack) stack.Pop();
    var data = stack;
    if (data.Any()) {
      object o = data.Pop();
      if (o is Reorder r) {
        // Recurse.
        var s = new Stack(r.stack); // This reverses the stack.
        s.Push(new Stack());
        s = RunReorderPost(s);
        var newCode = (Stack) s.Pop();
        code = Append(newCode, code);
      } else if (o is Dummy d) {
        // We just drop it.
      } else {
        code = Cons(o, code);
      }
    }
    return Cons(code, data);
  }

  public IEnumerable<Stack> EvalStream(Stack stack) {
    while (! IsHalted(stack)) {
      stack = Eval(stack);
      yield return stack;
    }
  }

  public IEnumerable<Stack> EvalStream(Stack stack, Func<Stack, bool> isHalted, Func<Stack, Stack> eval) {
    while (! isHalted(stack)) {
      stack = eval(stack);
      yield return stack;
    }
  }

  public Stack Eval(Stack stack) {
    return Eval(stack, new [] { instructions });
  }

  public static Stack Eval(Stack stack, IEnumerable<Dictionary<string, Instruction>> instructionSets) {
    if (! stack.Any()) {
      // We add an empty stack which causes it to halt.
      stack.Push(new Stack());
      return stack; // halt
    }
    object first = stack.Pop();
    if (first is Stack code) {
      if (! code.Any()) {
        // The program is halted.
        return Cons(code, stack);
      }
      var data = stack;
      object obj = code.Pop();
      Instruction ins;
      if (obj is Symbol s) {
        foreach(var _instructions in instructionSets) {
          if (_instructions.TryGetValue(s.Item1, out ins)) {
            // Console.WriteLine("Got an instruction!");
            obj = ins;
            break;
          }
        }
      }
      if (obj is Instruction i) {
        ins = i;
        var result = ins.Apply(data);
        // Console.WriteLine("result " + string.Join(" ", result.ToArray()));
        if (! result.Any())
          return Cons(code, result);

        object ret = result.Peek();
        if (ret is Continuation continuation) {
          code = Append(continuation.code, code);
          result.Pop();
        }
        data = result;
      } else {
        data = Cons(obj, data);
      }
      return Cons(code, data);
    } else {
      // No code stack as first item, so we add an empty stack which causes it
      // to halt in the next eval.
      return Cons(new Stack(), Cons(first, stack));
    }
  }

  public static bool IsHalted(Stack s) {
    if (! s.Any())
      return false;
    var x = s.Peek();
    if (x is Stack code) {
      return ! code.Any();
    } else {
      return false;
    }
  }

  public Stack Run(Stack s, int maxSteps = -1) {
    int steps = 0;
    if (maxSteps < 0) {
      while (! IsHalted(s))
        s = Eval(s);
    } else {
      while (! IsHalted(s) && steps++ < maxSteps)
        s = Eval(s);
    }
    return s;
  }

  public Stack Run(Stack s,
                   Func<Stack, bool> isHalted,
                   Func<Stack, Stack> eval,
                   int maxSteps = -1) {
    int steps = 0;
    if (maxSteps < 0) {
      while (! isHalted(s))
        s = eval(s);
    } else {
      while (! isHalted(s) && steps++ < maxSteps)
        s = eval(s);
    }
    return s;
  }

  public string StackToString(Stack s, IEnumerable<Dictionary<string, Instruction>> instructionSets = null) {
    if (instructionSets == null)
      instructionSets = new [] { instructions };
    var sb = new StringBuilder();
    ToStringHelper(s, sb, instructionSets);
    return sb.ToString();
  }

  void ToStringHelper(Stack s, StringBuilder sb, IEnumerable<Dictionary<string, Instruction>> instructionSets) {
    sb.Append("[");
    var a = s.ToArray();
    Array.Reverse(a);
    s = new Stack(a);
    while (s.Any()) {
      object x = s.Pop();
      if (x is Stack substack)
        ToStringHelper(substack, sb, instructionSets);
      else if (x is Type t)
        sb.Append(t.PrettyName());
      else if (x is Instruction i)
        foreach(var _instructions in instructionSets) {
          var kv = _instructions.FirstOrDefault(_kv => _kv.Value == i);
          if (kv.Value == i) {
            sb.Append(kv.Key);
            break;
          }
        }
      else
        sb.Append(x.ToString());
      if (s.Any())
        sb.Append(" ");
    }
    sb.Append("]");
  }

  object Duplicate(object o) {
    if (o is Stack s) {
      return s.Clone();
    } else {
      return o;
    }
  }
}

}
