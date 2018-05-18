using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace SeawispHunter.PushForth {


public class DeferInstruction : TypedInstruction {
  internal string name;
  internal IEnumerable<Type> _inputTypes = Type.EmptyTypes;
  internal IEnumerable<Type> _outputTypes = Type.EmptyTypes;
  public IEnumerable<Type> inputTypes => _inputTypes;
  public IEnumerable<Type> outputTypes => _outputTypes;

  public DeferInstruction(string name,
                          IEnumerable<Type> inputTypes,
                          IEnumerable<Type> outputTypes) {
    this.name = name;
    this._inputTypes = inputTypes;
    this._outputTypes = outputTypes;
  }

  public DeferInstruction(string name, TypedInstruction ins)
    : this(name, ins.inputTypes, ins.outputTypes) { }

  public Stack Apply(Stack stack) {
    var code = new Stack();
    code.Push(new Symbol(name));
    int inputCount = inputTypes.Count();
    for (int i = 0; i < inputCount; i++)
      code.Push(stack.Pop());
    stack.Push(new Defer(code, outputTypes.FirstOrDefault()));
    return stack;
  }
}

public class Defer : Tuple<Stack, Type>, IReprType {
  public Defer(Stack s) : base(s, null) { }
  public Defer(Stack s, Type t) : base(s, t) { }
  public Stack stack => Item1;
  public Type type => Item2;
  public override string ToString() {
    if (type != null)
      return $"R<{type.PrettyName()}>{stack.ToRepr()}";
    else
      return $"R{stack.ToRepr()}";
  }
}

/** Stand-in that represents another type. */
public interface IReprType {
  Type type { get; }
}

public class Dummy : IReprType {
  public Dummy(Type type) {
    this.type = type;
  }
  public Type type { get; set; }
  public override string ToString() {
    return $"D({type.PrettyName()})";
  }
}

}
