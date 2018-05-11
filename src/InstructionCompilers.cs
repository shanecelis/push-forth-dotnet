using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace SeawispHunter.PushForth {
// public abstract class InstructionCompiler : Instruction {
//   public ILStack ilStack;
//   public abstract Stack Apply(Stack stack);
// }

public class InstructionCompiler : Instruction {

  Action<ILStack> action;
  int argCount;
  public InstructionCompiler(int argCount, Action<ILStack> action) {
    this.argCount = argCount;
    this.action = action;
  }

  public InstructionCompiler(MethodInfo methodInfo)
    : this(methodInfo.GetParameters().Length,
             ilStack => {
             ilStack.il.Emit(OpCodes.Call, methodInfo);
             for(int i = 0; i < methodInfo.GetParameters().Length; i++)
               ilStack.types.Pop();

             if (methodInfo.ReturnType != typeof(void)) {
               if (methodInfo.ReturnType.IsValueType)
                 ilStack.il.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType);
               ilStack.types.Push(methodInfo.ReturnType);
             }
           }) { }

  public Stack Apply(Stack stack) {
    var args = new Queue();
    // for(int i = 0; i < argCount; i++) {
    //   object a;
    //   a = stack.Pop();
    //   if (!(a is ILStackIndex))
    //     args.Enqueue(a);
    //     // ilStack.Push(a);
    // }
    Action<ILStack> b = (ILStack ilStack) => {
      if (ilStack.count < argCount)
        throw new Exception($"Need ${argCount} arguments but only ${ilStack.count} items on CLR stack.");
      // Add arguments.
      // foreach(object o in args)
      //   ilStack.Push(o);
      action(ilStack);
    };
    stack.Push(b);
    // action(stack, ilStack);
    return stack;
  }
}

public class AddInstructionCompiler : InstructionCompiler {
  public AddInstructionCompiler() : base(2,
      ilStack => {
        ilStack.il.Emit(OpCodes.Add);
        ilStack.types.Pop();
      }) { }
}

public class MathOpCompiler : InstructionCompiler {
  public MathOpCompiler(char op) : base(2,
    ilStack => {
      switch (op) {
        case '+':
        ilStack.il.Emit(OpCodes.Add);
        break;
        case '-':
        ilStack.il.Emit(OpCodes.Sub);
        break;
        case '*':
        ilStack.il.Emit(OpCodes.Mul);
        break;
        case '/':
        ilStack.il.Emit(OpCodes.Div);
        break;
        default:
        throw new Exception("No math operation for " + op);
      }
      ilStack.types.Pop();
    }) { }
}

// public class Compiled: Tuple<Expression> {
//   public Compiled(Expression s) : base(s) { }

//   public Expression expression => Item1;
// }

// public class BinaryInstructionCompiler<X, Y> : Instruction {
//   Action<Stack, X, Y> func;
//   public BinaryInstructionCompiler(Action<Stack, X, Y> func) {
//     this.func = func;
//   }

//   public Stack Apply(Stack stack) {
//     object a, b;
//     a = stack.Pop();
//     b = stack.Pop();
//     func(stack, (X) a, (Y) b);
//     // Expression.Invoke(func, Compile(stack), Compile(a), Compile(b));
//     return stack;
//   }

//   public static Expression Compile(object x) {
//     if (x is Expression e)
//       return e;
//     else
//       return Expression.Constant(x);
//   }

//   public static Instruction WithResult<Z>(Func <X,Y,Z> func) {
//     return new InstructionFunc(stack =>
//         {
//           Expression<Func<Stack, Stack>> f = (_stack) => {
//             object a, b;
//             a = _stack.Pop();
//             b = _stack.Pop();
//             _stack.Push(func((X) a, (Y) b));
//             return _stack;
//           };
//           stack.Push(f);
//           return stack;
//         });
//   }
// }

}
