using System;
using System.Collections;
using Xunit;
using SeawispHunter.PushForth;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;

namespace test
{
public class CompilerTests
{
  [Fact]
  public void TestCompiling() {
    Expression<Func<int, int, int>> f = (a, b) => a + b;
    var func = f.Compile();
    Assert.Equal(3, func(1, 2));
  }

  [Fact]
  public void TestExpressions() {
    var c = Expression.Constant(1);
    Assert.Equal(typeof(int), c.Type);
    Assert.Equal(ExpressionType.Constant, c.NodeType);
  }
  private delegate int HelloDelegate(string msg, int ret);

  [Fact]
  public void TestDynamicMethod() {
    // https://msdn.microsoft.com/en-us/library/system.reflection.emit.dynamicmethod(v=vs.110).aspx
    Type[] helloArgs = {typeof(string), typeof(int)};

    DynamicMethod hello = new DynamicMethod("Hello",
                                            typeof(int),
                                            helloArgs,
                                            typeof(string).Module);

    // Create an array that specifies the parameter types of the
    // overload of Console.WriteLine to be used in Hello.
    Type[] writeStringArgs = {typeof(string)};
    // Get the overload of Console.WriteLine that has one
    // String parameter.
    MethodInfo writeString = typeof(Console).GetMethod("WriteLine",
                                                       writeStringArgs);

    // Get an ILGenerator and emit a body for the dynamic method,
    // using a stream size larger than the IL that will be
    // emitted.
    ILGenerator il = hello.GetILGenerator(256);
    // Load the first argument, which is a string, onto the stack.
    // il.Emit(OpCodes.Ldarg_0);
    // Call the overload of Console.WriteLine that prints a string.
    // il.EmitCall(OpCodes.Call, writeString, null);
    // The Hello method returns the value of the second argument;
    // to do this, load the onto the stack and return.
    il.Emit(OpCodes.Ldarg_1);
    il.Emit(OpCodes.Ret);
    var hi = (HelloDelegate) hello.CreateDelegate(typeof(HelloDelegate));
    Assert.Equal(1, hi("yo", 1));
  }

  private delegate int AddDelegate(int a, int b);
  [Fact]
  public void TestAddMethod() {
    // https://msdn.microsoft.com/en-us/library/system.reflection.emit.dynamicmethod(v=vs.110).aspx
    Type[] helloArgs = {typeof(int), typeof(int)};

    DynamicMethod hello = new DynamicMethod("Hello",
                                            typeof(int),
                                            helloArgs,
                                            typeof(string).Module);

    ILGenerator il = hello.GetILGenerator(256);
    // Load the first argument, which is a string, onto the stack.
    il.Emit(OpCodes.Ldarg_0);
    il.Emit(OpCodes.Ldarg_1);
    il.Emit(OpCodes.Add);
    il.Emit(OpCodes.Ret);
    var d = (AddDelegate) hello.CreateDelegate(typeof(AddDelegate));
    Assert.Equal(3, d(2, 1));
    // Good.
    var f = (Func<int, int, int>) hello.CreateDelegate(typeof(Func<int, int, int>));
    Assert.Equal(4, f(3, 1));
  }

  // [Fact]
  // public void TestCompilingInstruction() {
  //   Expression<Func<int, int, int>> f = (a, b) => a + b;
  //   var i = BinaryInstructionCompiler<int, int>.WithResult<int>(f);
  //   var s = new Stack();
  //   s.Push(1);
  //   s.Push(2);
  //   var r = i.Apply(s);
  //   Assert.Equal(1, r.ToArray().Count());
  //   var e = (Expression<Func<Stack, Stack>>) r.Pop();
  //   var func = (Func<Stack, Stack>) e.Compile();
  //   s = new Stack();
  //   s.Push(1);
  //   s.Push(2);
  //   r = func(s);
  //   Assert.Equal(1, r.ToArray().Count());
  //   Assert.Equal(3, r.Pop());
  // }

