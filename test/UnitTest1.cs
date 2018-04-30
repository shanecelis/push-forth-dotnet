using System;
using System.Collections;
using Xunit;
using SeawispHunter.PushForth;

namespace test
{
public class UnitTest1
{
  // [Fact]
  public void Test1()
  {
    var c = new Stack();
    c.Push("+");
    c.Push(1);
    c.Push(2);
    var d = new Stack();
    d.Push(c);
    var c_f = new Stack();
    var d_f = new Stack();
    d_f.Push(3);
    d_f.Push(c_f);
    var d1 = Interpreter.Eval(d);
    var d2 = Interpreter.Eval(d1);
    var d3 = Interpreter.Eval(d2);

    Assert.Equal(Interpreter.ParseString("[[1 +] 2]"), d1);
    Assert.Equal(d_f, d3);
  }

  [Fact]
  public void TestParsing() {
    var a = new Stack();
    Assert.Equal(a, Interpreter.ParseString("[]"));
    a.Push("hi");
    Assert.Equal(a, Interpreter.ParseString("[hi]"));
    a.Push("bye");
    Assert.Equal(a, Interpreter.ParseString("[hi bye]"));
    Assert.Equal(a, Interpreter.ParseString("[hi bye]"));
  }
}
}
