using System;

namespace SeawispHunter.PushForth {

// public interface FuncFactory<T> {
public interface InstructionFactory {
  Instruction Nullary<X>(Func<X> func);
  Instruction Unary<X,Y>(Func<X,Y> func);
  Instruction Binary<X,Y,Z>(Func<X,Y,Z> func);
  Instruction Trinary<X,Y,Z,W>(Func<X,Y,Z,W> func);

  Instruction Nullary(Action action);
  Instruction Unary<X>(Action<X> action);
  Instruction Binary<X,Y>(Action<X,Y> action);
  Instruction Trinary<X,Y,Z>(Action<X,Y,Z> action);
}

}
