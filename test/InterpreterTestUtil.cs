using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SeawispHunter.PushForth;

namespace SeawispHunter.PushForth {

public class InterpreterTestUtil {
  protected Interpreter interpreter;
  public InterpreterTestUtil() {
    interpreter = new Interpreter(false);
    UniqueVariable.Clear();
  }
  public string Run(string code) {
    var d0 = Interpreter.ParseString(code);
    var d1 = interpreter.Run(d0);
    // Assert.Equal(Interpreter.ParseString("[[[1 +] [[1 +] while] i] 0]"), d1);
    return interpreter.StackToString(d1);
  }

  public string Reorder(string code) {
    var d0 = Interpreter.ParseString(code);
    var d1 = interpreter.Reorder(d0);
    // Assert.Equal(Interpreter.ParseString("[[[1 +] [[1 +] while] i] 0]"), d1);
    return interpreter.StackToString(d1);
  }

  public string Eval(string code) {
    var d0 = Interpreter.ParseString(code);
    var d1 = interpreter.Eval(d0);
    // Assert.Equal(Interpreter.ParseString("[[[1 +] [[1 +] while] i] 0]"), d1);
    return interpreter.StackToString(d1);
  }

  public IEnumerable<string> EvalStream(string code) {
    var d0 = Interpreter.ParseString(code);
    return interpreter.EvalStream(d0).Select(x => interpreter.StackToString(x));
  }

  public bool IsHalted(string program) {
    var d0 = Interpreter.ParseString(program);
    var d1 = Interpreter.IsHalted(d0);
    return d1;
  }
}
}
