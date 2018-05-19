using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace SeawispHunter.PushForth {

/** The basic Instruction interface. */
public interface Instruction {
  // string name { get; }
  Stack Apply(Stack stack);
}

/** Include the types of inputs and outputs. These may include generic types via
    typeof(Variable.{A,B,C,...}). */
public interface TypedInstruction : Instruction {
  IEnumerable<Type> inputTypes { get; }
  IEnumerable<Type> outputTypes { get; }
}

public class NoopInstruction : Instruction {
  public Stack Apply(Stack stack) {
    return stack;
  }
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

public class ReorderInstruction : ReorderWrapper {

  public bool leaveReorderItems = true;
  Instruction _instruction = null;
  Instruction instruction {
    get {
      if (_instruction == null) {
        if (leaveReorderItems) {
          _instruction = new DeferInstruction(name,
                                              inputTypes,
                                              outputTypes);
        } else {
          _instruction = new ConsumeInstruction(true,
                                                inputTypes,
                                                outputTypes);
        }
      }
      return _instruction;
    }
  }

  public ReorderInstruction(string name,
                            IEnumerable<Type> inputTypes,
                            IEnumerable<Type> outputTypes)
    : base(name, inputTypes, outputTypes, null) {
    innerInstruction
      = new InstructionFunc(stack =>
                            this.instruction.Apply(stack));
  }
}

public class TypeCheckInstruction : ReorderInstruction {
  public TypeCheckInstruction(string name,
                              IEnumerable<Type> consumes,
                              IEnumerable<Type> produces)
    : base(name, consumes, produces) { }

  public override Stack TypeMismatch(Stack stack, ICollection passedTypes, object o, Type consume) {
    throw new Exception($"Type check instruction {name} expected type {consume} but got {o}");
  }
}

}
