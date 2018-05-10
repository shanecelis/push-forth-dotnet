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
  public readonly ILGenerator il;
  public int count => types.Count;
  public Stack<Type> types = new Stack<Type>();

  public ILStack(ILGenerator il) {
    this.il = il;
  }

  public void Push(object o) {
    if (o is int i) {
      il.Emit(OpCodes.Ldc_I4, i);
      types.Push(typeof(int));
    } else if (o is float f) {
      il.Emit(OpCodes.Ldc_R4, f);
      types.Push(typeof(float));
    } else if (o is double d) {
      il.Emit(OpCodes.Ldc_R8, d);
      types.Push(typeof(double));
    } else if (o is string s) {
      il.Emit(OpCodes.Ldstr, s);
      types.Push(typeof(string));
    } else if (o is Symbol sym) {
      il.Emit(OpCodes.Ldstr, sym.name);
      il.Emit(OpCodes.Newobj,
                 typeof(Symbol).GetConstructor(new [] { typeof(string) }));
      types.Push(typeof(Symbol));
    } else if (o is Stack stack) {
      int c = stack.Count;
      foreach(object x in stack) {
        Push(x);
      }
      MakeReturnStack(c);
    } else {
      throw new Exception("NYI " + o);
    }
  }

  public void PushStackContents(Stack s) {
    foreach(object x in s) {
      Push(x);
    }
  }

  public object Pop() {
    types.Pop();
    il.Emit(OpCodes.Pop);
    return Peek();
  }

  public void Clear() {
    int c = count;
    for (int i = 0; i < c; i++)
      Pop();
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
    var localStack = il.DeclareLocal(typeof(Stack));
    var tempTypes = types.Distinct();
    var tempVars = new Dictionary<Type, LocalBuilder>();
    foreach(var tempType in tempTypes)
      tempVars[tempType] = il.DeclareLocal(tempType);
    il.Emit(OpCodes.Newobj,
            typeof(Stack).GetConstructor(Type.EmptyTypes));
    il.Emit(OpCodes.Stloc, localStack.LocalIndex);
    var pushMethod = typeof(Stack).GetMethod("Push");
    for(int i = 0; i < _count; i++) {
      il.Emit(OpCodes.Stloc, tempVars[types.Peek()].LocalIndex);
      il.Emit(OpCodes.Ldloc, localStack.LocalIndex);
      il.Emit(OpCodes.Ldloc, tempVars[types.Peek()].LocalIndex);
      if (types.Peek().IsValueType)
        il.Emit(OpCodes.Box, types.Peek());
      il.Emit(OpCodes.Callvirt, pushMethod);
      types.Pop();
    }
    il.Emit(OpCodes.Ldloc, localStack.LocalIndex);
    // ilgen.EndScope();
    types.Push(typeof(Stack));
  }

  // Make a return stack.
  // Should return a local variable reference or something.
  public void MakeReturnArray() {
    var localStack = il.DeclareLocal(typeof(int[]));
    var localTemp = il.DeclareLocal(typeof(int));
    il.Emit(OpCodes.Ldc_I4, count);
    il.Emit(OpCodes.Newarr, typeof(int));
    il.Emit(OpCodes.Stloc_0);
    for(int i = 0; i < count; i++) {
      // Store what's on the top of the stack.
      il.Emit(OpCodes.Stloc_1);
      // Load the array.
      il.Emit(OpCodes.Ldloc_0);
      // Set the last available index.
      // ilgen.Emit(OpCodes.Ldc_I4, count - 1 - i);
      il.Emit(OpCodes.Ldc_I4, i);
      // Load the what was on the top of the stack.
      il.Emit(OpCodes.Ldloc_1);
      // Store it to the array.
      il.Emit(OpCodes.Stelem_I4);
    }
    il.Emit(OpCodes.Ldloc_0);
    types.Clear();
  }
}

}
