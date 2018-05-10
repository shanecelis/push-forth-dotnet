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

public static class CompilerFunctions {
  public static object Car(Stack s) => s.Peek();
  public static Stack Cdr(Stack s) {
    s = (Stack) s.Clone();
    s.Pop();
    return s;
  }
}

public class ILStack {
  public ILGenerator ilgen = null;
  public int count => types.Count;
  public Stack<Type> types = new Stack<Type>();
  public void Push(object o) {
    if (o is int i) {
      ilgen.Emit(OpCodes.Ldc_I4, i);
      types.Push(typeof(int));
    } else if (o is float f) {
      ilgen.Emit(OpCodes.Ldc_R4, f);
      types.Push(typeof(float));
    } else if (o is double d) {
      ilgen.Emit(OpCodes.Ldc_R8, d);
      types.Push(typeof(double));
    } else if (o is string s) {
      ilgen.Emit(OpCodes.Ldstr, s);
      types.Push(typeof(string));
    } else if (o is Symbol sym) {
      ilgen.Emit(OpCodes.Ldstr, sym.name);
      ilgen.Emit(OpCodes.Newobj,
                 typeof(Symbol).GetConstructor(new [] { typeof(string) }));
      types.Push(typeof(Symbol));
    } else if (o is Stack stack) {
      int c = stack.Count;
      foreach(var x in stack) {
        Push(x);
      }
      MakeReturnStack(c);
    } else {
      throw new Exception("NYI");
    }
  }

  public object Pop() {
    types.Pop();
    ilgen.Emit(OpCodes.Pop);
    return Peek();
  }

  public object Peek() {
    return new ILStackIndex(count - 1);
  }

  // Make a return stack.
  // Should return a local variable reference or something.
  public void MakeReturnStack(int _count) {
    if (_count > count)
      throw new Exception($"Trying to make a stack of {_count} items when {count} are available.");
    // ilgen.BeginScope();
    var localStack = ilgen.DeclareLocal(typeof(Stack));
    var localTemp = ilgen.DeclareLocal(typeof(int));
    ilgen.Emit(OpCodes.Newobj,
               typeof(Stack).GetConstructor(Type.EmptyTypes));
    ilgen.Emit(OpCodes.Stloc_0);
    var pushMethod = typeof(Stack).GetMethod("Push");
    for(int i = 0; i < _count; i++) {
      ilgen.Emit(OpCodes.Stloc_1);
      ilgen.Emit(OpCodes.Ldloc_0);
      ilgen.Emit(OpCodes.Ldloc_1);
      if (types.Peek().IsValueType)
        ilgen.Emit(OpCodes.Box, types.Peek());
      ilgen.Emit(OpCodes.Callvirt, pushMethod);
      types.Pop();
    }
    ilgen.Emit(OpCodes.Ldloc_0);
    // ilgen.EndScope();
    types.Push(typeof(Stack));
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
    types.Clear();
  }
}

// public abstract class InstructionCompiler : Instruction {
//   public ILStack ilStack;
//   public abstract Stack Apply(Stack stack);
// }

public class InstructionCompiler : Instruction {

  public ILStack ilStack;
  Action<Stack, ILStack> action;
  int argCount;
  public InstructionCompiler(int argCount, Action<Stack, ILStack> action) {
    this.argCount = argCount;
    this.action = action;
  }

  public InstructionCompiler(MethodInfo methodInfo)
    : this(methodInfo.GetParameters().Length,
           (stack, ilStack) => {
             ilStack.ilgen.Emit(OpCodes.Call, methodInfo);
             for(int i = 0; i < methodInfo.GetParameters().Length; i++)
               ilStack.types.Pop();

             if (methodInfo.ReturnType != typeof(void)) {
               if (methodInfo.ReturnType.IsValueType)
                 ilStack.ilgen.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType);
               ilStack.types.Push(methodInfo.ReturnType);
               stack.Push(ilStack.Peek());
             }
           }) { }

  public Stack Apply(Stack stack) {
    for(int i = 0; i < argCount; i++) {
      object a;
      a = stack.Pop();
      if (!(a is ILStackIndex))
        ilStack.Push(a);
    }
    action(stack, ilStack);
    return stack;
  }
}

public class AddInstructionCompiler : InstructionCompiler {
  public AddInstructionCompiler() : base(2, (stack, ilStack) => {
      ilStack.ilgen.Emit(OpCodes.Add);
      ilStack.types.Pop();
      stack.Push(ilStack.Peek());
    }) { }
}

public class MathOpCompiler : InstructionCompiler {
  public MathOpCompiler(char op) : base(2, (stack, ilStack) => {
      switch (op) {
        case '+':
        ilStack.ilgen.Emit(OpCodes.Add);
        break;
        case '-':
        ilStack.ilgen.Emit(OpCodes.Sub);
        break;
        case '*':
        ilStack.ilgen.Emit(OpCodes.Mul);
        break;
        case '/':
        ilStack.ilgen.Emit(OpCodes.Div);
        break;
        default:
        throw new Exception("No math operation for " + op);
      }
      ilStack.types.Pop();
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
