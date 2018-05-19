using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SeawispHunter.PushForth;

namespace SeawispHunter.PushForth {

public class InterpreterTests : InterpreterTestUtil {

  [Fact]
  public void TestStackQueueBehavior() {
    var s = new Stack(new [] {1, 2, 3});
    var q = new Stack(new [] {1, 2, 3});
    var w = new Stack(new [] {1, 2, 4});
    Assert.Equal(s, q);
    Assert.NotEqual(s, w);
    Assert.False(s == q);
    Assert.False(s.Equals(q));
    Assert.False(s.Equals(w));
    Assert.True(s != w);
    Assert.True(s.Contains(2));
    Assert.False(s.Contains(4));
    Assert.Equal("[3 2 1]", s.ToRepr());
    Assert.Equal(new object[] {3, 2, 1}, s.ToArray());
    Assert.Equal(new object[] {3, 2, 1}, s.GetEnumerator().ToEnumerable().Cast<object>().ToArray());
    var r = new Stack(s);
    Assert.Equal("[1 2 3]", r.ToRepr());
    var t = new Stack(new Queue(new [] {1, 2, 3}));
    Assert.Equal("[3 2 1]", t.ToRepr());
    var u = new Stack(new Stack(new [] {1, 2, 3}));
    Assert.Equal("[1 2 3]", u.ToRepr());
    var v = new Queue(new [] {1, 2, 3});
    Assert.Equal(new object[] {1, 2, 3}, v.ToArray());
    Assert.Equal(new object[] {1, 2, 3}, v.GetEnumerator().ToEnumerable().Cast<object>().ToArray());

    // Enumerating does not change the stack.
    foreach (var x in s)
      ;
    Assert.Equal("[3 2 1]", s.ToRepr());
  }

  [Fact]
  public void Test1() {
    var interpreter = new Interpreter();
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
    var interpreter = new Interpreter();
    var d0 = "[[+] 1]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] 1]".ToStack(), d1);
  }

  [Fact]
  public void TestSkipping()
  {
    var code = new Stack();
    code.Push(new Symbol("a"));
    code.Push(new Symbol("+"));
    Assert.Equal("[+ a]".ToStack(), code);

    var interpreter = new Interpreter();
    var d0 = "[[2 1 a +]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[1 a +] 2]".ToStack(), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal("[[a +] 1 2]".ToStack(), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal("[[+] a 1 2]".ToStack(), d3);
    var d4 = interpreter.Eval(d3);
    Assert.Equal(interpreter.ParseWithResolution("[[+ a] 1 2]"), d4);
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
    code.Push(new Symbol("add"));
    Assert.Equal("[add a]".ToStack(), code);

    var interpreter = new Interpreter();
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
  public void TestSkippingSecondArg()
  {
    var interpreter = new Interpreter();
    var d0 = "[[2 a 1 +]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[a 1 +] 2]".ToStack(), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal("[[1 +] a 2]".ToStack(), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal("[[+] 1 a 2]".ToStack(), d3);
    var d4 = interpreter.Eval(d3);
    Assert.Equal(interpreter.ParseWithResolution("[[+ a ] 1 2]"), d4);
    var d5 = interpreter.Eval(d4);
    Assert.Equal("[[a] 3]".ToStack(), d5);
    var d6 = interpreter.Eval(d5);
    Assert.Equal("[[] a 3]".ToStack(), d6);
  }

  [Fact]
  public void TestSkippingSecondArgWithResolution()
  {
    var interpreter = new Interpreter();
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
  public void TestArgumentOrder() {
    var interpreter = new Interpreter();
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

    b = "[3 4]".ToStack();
    Assert.Equal(c, Interpreter.Append(new Queue(new [] {2, 1}), b));

    b = "[3 4]".ToStack();
    Assert.Equal(c, Interpreter.Append(new [] {2, 1}, b));

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
    var interpreter = new Interpreter();
    var d0 = "[[dup] 5]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] 5 5]".ToStack(), d1);
  }

  [Fact]
  public void TestDupNoData() {
    var interpreter = new Interpreter();
    var d0 = "[[dup]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[]]".ToStack(), d1);
  }

  [Fact]
  public void TestSwap() {
    var interpreter = new Interpreter();
    var d0 = "[[swap] 1 2]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] 2 1]".ToStack(), d1);
  }

  [Fact]
  public void TestSwapLessData() {
    var interpreter = new Interpreter();
    var d0 = "[[swap] 2]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] 2]".ToStack(), d1);
  }

