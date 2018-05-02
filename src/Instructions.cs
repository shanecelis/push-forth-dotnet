using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeawispHunter.PushForth {

public interface Instruction {
  Stack Apply(Stack stack);
}

public class InstructionFunc : Instruction {
  Func<Stack, Stack> func;
  public InstructionFunc(Func<Stack, Stack> func) {
    this.func = func;
  }

  public InstructionFunc(Action<Stack> action)
    : this(stack => { action(stack); return stack; }) {
  }

  public Stack Apply(Stack stack) {
    return func(stack);
  }
}

public class NoResultException : Exception { }

public class NullaryFunc<X> : Instruction {
  Func<X> func;
  public NullaryFunc(Func<X> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    try {
      stack.Push(func());
    } catch(NoResultException) {
      // No-op.
    }
    return stack;
  }
}

public class UnaryFunc<X, Y> : Instruction {
  Func<X, Y> func;
  public UnaryFunc(Func<X, Y> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 1)
      return stack;
    object a;
    a = stack.Pop();
    if (! (a is X)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(code);
      return stack;
    }
    try {
      stack.Push(func((X) a));
    } catch(NoResultException) {
      // No-op.
    }
    return stack;
  }
}

public class BinaryFunc<X, Y, Z> : Instruction {
  Func<X, Y, Z> func;
  public BinaryFunc(Func<X, Y, Z> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 2)
      return stack;
    object a, b;
    a = stack.Pop();
    if (! (a is X)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(new Continuation(code));
      return stack;
    }
    b = stack.Pop();
    if (! (b is Y)) {
      var code = new Stack();
      code.Push(b);
      code.Push(this);
      stack.Push(a);
      stack.Push(new Continuation(code));
      return stack;
    }
    try {
      stack.Push(func((X) a, (Y) b));
    } catch(NoResultException) {
      // No-op.
    }
    return stack;
  }
}

public class TrinaryFunc<X, Y, Z> : Instruction {
  Action<Stack, X, Y, Z> func;
  public static TrinaryFunc<X, Y, Z> Func<W>(Func<X, Y, Z, W> func) {
    return new TrinaryFunc<X, Y, Z>((stack, a, b, c) => {
        try {
          stack.Push(func(a, b, c));
        } catch(NoResultException) {
          // No-op.
        }
      });
  }
  public TrinaryFunc(Action<Stack, X, Y, Z> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 3)
      return stack;
    object a, b, c;
    a = stack.Pop();
    if (! (a is X)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(new Continuation(code));
      return stack;
    }
    b = stack.Pop();
    if (! (b is Y)) {
      var code = new Stack();
      code.Push(b);
      code.Push(this);
      stack.Push(a);
      stack.Push(new Continuation(code));
      return stack;
    }
    c = stack.Pop();
    if (! (c is Z)) {
      var code = new Stack();
      code.Push(c);
      code.Push(this);
      stack.Push(b);
      stack.Push(a);
      stack.Push(new Continuation(code));
      return stack;
    }
    func(stack, (X) a, (Y) b, (Z) c);
    return stack;
  }
}

}
