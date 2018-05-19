using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SeawispHunter.PushForth;

namespace SeawispHunter.PushForth {

public class TypeInterpreterTests : InterpreterTestUtil {

  [Fact]
  public void TestDetermineInputAndOutput() {
    UniqueVariable.Clear();
    interpreter = reorderInterpreter;
    var s = "[[if typeof(int)]]";
    var _if
      = new DetermineTypesInstruction("[bool 'a 'a]",
                                      "['a]")
    { getType = o => o is Type t ? t : o.GetType() };
    interpreter.instructions["if"] = _if;

    Assert.Equal("[[] int 'a0 ['a0 'a0 bool]]", Run(s));
  }

  [Fact]
  public void TestDetermineInputAndOutput2() {
    UniqueVariable.Clear();
    var s = "[[if typeof(int) + typeof(char)]]".ToStack();
    var _if
      = new DetermineTypesInstruction("[bool 'a 'a]",
                                  "['a]")
      { getType = o => o is Type t ? t : o.GetType() };
    var add
      = new DetermineTypesInstruction("[int int]",
                                  "[int]")
      { getType = o => o is Type t ? t : o.GetType() };
    interpreter.instructions["if"] = _if;
    interpreter.instructions["+"] = add;

    var s2 = interpreter.Run(s);
    Assert.Equal("[[] char int { a0 -> int } ['a0 'a0 bool]]", s2.ToRepr());
    s2.Pop();
    var (consumes, produces) = TypeInterpreter.ConsumesAndProduces(s2);
    Assert.Equal("[bool int int]", consumes.ToRepr());
    Assert.Equal("[int char]", produces.ToRepr());
  }

  [Fact]
  public void TestDetermineInputAndOutput3() {
    UniqueVariable.Clear();
    interpreter = typeInterpreter;
    var s = "[[if typeof(int) + typeof(char)]]".ToStack();
    var _if
      = new DetermineTypesInstruction("[bool 'a 'a]",
                                      "['a]")
      { getType = o => o is Type t ? t : o.GetType() };
    var add
      = new DetermineTypesInstruction("[int int]",
                                      "[int]")
      { getType = o => o is Type t ? t : o.GetType() };
    interpreter.instructions["if"] = _if;
    interpreter.instructions["+"] = add;

    var s2 = interpreter.Run(s);
    Assert.Equal("[[] char int { a0 -> int } ['a0 'a0 bool]]", s2.ToRepr());
    s2.Pop();
    var (consumes, produces) = TypeInterpreter.ConsumesAndProduces(s2);
    Assert.Equal("[bool int int]", consumes.ToRepr());
    Assert.Equal("[int char]", produces.ToRepr());
  }

  [Fact]
  public void TestTypeCheckDup() {
    interpreter = cleanInterpreter;
    UniqueVariable.Clear();
    var s = "[[typeof(int) dup]]";
    var dup
      = new DetermineTypesInstruction("['a]",
                                      "['a 'a]")
      { getType = o => o is Type t ? t : o.GetType() };
    interpreter.instructions["dup"] = dup;
    Assert.Equal("[[] 'a0 'a0 { a0 -> int }]", Run(s));
    var s2 = lastRun;
    s2.Pop();
    var (consumes, produces) = TypeInterpreter.ConsumesAndProduces(s2);
    Assert.Equal("[]", consumes.ToRepr());
    Assert.Equal("[int int]", produces.ToRepr());
  }

  [Fact]
  public void TestTypeCheckDupInterp() {
    interpreter = typeInterpreter;
    UniqueVariable.Clear();
    var s = "[[typeof(int) dup]]";
    // var dup
    //   = new DetermineTypesInstruction("['a]",
    //                                   "['a 'a]")
    //   { getType = o => o is Type t ? t : o.GetType() };
    // interpreter.instructions["dup"] = dup;
    Assert.Equal("[[] 'a0 'a0 { a0 -> int }]", Run(s));
    var s2 = lastRun;
    s2.Pop();
    var (consumes, produces) = TypeInterpreter.ConsumesAndProduces(s2);
    Assert.Equal("[]", consumes.ToRepr());
    Assert.Equal("[int int]", produces.ToRepr());
  }

