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

public class PolymorphicTests : InterpreterTestUtil{
  [Fact]
  public void TestPolymorphicInstruction() {
    interpreter = cleanInterpreter;
    var addInt = StrictInstruction.factory.Binary((int a, int b) => a + b);
    var addFloat = StrictInstruction.factory.Binary((float a, float b) => a + b);
    var addString = StrictInstruction.factory.Binary((string a, string b) => a + b);

    var addPoly = new PolymorphicInstruction(new [] { addInt, addFloat, addString });
    interpreter.AddInstruction("+", addPoly);
    Assert.Equal("[[] 3]", Run("[[1 2 +]]"));
    Assert.Equal("[[] 3.5]", Run("[[1f 2.5f +]]"));
    Assert.Equal(@"[[] ""helloworld""]", Run(@"[[""hello"" ""world"" +]]"));
    // Assert.Equal(@"[[] helloworld]", Run(@"[[""hello"" ""world"" +]]"));
    lastRun.Pop();
    Assert.Equal("helloworld", lastRun.Pop());

    Assert.Throws<Exception>(() => Run("[[1 2.5f +]]"));
  }

  [Fact]
  public void TestPolymorphicBadInstructions() {
    interpreter = reorderInterpreter;
    var addInt = StrictInstruction.factory.Trinary((int a, int b, int dummy) => a + b);
    var addFloat = StrictInstruction.factory.Binary((float a, float b) => a + b);
    var addString = StrictInstruction.factory.Binary((string a, string b) => a + b);
    Assert.Throws<Exception>(() => new PolymorphicInstruction(new [] { addInt, addFloat, addString }));
  }

  [Fact]
  public void TestPolymorphicReorder() {
    interpreter = reorderInterpreter;
    var factory = ReorderWrapper.GetFactory(StrictInstruction.factory);
    var addInt = factory.Binary((int a, int b) => a + b);
    var addFloat = factory.Binary((float a, float b) => a + b);
    var addString = factory.Binary((string a, string b) => a + b);

    var addPoly
      = new PolymorphicInstruction(new [] { addInt, addFloat, addString })
      { tryBestFit = true };
    interpreter.AddInstruction("+", addPoly);
    Assert.Equal("[[] 1.1 5]", Run("[[3 1.1 2 +]]"));
    Assert.Equal("[[] 1.1 2]", Run("[[1.1 2 +]]"));
    Assert.Equal(@"[[] 2 1.1 ""hi there""]", Run(@"[[""hi "" 1.1 2 ""there"" +]]"));
    Assert.Equal(@"[[] sym 2 1.1 ""hi there""]", Run(@"[[""hi "" 1.1 2 ""there"" sym +]]"));
  }
}

}
