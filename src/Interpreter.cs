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

// public class ReorderInterpreter : Interpreter {
//   public ReorderInterpreter(Dictionary<string, Instruction> instructions) {
//     foreach(var kv in instructions) {
//       if (kv.Value is TypedInstruction ti) {
//         this.instructions.Add(kv.Key,
//                               new ReorderInstruction(kv.Key,
//                                                      ti.inputTypes,
//                                                      ti.outputTypes));
//       } else {
//         throw new Exception($"Can't make reorder instruction out of '{kv.Key}'.");
//       }

//     }
//   }
// }

public class Interpreter : StrictInterpreter {

  public Interpreter() {
    this.instructionFactory = ReorderWrapper.GetFactory(StrictInstruction.factory);
    isStrict = false;
  }
}

public class StrictInterpreter {

  public Dictionary<string, Instruction> instructions {
    get {
      if (_instructions == null) {
        _instructions = new Dictionary<string, Instruction>();
        LoadInstructions();
      }
      return _instructions;
    }
  }

  public Dictionary<string, Instruction> _instructions = null;
  // XXX a FuncFactory<TypedInstruction> can't convert to a TypedInstructionFactory but
  // it can go the other way.
  public FuncFactory<TypedInstruction> instructionFactory = StrictInstruction.factory;

  public StrictInterpreter() { }
  protected bool isStrict = true;

  public virtual void LoadInstructions() {

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
    AddInstruction("!", (Stack stack, Symbol s, object x) => {
        AddInstruction(s.name, () => x);
      });
    AddInstruction("if", (Stack stack, bool condition, Stack consequent, Stack otherwise)=> {
        if (condition)
          stack.Push(new Continuation(consequent));
        else
          stack.Push(new Continuation(otherwise));
      });
    // How can I get a generic function.  I want something like this:
    // AddInsuruction<T>("if2", (Stack stack, bool condition, T consequent, T otherwise)=> {
    //     if (condition)
    //       stack.Push(new Continuation(consequent));
    //     else
    //       stack.Push(new Continuation(otherwise));
    //   });

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
    AddInstruction("while", (Stack stack, Stack x, Stack z) => {
        if (z.Any()) {
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
          stack.Push(z);
          stack.Push(new Continuation(code));
        }
      });

    AddInstruction("==", (int a, int b) => a == b);
    AddInstruction("<", (int a, int b) => a < b);
    AddInstruction(">", (int a, int b) => a > b);
    AddInstruction("while2", (Stack stack, Stack x, bool z, object y) => {
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

  public virtual void AddInstruction(string name, Instruction i) {
    instructions[name] = i;
  }

  public void AddInstruction(string name, Action action) {
    AddInstruction(name, instructionFactory.Nullary(action));
  }

  public void AddInstruction(string name, Action<Stack> func) {
    AddInstruction(name, instructionFactory.Nullary(func));
  }

  public void AddInstruction<X>(string name, Func<X> func) {
    AddInstruction(name, instructionFactory.Nullary(func));
  }

  public void AddInstruction<X>(string name, Action<Stack,X> func) {
    AddInstruction(name, instructionFactory.Unary(func));
  }

  public void AddInstruction<X,Y>(string name, Func<X,Y> func) {
    AddInstruction(name, instructionFactory.Unary(func));
  }

  public void AddInstruction<X,Y,Z>(string name, Func<X,Y,Z> func) {
    AddInstruction(name, instructionFactory.Binary(func));
  }

  public void AddInstruction<X,Y>(string name, Action<Stack,X,Y> func) {
    AddInstruction(name, instructionFactory.Binary(func));
  }

  public void AddInstruction<X,Y,Z,W>(string name, Func<X,Y,Z,W> func) {
    AddInstruction(name, instructionFactory.Trinary(func));
  }

  public void AddInstruction<X,Y,Z>(string name, Action<Stack,X,Y,Z> func) {
    AddInstruction(name, instructionFactory.Trinary(func));
  }

  // XXX stack extensions?
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
    return b;
  }

  public static Stack ShallowCopy(Stack a) {
    return Append(a, new Stack());
    // How's this versus a.Clone()?
  }

  public Stack ParseWithResolution(string s) {
    return StackParser.ParseWithResolution(s, instructions);
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

  public static Stack Run(Stack s,
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
      else if (x is IDictionary d)
        sb.Append(d.ToRepr());
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
