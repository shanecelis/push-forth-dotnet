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

public class NullaryInstruction : Instruction {
  Action<Stack> func;
  public NullaryInstruction(Action<Stack> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    func(stack);
    return stack;
  }

  public static NullaryInstruction WithResult<X>(Func <X> func) {
    return new NullaryInstruction((stack) => {
        stack.Push(func());
      });
  }

}

public class UnaryInstruction<X> : Instruction {
  Action<Stack, X> func;
  public UnaryInstruction(Action<Stack, X> func) {
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
      stack.Push(new Continuation(code));
      return stack;
    }
    func(stack, (X) a);
    return stack;
  }

  public static UnaryInstruction<X> WithResult<Y>(Func <X,Y> func) {
    return new UnaryInstruction<X>((stack, a) => {
          stack.Push(func(a));
      });
  }
}

public class BinaryInstruction<X, Y> : Instruction {
  Action<Stack, X, Y> func;
  Func<object, bool> predicateX = x => x is X;
  Func<object, bool> predicateY = y => y is Y;
  public BinaryInstruction(Action<Stack, X, Y> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 2)
      return stack;
    object a, b;
    a = stack.Pop();
    if (! predicateX(a)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(new Continuation(code));
      return stack;
    }
    b = stack.Pop();
    if (! predicateY(b)) {
      var code = new Stack();
      code.Push(b);
      code.Push(this);
      stack.Push(a);
      stack.Push(new Continuation(code));
      return stack;
    }
    func(stack, (X) a, (Y) b);
    return stack;
  }

  public static BinaryInstruction<X,Y> WithResult<Z>(Func <X,Y,Z> func) {
    return new BinaryInstruction<X,Y>((stack, a, b) => {
          stack.Push(func(a, b));
      });
  }

  public static BinaryInstruction<X,Y> Reorder<Z>(string name) {
    var ins = new BinaryInstruction<X,Y>((stack, a, b) => {
        var s = new Stack();
        s.Push(new Symbol(name));
        s.Push(a);
        s.Push(b);
        stack.Push(new Dummy(typeof(Z)));
        stack.Push(new Reorder(s));
      });
    ins.predicateX = a => a is X || (a is Dummy d && d.type is X);
    ins.predicateY = a => a is Y || (a is Dummy d && d.type is Y);
    return ins;
  }
}

public class Reorder : Tuple<Stack> {
  public Reorder(Stack s) : base(s) { }
  public Stack stack => Item1;
  public Type type;
}

public class Dummy {
  public Dummy(Type type) {
    this.type = type;
  }
  public Type type;
  public override string ToString() {
    return $"Dummy({type})";
  }
}

public class TrinaryInstruction<X, Y, Z> : Instruction {
  Action<Stack, X, Y, Z> func;
  public TrinaryInstruction(Action<Stack, X, Y, Z> func) {
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

  public static TrinaryInstruction<X, Y, Z> WithResult<W>(Func<X, Y, Z, W> func) {
    return new TrinaryInstruction<X, Y, Z>((stack, a, b, c) => {
        stack.Push(func(a, b, c));
      });
  }
}

}
