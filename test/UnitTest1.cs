using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SeawispHunter.PushForth;

namespace SeawispHunter.PushForth 
{
public class UnitTest1
{

  Interpreter interpreter;
  public UnitTest1() {
    interpreter = new Interpreter(false);
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

  [Fact]
  public void Test1()
  {
    var interpreter = new Interpreter();
    var c = new Stack();
    c.Push(new Symbol("+"));
    c.Push(1);
    c.Push(2);
    var d0 = new Stack();
    d0.Push(c);
    Assert.Equal(Interpreter.ParseString("[[2 1 +]]"), d0);
    var c_f = new Stack();
    var d_f = new Stack();
    d_f.Push(3);
    d_f.Push(c_f);
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[1 +] 2]"), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal(Interpreter.ParseString("[[+] 1 2]"), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal(Interpreter.ParseString("[[] 3]"), d3);
    Assert.Equal(d_f, d3);
  }

  [Fact]
  public void TestMissing()
  {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[+] 1]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[] 1]"), d1);
  }

  [Fact]
  public void TestSkipping()
  {
    var code = new Stack();
    code.Push(new Symbol("a"));
    code.Push(new Symbol("+"));
    Assert.Equal(Interpreter.ParseString("[+ a]"), code);

    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[2 1 a +]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[1 a +] 2]"), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal(Interpreter.ParseString("[[a +] 1 2]"), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal(Interpreter.ParseString("[[+] a 1 2]"), d3);
    var d4 = interpreter.Eval(d3);
    Assert.Equal(interpreter.ParseWithResolution("[[+ a] 1 2]"), d4);
    var d5 = interpreter.Eval(d4);
    Assert.Equal(Interpreter.ParseString("[[a] 3]"), d5);
    var d6 = interpreter.Eval(d5);
    Assert.Equal(Interpreter.ParseString("[[] a 3]"), d6);
  }


  [Fact]
  public void TestBoolParsing()
  {
    var d0 = Interpreter.ParseString("[true false]");
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
    Assert.Equal(Interpreter.ParseString("[add a]"), code);

    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[2 1 a add]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[1 a add] 2]"), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal(Interpreter.ParseString("[[a add] 1 2]"), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal(Interpreter.ParseString("[[add] a 1 2]"), d3);
    var d4 = interpreter.Eval(d3);
    Assert.Equal(Interpreter.ParseString("[[add a] 1 2]"), d4);
    var d5 = interpreter.Eval(d4);
    Assert.Equal(Interpreter.ParseString("[[a] 3]"), d5);
    var d6 = interpreter.Eval(d5);
    Assert.Equal(Interpreter.ParseString("[[] a 3]"), d6);
  }

  [Fact]
  public void TestSkippingSecondArg()
  {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[2 a 1 +]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[a 1 +] 2]"), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal(Interpreter.ParseString("[[1 +] a 2]"), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal(Interpreter.ParseString("[[+] 1 a 2]"), d3);
    var d4 = interpreter.Eval(d3);
    Assert.Equal(interpreter.ParseWithResolution("[[+ a ] 1 2]"), d4);
    var d5 = interpreter.Eval(d4);
    Assert.Equal(Interpreter.ParseString("[[a] 3]"), d5);
    var d6 = interpreter.Eval(d5);
    Assert.Equal(Interpreter.ParseString("[[] a 3]"), d6);
  }

  [Fact]
  public void TestSkippingSecondArgWithResolution()
  {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[2 a 1 add]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[a 1 add] 2]"), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal(Interpreter.ParseString("[[1 add] a 2]"), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal(Interpreter.ParseString("[[add] 1 a 2]"), d3);
    var d4 = interpreter.Eval(d3);
    Assert.Equal(Interpreter.ParseString("[[add a ] 1 2]"), d4);
    var d5 = interpreter.Eval(d4);
    Assert.Equal(Interpreter.ParseString("[[a] 3]"), d5);
    var d6 = interpreter.Eval(d5);
    Assert.Equal(Interpreter.ParseString("[[] a 3]"), d6);
  }

  [Fact]
  public void TestArgumentOrder() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[5 4 minus]]");
    var d1 = interpreter.Eval(d0);
    var d2 = interpreter.Eval(d1);
    var d3 = interpreter.Eval(d2);
    // This is how Push3 does it.
    // (5 4 integer.-) => (1)
    // Not sure how I feel about it.  Feels weird.
    // I'm going to do it differently.
    Assert.Equal(Interpreter.ParseString("[[] -1]"), d3);
  }

  [Fact]
  public void TestAppend() {
    var a = Interpreter.ParseString("[1 2]");
    var b = Interpreter.ParseString("[3 4]");
    var c = Interpreter.ParseString("[1 2 3 4]");
    Assert.Equal(c, Interpreter.Append(a, b));

    Assert.Equal(a, Interpreter.Append(a, new Stack()));
    Assert.Equal(a, Interpreter.Append(new Stack(), a));
  }

  [Fact]
  public void TestParsing() {
    var a = new Stack();
    Assert.Equal(a, Interpreter.ParseString("[]"));
    a.Push(new Symbol("hi"));
    Assert.Equal(a, Interpreter.ParseString("[hi]"));
    a.Push(new Symbol("bye"));
    Assert.Equal(a, Interpreter.ParseString("[bye hi]"));
    a.Push("string");
    Assert.Equal(a, Interpreter.ParseString("[\"string\" bye hi]"));
    Assert.Equal(a, Interpreter.ParseString("[\"string\"   bye   hi]"));
    Assert.Equal(a, Interpreter.ParseString("[  \"string\"   bye   hi]"));
    a.Push(1);
    Assert.Equal(a, Interpreter.ParseString("[1  \"string\"   bye   hi]"));
    a.Push(2f);
    Assert.Equal(a, Interpreter.ParseString("[2f 1  \"string\"   bye   hi]"));
    a.Push(3f);
    Assert.Equal(a, Interpreter.ParseString("[3.0f  2f 1  \"string\"   bye   hi]"));
    a.Push(4.0);
    Assert.Equal(a, Interpreter.ParseString("[4.0 3.0f  2f 1  \"string\"   bye   hi]"));
    a.Push(-4.0);
    Assert.Equal(a, Interpreter.ParseString("[-4.0 4.0 3.0f  2f 1  \"string\"   bye   hi]"));
  }

  [Fact]
  public void TestNestedParsing() {
    var a = new Stack();
    var b = new Stack();
    a.Push(b);
    Assert.Equal(a, Interpreter.ParseString("[[]]"));
    b.Push(1);
    Assert.Equal(a, Interpreter.ParseString("[[1]]"));
    a.Push(2);
    Assert.Equal(a, Interpreter.ParseString("[2[1]]"));
    Assert.Equal(a, Interpreter.ParseString("[2 [1]]"));
  }

  [Fact]
  public void TestDup() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[dup] 5]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[] 5 5]"), d1);
  }

  [Fact]
  public void TestDupNoData() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[dup]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[]]"), d1);
  }

  [Fact]
  public void TestSwap() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[swap] 1 2]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[] 2 1]"), d1);
  }

  [Fact]
  public void TestSwapLessData() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[swap] 2]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[] 2]"), d1);
  }

  [Fact]
  public void TestICombinatorWithBadArgument() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[i] 2]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(interpreter.ParseWithResolution("[[i 2]]"), d1);
  }

  [Fact]
  public void TestICombinatorWithStack() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[i] [1 2 +]]");
    var d1 = interpreter.Eval(d0);
    // Assert.Equal(Interpreter.ParseString("[[1 2 +]]"), d1);
    Assert.Equal("[[1 2 +]]", interpreter.StackToString(d1));
    var d2 = interpreter.Eval(d1);
    Assert.NotEqual(Interpreter.ParseString("[[] [1 2 +]]"), d2);
    // Assert.Equal(Interpreter.ParseString("[[] 3]"), d2);
  }

  [Fact]
  public void TestCar() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[car] [2]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[] 2]"), d1);
    Assert.Equal("[[] 2 [2 3]]", Run("[[dup car] [2 3]]"));
    Assert.Equal("[[] [2] [[2] 3]]", Run("[[dup car] [[2] 3]]"));
  }

  [Fact]
  public void TestCdr() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[cdr] [2 5]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[] [5]]"), d1);
  }

  [Fact]
  public void TestContinuationProblem() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[pop] 1 [2 5]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[] [2 5]]"), d1);
  }

  [Fact]
  public void TestCat() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[cat] 1 2]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[] [1 2]]"), d1);
  }

  [Fact]
  public void TestSplit() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[split] [1 2]]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[] 1 2]"), d1);
  }

  [Fact]
  public void TestSplitMissingArg() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[split] 0 [1 2]]]");
    var d1 = interpreter.Eval(d0);
    var d2 = interpreter.Eval(d1);
    var d3 = interpreter.Eval(d2);
    Assert.Equal(Interpreter.ParseString("[[] 0 1 2]"), d3);
  }

  [Fact]
  public void TestUnit() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[unit] 0 [1 2]]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[] [0] [1 2]]"), d1);
  }

  [Fact]
  public void TestCons() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[cons] 0 [1 2]]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[] [0 1 2]]"), d1);
  }

  [Fact]
  public void TestStackToString() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[cons] 0 [1 2]]]");
    Assert.Equal("[[cons] 0 [1 2]]", interpreter.StackToString(d0));
    Assert.Equal("[[cons] 0 [1 2]]", interpreter.StackToString(d0));
  }

  [Fact]
  public void TestStackToString2() {
    var interpreter = new Interpreter();
    var d0 = interpreter.ParseWithResolution("[[cons] 0 [1 2]]]");
    Assert.Equal("[[cons] 0 [1 2]]", interpreter.StackToString(d0));
    Assert.Equal("[[cons] 0 [1 2]]", interpreter.StackToString(d0));
  }

  [Fact]
  public void TestWhile() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[while] [1 +] [[]] 0]");
    var d1 = interpreter.Eval(d0);
    // Assert.Equal(Interpreter.ParseString("[[[1 +] [[1 +] while] i] 0]"), d1);
    Assert.Equal("[[1 + [[1 +] while] i] 0]", interpreter.StackToString(d1));
    var d2 = interpreter.Eval(d1);
    Assert.Equal("[[+ [[1 +] while] i] 1 0]", interpreter.StackToString(d2));
    var d3 = interpreter.Eval(d2);
    Assert.Equal("[[[[1 +] while] i] 1]", interpreter.StackToString(d3));
    var d4 = interpreter.Eval(d3);
    Assert.Equal("[[i] [[1 +] while] 1]", interpreter.StackToString(d4));
    var d5 = interpreter.Eval(d4);
    Assert.Equal("[[[1 +] while] 1]", interpreter.StackToString(d5));
    var d6 = interpreter.Eval(d5);
    Assert.Equal("[[while] [1 +] 1]", interpreter.StackToString(d6));
    var d7 = interpreter.Eval(d6);
    Assert.Equal("[[] [1 +] 1]", interpreter.StackToString(d7));
  }

  [Fact]
  public void TestWhile2() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[while2] [1 + dup 5 >] true 0]");
    var d1 = interpreter.Run(d0);
    // Assert.Equal(Interpreter.ParseString("[[[1 +] [[1 +] while] i] 0]"), d1);
    Assert.Equal("[[] 5]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestWhile3() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[while3] [1 + dup 5 >] true 0]");
    var d1 = interpreter.Run(d0);
    // Assert.Equal(Interpreter.ParseString("[[[1 +] [[1 +] while] i] 0]"), d1);
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
  public void TestWhile2False() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[while2] [1 + dup 5 >] false 0]");
    var d1 = interpreter.Run(d0);
    // Assert.Equal(Interpreter.ParseString("[[[1 +] [[1 +] while] i] 0]"), d1);
    Assert.Equal("[[] 0]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestRun() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[while] [1 +] [[]] 0]");
    var d1 = interpreter.Run(d0);
    Assert.Equal("[[] [1 +] 1]", interpreter.StackToString(d1));
  }

  [Fact]
  public void TestEval() {
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[eval] [[+] 1 2]]");
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
    var interpreter = new Interpreter();
    var d0 = Interpreter.ParseString("[[2 a 1 +]]");
    var d1 = interpreter.Eval(d0);
    Assert.Equal(Interpreter.ParseString("[[a 1 +] 2]"), d1);
    var d2 = interpreter.Eval(d1);
    Assert.Equal(Interpreter.ParseString("[[1 +] a 2]"), d2);
    var d3 = interpreter.Eval(d2);
    Assert.Equal(Interpreter.ParseString("[[+] 1 a 2]"), d3);
    var d4 = interpreter.Eval(d3);
    Assert.Equal(interpreter.ParseWithResolution("[[+ a ] 1 2]"), d4);
    // var d4b = Interpreter.ParseString("[[' reorder run] [[+ a ] 1 2] []]");
    // var d4b = Interpreter.ParseString("[[reorder] [[+ a ] 1 2] []]");
    // var d5 = interpreter.Eval(d4b);
    Assert.Equal("[[] [[a] R<System.Int32>[2 1 +]]]",
                 Eval("[[reorder-pre] [[+ a ] 1 2]]"));
    var e1 = interpreter.Eval("[[reorder-pre] [[+ a ] 1 2]]".ToStack());
    Assert.Equal("[[] [[a] R<System.Int32>[2 1 +]]]",
                 e1.ToRepr());
    e1.Pop();
    var s = (Stack) e1.Pop();
    var e2 = interpreter.ReorderPost(s);

    Assert.Equal("[[2 1 + a]]",
                 e2.ToRepr());

    // R<T>[1,2,3] is not parsing properly.
    // Assert.Equal("[[] [[a] R<System.Int32> [2 1 +]]]",
    //              Eval("[[reorder-post] [[] a R<System.Int32> [2 1 +]]]"));

    // Assert.Equal("[[] [[a] R<System.Int32> [2 1 +]]]",
    //              Eval("[[reorder-post] [[a] R<System.Int32> [2 1 +]]]"));
    // var d6 = interpreter.Eval(d5);
    // Assert.Equal(Interpreter.ParseString("[[2 1 +] [[] a]]"), d6);
  }

  [Fact]
  public void testNestedReordering() {
    var s1 = "[[+] 1]".ToStack();
    var code = s1.Pop();
    s1.Push(new Reorder("[2 3 +]".ToStack(), typeof(int)));
    var i = interpreter.reorderInstructions["+"];
    s1 = i.Apply(s1);
    Assert.Equal("[R<System.Int32>[1 R<System.Int32>[2 3 +] +]]", s1.ToRepr());

    s1 = "[[+] 1]".ToStack();
    code = s1.Pop();
    s1.Push(new Reorder("[2 3 +]".ToStack(), typeof(int)));
    s1.Push(code);
    Assert.Equal("[[+] R<System.Int32>[2 3 +] 1]", s1.ToRepr());
    var s2 = interpreter.ReorderPre(s1);
    // Assert.Equal("", s2.ToRepr());
    Assert.Equal("[[] R<System.Int32>[1 R<System.Int32>[2 3 +] +]]", interpreter.StackToString(s2, new [] {interpreter.reorderInstructions, interpreter.instructions }));
  }

  [Fact]
  public void testLotsOfReordering() {
    Assert.Equal("[[] d c b a 10]", Run("[[2 a 3 b c 5 + d +]]"));

    Stack s1, s2;
    Assert.Equal("[[] d c b a R<System.Int32>[2 R<System.Int32>[3 5 +] +]]", (s1 = interpreter.RunReorderPre("[[2 a 3 b c 5 + d +]]".ToStack())).ToRepr());

    Assert.Equal("[[2 3 5 + + a b c d]]", (s2 = interpreter.RunReorderPost(s1)).ToRepr());
    var strict = new Interpreter(true);
    Assert.Equal("[[] d c b a 10]", strict.Run(s2).ToRepr());
  }

  [Fact]
  public void testMoreReordering() {
    var strict = new Interpreter(true);
    // The non-strict interpreter moves around arguments such that they will continue to work.
    Assert.Equal("[[] d c b a 6]", Run("[[2 a negate 3 b c 5 + d +]]"));
    Assert.Equal("[[2 negate 3 5 + + a b c d]]", interpreter.Reorder("[[2 a negate 3 b c 5 + d +]]".ToStack()).ToRepr());
    Assert.Equal("[[] d c b a 6]", strict.Run("[[2 negate 3 5 + + a b c d]]".ToStack()).ToRepr());
    // Can't run the original with the strict interpreter.
    Assert.Throws<InvalidCastException>(() => strict.Run("[[2 a negate 3 b c 5 + d +]]".ToStack()));
  }

  [Fact]
  public void testMoreReordering2() {
    var strict = new Interpreter(true);
    // The non-strict interpreter moves around arguments such that they will continue to work.
    Assert.Equal("[[] d c b a 6]", Run("[[2 a negate 3 b c 5 + d + +]]"));
    Assert.Equal("[[2 negate 3 5 + + a b c d]]", interpreter.Reorder("[[2 a negate 3 b c 5 + d + +]]".ToStack()).ToRepr());
    // The same output is produced.
    Assert.Equal("[[] d c b a 6]", strict.Run("[[2 negate 3 5 + + a b c d]]".ToStack()).ToRepr());
    // Can't run the original with the strict interpreter.
    Assert.Throws<InvalidCastException>(() => strict.Run("[[2 a negate 3 b c 5 + d + +]]".ToStack()));
    Assert.Throws<InvalidOperationException>(() => strict.Run("[[2 +]]".ToStack()));
    Assert.Throws<InvalidOperationException>(() => strict.Run("[[+]]".ToStack()));
  }

  [Fact]
  public void testMoreReordering3() {
    var e = EvalStream("[[2 a ! a]]").GetEnumerator();
    e.MoveNext();
    Assert.Equal("[[a ! a] 2]", e.Current);
    e.MoveNext();
    Assert.Equal("[[! a] a 2]", e.Current);
    e.MoveNext();
    Assert.Equal("[[a]]", e.Current);
    e.MoveNext();
    Assert.Equal("[[] 2]", e.Current);

    // The symbol a is set as an instruction. We need a new interpreter to not
    // be polluted by it.
    interpreter = new Interpreter();
    e = EvalStream("[[2 a 3 ! a]]").GetEnumerator();
    Assert.True(e.MoveNext());
    Assert.Equal("[[a 3 ! a] 2]", e.Current);
    e.MoveNext();
    Assert.Equal("[[3 ! a] a 2]", e.Current);
    e.MoveNext();
    Assert.Equal("[[! a] 3 a 2]", e.Current);
    e.MoveNext();
    Assert.Equal("[[! 3 a] a 2]", e.Current);
    e.MoveNext();
    Assert.Equal("[[3 a]]", e.Current);
    e.MoveNext();
    Assert.Equal("[[a] 3]", e.Current);
    e.MoveNext();
    Assert.Equal("[[] 2 3]", e.Current);
    Assert.False(e.MoveNext());

    interpreter = new Interpreter();
    var e2 = interpreter.EvalStream("[[! a] 3 a 2]".ToStack(), Interpreter.IsHalted, interpreter.ReorderPre)
      .Select(s => interpreter.StackToString(s, new [] {interpreter.reorderInstructions, interpreter.instructions}))
      .GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[[! 3 a] a 2]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[[3 a] R[2 a !]]", e2.Current);

    Assert.Equal("[[2 a ! 3 a]]", Reorder("[[2 a 3 ! a]]"));

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

}
}
