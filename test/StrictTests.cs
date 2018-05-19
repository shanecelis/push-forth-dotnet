using System;
using System.Collections;
using Xunit;
using SeawispHunter.PushForth;

namespace test {

public class StrictTests : InterpreterTestUtil {

  public StrictTests() {
    interpreter = strictInterpreter;
  }

  [Fact]
  public void Test1()
  {
    var c = new Stack();
    c.Push(new Symbol("+"));
    c.Push(1);
    c.Push(2);
    var d0 = new Stack();
    d0.Push(c);
    Assert.Equal("[[2 1 +]]".ToStack(), d0);
    var c_f = new Stack();
    var d_f = new Stack();
    d_f.Push(3);
    d_f.Push(c_f);
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[1 +] 2]".ToStack(), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal("[[+] 1 2]".ToStack(), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal("[[] 3]".ToStack(), d3);
    Assert.Equal(d_f, d3);
  }

  [Fact]
  public void TestMissing()
  {
    var d0 = "[[+] 1]".ToStack();
    Assert.Throws<InvalidOperationException>(() => interpreter.Eval(d0));
  }

  [Fact]
  public void TestSkipping()
  {
    var code = new Stack();
    code.Push(new Symbol("a"));
    code.Push(new Symbol("add"));
    Assert.Equal("[add a]".ToStack(), code);

    var d0 = "[[2 1 a add]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[1 a add] 2]".ToStack(), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal("[[a add] 1 2]".ToStack(), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal("[[add] a 1 2]".ToStack(), d3);
    var d4 = interpreter.Eval(d3);
    Assert.Equal("[[add a] 1 2]".ToStack(), d4);
    var d5 = interpreter.Eval(d4);
    Assert.Equal("[[a] 3]".ToStack(), d5);
    var d6 = interpreter.Eval(d5);
    Assert.Equal("[[] a 3]".ToStack(), d6);
  }


  [Fact]
  public void TestBoolParsing()
  {
    var d0 = "[true false]".ToStack();
    var s = new Stack();
    s.Push(false);
    s.Push(true);
    Assert.Equal(s, d0);
  }
  [Fact]
  public void TestSkippingWithResolution()
  {
    var code = new Stack();
    code.Push(new Symbol("a"));
    code.Push(new Symbol("+"));
    Assert.Equal("[+ a]".ToStack(), code);

    var d0 = "[[2 1 a +]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[1 a +] 2]".ToStack(), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal("[[a +] 1 2]".ToStack(), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal("[[+] a 1 2]".ToStack(), d3);
    Assert.Throws<InvalidCastException>(() => interpreter.Eval(d3));
    // Assert.Equal(interpreter.ParseWithResolution("[[add a] 1 2]"), d4);
    // var d5 = interpreter.Eval(d4);
    // Assert.Equal("[[a] 3]".ToStack(), d5);
    // var d6 = interpreter.Eval(d5);
    // Assert.Equal("[[] a 3]".ToStack(), d6);
  }

  [Fact]
  public void TestSkippingSecondArg()
  {
    var d0 = "[[2 a 1 add]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[a 1 add] 2]".ToStack(), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal("[[1 add] a 2]".ToStack(), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal("[[add] 1 a 2]".ToStack(), d3);
    var d4 = interpreter.Eval(d3);
    Assert.Equal("[[add a ] 1 2]".ToStack(), d4);
    var d5 = interpreter.Eval(d4);
    Assert.Equal("[[a] 3]".ToStack(), d5);
    var d6 = interpreter.Eval(d5);
    Assert.Equal("[[] a 3]".ToStack(), d6);
  }

  [Fact]
  public void TestSkippingSecondArgWithResolution()
  {
    var d0 = "[[2 a 1 +]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[a 1 +] 2]".ToStack(), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal("[[1 +] a 2]".ToStack(), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal("[[+] 1 a 2]".ToStack(), d3);
    Assert.Throws<InvalidCastException>(() => interpreter.Eval(d3));
    // Assert.Equal(interpreter.ParseWithResolution("[[add a ] 1 2]"), d4);
    // var d5 = interpreter.Eval(d4);
    // Assert.Equal("[[a] 3]".ToStack(), d5);
    // var d6 = interpreter.Eval(d5);
    // Assert.Equal("[[] a 3]".ToStack(), d6);
  }

  [Fact]
  public void TestArgumentOrder() {
    var d0 = "[[5 4 minus]]".ToStack();
    var d1 = interpreter.Eval(d0);
    var d2 = interpreter.Eval(d1);
    var d3 = interpreter.Eval(d2);
    // This is how Push3 does it.
    // (5 4 integer.-) => (1)
    // Not sure how I feel about it.  Feels weird.
    // I'm going to do it differently.
    Assert.Equal("[[] -1]".ToStack(), d3);
  }

  [Fact]
  public void TestAppend() {
    var a = "[1 2]".ToStack();
    var b = "[3 4]".ToStack();
    var c = "[1 2 3 4]".ToStack();
    Assert.Equal(c, Interpreter.Append(a, b));

    Assert.Equal(a, Interpreter.Append(a, new Stack()));
    Assert.Equal(a, Interpreter.Append(new Stack(), a));
  }

  [Fact]
  public void TestParsing() {
    var a = new Stack();
    Assert.Equal(a, "[]".ToStack());
    a.Push(new Symbol("hi"));
    Assert.Equal(a, "[hi]".ToStack());
    a.Push(new Symbol("bye"));
    Assert.Equal(a, "[bye hi]".ToStack());
    a.Push("string");
    Assert.Equal(a, "[\"string\" bye hi]".ToStack());
    Assert.Equal(a, "[\"string\"   bye   hi]".ToStack());
    Assert.Equal(a, "[  \"string\"   bye   hi]".ToStack());
    a.Push(1);
    Assert.Equal(a, "[1  \"string\"   bye   hi]".ToStack());
    a.Push(2f);
    Assert.Equal(a, "[2f 1  \"string\"   bye   hi]".ToStack());
    a.Push(3f);
    Assert.Equal(a, "[3.0f  2f 1  \"string\"   bye   hi]".ToStack());
    a.Push(4.0);
    Assert.Equal(a, "[4.0 3.0f  2f 1  \"string\"   bye   hi]".ToStack());
    a.Push(-4.0);
    Assert.Equal(a, "[-4.0 4.0 3.0f  2f 1  \"string\"   bye   hi]".ToStack());
  }

  [Fact]
  public void TestNestedParsing() {
    var a = new Stack();
    var b = new Stack();
    a.Push(b);
    Assert.Equal(a, "[[]]".ToStack());
    b.Push(1);
    Assert.Equal(a, "[[1]]".ToStack());
    a.Push(2);
    Assert.Equal(a, "[2[1]]".ToStack());
    Assert.Equal(a, "[2 [1]]".ToStack());
  }

  [Fact]
  public void TestDup() {
    var d0 = "[[dup] 5]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] 5 5]".ToStack(), d1);
  }

  [Fact]
  public void TestDupNoData() {
    var d0 = "[[dup]]".ToStack();
    Assert.Throws<InvalidOperationException>(() => interpreter.Eval(d0));
  }

  [Fact]
  public void TestSwap() {
    var d0 = "[[swap] 1 2]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] 2 1]".ToStack(), d1);
  }

  [Fact]
  public void TestSwapLessData() {
    var d0 = "[[swap] 2]".ToStack();
    Assert.Throws<InvalidOperationException>(() => interpreter.Eval(d0));
  }

  [Fact]
  public void TestICombinatorWithBadArgument() {
    var d0 = "[[i] 2]".ToStack();
    Assert.Throws<InvalidCastException>(() => interpreter.Eval(d0));
  }

  [Fact]
  public void TestICombinatorWithStack() {
    var d0 = "[[i] [1 2 +]]".ToStack();
    var d1 = interpreter.Eval(d0);
    // Assert.Equal("[[1 2 +]]".ToStack(), d1);
    Assert.Equal("[[1 2 +]]", interpreter.StackToString(d1));
    var d2 = interpreter.Eval(d1);
    Assert.NotEqual("[[] [1 2 +]]".ToStack(), d2);
    // Assert.Equal("[[] 3]".ToStack(), d2);
  }

  [Fact]
  public void TestCar() {
    var d0 = "[[car] [2]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] 2]".ToStack(), d1);
  }

  [Fact]
  public void TestCdr() {
    var d0 = "[[cdr] []]".ToStack();
    Assert.Throws<InvalidOperationException>(() => interpreter.Eval(d0));
  }

  [Fact]
  public void TestContinuationProblem() {
    var d0 = "[[pop] 1 [2 5]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] [2 5]]".ToStack(), d1);
  }

