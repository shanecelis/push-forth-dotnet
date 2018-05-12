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
  public readonly ILGenerator il;
  public int count => types.Count;
  public Stack<Type> types = new Stack<Type>();
  public Stack<Stack<Type>> stackTypes = new Stack<Stack<Type>>();
  Dictionary<Type, LocalBuilder> tempVars
    = new Dictionary<Type, LocalBuilder>();

  public ILStack(ILGenerator il) {
    this.il = il;
  }

  public LocalBuilder GetTemp(Type t) {
    LocalBuilder lb;
    if (! tempVars.TryGetValue(t, out lb)) {
      lb = tempVars[t] = il.DeclareLocal(t);
    }
    return lb;
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
    } else if (o is ILStackIndex) {
      // Skip it.
    } else {
      throw new Exception($"Don't know how to push object {o} of type {o.GetType()}.");
    }
  }

  public void PushStackContents(Stack s) {
    foreach(object x in s) {
      Push(x);
    }
  }

  public object Pop() {
    if (typeof(Stack) == types.Pop())
      stackTypes.Pop();
    il.Emit(OpCodes.Pop);
    return Peek();
  }

  public void Clear() {
    int c = count;
    for (int i = 0; i < c; i++)
      Pop();
  }

  public void UnrollStack() {
    /*
      If this weren't in IL. It'd be:

      // foreach(var x in stack) {
      while(stack.Any()) {
      object o = stack.Pop();
      ilStack.Push((int|float|double) o);
      }
    */
    if (types.Pop() != typeof(Stack))
      throw new Exception("Must have stack to unroll.");
    var _stackTypes = stackTypes.Pop();
    var popMethod = typeof(Stack).GetMethod("Pop");
    var stackValue = GetTemp(typeof(Stack));
    il.Emit(OpCodes.Stloc, stackValue.LocalIndex);
    while (_stackTypes.Any()) {
      il.Emit(OpCodes.Ldloc, stackValue.LocalIndex);
      il.Emit(OpCodes.Call, popMethod);
      if (_stackTypes.Peek().IsValueType)
        il.Emit(OpCodes.Unbox_Any, _stackTypes.Peek());
      types.Push(_stackTypes.Peek());
      _stackTypes.Pop();
    }
  }

  // public void UnrollStack() {
  //   /*
  //     If this weren't in IL. It'd be:

  //     // foreach(var x in stack) {
  //     while(stack.Any()) {
  //       object o = stack.Pop();
  //       ilStack.Push((int|float|double) o);
  //     }
  //    */
  //   // https://www.codeproject.com/Articles/362076/Understanding-Common-Intermediate-Language-CIL
  //   var popMethod = typeof(Stack).GetMethod("Pop");
  //   var anyMethod = typeof(PushForthExtensions).GetMethod("Any");
  //   var isValueTypeMethod = typeof(ILStack).GetMethod("IsValueType");
  //   var testLabel = il.DefineLabel();
  //   var bodyLabel = il.DefineLabel();
  //   var dontUnboxLabel = il.DefineLabel();
  //   il.Emit(OpCodes.Jmp, testLabel);
  //   il.MarkLabel(bodyLabel);
  //   // Do body
  //   il.Emit(OpCodes.Dup);
  //   il.Emit(OpCodes.Call, popMethod);
  //   il.Emit(OpCodes.Dup);
  //   il.Emit(OpCodes.Call, isValueTypeMethod);
  //   il.Emit(OpCodes.Brfalse, dontUnboxLabel);
  // }

  public object Peek() {
    return new ILStackIndex(count - 1);
  }

  // Make a return stack.
  // Should return a local variable reference or something.
  public void MakeReturnStack(int stackCount) {
    if (stackCount > count)
      throw new Exception($"Trying to make a stack of {stackCount} items when only {count} are available.");
    // ilgen.BeginScope();
    var localStack = GetTemp(typeof(Stack));

    var tempTypes = types.Take(stackCount).Distinct();
    // var tempVars = new Dictionary<Type, LocalBuilder>();
    foreach(var tempType in tempTypes)
      tempVars[tempType] = il.DeclareLocal(tempType);
    il.Emit(OpCodes.Newobj,
            typeof(Stack).GetConstructor(Type.EmptyTypes));
    il.Emit(OpCodes.Stloc, localStack.LocalIndex);
    var pushMethod = typeof(Stack).GetMethod("Push");
    var _stackTypes = new Stack<Type>();
    for(int i = 0; i < stackCount; i++) {
      _stackTypes.Push(types.Peek());
      var temp = GetTemp(types.Peek());
      il.Emit(OpCodes.Stloc, temp.LocalIndex);
      il.Emit(OpCodes.Ldloc, localStack.LocalIndex);
      il.Emit(OpCodes.Ldloc, temp.LocalIndex);
      if (types.Peek().IsValueType)
        il.Emit(OpCodes.Box, types.Peek());
      il.Emit(OpCodes.Call, pushMethod);
      types.Pop();
    }
    il.Emit(OpCodes.Ldloc, localStack.LocalIndex);
    // ilgen.EndScope();
    stackTypes.Push(_stackTypes);
    types.Push(typeof(Stack));
  }

  public void ReverseStack() {
    if (types.Peek() != typeof(Stack))
      throw new Exception("A Stack type must be on top to reverse.");

    il.Emit(OpCodes.Newobj,
            typeof(Stack).GetConstructor(new [] { typeof(ICollection) }));
    var s = stackTypes.Pop();
    stackTypes.Push(new Stack<Type>(s));
  }

  /*
    On the IL stack there is [(Stack s) ...]. This method will push o onto s.
   */
  public void PushPush(object o) {
    if (types.Peek() != typeof(Stack))
      throw new Exception("Must have a stack on top.");
    il.Emit(OpCodes.Dup);
    Push(o);
    var pushMethod = typeof(Stack).GetMethod("Push");
    if (types.Peek().IsValueType)
      il.Emit(OpCodes.Box, types.Peek());
    il.Emit(OpCodes.Call, pushMethod);
    types.Pop();
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
