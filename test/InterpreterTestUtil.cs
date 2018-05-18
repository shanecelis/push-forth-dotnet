using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SeawispHunter.PushForth;

namespace SeawispHunter.PushForth {

public class InterpreterTestUtil {
  protected StrictInterpreter interpreter;
  protected Interpreter nonstrictInterpreter;
  protected StrictInterpreter strictInterpreter;
  protected ReorderInterpreter reorderInterpreter;
  protected StrictInterpreter cleanInterpreter;
  protected Stack lastRun;
  protected Stack lastEval;
  public InterpreterTestUtil() {
    // Maybe I should do these lazily.
    nonstrictInterpreter = new Interpreter();
    strictInterpreter = new StrictInterpreter();
    reorderInterpreter = new ReorderInterpreter();
    cleanInterpreter = new StrictInterpreter();
    cleanInterpreter.instructions.Clear();
    interpreter = nonstrictInterpreter;
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
