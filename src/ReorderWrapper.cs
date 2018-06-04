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

/** Wraps an instruction such that mistyped arguments are _reordered_ and lack
    of arguments causes noops.
 */
public class ReorderWrapper : TypedInstruction {
  public IEnumerable<Type> inputTypes { get; set; }
  public IEnumerable<Type> outputTypes { get; set; }
  public readonly string name;
  public Func<object, Type> getType = o => o is IReprType d ? d.type : (o != null ? o.GetType() : typeof(object));
  public Func<Type, object> putType = o => new Dummy(o);
  protected Instruction innerInstruction;
  protected Dictionary<string, Type> lastBinding = null;
  // public bool updateVariableBindings = false;
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

  public virtual Stack Apply(Stack stack) {
    var acceptedArguments = new Stack();
    var binding = lastBinding = new Dictionary<string, Type>();
    foreach(Type consume in inputTypes.Reverse()) {
      if (! stack.Any()) {
        // Not enough elements.
        return NotEnoughElements(stack, acceptedArguments);
      }

      object o = stack.Pop();
      var t = getType(o);
      Type consumeType = consume;
      // if (t == consume) {
      if (Variable.IsVariableType(consume)) {
        // XXX What is going on here?
        // We accept it. But we should track that it now means this type
        // and fail it it changes.
        var v = Variable.Instantiate(consume);
        if (binding.TryGetValue(v.name, out Type vType)) {
          consumeType = vType;
        } else {
          if (t == null)
            throw new Exception($"Got null for type from object {o}.");

          binding.Add(v.name, t);
          consumeType = t;
        }
      }
      if (consumeType == null)
        Console.WriteLine($"consumeType is null for {consume} and object {o} with type {t} has bindings {binding.ToRepr()}");
      // if (consumeType == null || consumeType.IsAssignableFrom(t)) {
      if (consumeType.IsAssignableFrom(t)) {
        acceptedArguments.Push(o);
      } else {
        return TypeMismatch(stack, acceptedArguments, o, consumeType);
      }
    }

    // Push the good arguments back onto the stack.
    foreach(var arg in acceptedArguments)
      stack.Push(arg);
    // Then run the real instruction.
    if (innerInstruction is DeferInstruction di)
      stack = di.ApplyWithBindings(stack, binding);
    else
      stack = innerInstruction.Apply(stack);
    // if (updateVariableBindings && stack.Any()) {
    //   var temp = new Stack();
    //   while (stack.Peek() is Defer r) {
    //     if (Variable.IsVariableType(r.type)) {
    //       var v = Variable.Instantiate(r.type);
    //       r.type = binding[v.name];
    //     }
    //     temp.Push(stack.Pop());
    //   }
    //   while (temp.Any())
    //     stack.Push(temp.Pop());
    // }
    return stack;
  }

  public override string ToString() {
    return "[" + string.Join(" ", inputTypes.Select(t => t.PrettyName())) + "] -> "
      + "[" + string.Join(" ", outputTypes.Select(t => t.PrettyName())) + "]";
  }

  public static FuncFactory<TypedInstruction> GetFactory(FuncFactory<TypedInstruction> innerFactory) {
    return innerFactory.Compose(i => new ReorderWrapper(null, i));
    // return new ReorderInstructionFactory(innerFactory);
  }
}

}
