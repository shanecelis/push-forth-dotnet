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
    if (name != null)
      code.Push(new Symbol(name));
    else
      code.Push(this);
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


  class ReorderInstructionFactory : FuncFactory<TypedInstruction> {
    public FuncFactory<TypedInstruction> innerFactory;
    public ReorderInstructionFactory(FuncFactory<TypedInstruction> innerFactory) {
      this.innerFactory = innerFactory;
    }
    public TypedInstruction Nullary<X>(Func <X> func) {
      return new ReorderWrapper(null, innerFactory.Nullary(func));
    }

    public TypedInstruction Unary<X,Y>(Func <X,Y> func) {
      return new ReorderWrapper(null, innerFactory.Unary(func));
    }

    public TypedInstruction Binary<X,Y,Z>(Func <X,Y,Z> func) {
      return new ReorderWrapper(null, innerFactory.Binary(func));
    }
    public TypedInstruction Trinary<X,Y,Z,W>(Func <X,Y,Z,W> func) {
      return new ReorderWrapper(null, innerFactory.Trinary(func));
    }

    public TypedInstruction Nullary(Action func) {
      return new ReorderWrapper(null, innerFactory.Nullary(func));
    }

    public TypedInstruction Unary<X>(Action<X> func) {
      return new ReorderWrapper(null, innerFactory.Unary(func));
    }

    public TypedInstruction Binary<X,Y>(Action<X,Y> func) {
      return new ReorderWrapper(null, innerFactory.Binary(func));
    }
    public TypedInstruction Trinary<X,Y,Z>(Action<X,Y,Z> func) {
      return new ReorderWrapper(null, innerFactory.Trinary(func));
    }

    public TypedInstruction Nullary(Action<Stack> func) {
      return new ReorderWrapper(null, innerFactory.Nullary(func));
    }

    public TypedInstruction Unary<X>(Action<Stack,X> func) {
      return new ReorderWrapper(null, innerFactory.Unary(func));
    }

    public TypedInstruction Binary<X,Y>(Action<Stack,X,Y> func) {
      return new ReorderWrapper(null, innerFactory.Binary(func));
    }
    public TypedInstruction Trinary<X,Y,Z>(Action<Stack,X,Y,Z> func) {
      return new ReorderWrapper(null, innerFactory.Trinary(func));
    }
  }

  public static FuncFactory<TypedInstruction> GetFactory(FuncFactory<TypedInstruction> innerFactory) {
    return new ReorderInstructionFactory(innerFactory);
  }
}

}
