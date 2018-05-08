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
    il.Emit(OpCodes.Ldarg_0);
    // Call the overload of Console.WriteLine that prints a string.
    il.EmitCall(OpCodes.Call, writeString, null);
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

}
}