  [Fact]
  public void TestCat() {
    var d0 = "[[cat] 1 2]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] [1 2]]".ToStack(), d1);
  }

  [Fact]
  public void TestSplit() {
    var d0 = "[[split] [1 2]]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] 1 2]".ToStack(), d1);
  }

  [Fact]
  public void TestSplitMissingArg() {
    var d0 = "[[split] 0 [1 2]]]".ToStack();
    Assert.Throws<InvalidCastException>(() => interpreter.Eval(d0));
  }

  [Fact]
  public void TestUnit() {
    var d0 = "[[unit] 0 [1 2]]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] [0] [1 2]]".ToStack(), d1);
  }

  [Fact]
  public void TestCons() {
    var d0 = "[[cons] 0 [1 2]]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] [0 1 2]]".ToStack(), d1);
  }

  [Fact]
  public void TestStackToString() {
    var d0 = "[[cons] 0 [1 2]]]".ToStack();
    Assert.Equal("[[cons] 0 [1 2]]", interpreter.StackToString(d0));
    Assert.Equal("[[cons] 0 [1 2]]", interpreter.StackToString(d0));
  }

  [Fact]
  public void TestStackToString2() {
    var d0 = interpreter.ParseWithResolution("[[cons] 0 [1 2]]]");
    Assert.Equal("[[cons] 0 [1 2]]", interpreter.StackToString(d0));
    Assert.Equal("[[cons] 0 [1 2]]", interpreter.StackToString(d0));
  }

  // [Fact]
  // public void TestWhile() {
  //   var d0 = "[[while] [1 +] [[]] 0]".ToStack();
  //   var d1 = interpreter.Eval(d0);
  //   // Assert.Equal("[[[1 +] [[1 +] while] i] 0]".ToStack(), d1);
  //   Assert.Equal("[[1 + [[1 +] while] i] 0]", interpreter.StackToString(d1));
  //   var d2 = interpreter.Eval(d1);
  //   Assert.Equal("[[+ [[1 +] while] i] 1 0]", interpreter.StackToString(d2));
  //   var d3 = interpreter.Eval(d2);
  //   Assert.Equal("[[[[1 +] while] i] 1]", interpreter.StackToString(d3));
  //   var d4 = interpreter.Eval(d3);
  //   Assert.Equal("[[i] [[1 +] while] 1]", interpreter.StackToString(d4));
  //   var d5 = interpreter.Eval(d4);
  //   Assert.Equal("[[[1 +] while] 1]", interpreter.StackToString(d5));
  //   var d6 = interpreter.Eval(d5);
  //   Assert.Equal("[[while] [1 +] 1]", interpreter.StackToString(d6));
  //   var d7 = interpreter.Eval(d6);
  //   Assert.Equal("[[] [1 +] 1]", interpreter.StackToString(d7));
  // }

  [Fact]
  public void TestWhile2() {
    var d0 = "[[while2] [1 + dup 5 >] true 0]".ToStack();
    var d1 = interpreter.Run(d0);
    // Assert.Equal("[[[1 +] [[1 +] while] i] 0]".ToStack(), d1);
    Assert.Equal("[[] 5]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestWhile3() {
    var d0 = "[[while3] [1 + dup 5 >] true 0]".ToStack();
    var d1 = interpreter.Run(d0);
    // Assert.Equal("[[[1 +] [[1 +] while] i] 0]".ToStack(), d1);
    Assert.Equal("[[] 5]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestWhile3Post() {
    Assert.Equal("[[] 2 5]", Run("[[while3 2] [1 + dup 5 >] true 0]"));
    Assert.Throws<InvalidCastException>(() => Run("[[while3 2] [1 + dup 5 >] blah true 0]"));
  }

  [Fact]
  public void TestWhile2False() {
    var d0 = "[[while2] [1 + dup 5 >] false 0]".ToStack();
    var d1 = interpreter.Run(d0);
    // Assert.Equal("[[[1 +] [[1 +] while] i] 0]".ToStack(), d1);
    Assert.Equal("[[] 0]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestRun() {
    var d0 = "[[while] [cdr swap 1 + swap] [[]] 0]".ToStack();
    var d1 = interpreter.Run(d0);
    Assert.Equal("[[] 1]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestEval() {
    var d0 = "[[eval] [[+] 1 2]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] [[] 3]]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestParseTypeof() {
    var d0 = "[typeof(int)]".ToStack();
    Assert.Equal(typeof(int), d0.Peek());
  }
}
}
