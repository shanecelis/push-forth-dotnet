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

  public static NullaryInstruction Reorder<X>(string name) {
    var ins = new NullaryInstruction((stack) => {
        var s = new Stack();
        s.Push(new Symbol(name));
        stack.Push(new Reorder(s, typeof(X)));
      });
    return ins;
  }
}

public class UnaryInstruction<X> : Instruction {
  Action<Stack, object> func;
  Func<object, bool> predicateX = x => x is X;

  public UnaryInstruction(Action<Stack, X> func)
    : this(true, (stack, x) => func(stack, (X) x)) {
  }
  public UnaryInstruction(bool dummy, Action<Stack, object> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 1)
      return stack;
    object a;
    a = stack.Pop();
    if (! predicateX(a)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(new Continuation(code));
      return stack;
    }
    func(stack, a);
    return stack;
  }

  public static UnaryInstruction<X> WithResult<Y>(Func <X,Y> func) {
    return new UnaryInstruction<X>((stack, a) => {
          stack.Push(func(a));
      });
  }

  public static UnaryInstruction<X> Reorder<Z>(string name) {
    var ins = new UnaryInstruction<X>(true, (stack, a) => {
        var s = new Stack();
        s.Push(new Symbol(name));
        s.Push(a);
        stack.Push(new Reorder(s, typeof(Z)));
      });
    ins.predicateX = x => x is X || (x is Reorder r && r.type == typeof(X));
    return ins;
  }
}

public class BinaryInstruction<X, Y> : Instruction {
  Action<Stack, object, object> func;
  Func<object, bool> predicateX = x => x is X;
  Func<object, bool> predicateY = y => y is Y;
  public BinaryInstruction(Action<Stack, X, Y> func)
    : this(true, (stack, a, b) => func(stack, (X) a, (Y) b)) {
  }

  BinaryInstruction(bool dummy, Action<Stack, object, object> func) {
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
    func(stack, a, b);
    return stack;
  }

  public static BinaryInstruction<X,Y> WithResult<Z>(Func <X,Y,Z> func) {
    return new BinaryInstruction<X,Y>((stack, a, b) => {
          stack.Push(func(a, b));
      });
  }

  public static BinaryInstruction<X,Y> Reorder(string name, Type t) {
    var ins = new BinaryInstruction<X,Y>(true, (stack, a, b) => {
        var s = new Stack();
        s.Push(new Symbol(name));
        s.Push(a);
        s.Push(b);
        stack.Push(new Reorder(s, t));
      });
    ins.predicateX = x => x is X || (x is Reorder r && r.type == typeof(X));
    ins.predicateY = y => y is Y || (y is Reorder r && r.type == typeof(Y));
    return ins;
  }
}

public class Reorder : Tuple<Stack, Type> {
  public Reorder(Stack s) : base(s, null) { }
  public Reorder(Stack s, Type t) : base(s, t) { }
  public Stack stack => Item1;
  public Type type => Item2;
  public override string ToString() {
    if (type != null)
      return $"R<{type}>{stack.ToRepr()}";
    else
      return $"R{stack.ToRepr()}";
  }
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
  Action<Stack, object, object, object> func;
  Func<object, bool> predicateX = x => x is X;
  Func<object, bool> predicateY = y => y is Y;
  Func<object, bool> predicateZ = z => z is Z;
  public TrinaryInstruction(Action<Stack, X, Y, Z> func)
    : this(true, (stack, a, b, c) => func(stack, (X) a, (Y) b, (Z) c)){
  }

  TrinaryInstruction(bool dummy, Action<Stack, object, object, object> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 3)
      return stack;
    object a, b, c;
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
    c = stack.Pop();
    if (! predicateZ(c)) {
      var code = new Stack();
      code.Push(c);
      code.Push(this);
      stack.Push(b);
      stack.Push(a);
      stack.Push(new Continuation(code));
      return stack;
    }
    func(stack, a, b, c);
    return stack;
  }

  public static TrinaryInstruction<X, Y, Z> WithResult<W>(Func<X, Y, Z, W> func) {
    return new TrinaryInstruction<X, Y, Z>((stack, a, b, c) => {
        stack.Push(func(a, b, c));
      });
  }

  public static TrinaryInstruction<X,Y,Z> Reorder<W>(string name) {
    var ins = new TrinaryInstruction<X,Y,Z>(true, (stack, a, b, c) => {
        var s = new Stack();
        s.Push(new Symbol(name));
        s.Push(a);
        s.Push(b);
        s.Push(c);
        stack.Push(new Reorder(s, typeof(W)));
      });
    ins.predicateX = x => x is X || (x is Reorder r && r.type == typeof(X));
    ins.predicateY = y => y is Y || (y is Reorder r && r.type == typeof(Y));
    ins.predicateZ = z => z is Z || (z is Reorder r && r.type == typeof(Z));
    return ins;
  }
}

}
