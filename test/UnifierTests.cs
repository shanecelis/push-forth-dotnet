using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SeawispHunter.PushForth;

namespace test
{
public class UnifierTests
{
  [Fact]
  public void TestIsConstant()
  {
    // strings are its vars
    // Assert.False(Unifier.IsConstant("a"));
    // Assert.False(Unifier.IsConstant("[]".ToStack()));
    // Assert.True(Unifier.IsConstant(9));
    // Assert.False(Unifier.IsConstant(new Stack(new object[] { "b", "a" }).Cdr()));
    // Assert.False(Unifier.IsConstant(new Stack(new object[] { "b" }).Cdr()));
  }

  [Fact]
  public void TestSubsitute() {
    var d = new Dictionary<string, object>();
    d["a"] = 1;
    Assert.Equal(1, Unifier.Substitute(d, V("a")));
    Assert.Equal("b", Unifier.Substitute(d, "b"));
    Assert.Equal(new [] { 1 }, Unifier.Substitute(d, new object[] { V("a") }));
    Assert.Equal(new [] { "b" }, Unifier.Substitute(d, new object[] { "b" }));
    Assert.Equal(new object[] { "b", 1 }, Unifier.Substitute(d, new object[] { "b", V("a") }));
    Assert.True(Unifier.Substitute(d, new object[] { "b", V("a") }) is IEnumerable);
    Assert.True(new object[] { "b", V("a") } is IEnumerable);
    Assert.True(new Stack(new object[] { "b", V("a") }) is IEnumerable);
    Assert.True(new Stack(new object[] { "b", V("a") }).Cdr() is IEnumerable);
  }

  [Fact]
  public void TestOccurs() {
    Dictionary<string, object> dict;
    Assert.Equal("{ a -> 9 }", (dict = Unifier.Unify(V("a"), 9)).ToRepr());
    Assert.False(Occurs("a", new Stack()));
    Assert.True(Occurs("a", new Stack(new object[] { V("a") })));
    Assert.True(Occurs("a", new Stack(new object[] { 9, 3, V("a") })));
    Assert.True(Occurs("a", new Stack(new object[] { 9, 3, new Stack(new object[] { 1, V("a"), 2 }) })));

    Assert.False(Occurs("a", new Stack(new object[] { 9, 3, new Stack(new object[] { 1, 2 }) })));
    Assert.True(Occurs("a", new Stack(new object[] { 9, 3, new Stack(new object[] { 1, 2 }), V("a")})));
  }

  public bool Occurs(string s, object o) {
    return Unifier.OccurCheck(new Variable(s), o);
  }

  public Variable V(string s) => new Variable(s);

  // Convert symbols into variables.
  public Stack ToVarStack(string str) {
    var s = str.ToStack();

    // return new Stack(new Stack(s.Cast<object>().Select(x => (x is Symbol sym) ? V(sym.name) : x).ToArray()));
    return s.Map(x => (x is Symbol sym && sym.name.Length == 1) ? V(sym.name) : x);
  }

  [Fact]
  public void TestUnifier() {
    Assert.Equal("{ a -> 9 }", Unifier.Unify(V("a"), 9).ToRepr());
    Assert.Equal("{ a -> [9] }", Unifier.Unify(V("a"), new Stack(new [] { 9 })).ToRepr());
    Assert.Equal("{ a -> E(9) }", Unifier.Unify(V("a"), new Stack(new object[] { 9 }).Cast<object>().CastBack()).ToRepr());
    Assert.Equal("{ }", Unifier.Unify(9, 9).ToRepr());
    // Assert.Equal(new Stack(new object[] { V("x"), 2, 1 }), @"[1 2 ""x""]".ToStack());
    Assert.Equal("{ x -> 1 }", Unifier.Unify(new Stack(new [] { V("x") }), @"[1]".ToStack()).ToRepr());
    Assert.Equal("{ x -> 1 }", Unifier.Unify(@"[1]".ToStack(), new Stack(new [] { V("x") })).ToRepr());
    Assert.Equal("{ x -> 1 }", Unifier.Unify(@"[1]".ToStack(), ToVarStack(@"[x]")).ToRepr());
    Assert.Equal("{ x -> 1 }", Unifier.Unify(@"[1]".ToStack(), ToVarStack("[x]")).ToRepr());
    Assert.Equal("{ x -> 1 }", Unifier.Unify(@"[1 2]".ToStack(), ToVarStack("[x 2]")).ToRepr());
    Assert.Equal("{ x -> [1] }", Unifier.Unify(@"[[1] 2]".ToStack(), ToVarStack("[x 2]")).ToRepr());
    Assert.Equal("{ x -> 1 }", Unifier.Unify(@"[0 1 2]".ToStack(), ToVarStack("[0 x 2]")).ToRepr());
    Assert.Equal("{ x -> 1, y -> 2 }", Unifier.Unify(ToVarStack("[0 1 y]"), ToVarStack("[0 x 2]")).ToRepr());
    Assert.Equal("{ x -> 1, y -> 1 }", Unifier.Unify(ToVarStack("[0 1 y]"), ToVarStack("[0 x x]")).ToRepr());

    // No inconsistent values.
    Assert.Throws<Exception>(() => Unifier.Unify(@"[1 2]".ToStack(), ToVarStack("[x x]")).ToRepr());
    // No cyclic bindings.
    Assert.Throws<Exception>(() => Unifier.Unify(ToVarStack("[[1 x] 2]"), ToVarStack("[x 2]")).ToRepr());
    // Assert.Equal("{ x -> 1 }", Unifier.Unify(ToVarStack("[[1 x] 2]"), ToVarStack("[x 2]")).ToRepr());
  }

  [Fact]
  public void TestUnifierApplied() {
    var popInputs = ToVarStack("[a]");
    var popOutputs = ToVarStack("[]");
    var dupInputs = ToVarStack("[b]");
    var dupOutputs = ToVarStack("[b b]");
    var addInputs = ToVarStack("[int int]");
    var addOutputs = ToVarStack("[int]");
    var code = "[pop dup +]".ToStack();
    var codeInputs = ToVarStack("[x y]");
    var codeOutputs = ToVarStack("[int]");
    // binding y -> int, x -> ?
    var data = ToVarStack("[int int char]");
    // Assert.True(false);


    // Unifier.Unify(
  }
}
}
