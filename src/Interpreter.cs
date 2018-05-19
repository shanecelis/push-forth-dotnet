﻿using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeawispHunter.PushForth {

/** Everything on the stack is what it is, but barewords are converted into
    symbols. */
public class Symbol : Tuple<string> {
  public Symbol(string s) : base(s) { }

  public string name => Item1;
  public override string ToString() => Item1;
}

/** Mark a stack as something that should be pushed back onto the code stack.
    This is push-forth's primary means of execution control. */
public class Continuation : Tuple<Stack> {
  public Continuation(Stack s) : base(s) { }

  public Stack code => Item1;
  public override string ToString() {
    return "C" + code.ToRepr();
  }
}

/**
   This is a bare bones interpreter.  It has no built-in instructions.

   Without instructions it's a very complicated way to move data from one stack
   to another.
 */
public class Interpreter {

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
  // XXX a FuncFactory<TypedInstruction> can't convert to a
  // TypedInstructionFactory but it can go the other way.
  public FuncFactory<TypedInstruction> instructionFactory = StrictInstruction.factory;

  public Interpreter() { }

  public virtual void LoadInstructions() {
    // Add all your instructions here.
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

  /** Parse a stack and convert symbols to instructions. */
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

  public static Stack Eval(Stack stack,
                           IEnumerable<Dictionary<string,
                                                  Instruction>> instructionSets) {
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

}

}
