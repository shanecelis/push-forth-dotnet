using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace SeawispHunter.PushForth {

public class CompilationUnit : IReprType {
  Action<ILStack> _emitter;
  public Type _type = null;
  public CompilationUnit(Action<ILStack> emitter, Type type) {
    this._emitter = emitter;
    this._type = type;
  }
  public Type type { get => _type; set => _type = value; }
  public Action<ILStack> emitter => _emitter;
  public override string ToString()
    => "CU(" + (type != null ? type.PrettyName() : "") + ")";
}

public class InstructionCompiler : Instruction {
  // Func<Stack, CompilationUnit> action;
  int argCount;
  Action<ILStack> emitter;
  Func<Stack, Type> getReturnType;
  public InstructionCompiler(int argCount,
                             Action<ILStack> emitter,
                             Type returnType)
    : this(argCount, emitter, _ => returnType) { }
        // var argumentEmitter = SetupArguments(argCount, stack);
        // return new CompilationUnit(ilStack => {
        //                              argumentEmitter(ilStack);
        //                              emitter(ilStack);
        //                            },
        //   returnType);

  public InstructionCompiler(int argCount, 
                             Action<ILStack> emitter,
                             Func<Stack, Type> getReturnType) {
    this.argCount = argCount;
    this.emitter = emitter;
    this.getReturnType = getReturnType;
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
               if (! p.ParameterType.IsAssignableFrom(typeList[i]))
                 throw new Exception($"Cannot assign parameter {p.ParameterType.PrettyName()} from argument type {typeList[i].PrettyName()}.");
               if (! p.ParameterType.IsValueType
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
               // It's returned unboxed, typically.
               // if (methodInfo.ReturnType.IsValueType)
               //   ilStack.il.Emit(OpCodes.Unbox_Any, methodInfo.ReturnType);
               ilStack.types.Push(methodInfo.ReturnType);
             }
             },
           methodInfo.ReturnType) { }

  public Stack Apply(Stack stack) {
    Type returnType = null;
    try {
      returnType = getReturnType(stack);
    } catch(Exception e) {
      throw new Exception("Unable to get return type", e);
    }
    var argumentEmitter = SetupArguments(argCount, stack);
    var cu = new CompilationUnit(ilStack => {
        argumentEmitter(ilStack);
        emitter(ilStack);
      },
      returnType);
    stack.Push(cu);
    return stack;
  }

  public static Action<ILStack> SetupArguments(int argCount, Stack stack) {
    // XXX this magic line will change the order of the arguments.
    // var args = new Queue();
    var args = new Stack();

    if (stack.Count < argCount)
      throw new Exception($"Need {argCount} arguments but only {stack.Count} items on stack.");
    // bool hasDeferred = false;
    for(int i = 0; i < argCount; i++) {
      object a;
      a = stack.Pop();
      args.Push(a);
      // if (a is CompilationUnit)
      //   hasDeferred = true;
        // ilStack.Push(a);
    }
    // if (hasDeferred) {
    //   // Must compile.
    // } else {
    //   // Can interpret and compile result.
    // }
    Action<ILStack> emitter = (ILStack ilStack) => {
      // if (ilStack.count < argCount)
      //   throw new Exception($"Need {argCount} arguments but only {ilStack.count} items on CIL stack.");
      // Add arguments.
      foreach(object o in args) {
        if (o is CompilationUnit cu)
          cu.emitter(ilStack);
        else
          ilStack.Push(o);
      }
    };
    return emitter;
  }
}

public class AddInstructionCompiler : MathOpCompiler {
  public AddInstructionCompiler() : base("+") { }
}

public class MathOpCompiler : InstructionCompiler {
  public MathOpCompiler(string op) : base(2,
    ilStack => {
      var arg2 = ilStack.types.Pop();
      var arg1 = ilStack.types.Pop();
      if (! arg2.IsNumericType())
        throw new Exception($"Second argument type {arg2.PrettyName()} is non-numeric for ${op} operation.");
      if (! arg1.IsNumericType())
        throw new Exception($"Second argument type {arg1.PrettyName()} is non-numeric for ${op} operation.");
      switch (op) {
        case "+":
        ilStack.il.Emit(OpCodes.Add);
        break;
        case "-":
        ilStack.il.Emit(OpCodes.Sub);
        break;
        case "*":
        ilStack.il.Emit(OpCodes.Mul);
        break;
        case "/":
        ilStack.il.Emit(OpCodes.Div);
        break;
        default:
        switch (op) {
          case ">":
            ilStack.il.Emit(OpCodes.Cgt);
            break;
          case "<":
            ilStack.il.Emit(OpCodes.Clt);
            break;
          case ">=":
            ilStack.il.Emit(OpCodes.Clt);
            ilStack.il.Emit(OpCodes.Ldc_I4_0);
            ilStack.il.Emit(OpCodes.Ceq);
            break;
          case "<=":
            ilStack.il.Emit(OpCodes.Cgt);
            ilStack.il.Emit(OpCodes.Ldc_I4_0);
            ilStack.il.Emit(OpCodes.Ceq);
            break;
          case "==":
            ilStack.il.Emit(OpCodes.Ceq);
            break;
          default:
            throw new Exception("No math operation for " + op);
        }
        ilStack.types.Push(typeof(bool));
        return;
      }
      ilStack.types.Push(arg1);
    },
    stack => IsComparison(op) ? typeof(bool) : stack.Peek().GetReprType()) { }

  private static bool IsComparison(string op) {
    switch (op) {
      case ">":
      case "<":
      case ">=":
      case "<=":
      case "==":
        return true;
      default:
        return false;
    }
  }
}

}
