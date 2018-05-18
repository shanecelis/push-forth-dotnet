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

// XXX This can probably be removed in favor of StrictInstruction.
public class TypeCheckInstruction : ReorderInstruction {
  public TypeCheckInstruction(string name,
                              IEnumerable<Type> consumes,
                              IEnumerable<Type> produces)
    : base(name, consumes, produces) { }

  public override Stack TypeMismatch(Stack stack, ICollection passedTypes, object o, Type consume) {
    throw new Exception($"Type check instruction {name} expected type {consume} but got {o}");
  }
}

// XXX This can probably be removed in favor of ReorderWrapper.
public class ReorderInstruction : TypedInstruction {
  public IEnumerable<Type> inputTypes { get; set; }
  public IEnumerable<Type> outputTypes { get; set; }
  public IEnumerable<Type> consumes => inputTypes;
  public IEnumerable<Type> produces => outputTypes;
  public readonly string name;
  public Func<object, Type> getType = o => o is IReprType d ? d.type : o.GetType();
  public Func<Type, object> putType = o => new Dummy(o);
  public bool leaveReorderItems = true;

  public ReorderInstruction(string name,
                            IEnumerable<Type> inputTypes,
                            IEnumerable<Type> outputTypes) {
    this.name = name;
    this.inputTypes = inputTypes;
    this.outputTypes = outputTypes;
  }

  public virtual Stack TypeMismatch(Stack stack, ICollection passedTypes, object o, Type consume) {
    // Put the good arguments back.
    stack = Interpreter.Append(passedTypes, stack);
    // foreach(var passed in passedTypes)
    //   stack.Push(passed);
    var code = new Stack();
    code.Push(o);
    code.Push(new Symbol(name));
    stack.Push(new Continuation(code));
    return stack;
  }

  public virtual Stack NotEnoughElements(Stack stack, Queue passedTypes) {
    foreach(object p in passedTypes)
      stack.Push(p);
    return stack;
  }

  public Stack Apply(Stack stack) {
    var passedTypes = new Queue();
    foreach(Type consume in consumes) {
      if (! stack.Any()) {
        // Not enough elements.
        return NotEnoughElements(stack, passedTypes);
      }

      object o = stack.Pop();
      var t = getType(o);
      // if (t == consume) {
      if (consume == typeof(Variable)) {
        // XXX What is going on here?
        passedTypes.Enqueue(o);
      } else if (consume.IsAssignableFrom(t)) {
        passedTypes.Enqueue(o);
      } else {
        return TypeMismatch(stack, passedTypes, o, consume);
      }
    }

    if (leaveReorderItems) {
      var code = new Stack();
      code.Push(new Symbol(name));
      foreach(var t in passedTypes)
        code.Push(t);
      stack.Push(new Defer(code, produces.FirstOrDefault()));
    }

    // Everything checks out. Add the types we produced.
    foreach(var produced in (leaveReorderItems ? produces.Skip(1) : produces)) {
      stack.Push(putType(produced));
    }

    return stack;
  }

  public override string ToString() {
    return "(" + string.Join(",", consumes.Select(t => t.PrettyName())) + ") -> "
      + "(" + string.Join(",", produces.Select(t => t.PrettyName())) + ")";
  }
}

}