  [Fact]
  public void TestICombinatorWithBadArgument() {
    var interpreter = new Interpreter();
    var d0 = "[[i] 2]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal(interpreter.ParseWithResolution("[[i 2]]"), d1);
  }

  [Fact]
  public void TestICombinatorWithStack() {
    var interpreter = new Interpreter();
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
    var interpreter = new Interpreter();
    var d0 = "[[car] [2]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] 2]".ToStack(), d1);
    Assert.Equal("[[] 2 [2 3]]", Run("[[dup car] [2 3]]"));
    Assert.Equal("[[] [2] [[2] 3]]", Run("[[dup car] [[2] 3]]"));
  }

  [Fact]
  public void TestCdr() {
    var interpreter = new Interpreter();
    var d0 = "[[cdr] [2 5]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] [5]]".ToStack(), d1);
  }

  [Fact]
  public void TestContinuationProblem() {
    var interpreter = new Interpreter();
    var d0 = "[[pop] 1 [2 5]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] [2 5]]".ToStack(), d1);
  }

  [Fact]
  public void TestCat() {
    var interpreter = new Interpreter();
    var d0 = "[[cat] 1 2]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] [1 2]]".ToStack(), d1);
  }

  [Fact]
  public void TestSplit() {
    var interpreter = new Interpreter();
    var d0 = "[[split] [1 2]]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] 1 2]".ToStack(), d1);
  }

  [Fact]
  public void TestSplitMissingArg() {
    var interpreter = new Interpreter();
    var d0 = "[[split] 0 [1 2]]]".ToStack();
    var d1 = interpreter.Eval(d0);
    var d2 = interpreter.Eval(d1);
    var d3 = interpreter.Eval(d2);
    Assert.Equal("[[] 0 1 2]".ToStack(), d3);
  }

  [Fact]
  public void TestUnit() {
    var interpreter = new Interpreter();
    var d0 = "[[unit] 0 [1 2]]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] [0] [1 2]]".ToStack(), d1);
  }

  [Fact]
  public void TestCons() {
    var interpreter = new Interpreter();
    var d0 = "[[cons] 0 [1 2]]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] [0 1 2]]".ToStack(), d1);
  }

  [Fact]
  public void TestStackToString() {
    var interpreter = new Interpreter();
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
    var interpreter = new Interpreter();
    var d0 = "[[while2] [1 + dup 5 >] true 0]".ToStack();
    var d1 = interpreter.Run(d0);
    // Assert.Equal("[[[1 +] [[1 +] while] i] 0]".ToStack(), d1);
    Assert.Equal("[[] 5]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestWhile3() {
    var interpreter = new Interpreter();
    var d0 = "[[while3] [1 + dup 5 >] true 0]".ToStack();
    var d1 = interpreter.Run(d0);
    // Assert.Equal("[[[1 +] [[1 +] while] i] 0]".ToStack(), d1);
    Assert.Equal("[[] 5]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestWhile3Post() {
    Assert.Equal("[[] 2 5]", Run("[[while3 2] [1 + dup 5 >] true 0]"));
  }

  [Fact]
  public void TestWhile4Post() {
    Assert.Equal("[[] 2 5]", Run("[[while4 2] [1 + dup 5 >] 0]"));
  }

  [Fact]
  public void TestIf() {
    Assert.Equal("[[] 1]", Run("[[if] true [1] [0]]"));
    Assert.Equal("[[] 0]", Run("[[if] false [1] [0]]"));

    Assert.Equal("[[] 2]", Run("[[if] true [1 1 +] [0]]"));
    Assert.Equal("[[] 0]", Run("[[if] false [1] [0 0 +]]"));

    Assert.Equal("[[] 2]", Run("[[if] true [1 1 +] [0]]"));
    Assert.Equal("[[] 0]", Run("[[if] false [1] [0 0 +]]"));
  }

  [Fact]
  public void TestWhile2False() {
    var interpreter = new Interpreter();
    var d0 = "[[while2] [1 + dup 5 >] false 0]".ToStack();
    var d1 = interpreter.Run(d0);
    // Assert.Equal("[[[1 +] [[1 +] while] i] 0]".ToStack(), d1);
    Assert.Equal("[[] 0]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestRun() {
    interpreter = strictInterpreter;
    var d0 = "[[while] [cdr swap 1 + swap] [1 2] 0]".ToStack();
    var d1 = interpreter.Run(d0);
    Assert.Equal("[[] 2]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestWhileRun() {
    interpreter = strictInterpreter;
    var e2 = interpreter.EvalStream("[[while] [cdr swap 1 + swap] [1 2] 0]".ToStack()).Select(x => interpreter.StackToString(x)).GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[[cdr swap 1 + swap [[cdr swap 1 + swap] while] i] [1 2] 0]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.Equal("[[[[cdr swap 1 + swap] while] i] [2] 1]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[[i] [[cdr swap 1 + swap] while] [2] 1]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[[[cdr swap 1 + swap] while] [2] 1]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[[while] [cdr swap 1 + swap] [2] 1]", e2.Current);

    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.True(e2.MoveNext());
    Assert.Equal("[[while] [cdr swap 1 + swap] [] 2]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[[] 2]", e2.Current);
    Assert.False(e2.MoveNext());
  }

  [Fact]
  public void TestEval() {
    var d0 = "[[eval] [[+] 1 2]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[] [[] 3]]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestEvalRules() {
    Assert.Equal("[[] 1 2 3]", Eval("[1 2 3]"));
    Assert.Equal("[[]]", Eval("[]"));
  }

  [Fact]
  public void TestIsHalted() {
    Assert.False(IsHalted("[1 2 3]"));
    Assert.True(IsHalted("[[] 1 2 3]"));
    Assert.False(IsHalted("[]"));
    Assert.True(IsHalted("[[]]"));
  }

  [Fact]
  public void TestReorder() {
    interpreter = reorderInterpreter;
    var d0 = "[[2 a 1 +]]".ToStack();
    var d1 = interpreter.Eval(d0);
    Assert.Equal("[[a 1 +] 2]".ToStack(), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal("[[1 +] a 2]".ToStack(), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal("[[+] 1 a 2]".ToStack(), d3);
    var d4 = interpreter.Eval(d3);
    // Assert.Equal(interpreter.ParseWithResolution("[[+ a ] 1 2]"), d4);
    Assert.Equal("[[+ a] 1 2]", interpreter.StackToString(d4));
    // var d4b = "[[' reorder run] [[+ a ] 1 2] []]".ToStack();
    // var d4b = "[[reorder] [[+ a ] 1 2] []]".ToStack();
    // var d5 = interpreter.Eval(d4b);
    Assert.Equal("[[a] R<int>[2 1 +]]",
                 Eval("[[+ a ] 1 2]"));
    var e1 = interpreter.Eval("[[+ a ] 1 2]".ToStack());
    Assert.Equal("[[a] R<int>[2 1 +]]",
                 e1.ToRepr());
    var s = e1;
    Assert.Equal("[[a] R<int>[2 1 +]]",
                 s.ToRepr());
    var e2 = ReorderInterpreter.ReorderPost(s);
    Assert.Equal("[[2 1 + a]]",
                 e2.ToRepr());

    // R<T>[1,2,3] is not parsing properly.
    // Assert.Equal("[[] [[a] R<System.Int32> [2 1 +]]]",
    //              Eval("[[reorder-post] [[] a R<System.Int32> [2 1 +]]]"));

    // Assert.Equal("[[] [[a] R<System.Int32> [2 1 +]]]",
    //              Eval("[[reorder-post] [[a] R<System.Int32> [2 1 +]]]"));
    // var d6 = interpreter.Eval(d5);
    // Assert.Equal("[[2 1 +] [[] a]]".ToStack(), d6);
  }
  [Fact]
  public void TestPivotString() {
    var e2 = interpreter.EvalStream("[[1 1 +]]".ToStack()).Select(x => x.ToPivot()).GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ 1 • 1]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ • 1 1]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[• 2]", e2.Current);
    Assert.False(e2.MoveNext());
  }

  [Fact]
  public void TestParsePivot() {
    var s = "[[1 1 +]]".ToStack();
    Assert.Equal(s, StackParser.ParsePivot("[+ 1 1 •]"));
  }

  [Fact]
  public void TestTypeCheckWithReorder() {
    interpreter = reorderInterpreter;
    var s = "[[1 1 +]]".ToStack();
    var typedS = s.Map(o => o.GetType());
    Assert.Equal("[[int int Symbol]]", typedS.ToRepr());
    var typedS2 = s.Map(o => {
        if (o is Symbol)
          return o;
        else
          return o.GetType();
      });
    Assert.Equal("[[int int +]]", typedS2.ToRepr());
    interpreter.instructions["+"] = new ReorderInstruction("+",
                                                           new [] { typeof(int), typeof(int) },
                                                           new [] { typeof(int) })
      { getType = o => o is Type t ? t : o.GetType(),
        putType = t => t,
        leaveReorderItems = false };
    var e2 = interpreter
      .EvalStream(typedS2)
      .Select(x => x.ToPivot())
      .GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ int • int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ • int int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[• int]", e2.Current);
    Assert.False(e2.MoveNext());
  }

  [Fact]
  public void TestTypeCheckWithoutReorder() {
    interpreter = cleanInterpreter;
    var s = "[[1 1 +]]".ToStack();
    var typedS = s.Map(o => o.GetType());
    Assert.Equal("[[int int Symbol]]", typedS.ToRepr());
    var typedS2 = s.Map(o => {
        if (o is Symbol)
          return o;
        else
          return o.GetType();
      });
    Assert.Equal("[[int int +]]", typedS2.ToRepr());
    interpreter.instructions["+"] = new TypeCheckInstruction("+",
                                                             new [] { typeof(int), typeof(int) },
                                                             new [] { typeof(int) })
    { getType = o => o is Type t ? t : o.GetType(),
      putType = t => t,
      leaveReorderItems = false };
    var e2 = interpreter
      .EvalStream(typedS2)
      .Select(x => x.ToPivot())
      .GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ int • int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ • int int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[• int]", e2.Current);
    Assert.False(e2.MoveNext());
  }

  [Fact]
  public void TestTypeCheckWithoutReorder2() {
    interpreter = cleanInterpreter;
    var s = "[[1 1 +]]".ToStack();
    var typedS = s.Map(o => o.GetType());
    Assert.Equal("[[int int Symbol]]", typedS.ToRepr());
    var typedS2 = s.Map(o => {
        if (o is Symbol)
          return o;
        else
          return o.GetType();
      });
    Assert.Equal("[[int int +]]", typedS2.ToRepr());
    interpreter.instructions["+"] = new TypeCheckInstruction("+",
                                                             new [] { typeof(int), typeof(int) },
                                                             new [] { typeof(int) })
    { getType = o => o is Type t ? t : o.GetType(),
      putType = t => t,
      leaveReorderItems = false };
    var e2 = interpreter
      .EvalStream(typedS2)
      .Select(x => x.ToPivot())
      .GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ int • int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ • int int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[• int]", e2.Current);
    Assert.False(e2.MoveNext());
  }

  [Fact]
  public void TestTypeCheckNoReorderUnify() {
    interpreter = strictInterpreter;
    var s = "[[1 1 true if]]".ToStack();
    var typedS = s.Map(o => o.GetType());
    Assert.Equal("[[int int bool Symbol]]", typedS.ToRepr());
    var typedS2 = s.Map(o => o is Symbol ? o : o.GetType());
    Assert.Equal("[[int int bool if]]", typedS2.ToRepr());
    // var a = new Variable("a");
    interpreter.instructions["if"]
      = new TypeCheckInstruction2("if",
                                  "[bool 'a 'a]",
                                  "['a]")
                                  // new object[] { typeof(bool), a, a },
                                  // new TypeOrVariable[] { a })
    { getType = o => o is Type t ? t : o.GetType(),
      putType = t => t,
      leaveReorderItems = false };

    var e2 = interpreter
      .EvalStream(typedS2)
      .Select(x => x.ToPivot())
      .GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[if bool int • int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[if bool • int int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[if • bool int int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[• int]", e2.Current);
    Assert.False(e2.MoveNext());

    s = "[[1 'c' true if]]".ToStack();
    typedS2 = s.Map(o => o is Symbol ? o : o.GetType());
    e2 = interpreter
      .EvalStream(typedS2)
      .Select(x => x.ToPivot())
      .GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[if bool char • int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[if bool • char int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[if • bool char int]", e2.Current);
    Assert.Throws<Exception>(() => e2.MoveNext());
    // Assert.Equal("[• int]", e2.Current);
    // Assert.False(e2.MoveNext());
  }

  [Fact]
  public void TestTypeCheckRequiringReorder() {
    interpreter = strictInterpreter;
    var s = "[[1 1 a +]]".ToStack();
    var typedS = s.Map(o => o.GetType());
    Assert.Equal("[[int int Symbol Symbol]]", typedS.ToRepr());
    var typedS2 = s.Map(o => {
        if (o is Symbol)
          return o;
        else
          return o.GetType();
      });
    Assert.Equal("[[int int a +]]", typedS2.ToRepr());
    interpreter.instructions["+"] = new ReorderInstruction("+",
                                                           new [] { typeof(int), typeof(int) },
                                                           new [] { typeof(int) })
      { getType = o => o is Type t ? t : o.GetType(),
        putType = t => t,
        leaveReorderItems = false };
    var e2 = interpreter
      .EvalStream(typedS2)
      .Select(x => x.ToPivot())
      .GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ a int • int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ a • int int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ • a int int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[a + • int int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[a • int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[• a int]", e2.Current);
    Assert.False(e2.MoveNext());
  }

  [Fact]
  public void TestTypeCheckingWithIncorrectTypes() {
    interpreter = strictInterpreter;
    var s = "[[1 1 a +]]".ToStack();
    var typedS = s.Map(o => o.GetType());
    Assert.Equal("[[int int Symbol Symbol]]", typedS.ToRepr());

    var typedS3 = typedS.Map(o => o.GetType());
    Assert.Equal("[[RuntimeType RuntimeType RuntimeType RuntimeType]]", typedS3.ToRepr());
    var typedS2 = s.Map(o => {
        if (o is Symbol)
          return o;
        else
          return o.GetType();
      });
    Assert.Equal("[[int int a +]]", typedS2.ToRepr());
    interpreter.instructions["+"] = new TypeCheckInstruction("+",
                                                             new [] { typeof(int), typeof(int) },
                                                             new [] { typeof(int) })
    { getType = o => o is Type t ? t : o.GetType(),
      putType = t => t,
      leaveReorderItems = false };
    var e2 = interpreter
      .EvalStream(typedS2)
      .Select(x => x.ToPivot())
      .GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ a int • int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ a • int int]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[+ • a int int]", e2.Current);
    Assert.Throws<Exception>(() => e2.MoveNext());
  }

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
}

}
