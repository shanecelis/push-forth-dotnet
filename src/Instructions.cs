using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace SeawispHunter.PushForth {

using TypeOrVar = OneOf<Type, Variable>;

public interface Instruction {
  Stack Apply(Stack stack);
}

public interface TypedInstruction : Instruction {
  IEnumerable<TypeOrVar> inputTypes { get; }
  IEnumerable<TypeOrVar> outputTypes { get; }
  // IEnumerable<OneOf<Type, Variable>> inputTypes { get; }
  // IEnumerable<OneOf<Type, Variable>> outputTypes { get; }
  // IEnumerable inputTypes { get; }
  // IEnumerable outputTypes { get; }
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

public class NullaryInstruction : Instruction {
  Action<Stack> func;
  public NullaryInstruction(Action<Stack> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    func(stack);
    return stack;
  }

  public static NullaryInstruction WithResult<X>(Func <X> func) {
    return new NullaryInstruction((stack) => {
        stack.Push(func());
      });
  }

  public static NullaryInstruction Reorder<X>(string name) {
    var ins = new NullaryInstruction((stack) => {
        var s = new Stack();
        s.Push(new Symbol(name));
        stack.Push(new Defer(s, typeof(X)));
      });
    return ins;
  }
}

public class UnaryInstruction<X> : Instruction {
  Action<Stack, object> func;
  Func<object, bool> predicateX = x => x is X;

  public UnaryInstruction(Action<Stack, X> func)
    : this(true, (stack, x) => func(stack, (X) x)) {
  }
  public UnaryInstruction(bool dummy, Action<Stack, object> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 1)
      return stack;
    object a;
    a = stack.Pop();
    if (! predicateX(a)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(new Continuation(code));
      return stack;
    }
    func(stack, a);
    return stack;
  }

  public static UnaryInstruction<X> WithResult<Y>(Func <X,Y> func) {
    return new UnaryInstruction<X>((stack, a) => {
          stack.Push(func(a));
      });
  }

  public static UnaryInstruction<X> Reorder<Z>(string name) {
    var ins = new UnaryInstruction<X>(true, (stack, a) => {
        var s = new Stack();
        s.Push(new Symbol(name));
        s.Push(a);
        stack.Push(new Defer(s, typeof(Z)));
      });
    ins.predicateX = x => x is X || (x is Defer r && r.type == typeof(X));
    return ins;
  }
}

public class BinaryInstruction<X, Y> : Instruction {
  Action<Stack, object, object> func;
  Func<object, bool> predicateX = x => x is X;
  Func<object, bool> predicateY = y => y is Y;
  public BinaryInstruction(Action<Stack, X, Y> func)
    : this(true, (stack, a, b) => func(stack, (X) a, (Y) b)) {
  }

  BinaryInstruction(bool dummy, Action<Stack, object, object> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 2)
      return stack;
    object a, b;
    a = stack.Pop();
    if (! predicateX(a)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(new Continuation(code));
      return stack;
    }
    b = stack.Pop();
    if (! predicateY(b)) {
      var code = new Stack();
      code.Push(b);
      code.Push(this);
      stack.Push(a);
      stack.Push(new Continuation(code));
      return stack;
    }
    func(stack, a, b);
    return stack;
  }

  public static BinaryInstruction<X,Y> WithResult<Z>(Func <X,Y,Z> func) {
    return new BinaryInstruction<X,Y>((stack, a, b) => {
          stack.Push(func(a, b));
      });
  }

  public static BinaryInstruction<X,Y> Reorder(string name, Type t) {
    var ins = new BinaryInstruction<X,Y>(true, (stack, a, b) => {
        var s = new Stack();
        s.Push(new Symbol(name));
        s.Push(a);
        s.Push(b);
        stack.Push(new Defer(s, t));
      });
    ins.predicateX = x => x is X || (x is Defer r && r.type == typeof(X));
    ins.predicateY = y => y is Y || (y is Defer r && r.type == typeof(Y));
    return ins;
  }
}

public class TypeCheckInstruction : ReorderInstruction {

  public TypeCheckInstruction(string name,
                              IEnumerable<Type> consumes,
                              IEnumerable<Type> produces)
    : base(name, consumes, produces)
  { }

  public override Stack TypeMismatch(Stack stack, ICollection passedTypes, object o, Type consume) {
    throw new Exception($"Type check instruction {name} expected type {consume} but got {o}");
  }
}

public class ReorderInstruction : Instruction {
  public IEnumerable<Type> consumes;
  public IEnumerable<Type> produces;
  public readonly string name;
  public Func<object, Type> getType = o => o is IReprType d ? d.type : o.GetType();
  public Func<Type, object> putType = o => new Dummy(o);
  public bool leaveReorderItems = true;

  public ReorderInstruction(string name,
                            IEnumerable<Type> consumes,
                            IEnumerable<Type> produces) {
    this.name = name;
    this.consumes = consumes;
    this.produces = produces;
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

public class TrinaryInstruction<X, Y, Z> : Instruction {
  Action<Stack, object, object, object> func;
  Func<object, bool> predicateX = x => x is X;
  Func<object, bool> predicateY = y => y is Y;
  Func<object, bool> predicateZ = z => z is Z;
  public TrinaryInstruction(Action<Stack, X, Y, Z> func)
    : this(true, (stack, a, b, c) => func(stack, (X) a, (Y) b, (Z) c)){
  }

  TrinaryInstruction(bool dummy, Action<Stack, object, object, object> func) {
    this.func = func;
  }

  public Stack Apply(Stack stack) {
    if (stack.Count < 3)
      return stack;
    object a, b, c;
    a = stack.Pop();
    if (! predicateX(a)) {
      var code = new Stack();
      code.Push(a);
      code.Push(this);
      stack.Push(new Continuation(code));
      return stack;
    }
    b = stack.Pop();
    if (! predicateY(b)) {
      var code = new Stack();
      code.Push(b);
      code.Push(this);
      stack.Push(a);
      stack.Push(new Continuation(code));
      return stack;
    }
    c = stack.Pop();
    if (! predicateZ(c)) {
      var code = new Stack();
      code.Push(c);
      code.Push(this);
      stack.Push(b);
      stack.Push(a);
      stack.Push(new Continuation(code));
      return stack;
    }
    func(stack, a, b, c);
    return stack;
  }

  public static TrinaryInstruction<X, Y, Z> WithResult<W>(Func<X, Y, Z, W> func) {
    return new TrinaryInstruction<X, Y, Z>((stack, a, b, c) => {
        stack.Push(func(a, b, c));
      });
  }

  public static TrinaryInstruction<X,Y,Z> Reorder<W>(string name) {
    var ins = new TrinaryInstruction<X,Y,Z>(true, (stack, a, b, c) => {
        var s = new Stack();
        s.Push(new Symbol(name));
        s.Push(a);
        s.Push(b);
        s.Push(c);
        stack.Push(new Defer(s, typeof(W)));
      });
    ins.predicateX = x => x is X || (x is Defer r && r.type == typeof(X));
    ins.predicateY = y => y is Y || (y is Defer r && r.type == typeof(Y));
    ins.predicateZ = z => z is Z || (z is Defer r && r.type == typeof(Z));
    return ins;
  }
}

}
