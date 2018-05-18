
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SeawispHunter.PushForth;

namespace SeawispHunter.PushForth {

public class ReorderWrapperTests : InterpreterTestUtil {

  Interpreter strictInterpreter;
  Interpreter nonstrictInterpreter;
  Interpreter reorderInterpreter;
  public ReorderWrapperTests() {
    strictInterpreter = new Interpreter();
    strictInterpreter.instructions.Clear();
    strictInterpreter.instructions["+"] = StrictInstruction.Binary((int a, int b) => a + b);

    nonstrictInterpreter = new Interpreter();
    nonstrictInterpreter.instructions.Clear();
    nonstrictInterpreter.instructions["+"] =
      new ReorderWrapper("+", StrictInstruction.Binary((int a, int b) => a + b));

    reorderInterpreter = new Interpreter();
    reorderInterpreter.instructions.Clear();
    reorderInterpreter.instructions["+"] =
      new ReorderWrapper("+", new DeferInstruction("+", StrictInstruction.Binary((int a, int b) => a + b)));
  }

  [Fact]
  public void TestStrictInstruction() {
    interpreter = strictInterpreter;
    Assert.Equal("[[] 10]", Run("[[2 3 5 + +]]"));
    Assert.Throws<InvalidCastException>(() => Run("[[2 3 a 5 + +]]"));
    Assert.Throws<InvalidOperationException>(() => Run("[[3 5 + +]]"));
  }

  [Fact]
  public void TestReorderInstruction() {
    interpreter = nonstrictInterpreter;
    Assert.Equal("[[] 10]", Run("[[2 3 5 + +]]"));
    Assert.Equal("[[] a 10]", Run("[[2 3 a 5 + +]]"));
    Assert.Equal("[[] 8]", Run("[[3 5 + +]]"));
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
