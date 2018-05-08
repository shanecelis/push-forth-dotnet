using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace SeawispHunter.PushForth {

public class ILStackIndex : Tuple<int> {
  public ILStackIndex(int i) : base(i) { }
}

public class ILStack : Stack {
  public ILGenerator ilgen = null;
  public int count;
  public override void Push(object o) {
    if (o is int i) {
      ilgen.Emit(OpCodes.Ldc_I4, i);
      count++;
    }
    else
      throw new Exception("NYI");
  }

  public override object Pop() {
    count--;
    ilgen.Emit(OpCodes.Pop);
    return Peek();
  }

  public override object Peek() {
    return new ILStackIndex(count - 1);
  }

  // Make a return stack.
  // Should return a local variable reference or something.
  public void MakeReturnStack() {
  }
}

public abstract class InstructionCompiler : Instruction {
  public ILStack ilStack;
  public abstract Stack Apply(Stack stack);

}

public class BinaryInstructionCompiler : InstructionCompiler {

  public override Stack Apply(Stack stack) {
    object a, b;
    a = stack.Pop();
    b = stack.Pop();
    if (!(a is ILStackIndex))
      ilStack.Push(a);
    if (!(b is ILStackIndex))
      ilStack.Push(b);
    ilStack.ilgen.Emit(OpCodes.Add);
    ilStack.count--; // The result is on the stack.
    stack.Push(ilStack.Peek());
    // Expression.Invoke(func, Compile(stack), Compile(a), Compile(b));
    return stack;
  }
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