  [Fact]
  public void TestTypeCheckDupDup() {
    interpreter = cleanInterpreter;
    UniqueVariable.Clear();
    var s = "[[typeof(int) dup typeof(char) dup]]";
    var dup
      = new DetermineTypesInstruction("['a]",
                                      "['a 'a]")
      { getType = o => o is Type t ? t : o.GetType() };
    interpreter.instructions["dup"] = dup;
    Assert.Equal("[[] 'a1 'a1 { a1 -> char } 'a0 'a0 { a0 -> int }]", Run(s));
    var s2 = lastRun;
    s2.Pop();
    var (consumes, produces) = TypeInterpreter.ConsumesAndProduces(s2);
    Assert.Equal("[]", consumes.ToRepr());
    Assert.Equal("[int int char char]", produces.ToRepr());
  }

  [Fact]
  public void TestTypeCheckDupDupWithVars() {
    interpreter = cleanInterpreter;
    UniqueVariable.Clear();
    var s = "[[dup typeof(char) dup]]";
    var dup
      = new DetermineTypesInstruction("['a]",
                                      "['a 'a]")
      { getType = o => o is Type t ? t : o.GetType() };
    interpreter.instructions["dup"] = dup;
    Assert.Equal("[[] 'a1 'a1 { a1 -> char } 'a0 'a0 ['a0]]", Run(s));
    var s2 = lastRun;
    s2.Pop();
    var (consumes, produces) = TypeInterpreter.ConsumesAndProduces(s2);
    Assert.Equal("['a0]", consumes.ToRepr());
    Assert.Equal("['a0 'a0 char char]", produces.ToRepr());
  }

  [Fact]
  public void TestTypeCheckDupDupWithVars2() {
    interpreter = cleanInterpreter;
    UniqueVariable.Clear();
    var s = "[[dup typeof(char) dup]]";
    var dup
      = new DetermineTypesInstruction("['a]",
                                      "['a 'a]")
      { getType = o => o is Type t ? t : o.GetType() };
    interpreter.instructions["dup"] = dup;
    Assert.Equal("[[] 'a1 'a1 { a1 -> char } 'a0 'a0 ['a0]]", Run(s));
    var s2 = lastRun;
    s2.Pop();
    var (consumes, produces) = TypeInterpreter.ConsumesAndProduces(s2);
    Assert.Equal("['a0]", consumes.ToRepr());
    Assert.Equal("['a0 'a0 char char]", produces.ToRepr());
  }

  [Fact]
  public void TestTypeCheckDupDupWithVars3() {
    interpreter = typeInterpreter;
    UniqueVariable.Clear();
    var s = "[[dup typeof(char) dup]]";
    Assert.Equal("[[] 'a1 'a1 { a1 -> char } 'a0 'a0 ['a0]]", Run(s));
    var s2 = lastRun;
    s2.Pop();
    var (consumes, produces) = TypeInterpreter.ConsumesAndProduces(s2);
    Assert.Equal("['a0]", consumes.ToRepr());
    Assert.Equal("['a0 'a0 char char]", produces.ToRepr());
  }

  [Fact]
  public void TestSwap() {
    interpreter = typeInterpreter;
    UniqueVariable.Clear();
    var s = "[[typeof(int) typeof(char) swap]]";
    Assert.Equal("[[] 'a0 'b1 { a0 -> char, b1 -> int }]", Run(s));
    var s2 = lastRun;
    s2.Pop();
    var (consumes, produces) = TypeInterpreter.ConsumesAndProduces(s2);
    Assert.Equal("[]", consumes.ToRepr());
    Assert.Equal("[int char]", produces.ToRepr());
  }

  [Fact]
  public void TestSwap2() {
    interpreter = typeInterpreter;
    UniqueVariable.Clear();
    var s = "[[swap]]";
    Assert.Equal("[[] 'a0 'b1 ['b1 'a0]]", Run(s));
    var s2 = lastRun;
    s2.Pop();
    var (consumes, produces) = TypeInterpreter.ConsumesAndProduces(s2);
    Assert.Equal("['a0 'b1]", consumes.ToRepr());
    Assert.Equal("['b1 'a0]", produces.ToRepr());
  }

  [Fact]
  public void TestNoSwap() {
    interpreter = typeInterpreter;
    UniqueVariable.Clear();
    var s = "[[typeof(int) typeof(char) ]]";
    Assert.Equal("[[] char int]", Run(s));
    var s2 = lastRun;
    s2.Pop();
    var (consumes, produces) = TypeInterpreter.ConsumesAndProduces(s2);
    Assert.Equal("[]", consumes.ToRepr());
    Assert.Equal("[int char]", produces.ToRepr());
  }
}

}
