using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace SeawispHunter.PushForth {

// https://github.com/mcintyre321/OneOf/blob/1240b20094d25aa1af9d3e2c064f23a5ce372b11/OneOf.Tests/MixedReferenceAndValueTypeTests.cs
public class TypeOrVariable : OneOfBase<Type, Variable> {
  private TypeOrVariable() { }
  private TypeOrVariable(Type t) : base(0, value0 : t) { }
  private TypeOrVariable(Variable v) : base(1, value1 : v) { }
  public static implicit operator TypeOrVariable(Type t) {
    return new TypeOrVariable(t);
  }

  public static implicit operator TypeOrVariable(Variable v) {
    return new TypeOrVariable(v);
  }
}

public class TypeCheckInstruction2 : Instruction {
  public readonly IEnumerable<TypeOrVariable> consumes;
  public readonly IEnumerable<TypeOrVariable> produces;
  public readonly string name;
  public Func<object, Type> getType = o => o is IReprType d ? d.type : o.GetType();
  public Func<Type, object> putType = o => new Dummy(o);
  public bool leaveReorderItems = true;
  public Dictionary<string, Type> bindings = new Dictionary<string, Type>();

  public TypeCheckInstruction2(string name,
                               IEnumerable<TypeOrVariable> consumes,
                               IEnumerable<TypeOrVariable> produces) {
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
    foreach(var consume in consumes) {
      if (! stack.Any()) {
        // Not enough elements.
        return NotEnoughElements(stack, passedTypes);
      }

      object o = stack.Pop();
      var t = getType(o);
      // if (t == consume) {
      if (consume.TryPickT0(out Type type, out Variable v)) {
          if (type.IsAssignableFrom(t)) {
            passedTypes.Enqueue(o);
          } else {
            throw new Exception($"Type check instruction {name} expected type {type} but got {o}");
          }
      } else {
        // It's a variable.
        if (bindings.TryGetValue(v.name, out Type vtype)) {
          if (vtype.IsAssignableFrom(t)) {
            passedTypes.Enqueue(o);
          } else {
            throw new Exception($"Type check instruction {name} with variable {v.name} expected type {vtype} but got {o}");
          }
        } else {
          bindings[v.name] = t;
          passedTypes.Enqueue(o);
        }
      }
    }

    // Everything checks out. Add the types we produced.
    foreach(var produced in (leaveReorderItems ? produces.Skip(1) : produces)) {
      stack.Push(produced.Match(ptype => putType(ptype),
                                varx => bindings[varx.name]));
    }

    return stack;
  }

  public override string ToString() {
    return "(" + string.Join(",", consumes.Select(c => c.Match(t => t.PrettyName(), v => v.ToString()))) + ") -> "
      + "(" + string.Join(",", produces.Select(p => p.Match(t => t.PrettyName(), v => v.ToString()))) + ")";
  }
}
}
