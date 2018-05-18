using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeawispHunter.PushForth {

public class StrictInstruction : TypedInstruction {

  IEnumerable<Type> _inputTypes = new Type[] { };
  IEnumerable<Type> _outputTypes = new Type[] { };
  public IEnumerable<Type> inputTypes => _inputTypes;
  public IEnumerable<Type> outputTypes => _outputTypes;
  Action<Stack> func;
  public StrictInstruction(Action<Stack> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    func(stack);
    return stack;
  }

  public static StrictInstruction Nullary<X>(Func <X> func) {
    return new StrictInstruction((stack) => {
        stack.Push(func());
      }) { _outputTypes = new [] { typeof(X) } };
  }

  public static StrictInstruction Unary<X,Y>(Func <X,Y> func) {
    return new StrictInstruction((stack) => {
        stack.Push(func((X) stack.Pop()));
      }) { _inputTypes = new [] { typeof(X) },
           _outputTypes = new [] { typeof(Y) } };
  }

  public static StrictInstruction Binary<X,Y,Z>(Func <X,Y,Z> func) {
    return new StrictInstruction((stack) => {
        stack.Push(func((X) stack.Pop(), (Y) stack.Pop()));
      }) { _inputTypes = new [] { typeof(X), typeof(Y) },
           _outputTypes = new [] { typeof(Z) } };
  }
  public static StrictInstruction Trinary<X,Y,Z,W>(Func <X,Y,Z,W> func) {
    return new StrictInstruction((stack) => {
        stack.Push(func((X) stack.Pop(), (Y) stack.Pop(), (Z) stack.Pop()));
      }) { _inputTypes = new [] { typeof(X), typeof(Y), typeof(Z) },
           _outputTypes = new [] { typeof(W) } };
  }

  public static StrictInstruction Nullary(Action func) {
    return new StrictInstruction((_) => {
        func();
      });
  }

  public static StrictInstruction Unary<X>(Action<X> func) {
    return new StrictInstruction((stack) => {
        func((X) stack.Pop());
      }) { _inputTypes = new [] { typeof(X) } };
  }

  public static StrictInstruction Binary<X,Y>(Action<X,Y> func) {
    return new StrictInstruction((stack) => {
        func((X) stack.Pop(), (Y) stack.Pop());
      }) { _inputTypes = new [] { typeof(X), typeof(Y) } };
  }
  public static StrictInstruction Trinary<X,Y,Z>(Action<X,Y,Z> func) {
    return new StrictInstruction((stack) => {
        func((X) stack.Pop(), (Y) stack.Pop(), (Z) stack.Pop());
      }) { _inputTypes = new [] { typeof(X), typeof(Y), typeof(Z) } };
  }
}

public class StrictNullaryInstruction : Instruction {
  Action<Stack> func;
  public StrictNullaryInstruction(Action<Stack> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    func(stack);
    return stack;
  }

  public static StrictNullaryInstruction WithResult<X>(Func <X> func) {
    return new StrictNullaryInstruction((stack) => {
          stack.Push(func());
      });
  }
}

public class StrictUnaryInstruction<X> : Instruction {
  Action<Stack, X> func;
  public StrictUnaryInstruction(Action<Stack, X> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    object a;
    a = stack.Pop();
    func(stack, (X) a);
    return stack;
  }

  public static StrictUnaryInstruction<X> WithResult<Y>(Func <X,Y> func) {
    return new StrictUnaryInstruction<X>((stack, a) => {
        stack.Push(func(a));
      });
  }
}

public class StrictBinaryInstruction<X, Y> : Instruction {
  Action<Stack, X, Y> func;
  public StrictBinaryInstruction(Action<Stack, X, Y> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    object a, b;
    a = stack.Pop();
    b = stack.Pop();
    func(stack, (X) a, (Y) b);
    return stack;
  }

  public static StrictBinaryInstruction<X,Y> WithResult<Z>(Func <X,Y,Z> func) {
    return new StrictBinaryInstruction<X,Y>((stack, a, b) => {
        stack.Push(func(a, b));
      });
  }
}

public class StrictTrinaryInstruction<X, Y, Z> : Instruction {
  Action<Stack, X, Y, Z> func;
  public StrictTrinaryInstruction(Action<Stack, X, Y, Z> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    object a, b, c;
    a = stack.Pop();
    b = stack.Pop();
    c = stack.Pop();
    func(stack, (X) a, (Y) b, (Z) c);
    return stack;
  }

  public static StrictTrinaryInstruction<X, Y, Z> WithResult<W>(Func<X, Y, Z, W> func) {
    return new StrictTrinaryInstruction<X, Y, Z>((stack, a, b, c) => {
        stack.Push(func(a, b, c));
      });
  }
}

}
