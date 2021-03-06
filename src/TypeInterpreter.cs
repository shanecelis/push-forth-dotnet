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

namespace PushForth {

public class TypeInterpreter : StrictInterpreter {

  public TypeInterpreter(bool useNakedTypes = true) {
    if (useNakedTypes) {
      this.instructionFactory
        = StrictInstruction.factory
        .Compose(i => (TypedInstruction) new DetermineTypesInstruction(i)
            { getType = o => o is Type t ? t : o.GetType() });
    } else {
    this.instructionFactory
      = StrictInstruction.factory
      .Compose(i => (TypedInstruction) new DetermineTypesInstruction(i));
    }
  }

  /** Take the output from a bunch of DetermineTypeInstructions and create a
      list of what the program consumes and what it produces.
  */
  public static Tuple<Stack, Stack> ConsumesAndProduces(Stack stack) {
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
    return Tuple.Create(consumeStack, new Stack(producesStack));
  }
}
}
