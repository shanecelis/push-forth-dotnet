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

public class ReorderInterpreterTests : InterpreterTestUtil {

  public ReorderInterpreterTests() {
    // interpreter = new ReorderInterpreter();
  }


  [Fact]
  public void testMoreReordering3() {
    var e = EvalStream("[[a 2 ! a]]").GetEnumerator();
    e.MoveNext();
    Assert.Equal("[[2 ! a] a]", e.Current);
    e.MoveNext();
    Assert.Equal("[[! a] 2 a]", e.Current);
    e.MoveNext();
    Assert.Equal("[[a]]", e.Current);
    e.MoveNext();
    Assert.Equal("[[] 2]", e.Current);
  }

  [Fact]
  public void testMoreReordering3a() {
    // The symbol a is set as an instruction. We need a new interpreter to not
    // be polluted by it.
    var e = EvalStream("[[2 a 3 ! a]]").GetEnumerator();
    Assert.True(e.MoveNext());
    Assert.Equal("[[a 3 ! a] 2]", e.Current);
    e.MoveNext();
    Assert.Equal("[[3 ! a] a 2]", e.Current);
    e.MoveNext();
    Assert.Equal("[[! a] 3 a 2]", e.Current);
    e.MoveNext();
    Assert.Equal("[[a] 2]", e.Current);
    e.MoveNext();
    Assert.Equal("[[] 3 2]", e.Current);
    // e.MoveNext();
    // Assert.Equal("[[a] 3]", e.Current);
    // e.MoveNext();
    // Assert.Equal("[[] 2 3]", e.Current);
    Assert.False(e.MoveNext());
  }

  [Fact]
  public void testMoreReordering3b() {
    var e2 = reorderInterpreter.EvalStream("[[! a] 3 2 a]".ToStack())
      .Select(s => reorderInterpreter.StackToString(s))
      .GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[[! 2 a] 3 a]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[[2 a] R<Void>[a 3 !]]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[[a] 2 R<Void>[a 3 !]]", e2.Current);

    Assert.Equal("[[a 3 ! 2 a]]", Reorder("[[a 2 3 ! a]]"));

  }

  [Fact]
  public void TestWeirdStore() {
    interpreter = reorderInterpreter;
    var s = "[2 a]".ToStack();
    // Assert.Equal(new Symbol("a"), s.Peek());
    var i = interpreter.instructions["!"];
    // Assert.Equal("[C[! 2] a]", i.Apply(s).ToRepr());
    Assert.Equal("[R<Void>[a 2 !]]", i.Apply(s).ToRepr());

    s = "[2 a]".ToStack();
    i = interpreter.instructions["!int"];
    Assert.Equal("[R<Void>[a 2 !int]]", i.Apply(s).ToRepr());
  }

  [Fact]
  public void testNestedReordering() {
    interpreter = reorderInterpreter;
    var s1 = "[[+] 1]".ToStack();
    var code = s1.Pop();
    s1.Push(new Defer("[2 3 +]".ToStack(), typeof(int)));
    var i = interpreter.instructions["+"];
    s1 = i.Apply(s1);
    Assert.Equal("[R<int>[1 R<int>[2 3 +] +]]", s1.ToRepr());

    s1 = "[[+] 1]".ToStack();
    code = s1.Pop();
    s1.Push(new Defer("[2 3 +]".ToStack(), typeof(int)));
    s1.Push(code);
    Assert.Equal("[[+] R<int>[2 3 +] 1]", s1.ToRepr());
    var s2 = reorderInterpreter.Eval(s1);
    // Assert.Equal("", s2.ToRepr());
    Assert.Equal("[[] R<int>[1 R<int>[2 3 +] +]]", interpreter.StackToString(s2));
  }

  [Fact]
  public void testLotsOfReordering() {
    Assert.Equal("[[] d c b a 10]", Run("[[2 a 3 b c 5 + d +]]"));

    Stack s1, s2;
    // Assert.Equal("[[] d c b a R[2 R[3 5 +] +]]", (s1 = interpreter.RunReorderPre("[[2 a 3 b c 5 + d +]]".ToStack())).ToRepr());
    Assert.Equal("[[] d c b a R<int>[2 R<int>[3 5 +] +]]", (s1 = reorderInterpreter.Run("[[2 a 3 b c 5 + d +]]".ToStack())).ToRepr());

    Assert.Equal("[[2 3 5 + + a b c d]]", (s2 = ReorderInterpreter.RunReorderPost(s1)).ToRepr());
    Assert.Equal("[[] d c b a 10]", strictInterpreter.Run(s2).ToRepr());
  }

  [Fact]
  public void testMoreReordering() {
    var strict = new StrictInterpreter();
    // The non-strict interpreter moves around arguments such that they will continue to work.
    Assert.Equal("[[] d c b a 6]", Run("[[2 a negate 3 b c 5 + d +]]"));
    Assert.Equal("[[2 negate 3 5 + + a b c d]]", reorderInterpreter.Reorder("[[2 a negate 3 b c 5 + d +]]".ToStack()).ToRepr());
    Assert.Equal("[[] d c b a 6]", strict.Run("[[2 negate 3 5 + + a b c d]]".ToStack()).ToRepr());
    // Can't run the original with the strict interpreter.
    Assert.Throws<InvalidCastException>(() => strict.Run("[[2 a negate 3 b c 5 + d +]]".ToStack()));
  }

