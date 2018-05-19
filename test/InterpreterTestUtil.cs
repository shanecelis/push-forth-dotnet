using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SeawispHunter.PushForth;

namespace SeawispHunter.PushForth {

public class InterpreterTestUtil {
  protected Interpreter interpreter;
  protected NonstrictInterpreter nonstrictInterpreter;
  protected StrictInterpreter strictInterpreter;
  protected ReorderInterpreter reorderInterpreter;
  protected Interpreter cleanInterpreter;
  protected TypeInterpreter typeInterpreter;
  protected Stack lastRun;
  protected Stack lastEval;
  public InterpreterTestUtil() {
    // Maybe I should do these lazily.
    nonstrictInterpreter = new NonstrictInterpreter();
    strictInterpreter = new StrictInterpreter();
    reorderInterpreter = new ReorderInterpreter();
    cleanInterpreter = new Interpreter();
    typeInterpreter = new TypeInterpreter();
    cleanInterpreter.instructions.Clear();
    interpreter = nonstrictInterpreter;

    // With InstructionFunc you have to do all your own error handling.
    interpreter.instructions["add"]
      = strictInterpreter.instructions["add"]
      = new InstructionFunc(stack => {
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
  }
  public string Run(string code) {
    var d0 = code.ToStack();
    var d1 = lastRun = interpreter.Run(d0);
    // Assert.Equal(Interpreter.ParseString("[[[1 +] [[1 +] while] i] 0]"), d1);
    return interpreter.StackToString(d1);
  }

  public string Reorder(string code) {
    var d0 = code.ToStack();
    var d1 = reorderInterpreter.Reorder(d0);
    // Assert.Equal(Interpreter.ParseString("[[[1 +] [[1 +] while] i] 0]"), d1);
    return interpreter.StackToString(d1);
  }

  public string Eval(string code) {
    var d0 = code.ToStack();
    var d1 = lastEval = interpreter.Eval(d0);
    // Assert.Equal(Interpreter.ParseString("[[[1 +] [[1 +] while] i] 0]"), d1);
    return interpreter.StackToString(d1);
  }

  public IEnumerable<string> EvalStream(string code) {
    var d0 = code.ToStack();
    return interpreter.EvalStream(d0).Select(x => interpreter.StackToString(x));
  }

  public bool IsHalted(string program) {
    var d0 = program.ToStack();
    var d1 = Interpreter.IsHalted(d0);
    return d1;
  }
}
}
