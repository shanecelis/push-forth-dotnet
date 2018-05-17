using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace SeawispHunter.PushForth {

  using TypeOrVar = OneOf<Type, Variable>;

// https://github.com/mcintyre321/OneOf/blob/1240b20094d25aa1af9d3e2c064f23a5ce372b11/OneOf.Tests/MixedReferenceAndValueTypeTests.cs
// public class TypeOrVariable : OneOfBase<Type, Variable> {
//   private TypeOrVariable() { }
//   private TypeOrVariable(Type t) : base(0, value0 : t) { }
//   private TypeOrVariable(Variable v) : base(1, value1 : v) { }
//   public static implicit operator TypeOrVariable(Type t) {
//     return new TypeOrVariable(t);
//   }
//   public static implicit operator TypeOrVariable(Variable v) {
//     return new TypeOrVariable(v);
//   }
// }

public class TypeCheckInstruction2 : Instruction {
  public readonly IEnumerable consumes;
  public readonly IEnumerable produces;
  public readonly string name;
  public Func<object, Type> getType = o => o is IReprType d ? d.type : o.GetType();
  public Func<Type, object> putType = o => new Dummy(o);
  public bool leaveReorderItems = true;
  public Dictionary<string, Type> bindings = new Dictionary<string, Type>();

  public TypeCheckInstruction2(string name,
                               IEnumerable consumes,
                               IEnumerable produces) {
    this.name = name;
    this.consumes = consumes;
    this.produces = produces;
  }

  public TypeCheckInstruction2(string name,
                               string consumes,
                               string produces)
    : this(name,
           StackParser.ParseTypeSignature(consumes),
           StackParser.ParseTypeSignature(produces)) { }

  public virtual Stack NotEnoughElements(Stack stack, Queue passedTypes) {
    foreach(object p in passedTypes)
      stack.Push(p);
    return stack;
  }

  public Stack Apply(Stack stack) {
    // var consumeStack = (Stack) stack.Pop()
    //   var produceStack = (Stack) stack.Pop()
    var passedTypes = new Queue();
    foreach(var consume in consumes) {
      if (! stack.Any()) {
        // Not enough elements.
        return NotEnoughElements(stack, passedTypes);
      }

      object o = stack.Pop();
      var t = getType(o);
      if (consume is Type type) {
        if (type.IsAssignableFrom(t)) {
          passedTypes.Enqueue(o);
        } else {
          throw new Exception($"Type check instruction {name} expected type {type} but got {o}");
        }
      } else if (consume is Variable v) {
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
      } else {
        throw new Exception($"Expected Type or Variable not {consume} with type {consume.GetType().PrettyName()}.");
      }
    }

    // Everything checks out. Add the types we produced.
    // foreach(var produced in (leaveReorderItems ? produces.Skip(1) : produces)) {
    foreach(var produced in produces) {
      if (produced is Type ptype)
        stack.Push(putType(ptype));
      else if (produced is Variable varx)
        stack.Push(bindings[varx.name]);
    }

    return stack;
  }
  private static string CellToString(object o) {
    if (o is Type t)
      return t.PrettyName();
    else
      return o.ToString();
  }

  public override string ToString() {
    return "(" + string.Join(",", consumes.Cast<object>().Select(CellToString)) + ") -> "
      + "(" + string.Join(",", produces.Cast<object>().Select(CellToString)) + ")";
  }
}

public class TypeCheckInstruction3 : TypedInstruction {
  public IEnumerable<TypeOrVar> inputTypes => consumes;
  public IEnumerable<TypeOrVar> outputTypes => produces;
  public readonly IEnumerable<TypeOrVar> consumes;
  public readonly IEnumerable<TypeOrVar> produces;
  public readonly string name;
  public Func<object, Type> getType = o => o is IReprType d ? d.type : o.GetType();
  // public Dictionary<string, Type> bindings = new Dictionary<string, Type>();

  public TypeCheckInstruction3(string name,
                               IEnumerable<TypeOrVar> consumes,
                               IEnumerable<TypeOrVar> produces) {
    this.name = name;
    this.consumes = consumes;
    this.produces = produces;
  }

  public TypeCheckInstruction3(string name,
                               string consumes,
                               string produces)
    : this(name,
           StackParser.ParseTypeSignature2(consumes),
           StackParser.ParseTypeSignature2(produces)) { }

  public Stack Apply(Stack stack) {
    // var consumeStack = (Stack) stack.Pop();
    // Let's make the produceStack implicit.
    // var produceStack = (Stack) stack.Pop();
    var consumeStack = new Stack();
    var produceStack = stack;
    var passedTypes = new Queue();

    var uniqVars = new Dictionary<Variable, Variable>();

    foreach(var consume in consumes) {
      if (produceStack.Any()) {
        object o = produceStack.Pop();
        var t = getType(o);
        if (consume.TryPickT0(out Type type, out Variable v)) {
        // if (consume is Type type) {
          if (type.IsAssignableFrom(t)) {
            passedTypes.Enqueue(o);
          } else if (o is Variable w) {
            var w2 = uniqVars.GetOrCreate(w, x => x.MakeUnique());
            stack.Push(new Dictionary<string, object>() { { w2.name, type } });
          } else {
            throw new Exception($"Type check instruction {name} expected type {type} but got {o}");
          }
        // } else if (consume is Variable v) {
          } else {
          // It's a variable.
            var v2 = uniqVars.GetOrCreate(v, x => x.MakeUnique());
            stack.Push(new Dictionary<string, object>() { { v2.name, t } });
          // if (bindings.TryGetValue(v.name, out Type vtype)) {
          //   if (vtype.IsAssignableFrom(t)) {
          //     passedTypes.Enqueue(o);
          //   } else {
          //     throw new Exception($"Type check instruction {name} with variable {v.name} expected type {vtype} but got {o}");
          //   }
          // } else {
          //   bindings[v.name] = t;
          //   passedTypes.Enqueue(o);
          // }
        }
        //   else {
        //   throw new Exception($"Expected Type or Variable not {consume} with type {consume.GetType().PrettyName()}.");
        // } 
      } else {
        if (consume.TryPickT0(out Type type, out Variable v))
          consumeStack.Push(type);
        else {
          var v2 = uniqVars.GetOrCreate(v, x => x.MakeUnique());
          consumeStack.Push(v2);
        }
      }
    }
    if (consumeStack.Any())
      stack.Push(consumeStack);
    // Everything checks out. Add the types we produced.
    // foreach(var produced in (leaveReorderItems ? produces.Skip(1) : produces)) {
    foreach(var produced in produces) {
      if (produced.TryPickT0(out Type type, out Variable v)) {
      // if (produced is Type ptype)
        produceStack.Push(type);
      // else if (produced is Variable varx) {
      } else {
        // if (bindings.TryGetValue(var.name, out Type vtype))
        //   produceStack.Push(vtype);
        // else
        var v2 = uniqVars.GetOrCreate(v, x => x.MakeUnique());
        produceStack.Push(v2);
      }
    }
    // stack.Push(produceStack);
    return stack;
  }

  public static (Stack, Stack) ConsumesAndProduces(Stack stack) {
    var consumeStack = new Stack();
    var producesStack = new Stack();
    var bindings = new Dictionary<string, object>();
    while (stack.Any()) {
      object o = stack.Pop();
      if (o is Stack consumes) {
        consumeStack = Interpreter.Append(consumes, consumeStack);
      } else if (o is Dictionary<string, object> d) {
        foreach(var kv in d)
          bindings.Add(kv.Key, kv.Value);
      } else {
        producesStack.Push(o);
      }
    }
    consumeStack = ((IEnumerable) Unifier.Substitute(bindings, consumeStack)).ToStack();
    producesStack = ((IEnumerable) Unifier.Substitute(bindings, producesStack)).ToStack();
    return (consumeStack, new Stack(producesStack));
  }

  private static string CellToString(object o) {
    if (o is Type t)
      return t.PrettyName();
    else
      return o.ToString();
  }

  public override string ToString() {
    return "(" + string.Join(",", consumes.Cast<object>().Select(CellToString)) + ") -> "
      + "(" + string.Join(",", produces.Cast<object>().Select(CellToString)) + ")";
  }
}
}
