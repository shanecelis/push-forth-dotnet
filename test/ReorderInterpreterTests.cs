
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Xunit;
using SeawispHunter.PushForth;

namespace SeawispHunter.PushForth {

public class ReorderInterpreterTests : InterpreterTestUtil {

  public ReorderInterpreterTests() {
    // interpreter = new ReorderInterpreter();
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
    Assert.Equal("[[! 3 a] a 2]", e.Current);
    e.MoveNext();
    Assert.Equal("[[3 a]]", e.Current);
    e.MoveNext();
    Assert.Equal("[[a] 3]", e.Current);
    e.MoveNext();
    Assert.Equal("[[] 2 3]", e.Current);
    Assert.False(e.MoveNext());
  }

  [Fact]
  public void testMoreReordering3b() {
    var e2 = reorderInterpreter.EvalStream("[[! a] 3 a 2]".ToStack())
                                    // Interpreter.IsHalted,
                                    // interpreter.ReorderPre)
      // .Select(s => interpreter.StackToString(s, new [] { interpreter.reorderInstructions }))
      .Select(s => reorderInterpreter.StackToString(s))
      .GetEnumerator();
    Assert.True(e2.MoveNext());
    Assert.Equal("[[! 3 a] a 2]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[[3 a] R[2 a !]]", e2.Current);
    Assert.True(e2.MoveNext());
    Assert.Equal("[[a] 3 R[2 a !]]", e2.Current);

    Assert.Equal("[[2 a ! 3 a]]", Reorder("[[2 a 3 ! a]]"));

  }

  [Fact]
  public void TestWeirdStore() {
    interpreter = reorderInterpreter;
    var s = "[a 2]".ToStack();
    Assert.Equal(new Symbol("a"), s.Peek());
    var i = interpreter.instructions["!"];
    // Assert.Equal("[C[! 2] a]", i.Apply(s).ToRepr());
    Assert.Equal("[R[2 a !]]", i.Apply(s).ToRepr());

    s = "[a 2]".ToStack();
    i = interpreter.instructions["!int"];
    Assert.Equal("[R[2 a !int]]", i.Apply(s).ToRepr());
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

}
}