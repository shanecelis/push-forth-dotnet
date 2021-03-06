/* Original code Copyright (c) 2018 Shane Celis[1]
   Licensed under the MIT License[2]

   Original code posted here[3].

   This comment generated by code-cite[4].

   [1]: https://github.com/shanecelis
   [2]: https://opensource.org/licenses/MIT
   [3]: https://github.com/shanecelis/push-forth-dotnet/
   [4]: https://github.com/shanecelis/code-cite
*/

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using PushForth;

namespace PushForth {

public class ReorderWrapperTests : InterpreterTestUtil {

  new Interpreter strictInterpreter;
  new Interpreter nonstrictInterpreter;
  new Interpreter reorderInterpreter;
  public ReorderWrapperTests() {
    strictInterpreter = new Interpreter();
    strictInterpreter.instructions.Clear();
    strictInterpreter.instructions["+"] = StrictInstruction.factory.Binary((int a, int b) => a + b);

    nonstrictInterpreter = new Interpreter();
    nonstrictInterpreter.instructions.Clear();
    nonstrictInterpreter.instructions["+"] =
      new ReorderWrapper("+", StrictInstruction.factory.Binary((int a, int b) => a + b));

    reorderInterpreter = new Interpreter();
    reorderInterpreter.instructions.Clear();
    reorderInterpreter.instructions["+"] =
      new ReorderWrapper("+", new DeferInstruction("+", StrictInstruction.factory.Binary((int a, int b) => a + b)));
  }

  [Fact]
  public void TestStrictInstruction() {
    interpreter = strictInterpreter;
    Assert.Equal("[[] 10]", Run("[[2 3 5 + +]]"));
    Assert.Throws<InvalidCastException>(() => Run("[[2 3 a 5 + +]]"));
    Assert.Throws<InvalidOperationException>(() => Run("[[3 5 + +]]"));
  }

  [Fact]
  public void TestNonStrictInstruction() {
    interpreter = nonstrictInterpreter;
    Assert.Equal("[[] 10]", Run("[[2 3 5 + +]]"));
    Assert.Equal("[[] a 10]", Run("[[2 3 a 5 + +]]"));
    Assert.Equal("[[] 8]", Run("[[3 5 + +]]"));
  }

  [Fact]
  public void TestReorderInstruction() {
    interpreter = reorderInterpreter;
    Assert.Equal("[[] R<int>[2 R<int>[3 5 +] +]]", Run("[[2 3 5 + +]]"));
    Assert.Equal("[[2 3 5 + +]]", ReorderInterpreter.ReorderPost(lastRun).ToRepr());
    Assert.Equal("[[] a R<int>[2 R<int>[3 5 +] +]]", Run("[[2 3 a 5 + +]]"));
    Assert.Equal("[[2 3 5 + + a]]", ReorderInterpreter.RunReorderPost(lastRun).ToRepr());
    Assert.Equal("[[] R<int>[3 5 +]]", Run("[[3 5 + +]]"));
    Assert.Equal("[[3 5 +]]", ReorderInterpreter.RunReorderPost(lastRun).ToRepr());
  }

  // [Fact]
  // public void testLotsOfReordering() {
  //   Assert.Equal("[[] d c b a 10]", Run("[[2 a 3 b c 5 + d +]]"));

  //   Stack s1, s2;
  //   // Assert.Equal("[[] d c b a R[2 R[3 5 +] +]]", (s1 = interpreter.RunReorderPre("[[2 a 3 b c 5 + d +]]".ToStack())).ToRepr());
  //   Assert.Equal("[[] d c b a R<int>[2 R<int>[3 5 +] +]]", (s1 = interpreter.RunReorderPre("[[2 a 3 b c 5 + d +]]".ToStack())).ToRepr());

  //   Assert.Equal("[[2 3 5 + + a b c d]]", (s2 = interpreter.RunReorderPost(s1)).ToRepr());
  //   var strict = new Interpreter(true);
  //   Assert.Equal("[[] d c b a 10]", strict.Run(s2).ToRepr());
  // }
}
}
