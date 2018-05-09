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

public class ILStack {
  public ILGenerator ilgen = null;
  public int count;
  public void Push(object o) {
    if (o is int i) {
      ilgen.Emit(OpCodes.Ldc_I4, i);
      count++;
    }
    else {
      throw new Exception("NYI");
    }
  }

  public object Pop() {
    count--;
    ilgen.Emit(OpCodes.Pop);
    return Peek();
  }

  public object Peek() {
    return new ILStackIndex(count - 1);
  }

  // Make a return stack.
  // Should return a local variable reference or something.
  public void MakeReturnStack() {
    var localStack = ilgen.DeclareLocal(typeof(Stack));
    var localTemp = ilgen.DeclareLocal(typeof(int));
    ilgen.Emit(OpCodes.Newobj,
               typeof(Stack).GetConstructor(Type.EmptyTypes));
    ilgen.Emit(OpCodes.Stloc_0);
    var pushMethod = typeof(Stack).GetMethod("Push");
    for(int i = 0; i < count; i++) {
      ilgen.Emit(OpCodes.Stloc_1);
      ilgen.Emit(OpCodes.Ldloc_0);
      ilgen.Emit(OpCodes.Ldloc_1);
      ilgen.Emit(OpCodes.Box, typeof(int));
      ilgen.Emit(OpCodes.Callvirt, pushMethod);
    }
    ilgen.Emit(OpCodes.Ldloc_0);
  }

  // Make a return stack.
  // Should return a local variable reference or something.
  public void MakeReturnArray() {
    var localStack = ilgen.DeclareLocal(typeof(int[]));
    var localTemp = ilgen.DeclareLocal(typeof(int));
    ilgen.Emit(OpCodes.Ldc_I4, count);
    ilgen.Emit(OpCodes.Newarr, typeof(int));
    ilgen.Emit(OpCodes.Stloc_0);
    for(int i = 0; i < count; i++) {
      // Store what's on the top of the stack.
      ilgen.Emit(OpCodes.Stloc_1);
      // Load the array.
      ilgen.Emit(OpCodes.Ldloc_0);
      // Set the last available index.
      // ilgen.Emit(OpCodes.Ldc_I4, count - 1 - i);
      ilgen.Emit(OpCodes.Ldc_I4, i);
      // Load the what was on the top of the stack.
      ilgen.Emit(OpCodes.Ldloc_1);
      // Store it to the array.
      ilgen.Emit(OpCodes.Stelem_I4);
    }
    ilgen.Emit(OpCodes.Ldloc_0);
    count = 0;
  }
}

public abstract class InstructionCompiler : Instruction {
  public ILStack ilStack;
  public abstract Stack Apply(Stack stack);
}

public class BinaryInstructionCompiler : InstructionCompiler {
  Action<Stack, ILStack> action;
  public BinaryInstructionCompiler(Action<Stack, ILStack> action) {
    this.action = action;
  }

  public BinaryInstructionCompiler(MethodInfo methodInfo)
    : this((stack, ilStack) => {
        ilStack.ilgen.Emit(OpCodes.Call, methodInfo);
        ilStack.count--;
      }) { }

  public override Stack Apply(Stack stack) {
    object a, b;
    a = stack.Pop();
    b = stack.Pop();
    if (!(a is ILStackIndex))
      ilStack.Push(a);
    if (!(b is ILStackIndex))
      ilStack.Push(b);
    action(stack, ilStack);
    // ilStack.ilgen.Emit(OpCodes.Add);
    // ilStack.count--; // The result is on the stack.
    // stack.Push(ilStack.Peek());
    // Expression.Invoke(func, Compile(stack), Compile(a), Compile(b));
    return stack;
  }
}

public class AddInstructionCompiler : BinaryInstructionCompiler {
  public AddInstructionCompiler() : base((stack, ilStack) => {
      ilStack.ilgen.Emit(OpCodes.Add);
      ilStack.count--; // The result is on the stack.
      stack.Push(ilStack.Peek());
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
