using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using OneOf;

namespace SeawispHunter.PushForth {

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

/*
  This isn't just type checking. It determines the types of a given program.
 */
public class DetermineTypesInstruction : TypedInstruction {
  public IEnumerable<Type> inputTypes => consumes;
  public IEnumerable<Type> outputTypes => produces;
  public readonly IEnumerable<Type> consumes;
  public readonly IEnumerable<Type> produces;
  public Func<object, Type> getType = o => o is IReprType d ? d.type : o.GetType();

  public DetermineTypesInstruction(IEnumerable<Type> consumes,
                                   IEnumerable<Type> produces) {
    // this.name = name;
    this.consumes = consumes;
    this.produces = produces;
  }

  public DetermineTypesInstruction(string consumes,
                                   string produces)
    : this(StackParser.ParseTypeSignature3(consumes),
           StackParser.ParseTypeSignature3(produces)) { }

  public DetermineTypesInstruction(TypedInstruction instruction)
    : this(instruction.inputTypes, instruction.outputTypes) { }

  public Stack Apply(Stack stack) {
    // var consumeStack = (Stack) stack.Pop();
    var consumeStack = new Stack();
    // Let's make the produceStack implicit.
    // var produceStack = (Stack) stack.Pop();
    var produceStack = stack;
    // XXX I don't really do anything with passedTypes anymore.
    var passedTypes = new Queue();
    var uniqVars = new Dictionary<Variable, Variable>();
    var bindings = new Dictionary<string, object>();

    foreach(Type consume in consumes) {
      if (produceStack.Any()) {
        object o = produceStack.Pop();
        var t = getType(o);
        if (! Variable.IsVariableType(consume)) {
        // if (consume.TryPickT0(out Type type, out Variable v)) {
        // if (consume is Type type) {
          var type = consume;
          if (type.IsAssignableFrom(t)) {
            passedTypes.Enqueue(o);
          } else if (o is Variable w) {
            var w2 = uniqVars.GetOrCreate(w, x => x.MakeUnique());
            bindings.Add(w2.name, type);
            // stack.Push(new Dictionary<string, object>() { { w2.name, type } });
          } else {
            throw new Exception($"Type check instruction expected type {type} but got {o}");
          }
        // } else if (consume is Variable v) {
        } else {

          var v = Variable.Instantiate(consume);
            // var v = (Variable) consume;
          // It's a variable.
            var v2 = uniqVars.GetOrCreate(v, x => x.MakeUnique());
            // stack.Push(new Dictionary<string, object>() { { v2.name, t } });
            bindings.Add(v2.name, t);
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

        if (! Variable.IsVariableType(consume)) {
        // if (consume.TryPickT0(out Type type, out Variable v))
          var type = consume;
          consumeStack.Push(type);
        } else {
          var v = Variable.Instantiate(consume);
          var v2 = uniqVars.GetOrCreate(v, x => x.MakeUnique());
          consumeStack.Push(v2);
        }
      }
    }
    if (bindings.Any())
      stack.Push(bindings);
    if (consumeStack.Any())
      stack.Push(consumeStack);
    // Everything checks out. Add the types we produced.
    // foreach(var produced in (leaveReorderItems ? produces.Skip(1) : produces)) {
    foreach(Type produced in produces) {
      if (! Variable.IsVariableType(produced)) {
      // if (produced.TryPickT0(out Type type, out Variable v)) {
        var type = produced;
      // if (produced is Type ptype)
        produceStack.Push(type);
      // else if (produced is Variable varx) {
      } else {
        var v = Variable.Instantiate(produced);
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
