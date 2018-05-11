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
             var parameters = methodInfo.GetParameters();
             if (! methodInfo.IsStatic) {
               if (parameters.Length != 0) {
                 throw new Exception("NYI. Instance required on stack.");
               }
               if (ilStack.types.Peek() != methodInfo.DeclaringType) {
                 throw new Exception($"Instance method requires type {methodInfo.DeclaringType.PrettyName()} but on the top of the stack is {ilStack.types.Peek()}.");
               }
               ilStack.il.Emit(OpCodes.Dup);
             }

             List<Type> typeList = ilStack.types.ToList();
             for(int i = 0; i < parameters.Length; i++) {
               var p = parameters[parameters.Length - 1 - i];
               if (!p.ParameterType.IsValueType
                   && typeList[i].IsValueType) {
                 if (i == 0) {
                   // Fix it.
                   ilStack.il.Emit(OpCodes.Box, ilStack.types.Peek());
                 } else {
                   // Punt.
                   throw new Exception($"NYI. Parameter {i}, type {p.ParameterType.PrettyName()} requires some boxing of {typeList[i].PrettyName()} probably.");
                 }
               }
               ilStack.types.Pop();
             }
             ilStack.il.Emit(OpCodes.Call, methodInfo);

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
        throw new Exception($"Need ${argCount} arguments but only ${ilStack.count} items on CIL stack.");
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

}
