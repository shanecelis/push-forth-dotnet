using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeawispHunter.PushForth {

public class TypeInterpreter : StrictInterpreter {

  public TypeInterpreter() {
    this.instructionFactory
      = StrictInstruction.factory.Compose(i => (TypedInstruction) new DetermineTypesInstruction(i));
  }

  /** Take the output from a bunch of DetermineTypeInstructions and create a
      list of what the program consumes and what it produces.
  */
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
}
}