  [Fact]
  public void testMoreReordering2() {
    var strict = new StrictInterpreter();
    // The non-strict interpreter moves around arguments such that they will continue to work.
    Assert.Equal("[[] d c b a 6]", Run("[[2 a negate 3 b c 5 + d + +]]"));
    Assert.Equal("[[2 negate 3 5 + + a b c d]]", reorderInterpreter.Reorder("[[2 a negate 3 b c 5 + d + +]]".ToStack()).ToRepr());
    // The same output is produced.
    Assert.Equal("[[] d c b a 6]", strict.Run("[[2 negate 3 5 + + a b c d]]".ToStack()).ToRepr());
    // Can't run the original with the strict interpreter.
    Assert.Throws<InvalidCastException>(() => strict.Run("[[2 a negate 3 b c 5 + d + +]]".ToStack()));
    Assert.Throws<InvalidOperationException>(() => strict.Run("[[2 +]]".ToStack()));
    Assert.Throws<InvalidOperationException>(() => strict.Run("[[+]]".ToStack()));
  }

  [Fact]
  public void testMoreReordering4() {
    var strict = new StrictInterpreter();
    // The non-strict interpreter moves around arguments such that they will continue to work.
    Assert.Equal("[[] d c b a 3]", Run("[[2 a negate 3 pop b c 5 + d + +]]"));
    Assert.Equal("[[2 negate 5 + a 3 pop b c d]]", Reorder("[[2 a negate 3 pop b c 5 + d + +]]"));
    // The same output is produced.
    Assert.Equal("[[] d c b a 3]", strict.Run("[[2 negate 5 + a 3 pop b c d]]".ToStack()).ToRepr());
    // Can't run the original with the strict interpreter.
    Assert.Throws<InvalidCastException>(() => strict.Run("[[2 a negate 3 pop b c 5 + d + +]]".ToStack()));
    Assert.Throws<InvalidOperationException>(() => strict.Run("[[2 +]]".ToStack()));
    Assert.Throws<InvalidOperationException>(() => strict.Run("[[+]]".ToStack()));
  }

  [Fact]
  public void TestReorderBugFoundInWild() {
    interpreter = reorderInterpreter;
    Assert.Equal("[[]]", Run("[[dup]]"));
    Assert.Equal("[[]]", Run("[[pop]]"));
    // Assert.Equal("[[] R[-3 pop]]", Run("[[-3 pop]]"));
    Assert.Equal("[[] R<EmptyStack>[-3 pop]]", Run("[[-3 pop]]"));
    Assert.Equal("[[] R<EmptyStack>[R<EmptyStack>[R<EmptyStack>[-3 pop] dup] dup] R<EmptyStack>[] R<EmptyStack>[]]", Run("[[dup / pop < swap -3 pop dup dup]]"));
    Assert.Equal("[]", ReorderInterpreter.RunReorderPost("[]".ToStack()).ToRepr());
    Assert.Equal("[[]]", ReorderInterpreter.RunReorderPost("[[]]".ToStack()).ToRepr());
    Assert.Equal("[[-3 pop dup dup]]", Reorder("[[dup / pop < swap -3 pop dup dup]]"));
  }

  [Fact]
  public void TestReorderBugFoundInWild2() {
    interpreter = reorderInterpreter;
    Assert.Equal("[[-2 =]]", Reorder("[[< -2 = >]]"));
    Assert.Equal("[[]]", Reorder("[[dup]]"));
    Assert.Equal("[R<int>[-2 dup] R<int>[]]", reorderInterpreter.instructions["dup"].Apply("[-2]".ToStack()).ToRepr());
    Assert.Equal("[[-2 dup]]", Reorder("[[-2 dup]]"));
    Assert.Equal("[[-2 dup > =]]", Reorder("[[< -2 dup = >]]"));
  }

  [Fact]
  public void TestReorderBugFoundInWild3() {
    Assert.Equal("[[1 depth / x pop dup pop swap x dup]]", Reorder("[[x - < pop 1 do-times / dup > pop + swap x / < depth / < % dup *]]"));
    // Assert.Equal("", Reorder("[[do-times < % swap depth / -3 * [x] x - < pop 1 do-times / dup > pop + swap x / < depth / < % dup *]]"));
    Assert.Equal("[[depth -3 * depth / [x] x pop 1 do-times dup pop swap x dup]]", Reorder("[[do-times < % swap depth / -3 * [x] x - < pop 1 do-times / dup > pop + swap x / < depth / < % dup *]]"));
  }

  [Fact]
  public void TestReorderWithVoid() {
    Assert.Equal("[[1 3 + x 2 !]]", Reorder("[[1 x 2 ! 3 +]]"));
    Assert.Equal("[[] 4]", Run("[[1 x 2 ! 3 +]]"));
    interpreter.instructions.Remove("x");
    Assert.Equal("[[] 4]", Run("[[1 3 + x 2 !]]"));
  }

  /* XXX This demonstrates a real problem with assignment and actions.
     Anything that produces side effects should have its order preserved.

     ReorderWrapper might need to handle assignment effects specifically by
     walking over the stack, and changing each symbol to a R<int>[x] or
     something.
   */
  [Fact]
  public void TestReorderWithAssignment() {
    Assert.Equal("[[] 3]", Run("[[1 x 2 ! x +]]"));
    Assert.Equal("[[1 x 2 ! x +]]", Reorder("[[1 x 2 ! x +]]"));
    interpreter.instructions.Remove("x");
    Assert.Equal("[[] x 1]", Run("[[1 x + x 2 !]]"));
    interpreter.instructions.Remove("x");
    Assert.Equal("[[] 2 1]", Run("[[1 x 2 ! x]]"));
  }
}
}
