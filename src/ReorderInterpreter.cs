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
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PushForth {

// Doing some type sentinels here. This represents the type for an empty stack.
// Ugh.
public class EmptyStack { }

public class ReorderInterpreter : StrictInterpreter {

  DeferInstruction di = null;
  public ReorderInterpreter() {
    // XXX This weird data piping is because an Instruction doesn't and probably shouldn't know its own name.
    this.instructionFactory = StrictInstruction.factory
      .Compose(i => {
          di = new DeferInstruction(null, i);
          return (TypedInstruction) di;
        })
      .Compose(i => new ReorderWrapper(null, i));
    isStrict = false;
    // Let this be lazy, why don't cha?
    LoadInstructions();
  }

  public override void LoadInstructions() {
    base.LoadInstructions();
    // Damn, the reorder is more complicated than the thing in itself.
    instructions["pop"] = new InstructionFunc(stack => {
        if (stack.Any()) {
          var o = stack.Pop();
          var t = stack.Any() ? stack.Peek().GetType() : typeof(EmptyStack);
          var s = new Stack();
          s.Push(new Symbol("pop"));
          s.Push(o);
          stack.Push(new Defer(s, t));
        }
      });
  }

  public override void AddInstruction(string name, Instruction i) {
    // if (i is DeferInstruction di)
    //   di.name = name;
    base.AddInstruction(name, i);
    if (di != null)
      di.name = name;
    di = null;
  }

  public Stack Reorder(Stack stack) {
    var str = stack.ToRepr();
    try {
      return RunReorderPost(Run(stack));
    } catch (Exception e) {
      throw new Exception($"Failed to reorder stack {str}.", e);
    }
  }

  // public Stack ReorderPre(Stack stack) {
  //   return Eval(stack, new [] { reorderInstructions });
  // }

  public static bool IsReorderPostDone(Stack stack) {
    if (! stack.Any())
      return true;
    var code = (Stack) stack.Pop();
    var data = stack;
    bool done = ! data.Any();
    stack.Push(code);
    return done;
  }

  // public Stack RunReorderPre(Stack stack) {
  //   return Run(stack, IsHalted, ReorderPre);
  // }

  public static Stack RunReorderPost(Stack stack) {
    return Run(stack, IsReorderPostDone, ReorderPost);
  }

  public static Stack ReorderPost(Stack stack) {
    var code = (Stack) stack.Pop();
    var data = stack;
    if (data.Any()) {
      object o = data.Pop();
      if (o is Defer r) {
        // Recurse.
        var s = new Stack(r.stack); // This reverses the stack.
        s.Push(new Stack());
        s = RunReorderPost(s);
        var newCode = (Stack) s.Pop();
        code = Append(newCode, code);
      } else if (o is Dummy d) {
        // We just drop it.
      } else {
        code = Cons(o, code);
      }
    }
    return Cons(code, data);
  }
}

}
