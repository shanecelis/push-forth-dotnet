using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeawispHunter.PushForth {

public class ReorderInterpreter : StrictInterpreter {

  DeferInstruction di = null;
  public ReorderInterpreter() {
    // XXX This weird data piping is because an Instruction doesn't and probably shouldn't know its own name.
    this.instructionFactory = ReorderWrapper.GetFactory(StrictInstruction.factory.Compose(i => {
          di = new DeferInstruction(null, i);
          return (TypedInstruction) di;
        }));
    isStrict = false;
    LoadInstructions();

  }

  public override void LoadInstructions() {
    base.LoadInstructions();
    // Damn, the reorder is more complicated than the thing in itself.
    instructions["pop"] = new InstructionFunc(stack => {
          var o = stack.Pop();
          var t = stack.Any() ? stack.Peek().GetType() : null;
          var s = new Stack();
          s.Push(new Symbol("pop"));
          s.Push(o);
          stack.Push(new Defer(s, t));
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
    return RunReorderPost(Run(stack));
  }

  // public Stack ReorderPre(Stack stack) {
  //   return Eval(stack, new [] { reorderInstructions });
  // }

  public static bool IsReorderPostDone(Stack stack) {
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
