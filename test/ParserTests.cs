using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SeawispHunter.PushForth;
using Sprache;

namespace SeawispHunter.PushForth {

public class ParserTests {
  [Fact]
  public void TestParseTypeSignature() {
    var s = StackParser.ParseTypeSignature("[int]");
    // Assert.Equal(1, s.Count);
    Assert.Single(s);
    Assert.Equal(typeof(int), s.Pop());

    s = StackParser.ParseTypeSignature("['int]");
    Assert.Single(s);
    Assert.Equal(new Variable("int"), s.Pop());

    s = StackParser.ParseTypeSignature("[bool 'a 'a]");
    Assert.Equal(3, s.Count);
    Assert.Equal(typeof(bool), s.Pop());
    Assert.Equal(new Variable("a"), s.Pop());
    Assert.Equal(new Variable("a"), s.Pop());

    Assert.Throws<ParseException>(() => StackParser.ParseTypeSignature("[BOO 'a 'a]"));
  }

  [Fact]
  public void TestParseProgram() {
    var s = "[[1 1 +]]".ToStack();
    var t = @"[[stamper.stamp
      90 stamper.rotate-y
      stamper.push
      45 stamper.rotate-x
      0.7f stamper.scale*
      stamper.stamp
      stamper.pop
      ]]".ToStack();
    var code = (Stack) t.Pop();
    Assert.Equal(10, code.Count);
    Assert.Equal(new Symbol("stamper.stamp"), code.Pop());
    Assert.Equal(90, code.Pop());
    Assert.Equal(new Symbol("stamper.rotate-y"), code.Pop());
    Assert.Equal(new Symbol("stamper.push"), code.Pop());
  }

  [Fact]
  public void TestPrecedingSpace() {
    var s = "   []".ToStack();
  }
}
}
