using System;
using System.Collections;
using Xunit;
using SeawispHunter.PushForth;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text.RegularExpressions;

namespace test {

public class CompilerTests {

  Compiler compiler = new Compiler();
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
    // Func<int, int, int> f = (int x, int y) => x + y;
    //f(2, 1) => 3;
    // Expression<Func<int, int, int>> e = (int x, int y) => x + y;
    // Func<int, int, int> g = e.Compile();
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
    if (o is CompilationUnit cu) {
      cu.emitter(ils);
      // r.Pop();
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
    // var s = "[5 4 1]".ToStack();
    var s = new Stack();
    s.Push(1);
    s.Push(4);
    s.Push(5);
    var r = bi.Apply(s);
    // var o = r.Peek();
    // if (o is CompilationUnit cu) {
    //   cu.emitter(ils);
    //   // r.Pop();
    // }
    r = bi.Apply(r);
    var o = r.Peek();
    if (o is CompilationUnit cub) {
      cub.emitter(ils);
      // r.Pop();
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
    // var o = r.Peek();
    // if (o is CompilationUnit cu) {
    //   cu.emitter(ils);
    //   // r.Pop();
    // }
    r = bi.Apply(r);
    var o = r.Peek();
    if (o is CompilationUnit cub) {
      cub.emitter(ils);
      // r.Pop();
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
    // var o = r.Peek();
    // if (o is CompilationUnit cu) {
    //   cu.emitter(ils);
    //   // r.Pop();
    // }
    r = bi.Apply(r);
    var o = r.Peek();
    if (o is CompilationUnit cub) {
      cub.emitter(ils);
      // r.Pop();
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
    // ils.PushStackContents(s);
    var r = i.Apply(s);
    var o = r.Peek();
    if (o is CompilationUnit cu) {
      cu.emitter(ils);
      r.Pop();
    }
    il.Emit(OpCodes.Ret);
    var f = (Func<object>) dynMeth.CreateDelegate(typeof(Func<object>));

    // That's better.
    Assert.Equal(1, f());
    Assert.Equal((object)1, f());
  }

  [Fact]
  public void TestCarish() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(int),
                                              new Type[] {},
                                              typeof(CompilerTests).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var i = new InstructionCompiler(typeof(CompilerFunctions).GetMethod("Car"));
    var s = "[[1 2 3]]".ToStack();
    // ils.PushStackContents(s);
    var r = i.Apply(s);
    var o = r.Peek();
    if (o is CompilationUnit cu) {
      cu.emitter(ils);
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
  public void TestCdrish() {
    DynamicMethod dynMeth = new DynamicMethod("Run",
                                              typeof(Stack),
                                              new Type[] {},
                                              typeof(CompilerTests).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var i = new InstructionCompiler(typeof(CompilerFunctions).GetMethod("Cdr"));
    var s = "[[1 2 3]]".ToStack();
    // ils.PushStackContents(s);
    var r = i.Apply(s);
    var o = r.Peek();
    if (o is CompilationUnit cu) {
      cu.emitter(ils);
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
    var i = new MathOpCompiler("-");
    var s = "[1 2]".ToStack();
    // ils.PushStackContents(s);
    var r = i.Apply(s);
    var o = r.Peek();
    if (o is CompilationUnit cu) {
      cu.emitter(ils);
      r.Pop();
    }
    il.Emit(OpCodes.Ret);
    var f = (Func<int>) dynMeth.CreateDelegate(typeof(Func<int>));

    // That's better.
    Assert.Equal(1, f());
    Assert.NotEqual(-1, f());
  }

  [Fact]
  public void TestRegex() {
    string s;
    s = "[2]";
    Assert.Equal("2", Regex.Replace(s, @"[\[\] ]+", ""));
    s = "[3 [2 1 +] 2]";
    Assert.Equal("3212", Regex.Replace(s, @"[^0-9]+", ""));
  }

  [Fact]
  public void TestCompilerString() {
    Func<string> g;
    g = compiler.CompileStack<string>(@"[""hi"" ""what""]".ToStack());
    Assert.Equal("hi", g());
  }

  [Fact]
  public void TestCompilerPlus() {
    Func<int> h;
    h = compiler.Compile<int>("[[2 1 +]]".ToStack());
    Assert.Equal(3, h());
    Assert.NotEqual(0, h());

    h = compiler.Compile<int>("[[2 1 + 5 +]]".ToStack());
    Assert.Equal(8, h());
    Assert.NotEqual(0, h());

    Assert.Throws<Exception>(() => compiler.Compile<int>("[[2 1 + 5 + +]]".ToStack()));
  }

  [Fact]
  public void TestPop() {
    Func<Stack> h;
    h = compiler.Compile("[[pop] 1 2 3]]".ToStack());
    Assert.Equal("[[] 2 3]", h().ToRepr());

    Assert.Throws<Exception>(() => compiler.Compile("[[pop]]]".ToStack()));
  }

  [Fact]
  public void TestGreaterThan() {
    Func<Stack> h;
    h = compiler.Compile("[[>] 1 2 3]]".ToStack());
    Assert.Equal("[[] True 3]", h().ToRepr());

    // http://galileo.phys.virginia.edu/classes/551.jvn.fall01/primer.htm#stacks
    // All right. This is how Forths do it.
    h = compiler.Compile("[[2 3 >]]]".ToStack());
    Assert.Equal("[[] False]", h().ToRepr());

    h = compiler.Compile("[[2 3 <]]]".ToStack());
    Assert.Equal("[[] True]", h().ToRepr());

    Assert.Throws<Exception>(() => compiler.Compile("[[a 3 <]]]".ToStack()));
    Assert.Throws<Exception>(() => compiler.Compile("[[3 <]]]".ToStack()));
    Assert.Throws<Exception>(() => compiler.Compile("[[<]]]".ToStack()));
  }

  [Fact]
  public void TestDup() {
    Func<Stack> h;
    h = compiler.Compile("[[dup] 1 2 3]]".ToStack());
    Assert.Equal("[[] 1 1 2 3]", h().ToRepr());

    h = compiler.Compile("[[dup] [1] 2 3]]".ToStack());
    Assert.Equal("[[] [1] [1] 2 3]", h().ToRepr());

    // This makes it look like it's a deep copy, is it?
    h = compiler.Compile("[[dup cdr] [1] 2 3]]".ToStack());
    Assert.Equal("[[] [] [1] 2 3]", h().ToRepr());


    Assert.Throws<Exception>(() => compiler.Compile("[[dup]]]".ToStack()));
  }

  [Fact]
  public void TestCar() {
    Func<Stack> h;
    h = compiler.Compile("[[car] [1 2 3]]]".ToStack());
    Assert.Equal("[[] 1]", h().ToRepr());

    Assert.Throws<Exception>(() => compiler.Compile("[[car] a [1 2 3]]]".ToStack()));
    Assert.Throws<Exception>(() => compiler.Compile("[[car]]]".ToStack()));
  }

  [Fact]
  public void TestCdr() {
    Func<Stack> h;
    h = compiler.Compile("[[cdr] [1 2 3]]]".ToStack());
    Assert.Equal("[[] [2 3]]", h().ToRepr());

    Assert.Throws<Exception>(() => compiler.Compile("[[cdr] a [1 2 3]]]".ToStack()));
    Assert.Throws<Exception>(() => compiler.Compile("[[cdr]]]".ToStack()));
  }

  [Fact]
  public void TestSwap() {
    Func<Stack> h;
    h = compiler.Compile("[[swap] 1 2 3]]".ToStack());
    Assert.Equal("[[] 2 1 3]", h().ToRepr());

    Assert.Throws<Exception>(() => compiler.Compile("[[swap] 1]]".ToStack()));
    Assert.Throws<Exception>(() => compiler.Compile("[[swap]]]".ToStack()));
  }

  [Fact]
  public void TestCat() {
    Func<Stack> h;
    h = compiler.Compile("[[cat] 1 2]]".ToStack());
    Assert.Equal("[[] [1 2]]", h().ToRepr());
    Assert.Throws<Exception>(() => compiler.Compile("[[cat] 1]]".ToStack()));
  }

  [Fact]
  public void TestSplit() {
    Func<Stack> h;
    h = compiler.Compile("[[split] [1 2]]]".ToStack());
    Assert.Equal("[[] 1 2]", h().ToRepr());

    Assert.Throws<Exception>(() => compiler.Compile("[[split] a [1 2]]]".ToStack()));
  }

  [Fact]
  public void TestCons() {
    Func<Stack> h;

    // h = compiler.Compile("[[cons] [1 2] 0]]".ToStack());
    // Assert.Equal("[[] [0 1 2]]", h().ToRepr());

    h = compiler.Compile("[[cons] 0 [1 2]]]".ToStack());
    Assert.Equal("[[] [0 1 2]]", h().ToRepr());

    h = compiler.Compile("[[cons] [0] [1 2]]]".ToStack());
    Assert.Equal("[[] [[0] 1 2]]", h().ToRepr());

    Assert.Throws<Exception>(() => compiler.Compile("[[cons] [0] a [1 2]]]".ToStack()));
  }

  [Fact]
  public void TestUnit() {
    Func<Stack> h;
    h = compiler.Compile("[[unit] [] 2 3]]".ToStack());
    Assert.Equal("[[] [[]] 2 3]", h().ToRepr());

    h = compiler.Compile("[[unit] 1 2 3]]".ToStack());
    Assert.Equal("[[] [1] 2 3]", h().ToRepr());
    Assert.Throws<Exception>(() => compiler.Compile("[[unit]]]".ToStack()));
  }

  [Fact]
  public void TestCompilerStacks() {
    Func<Stack> h;
    h = compiler.Compile("[[] 1 2 3]]".ToStack());
    Assert.Equal("[[] 1 2 3]", h().ToRepr());

    h = compiler.Compile("[[1 1 1 + 3]]".ToStack());
    Assert.Equal("[[] 3 2 1]", h().ToRepr());

    h = compiler.Compile("[[2 1 +]]".ToStack());
    Assert.Equal("[[] 3]", h().ToRepr());

    h = compiler.Compile("[[2 1 + 5 +]]".ToStack());
    Assert.Equal("[[] 8]", h().ToRepr());

    h = compiler.Compile("[[2 1 + 5 + 3]]".ToStack());
    Assert.Equal("[[] 3 8]", h().ToRepr());

    h = compiler.Compile("[[2 1 + 5 + 3 2 1]]".ToStack());
    Assert.Equal("[[] 1 2 3 8]", h().ToRepr());

    h = compiler.Compile("[[1 1 1 + 3]]".ToStack());
    Assert.Equal("[[] 3 2 1]", h().ToRepr());

    h = compiler.Compile("[[] 3 2 1]".ToStack());
    Assert.Equal("[[] 3 2 1]", h().ToRepr());

    h = compiler.Compile("[[-] 5 1]".ToStack());
    // Assert.Equal("[[] 4]", h().ToRepr());
    // I guess I am doing it Push3's way.
    Assert.Equal("[[] -4]", h().ToRepr());

    h = compiler.Compile("[1 [1 1 1 + 3] 2 3]".ToStack());
    Assert.Equal("[[] 1 [1 1 1 + 3] 2 3]", h().ToRepr());
  }

  [Fact]
  public void TestCompilerInt() {
    Func<int> h;
    // h = compiler.Compile<int>("[[2 1 +]]".ToStack());
    // Assert.Equal(42, h());
    // Assert.NotEqual(0, h());
    h = compiler.CompileStack<int>("[2]".ToStack());
    Assert.Equal(2, h());
    Assert.NotEqual(0, h());

    // Assert.True(false);
    h = compiler.CompileStack<int>("[huh 3 2]".ToStack());
    int s = h();
    Assert.Equal(3, s);
    Assert.NotEqual(4, s);
  }

  [Fact]
  public void TestCompileInt() {
    Func<int> h;
    // h = compiler.Compile<int>("[[2 1 +]]".ToStack());
    // Assert.Equal(42, h());
    // Assert.NotEqual(0, h());
    h = CompileInt("[2]".ToStack());
    Assert.Equal(43, h());
    Assert.NotEqual(0, h());

    // Assert.True(false);
    h = CompileInt("[3 2]".ToStack());
    int s = h();
    Assert.Equal(43, s);
    Assert.NotEqual(4, s);
  }

  public Func<int> CompileInt(Stack program) {
    // var s = program.ToRepr();
    var dynMeth = new DynamicMethod("Program", // + Regex.Replace(s, @"[^0-9]+", ""),
                                    typeof(int),
                                    new Type[] {},
                                    typeof(Compiler).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var result = il.DeclareLocal(typeof(int));
    // ils.Push(2);
    // ils.Push(1);
    // il.Emit(OpCodes.Stloc, result.LocalIndex);
    // ils.Pop();
    // ils.Pop();
    // il.Emit(OpCodes.Ldloc, result.LocalIndex);
    il.Emit(OpCodes.Ldc_I4, 42);
    il.Emit(OpCodes.Ldc_I4, 43);
    il.Emit(OpCodes.Stloc, result.LocalIndex);
    il.Emit(OpCodes.Pop);
    il.Emit(OpCodes.Ldloc, result.LocalIndex);
    il.Emit(OpCodes.Ret);
    return (Func<int>) dynMeth.CreateDelegate(typeof(Func<int>));
  }

  [Fact]
  public void TestCompileStackOfStacks() {
    Func<Stack> f;
    Stack s;
    f = compiler.CompileStack("[1 [1]]".ToStack());
    s = f();
    Assert.Equal(2, s.Count);
    Assert.Equal("[1 [1]]", s.ToRepr());

    f = compiler.CompileStack("[1 [1]]".ToStack());
    s = f();
    Assert.Equal(2, s.Count);
    Assert.Equal("[1 [1]]", s.ToRepr());
  }

  [Fact]
  public void TestCompileStacks() {
    Func<Stack> f;
    f = compiler.CompileStack("[1]".ToStack());
    // Assert.True(false);
    // Stack s = new Stack(); //f();
    Stack s = f();
    // Running this messes up everything!
    // Assert.True(false);
    // Assert.Equal(null, s.Pop());
    Assert.True(1 == s.Count);
    Assert.Equal(1, s.Peek());
    // Assert.Equal(0, s.Count);
    // Assert.True(false);
    Assert.NotEqual("blah [[]]", s.ToRepr());
    Assert.Equal("[1]", s.ToRepr());

    f = compiler.CompileStack(@"[1 2]".ToStack());
    s = f();
    Assert.Equal(@"[1 2]", s.ToRepr());
    f = compiler.CompileStack(@"[1 2 ""hi""]".ToStack());
    s = f();
    Assert.Equal(@"[1 2 ""hi""]", s.ToRepr());

    f = compiler.CompileStack(@"[1 2 3f ""hi""]".ToStack());
    s = f();
    Assert.Equal(@"[1 2 3 ""hi""]", s.ToRepr());

    f = compiler.CompileStack(@"[1 2 3.2f ""hi""]".ToStack());
    s = f();
    Assert.Equal(@"[1 2 3.2 ""hi""]", s.ToRepr());

    f = compiler.CompileStack(@"[1 2 3.1 ""hi""]".ToStack());
    s = f();
    Assert.Equal(@"[1 2 3.1 ""hi""]", s.ToRepr());

    f = compiler.CompileStack(@"[]".ToStack());
    s = f();
    Assert.Equal(@"[]", s.ToRepr());
    // f = compiler.Compile("[[] 1]".ToStack());
    // Assert.Equal("[[] 1]", f().ToRepr());
    // Assert.Equal("[[] 1]", f().ToRepr());

    // f = compiler.Compile("[[5 2 1 +]]".ToStack());
    // Assert.Equal("[[] 3 5]", f().ToRepr());

    // f = compiler.Compile("[[2 1 +]]".ToStack());
    // Assert.Equal("[[] 3]", f().ToRepr());

    // h = compiler.Compile<int>("[[2 1 +]]".ToStack());
    // Assert.Equal(3, h());

    // h = compiler.Compile<int>("[[5 2 1 +]]".ToStack());
    // Assert.Equal(3, h());

    // Assert.Throws<Exception>(() => compiler.Compile<char>("[[5 2 1 +]]".ToStack()));
    Assert.Throws<Exception>(() => compiler.Compile<char>("[[5 2 1 +]]".ToStack()));
  }

  [Fact]
  public void TestCompileArgs() {
    Func<int, Stack> h;
    h = compiler.Compile<int>("[[x x *] ]".ToStack(), "x");
    Assert.Equal("[[] 1]", h(1).ToRepr());
    Assert.Equal("[[] 4]", h(2).ToRepr());
  }

  public string Run(string program) {
    Func<Stack> h;
    h = compiler.Compile(program.ToStack());
    return h().ToRepr();
  }

  [Fact]
  public void TestIf() {
    Assert.Equal("[[] 1]", Run("[[if] true [1] [0]]"));
    Assert.Equal("[[] 0]", Run("[[if] false [1] [0]]"));

    Assert.Equal("[[] 1]", Run("[[2 1 > if] [1] [0]]"));
    Assert.Equal("[[] 0]", Run("[[2 1 < if] [1] [0]]"));

    Assert.Equal("[[] 2]", Run("[[2 1 > if] [2] [0]]"));
    Assert.Equal("[[] 3]", Run("[[2 1 < if] [1] [3]]"));

    Assert.Equal("[[] 1 2]", Run("[[2 1 > if] [2 1] [0 0]]"));
    // Assert.Equal("[[] 3]", Run("[[2 1 < if] [1] [3 3]]"));
  }

  [Fact]
  public void TestOne() {
    Assert.Equal("[[] 1]", Run("[[one]]"));
  }

  [Fact]
  public void TestCount() {
    Assert.Equal("[[] 0]", Run("[[count] []]"));
    Assert.Equal("[[] 1]", Run("[[count] [a]]"));
  }

  [Fact]
  public void TestWhile4Post() {
    // Run("[[do-while] [1 + dup 5] 0]");
    Assert.Throws<Exception>(() => Run("[[do-while] [1 + dup 5] 0]"));
    // do-while :: s -> (s, bool) -> s
    // do-while :: [T ... ] -> [T ... ]
    Assert.Equal("[[] 5]", Run("[[do-while] [1 + dup 5 <] 0]"));
    // Assert.Equal("[[] 5]", Run("[[do-while] [1 cons dup count 5 <] []]"));
  }

  [Fact]
  public void TestCompileBadArgs() {
    Assert.Throws<Exception>(() => compiler.Compile<int>("[[x *] ]".ToStack(), "x"));
    Assert.Throws<Exception>(() => compiler.Compile<char>("[[x *] ]".ToStack(), "x"));
    Assert.Throws<Exception>(() => compiler.Compile<int>("[[*] ]".ToStack(), "x"));
  }

}
}
