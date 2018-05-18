using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace SeawispHunter.PushForth {

public class ReorderWrapper : TypedInstruction {
  public IEnumerable<Type> inputTypes { get; set; }
  public IEnumerable<Type> outputTypes { get; set; }
  public readonly string name;
  public Func<object, Type> getType = o => o is IReprType d ? d.type : o.GetType();
  public Func<Type, object> putType = o => new Dummy(o);
  Instruction innerInstruction;
  public ReorderWrapper(string name,
                        TypedInstruction innerInstruction)
    : this(name,
           innerInstruction.inputTypes,
           innerInstruction.outputTypes,
           innerInstruction) { }

  public ReorderWrapper(string name,
                        IEnumerable<Type> inputTypes,
                        IEnumerable<Type> outputTypes,
                        Instruction innerInstruction) {
    this.name = name;
    this.inputTypes = inputTypes;
    this.outputTypes = outputTypes;
    this.innerInstruction = innerInstruction;
  }

  public virtual Stack TypeMismatch(Stack stack, ICollection acceptedArguments, object o, Type consume) {
    // Put the good arguments back.
    stack = Interpreter.Append(acceptedArguments, stack);
    // foreach(var passed in passedTypes)
    //   stack.Push(passed);
    var code = new Stack();
    code.Push(o);
    code.Push(new Symbol(name));
    stack.Push(new Continuation(code));
    return stack;
  }

  public virtual Stack NotEnoughElements(Stack stack,
                                         IEnumerable acceptedArguments) {
    foreach(object p in acceptedArguments)
      stack.Push(p);
    return stack;
  }

  public Stack Apply(Stack stack) {
    var acceptedArguments = new Stack();
    foreach(Type consume in inputTypes) {
      if (! stack.Any()) {
        // Not enough elements.
        return NotEnoughElements(stack, acceptedArguments);
      }

      object o = stack.Pop();
      var t = getType(o);
      // if (t == consume) {
      if (consume == typeof(Variable)) {
        // XXX What is going on here?
        acceptedArguments.Push(o);
      } else if (consume.IsAssignableFrom(t)) {
        acceptedArguments.Push(o);
      } else {
        return TypeMismatch(stack, acceptedArguments, o, consume);
      }
    }

    // Push the good arguments back onto the stack.
    foreach(var arg in acceptedArguments)
      stack.Push(arg);
    // Then run the real instruction.
    return innerInstruction.Apply(stack);
  }

  public override string ToString() {
    return "[" + string.Join(" ", inputTypes.Select(t => t.PrettyName())) + "] -> "
      + "[" + string.Join(" ", outputTypes.Select(t => t.PrettyName())) + "]";
  }
}

}
