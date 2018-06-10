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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace PushForth {

/** Consume the inputs and optionally place the output types. */
public class ConsumeInstruction : TypedInstruction {
  internal IEnumerable<Type> _inputTypes = Type.EmptyTypes;
  internal IEnumerable<Type> _outputTypes = Type.EmptyTypes;
  public IEnumerable<Type> inputTypes => _inputTypes;
  public IEnumerable<Type> outputTypes => _outputTypes;
  bool pushOutputs;
  public ConsumeInstruction(bool pushOutputs,
                            IEnumerable<Type> inputTypes,
                            IEnumerable<Type> outputTypes) {
    this.pushOutputs = pushOutputs;
    this._inputTypes = inputTypes;
    this._outputTypes = outputTypes;
  }

  public ConsumeInstruction(bool pushOutputs, TypedInstruction ins)
    : this(pushOutputs, ins.inputTypes, ins.outputTypes) { }

  public Stack Apply(Stack stack) {
    int inputCount = inputTypes.Count();
    for (int i = 0; i < inputCount; i++)
      stack.Pop();
    if (pushOutputs) {
      foreach(var x in outputTypes)
        stack.Push(x);
    }
    return stack;
  }
}

// public class DoAndDeferInstruction : DeferInstruction {
//   TypedInstruction innerInstruction;
//   public DeferInstruction(string name, TypedInstruction ins)
//     : this(name, ins.inputTypes, ins.outputTypes) {
//     innerInstruction = ins;
//   }


//   public override Stack ApplyWithBindings(Stack stack, Dictionary<string, Type> bindings) {
//     stack = base.ApplyWithBindings(stack, bindings);
//     if (stack.Peek() is Defer d) {
//       // Let's run it.
//       d.code;
//     }
//   }
// }

/** Record an instruction that was called. */
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
    return ApplyWithBindings(stack, null);
  }

  public virtual Stack ApplyWithBindings(Stack stack, Dictionary<string, Type> bindings) {
    var code = new Stack();
    code.Push(new Symbol(name));
    int inputCount = inputTypes.Count();
    // XXX I could develop my own binding here instead of forking that off to ReorderWrapper.
    for (int i = 0; i < inputCount; i++)
      code.Push(stack.Pop());
    int outputCount = outputTypes.Count();
    if (outputCount == 0) {
      stack.Push(new Defer(code, typeof(void)));
    } else {
      foreach(var _outputType in outputTypes.Reverse()) {
        Type outputType = _outputType;
        if (bindings != null && Variable.IsVariableType(outputType)) {
          var v = Variable.Instantiate(outputType);
          outputType = bindings[v.name];
        }
        /*
          We add an entry for each thing, but only the top one has the code.

          Might need to do something more clever later.
        */
        stack.Push(new Defer(//++j == outputCount ? code : new Stack(),
                             code,
                             outputType));
      }
    }
    return stack;
  }
}

public class Defer : Tuple<Stack, Type>, IReprType {
  public Defer(Stack s) : base(s, null) { }
  public Defer(Stack s, Type t) : base(s, t) { }
  public Stack stack => Item1;
  private Type _type = null;
  public Type type {
    get => _type == null ? Item2 : _type;
    set => _type = value;
  }
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