  [Fact]
  public void TestAddStack() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(int),
                                              new [] {typeof(Stack)},
                                              typeof(CompilerTests).Module);

    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var bi = new AddInstructionCompiler();
    var s = new Stack();
    s.Push(1);
    s.Push(4);
    var r = bi.Apply(s);
    var o = r.Peek();
    if (o is Action<ILStack> a) {
      a(ils);
      r.Pop();
    }
    // Load the first arVgument, which is a string, onto the stack.
    il.Emit(OpCodes.Ret);
    var f = (Func<Stack, int>) dynMeth.CreateDelegate(typeof(Func<Stack, int>));
    Assert.Equal(5, f(s));
  }

  [Fact]
  public void TestAddStackMultipleTimes() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(int),
                                              new [] {typeof(Stack)},
                                              typeof(CompilerTests).Module);

    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var bi = new AddInstructionCompiler();
    var s = new Stack();
    s.Push(1);
    s.Push(4);
    s.Push(5);
    var r = bi.Apply(s);
    var o = r.Peek();
    if (o is Action<ILStack> a) {
      a(ils);
      r.Pop();
    }
    r = bi.Apply(r);
    o = r.Peek();
    if (o is Action<ILStack> b) {
      b(ils);
      r.Pop();
    }
    // Load the first argument, which is a string, onto the stack.
    il.Emit(OpCodes.Ret);
    var f = (Func<Stack, int>) dynMeth.CreateDelegate(typeof(Func<Stack, int>));
    Assert.Equal(10, f(s));
  }

  [Fact]
  public void TestAddStackFloat() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(float),
                                              new [] {typeof(Stack)},
                                              typeof(CompilerTests).Module);

    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var bi = new AddInstructionCompiler();
    var s = new Stack();
    s.Push(1.5f);
    s.Push(4f);
    s.Push(5f);
    var r = bi.Apply(s);
    var o = r.Peek();
    if (o is Action<ILStack> a) {
      a(ils);
      r.Pop();
    }
    r = bi.Apply(r);
    o = r.Peek();
    if (o is Action<ILStack> b) {
      b(ils);
      r.Pop();
    }
    // Load the first argument, which is a string, onto the stack.
    il.Emit(OpCodes.Ret);
    var f = (Func<Stack, float>) dynMeth.CreateDelegate(typeof(Func<Stack, float>));
    Assert.Equal(10.5f, f(s));
  }

  [Fact]
  public void TestAddStackDouble() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(double),
                                              new [] {typeof(Stack)},
                                              typeof(CompilerTests).Module);

    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var bi = new AddInstructionCompiler();
    var s = new Stack();
    s.Push(1.5);
    s.Push(4.5);
    s.Push(5.5);
    var r = bi.Apply(s);
    var o = r.Peek();
    if (o is Action<ILStack> a) {
      a(ils);
      r.Pop();
    }
    r = bi.Apply(r);
    o = r.Peek();
    if (o is Action<ILStack> b) {
      b(ils);
      r.Pop();
    }
    // Load the first argument, which is a string, onto the stack.
    il.Emit(OpCodes.Ret);
    var f = (Func<Stack, double>) dynMeth.CreateDelegate(typeof(Func<Stack, double>));
    Assert.Equal(11.5, f(s));
  }

  [Fact]
  public void TestReturnStack() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(Stack),
                                              new Type[] {},
                                              typeof(CompilerTests).Module);

    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    ils.Push(1);
    ils.Push(4);
    ils.Push(5);
    ils.MakeReturnStack(3);
    il.Emit(OpCodes.Ret);
    var f = (Func<Stack>) dynMeth.CreateDelegate(typeof(Func<Stack>));

    var r = new Stack();
    r.Push(5);
    r.Push(4);
    r.Push(1);
    // Unfortunately, it's kind of backwards.
    Assert.Equal(r, f());
  }

  [Fact]
  public void TestReturnArray() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(int[]),
                                              new Type[] {},
                                              typeof(CompilerTests).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    ils.Push(1);
    ils.Push(4);
    ils.Push(5);
    ils.MakeReturnArray();
    il.Emit(OpCodes.Ret);
    var f = (Func<int[]>) dynMeth.CreateDelegate(typeof(Func<int[]>));

    // That's better.
    Assert.Equal(new [] {5, 4, 1}, f());
  }

  [Fact]
  public void TestReturnString() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(string),
                                              new Type[] {},
                                              typeof(CompilerTests).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    ils.Push("hi");
    il.Emit(OpCodes.Ret);
    var f = (Func<string>) dynMeth.CreateDelegate(typeof(Func<string>));

    // That's better.
    Assert.Equal("hi", f());
  }

  [Fact]
  public void TestReturnSymbol() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(Symbol),
                                              new Type[] {},
                                              typeof(CompilerTests).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    ils.Push(new Symbol("hi"));
    il.Emit(OpCodes.Ret);
    var f = (Func<Symbol>) dynMeth.CreateDelegate(typeof(Func<Symbol>));

    // That's better.
    Assert.Equal(new Symbol("hi"), f());
  }

  [Fact]
  public void TestReturnStack2() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(Stack),
                                              new Type[] {},
                                              typeof(CompilerTests).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    ils.Push("[1 2 3]".ToStack());
    il.Emit(OpCodes.Ret);
    var f = (Func<Stack>) dynMeth.CreateDelegate(typeof(Func<Stack>));

    // That's better.
    Assert.Equal("[1 2 3]".ToStack(), f());
  }

  [Fact]
  public void TestCarObject() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(object),
                                              new Type[] {},
                                              typeof(CompilerTests).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var i = new InstructionCompiler(typeof(CompilerFunctions).GetMethod("Car"));
    var s = "[[1 2 3]]".ToStack();
    var r = i.Apply(s);
    var o = r.Peek();
    if (o is Action<ILStack> a) {
      a(ils);
      r.Pop();
    }
    il.Emit(OpCodes.Ret);
    var f = (Func<object>) dynMeth.CreateDelegate(typeof(Func<object>));

    // That's better.
    Assert.Equal(1, f());
    Assert.Equal((object)1, f());
  }

  [Fact]
  public void TestCar() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(int),
                                              new Type[] {},
                                              typeof(CompilerTests).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var i = new InstructionCompiler(typeof(CompilerFunctions).GetMethod("Car"));
    var s = "[[1 2 3]]".ToStack();
    var r = i.Apply(s);
    var o = r.Peek();
    if (o is Action<ILStack> a) {
      a(ils);
      r.Pop();
    }
    il.Emit(OpCodes.Unbox_Any, typeof(int));
    // il.Emit(OpCodes.Conv_I4);
    il.Emit(OpCodes.Ret);
    var f = (Func<int>) dynMeth.CreateDelegate(typeof(Func<int>));

    // That's better.
    Assert.Equal(1, f());
  }

  [Fact]
  public void TestCdr() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(Stack),
                                              new Type[] {},
                                              typeof(CompilerTests).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var i = new InstructionCompiler(typeof(CompilerFunctions).GetMethod("Cdr"));
    var s = "[[1 2 3]]".ToStack();
    var r = i.Apply(s);
    var o = r.Peek();
    if (o is Action<ILStack> a) {
      a(ils);
      r.Pop();
    }
    il.Emit(OpCodes.Ret);
    var f = (Func<Stack>) dynMeth.CreateDelegate(typeof(Func<Stack>));

    // That's better.
    Assert.Equal("[2 3]".ToStack(), f());
  }

  [Fact]
  public void TestSubtract() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(int),
                                              new Type[] {},
                                              typeof(CompilerTests).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var i = new MathOpCompiler('-');
    var s = "[1 2]".ToStack();
    var r = i.Apply(s);
    var o = r.Peek();
    if (o is Action<ILStack> a) {
      a(ils);
      r.Pop();
    }
    il.Emit(OpCodes.Ret);
    var f = (Func<int>) dynMeth.CreateDelegate(typeof(Func<int>));

    // That's better.
    Assert.Equal(-1, f());
  }
}
}
