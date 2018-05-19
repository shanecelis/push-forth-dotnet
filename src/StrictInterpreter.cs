using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SeawispHunter.PushForth {

public class StrictInterpreter : Interpreter {

  protected bool isStrict = true;

  public override void LoadInstructions() {
    base.LoadInstructions();

    AddInstruction("i", (Stack stack, Stack code) => {
        stack.Push(new Continuation(code));
      });
    AddInstruction("car", (Stack stack, Stack s) => {
        if (isStrict || s.Any())
          stack.Push(s.Pop());
      });
    AddInstruction("eval", (Stack stack) => {
        return Eval(stack);
      });
    AddInstruction("!", (Stack stack, Symbol s, object x) => {
        AddInstruction(s.name, () => x);
      });
    AddInstruction("if", (Stack stack, bool condition, Stack consequent, Stack otherwise)=> {
        if (condition)
          stack.Push(new Continuation(consequent));
        else
          stack.Push(new Continuation(otherwise));
      });
    // How can I get a generic function.  I want something like this:
    // AddInsuruction<T>("if2", (Stack stack, bool condition, T consequent, T otherwise)=> {
    //     if (condition)
    //       stack.Push(new Continuation(consequent));
    //     else
    //       stack.Push(new Continuation(otherwise));
    //   });

    // Needed this to track down a bug.
    AddInstruction("!int", (Stack stack, Symbol s, int x) => {
        AddInstruction(s.name, () => x);
      });
    AddInstruction("cdr",(Stack stack) => {
        if (isStrict || stack.Any())
          stack.Pop();
        return stack;
      });
    instructions["pop"] = instructionFactory.Operation(stack => {
        if (isStrict || stack.Any())
          stack.Pop();
      }, new Type[] { typeof(Variable.A) }, Type.EmptyTypes);
    instructions["dup"] = instructionFactory.Operation(stack => {
        if (isStrict || stack.Any())
          stack.Push(stack.Peek().Duplicate());
      },
      new [] { typeof(Variable.A) },
      new [] { typeof(Variable.A), typeof(Variable.A) });
    instructions["swap"] = instructionFactory.Operation(stack => {
        if (isStrict || stack.Count >= 2) {
          var a = stack.Pop();
          var b = stack.Pop();
          stack.Push(a);
          stack.Push(b);
        }
      },
      new [] { typeof(Variable.A), typeof(Variable.B) },
      new [] { typeof(Variable.B), typeof(Variable.A) });
    AddInstruction("cons", (object a, Stack b) => Cons(a, b));
    AddInstruction("cat", (object a, object b) => {
        var s = new Stack();
        s.Push(b);
        s.Push(a);
        return s;
      });
    AddInstruction("split", (Stack stack, Stack s) => {
        stack = Append(s, stack);
      });
    AddInstruction("unit", (object a) => {
        var s = new Stack();
        s.Push(a);
        return s;
      });

    AddInstruction("minus", (int a, int b) => a - b);
    AddInstruction("-", (int a, int b) => a - b);
    AddInstruction("+", (int a, int b) => a + b);
    AddInstruction("negate", (int a) => -a);
    // instructions["while"] = new InstructionFunc(stack => {
    //     if (stack.Count >= 3) {
    //       object x = stack.Pop();
    //       object y = stack.Pop();
    //       object z = stack.Pop();
    //       if (z is Stack Z && ! Z.Any()) {
    //         stack.Push(y);
    //       } else {
    //         var code = new Stack();
    //         // code.Push(instructions["i"]);
    //         code.Push(new Symbol("i"));
    //         var subcode = new Stack();
    //         // subcode.Push(instructions["while"]);
    //         subcode.Push(new Symbol("while"));
    //         subcode.Push(x);
    //         code.Push(subcode);
    //         code.Push(x);
    //         stack.Push(new Continuation(code));
    //       }
    //     }
    //     return stack;
    //   });
    AddInstruction("while", (Stack stack, Stack x, Stack z) => {
        if (z.Any()) {
          var code = new Stack();
          code.Push(instructions["i"]);
          // code.Push(new Symbol("i"));
          var subcode = new Stack();
          subcode.Push(instructions["while"]);
          // subcode.Push(new Symbol("while"));
          subcode.Push(x);
          code.Push(subcode);
          code = Append(x, code);
          // code.Push(x);
          stack.Push(z);
          stack.Push(new Continuation(code));
        }
      });

    AddInstruction("==", (int a, int b) => a == b);
    AddInstruction("<", (int a, int b) => a < b);
    AddInstruction(">", (int a, int b) => a > b);
    AddInstruction("while2", (Stack stack, Stack x, bool z, object y) => {
        if (! z) {
          stack.Push(y);
        } else {
          var code = new Stack();
          code.Push(instructions["i"]);
          // code.Push(new Symbol("i"));
          var subcode = new Stack();
          subcode.Push(instructions["while2"]);
          // subcode.Push(new Symbol("while"));
          subcode.Push(x);
          code.Push(subcode);
          code = Append(x, code);
          // code.Push(x);
          stack.Push(y);
          stack.Push(new Continuation(code));
        }
      });

    AddInstruction("while3", (Stack stack, Stack x, bool z) => {
        // Let's do it again but with no code re-writing to make it compilable.
        while (z) {
          // Must make a copy of the code x, as the Stack is destroyed when
          // it is run.

          // stack.Push(x);
          stack.Push(Append(x, new Stack()));
          stack = Run(stack);
          object code = stack.Pop(); // drop empty code stack.
          if (code is Stack s) {
            if (s.Any()) {
              if (isStrict)
                throw new Exception("Code stack not empty.");
              Console.WriteLine("Code stack had stuff in it.");
              break;
            }
          } else {
            Console.WriteLine("Got non-stack for code");
            break;
          }
          object Z = stack.Pop();
          if (Z is bool zb)
            z = zb;
          else {
            if (isStrict)
              throw new Exception("No boolean on top of stack for while.");
            Console.WriteLine("Got non-bool for z " + Z);
            z = false;
          }
          // z = (bool) stack.Pop();
        }
      });

    AddInstruction("while4", (Stack stack, Stack x) => {
        // Let's do it again but with no code re-writing to make it compilable.
        bool z;
        do {
          // Must make a copy of the code x, as the Stack is destroyed when
          // it is run.

          // stack.Push(x);
          // This is a shallow copy.
          stack.Push(Append(x, new Stack()));
          stack = Run(stack);
          object code = stack.Pop(); // drop empty code stack.
          if (code is Stack s) {
            if (s.Any()) {
              if (isStrict)
                throw new Exception("Code stack not empty.");
              Console.WriteLine("Code stack had stuff in it.");
              break;
            }
          } else {
            Console.WriteLine("Got non-stack for code");
            break;
          }
          object Z = stack.Pop();
          if (Z is bool zb)
            z = zb;
          else {
            if (isStrict)
              throw new Exception("No boolean on top of stack for while.");
            Console.WriteLine("Got non-bool for z " + Z);
            z = false;
          }
        } while (z);
      });
  }
}

public class NonstrictInterpreter : StrictInterpreter {

  public NonstrictInterpreter() {
    this.instructionFactory = ReorderWrapper.GetFactory(StrictInstruction.factory);
    isStrict = false;
  }
}


}
