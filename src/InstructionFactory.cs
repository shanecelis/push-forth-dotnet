using System;
using System.Collections;
using System.Collections.Generic;

namespace SeawispHunter.PushForth {

public interface FuncFactory<out T> {
  // These are just naked stack handlers.
  // Hmm... These are the only things I really need.
  T Operation(Func<Stack, Stack> func,
              IEnumerable<Type> inputTypes,
              IEnumerable<Type> outputTypes);

  T Operation(Action<Stack> action,
              IEnumerable<Type> inputTypes,
              IEnumerable<Type> outputTypes);

  T Nullary<X>(Func<X> func);
  T Unary<X,Y>(Func<X,Y> func);
  T Binary<X,Y,Z>(Func<X,Y,Z> func);
  T Trinary<X,Y,Z,W>(Func<X,Y,Z,W> func);

  T Nullary(Action action);
  T Unary<X>(Action<X> action);
  T Binary<X,Y>(Action<X,Y> action);
  T Trinary<X,Y,Z>(Action<X,Y,Z> action);

  T Nullary(Action<Stack> action);
  T Unary<X>(Action<Stack,X> action);
  T Binary<X,Y>(Action<Stack,X,Y> action);
  T Trinary<X,Y,Z>(Action<Stack,X,Y,Z> action);
}

// public interface InstructionFactory : FuncFactory<Instruction> { }
// public interface TypedInstructionFactory : FuncFactory<TypedInstruction> { }

public class FuncFactoryAdapter<S,T> : FuncFactory<T> {
  FuncFactory<S> factory;
  Func<S,T> converter;

  public FuncFactoryAdapter(FuncFactory<S> factory,
                            Func<S,T> converter) {
    this.factory = factory;
    this.converter = converter;
  }

  public T Operation(Func<Stack,Stack> func,
                     IEnumerable<Type> inputTypes,
                     IEnumerable<Type> outputTypes)
    => converter(factory.Operation(func, inputTypes, outputTypes));
  public T Operation(Action<Stack> action,
                     IEnumerable<Type> inputTypes,
                     IEnumerable<Type> outputTypes)
    => converter(factory.Operation(action, inputTypes, outputTypes));

  public T Nullary<X>(Func<X> func) => converter(factory.Nullary(func));
  public T Unary<X,Y>(Func<X,Y> func) => converter(factory.Unary(func));
  public T Binary<X,Y,Z>(Func<X,Y,Z> func) => converter(factory.Binary(func));
  public T Trinary<X,Y,Z,W>(Func<X,Y,Z,W> func) => converter(factory.Trinary(func));

  public T Nullary(Action action) => converter(factory.Nullary(action));
  public T Unary<X>(Action<X> action) => converter(factory.Unary(action));
  public T Binary<X,Y>(Action<X,Y> action) => converter(factory.Binary(action));
  public T Trinary<X,Y,Z>(Action<X,Y,Z> action) => converter(factory.Trinary(action));

  public T Nullary(Action<Stack> action) => converter(factory.Nullary(action));
  public T Unary<X>(Action<Stack,X> action) => converter(factory.Unary(action));
  public T Binary<X,Y>(Action<Stack,X,Y> action) => converter(factory.Binary(action));
  public T Trinary<X,Y,Z>(Action<Stack,X,Y,Z> action) => converter(factory.Trinary(action));
}

public static class FuncFactoryExtensions {
  public static FuncFactory<Y> Compose<X,Y>(this FuncFactory<X> factory, Func<X,Y> converter) {
    return new FuncFactoryAdapter<X,Y>(factory, converter);
  }
}

}