using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeawispHunter.PushForth {

/*
 */
public class StrictInstruction : TypedInstruction {

  internal IEnumerable<Type> _inputTypes = Type.EmptyTypes;
  internal IEnumerable<Type> _outputTypes = Type.EmptyTypes;
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

  class StrictInstructionFactory : FuncFactory<TypedInstruction> {
    public TypedInstruction Nullary<X>(Func <X> func) {
      return new StrictInstruction((stack) => {
          stack.Push(func());
        }) { _outputTypes = new [] { typeof(X) } };
    }

    public TypedInstruction Unary<X,Y>(Func <X,Y> func) {
      return new StrictInstruction((stack) => {
          stack.Push(func((X) stack.Pop()));
        }) { _inputTypes = new [] { typeof(X) },
        _outputTypes = new [] { typeof(Y) } };
    }

    public TypedInstruction Binary<X,Y,Z>(Func <X,Y,Z> func) {
      return new StrictInstruction((stack) => {
          stack.Push(func((X) stack.Pop(), (Y) stack.Pop()));
        }) { _inputTypes = new [] { typeof(X), typeof(Y) },
        _outputTypes = new [] { typeof(Z) } };
    }
    public TypedInstruction Trinary<X,Y,Z,W>(Func <X,Y,Z,W> func) {
      return new StrictInstruction((stack) => {
          stack.Push(func((X) stack.Pop(), (Y) stack.Pop(), (Z) stack.Pop()));
        }) { _inputTypes = new [] { typeof(X), typeof(Y), typeof(Z) },
        _outputTypes = new [] { typeof(W) } };
    }

    public TypedInstruction Nullary(Action func) {
      return new StrictInstruction((_) => {
          func();
        });
    }

    public TypedInstruction Unary<X>(Action<X> func) {
      return new StrictInstruction((stack) => {
          func((X) stack.Pop());
        }) { _inputTypes = new [] { typeof(X) } };
    }

    public TypedInstruction Binary<X,Y>(Action<X,Y> func) {
      return new StrictInstruction((stack) => {
          func((X) stack.Pop(), (Y) stack.Pop());
        }) { _inputTypes = new [] { typeof(X), typeof(Y) } };
    }
    public TypedInstruction Trinary<X,Y,Z>(Action<X,Y,Z> func) {
      return new StrictInstruction((stack) => {
          func((X) stack.Pop(), (Y) stack.Pop(), (Z) stack.Pop());
        }) { _inputTypes = new [] { typeof(X), typeof(Y), typeof(Z) } };
    }

    public TypedInstruction Nullary(Action<Stack> func) {
      return new StrictInstruction((stack) => {
          func(stack);
        });
    }

    public TypedInstruction Unary<X>(Action<Stack, X> func) {
      return new StrictInstruction((stack) => {
          func(stack, (X) stack.Pop());
        }) { _inputTypes = new [] { typeof(X) } };
    }

    public TypedInstruction Binary<X,Y>(Action<Stack, X,Y> func) {
      return new StrictInstruction((stack) => {
          func(stack, (X) stack.Pop(), (Y) stack.Pop());
        }) { _inputTypes = new [] { typeof(X), typeof(Y) } };
    }
    public TypedInstruction Trinary<X,Y,Z>(Action<Stack, X,Y,Z> func) {
      return new StrictInstruction((stack) => {
          func(stack, (X) stack.Pop(), (Y) stack.Pop(), (Z) stack.Pop());
        }) { _inputTypes = new [] { typeof(X), typeof(Y), typeof(Z) } };
    }
  }
  public static FuncFactory<TypedInstruction> factory = new StrictInstructionFactory();
}

}
