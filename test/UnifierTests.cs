using System;
using System.Collections;
using Xunit;
using SeawispHunter.PushForth;

namespace test
{
public class UnifierTests
{
  Unifier unifier = new Unifier();
  [Fact]
  public void TestIsConstant()
  {
    // strings are its vars
    Assert.False(Unifier.IsConstant("a"));
    Assert.False(Unifier.IsConstant("[]".ToStack()));
    Assert.True(Unifier.IsConstant(9));
  }

  [Fact]
  public void TestUnifier() {
    Assert.Equal("{ a -> 9 }", Unifier.Unify("a", 9).ToRepr());
  }
}
}
