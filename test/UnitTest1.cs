using System;
using System.Collections;
using Xunit;
using SeawispHunter.PushForth;

namespace test
{
public class UnitTest1
{
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
    Assert.Equal(Interpreter.ParseString("[[+ a] 1 2]"), d4);
    var d5 = interpreter.Eval(d4);
    Assert.Equal(Interpreter.ParseString("[[a] 3]"), d5);
    var d6 = interpreter.Eval(d5);
    Assert.Equal(Interpreter.ParseString("[[] a 3]"), d6);
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
    Assert.Equal(interpreter.ParseWithResolution("[[add a] 1 2]"), d4);
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
    Assert.Equal(Interpreter.ParseString("[[+ a ] 1 2]"), d4);
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
    Assert.Equal(interpreter.ParseWithResolution("[[add a ] 1 2]"), d4);
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
    Assert.Equal(Interpreter.ParseString("[[] 1]"), d3);
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
}
}
