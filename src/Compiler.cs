using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
// using System.Reflection.Emit.Lightweight;

namespace SeawispHunter.PushForth {

public class Compiler {

  public Dictionary<string, Instruction> instructions = new Dictionary<string, Instruction>();

  public Compiler() {
    instructions["+"] = new MathOpCompiler('+');
    instructions["-"] = new MathOpCompiler('-');
    instructions["*"] = new MathOpCompiler('*');
    instructions["/"] = new MathOpCompiler('/');
  }

  // public Assembly CompileAssembly(Stack program, string assemblyName, string className) {

  //       var asmName = new AssemblyName(assemblyName);
  //       // var asmBuilder = AssemblyBuilder.DefineDynamicAssembly(asmName, AssemblyBuilderAccess.Save);
  //       var asmBuilder = AssemblyBuilder.DefineDynamicAssembly//(asmName, AssemblyBuilderAccess.Save);
  //         (asmName, AssemblyBuilderAccess.Save);
  //       // var moduleBuilder = asmBuilder.DefineDynamicModule(asmName.Name + ".mod", asmName.Name + ".dll", false);
  //       // var moduleBuilder = asmBuilder.DefineDynamicModule(asmName.Name, asmName.Name + ".dll", true);
  //       // var moduleName = "MyModule";
  //       var moduleName = asmName.Name;
  //       // var moduleBuilder = asmBuilder.DefineDynamicModule(moduleName, moduleName + ".dll", true);
  //       var moduleBuilder = asmBuilder.DefineDynamicModule(moduleName, moduleName + ".dll");
  //       // var moduleBuilder = asmBuilder.DefineDynamicModule(asmName.Name + ".mod", asmName.Name + ".dll", false);
  //       // var moduleBuilder = asmBuilder.DefineDynamicModule(asmName.Name, asmName.Name + ".mod");

  //       // var typeBuilder = moduleBuilder.DefineType(className, TypeAttributes.Public, typeof(object), new Type[] { typeof(ICompiledBrain) });
  //       var typeBuilder = moduleBuilder.DefineType(className,
  //                                                  TypeAttributes.Public |
  //                                                  TypeAttributes.Class,
  //                                                  typeof(object));
  //       /*
  //         public static class Foo {
  //           public static Action<float[], float[]> GetBrain();
  //           public static int stateCount = 10;
  //         }
  //        */
  //       var methodBuilder = typeBuilder.DefineMethod("Run",
  //                                                    MethodAttributes.Static | MethodAttributes.Public,
  //                                                    typeof(Stack), new Type[] { });

  //       Type t = typeBuilder.CreateType();
  //       // asmBuilder.Save(moduleName + ".dll");
  //       return asmBuilder;

  // }

  public Func<Stack> Compile(Stack program) {
    var s = program.ToRepr();
    var dynMeth = new DynamicMethod("Program" + Regex.Replace(s, @"[^0-9]+", ""),
                                    typeof(Stack),
                                    new Type[] {},
                                    typeof(Compiler).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    object code;
    Stack data;
    if (program.Any()) {
      code = program.Pop();
      data = program;
      ils.PushStackContents(new Stack(data));
      data.Clear();
      data.Push(code);
      program = data;
    }
    // Stick an empty program on optimistically.
    while (! Interpreter.IsHalted(program)) {
      program = Interpreter.Eval(program, new [] { instructions });
      code = program.Pop();
      data = program;
      object o = data.Peek();
      if (o is Action<ILStack> a) {
        a(ils);
        data.Pop();
      }
      ils.PushStackContents(new Stack(data));
      data.Clear();
      data.Push(code);
      program = data;
      // data.Push(code);
      // program = data;
    }
    // ils.PushStackContents(new Stack(program));
    // ils.PushStackContents(program);
    ils.MakeReturnStack(ils.count);
    ils.ReverseStack();
    il.Emit(OpCodes.Dup);
    ils.Push(program.Peek());
    il.Emit(OpCodes.Call, typeof(Stack).GetMethod("Push"));
    ils.types.Pop();
    il.Emit(OpCodes.Ret);
    return (Func<Stack>) dynMeth.CreateDelegate(typeof(Func<Stack>));
  }

  public Func<Stack> CompileStack(Stack program) {
    var s = program.ToRepr();
    var dynMeth = new DynamicMethod("Program" + Regex.Replace(s, @"[^0-9]+", ""),
                                    typeof(Stack),
                                    new Type[] {},
                                    typeof(Compiler).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    // while (! Interpreter.IsHalted(program)) {
    //   program = Interpreter.Eval(program, new [] { instructions });
    //   var code = program.Pop();
    //   var data = program;
    //   object o = data.Peek();
    //   if (o is Action<ILStack> a) {
    //     a(ils);
    //     data.Pop();
    //   }
    //   data.Push(code);
    //   program = data;
    // }
    ils.PushStackContents(program);
    ils.MakeReturnStack(ils.count);
    il.Emit(OpCodes.Ret);
    // Console.WriteLine("il " + il);
    return (Func<Stack>) dynMeth.CreateDelegate(typeof(Func<Stack>));
  }

  public Func<T> CompileStack<T>(Stack program) {
    var s = program.ToRepr();
    var dynMeth = new DynamicMethod("Program" + Regex.Replace(s, @"[^0-9]+", ""),
                                    typeof(T),
                                    new Type[] {},
                                    typeof(Compiler).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var result = il.DeclareLocal(typeof(T));
    // while (! Interpreter.IsHalted(program)) {
    //   program = Interpreter.Eval(program, new [] { instructions });
    //   var code = program.Pop();
    //   var data = program;
    //   object o = data.Peek();
    //   if (o is Action<ILStack> a) {
    //     a(ils);
    //     data.Pop();
    //   }
    //   data.Push(code);
    //   program = data;
    // }
    ils.PushStackContents(new Stack(program));
    // ils.Push(2);
    // ils.Push(1);
    // il.Emit(OpCodes.Stloc, result.LocalIndex);
    // ils.Pop();
    // il.Emit(OpCodes.Ldloc, result.LocalIndex);
    if (! ils.types.Contains(typeof(T)))
      throw new Exception("No such type on stack.");
    while (ils.types.Any()) {
      var t = ils.types.Peek();
      if (t == typeof(T)) {
        il.Emit(OpCodes.Stloc, result.LocalIndex);
        ils.types.Pop();
        break;
      } else {
        ils.Pop();
      }
    }
    ils.Clear();
    il.Emit(OpCodes.Ldloc, result.LocalIndex);
    // if (typeof(T).IsValueType) {
    //   il.Emit(OpCodes.Unbox_Any, typeof(T));
    // }
    // il.Emit(OpCodes.Ldc_I4, 42);
    il.Emit(OpCodes.Ret);
    return (Func<T>) dynMeth.CreateDelegate(typeof(Func<T>));
  }

  public Func<int> CompileInt(Stack program) {
    var s = program.ToRepr();
    var dynMeth = new DynamicMethod("Program" + Regex.Replace(s, @"[^0-9]+", ""),
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

  public Func<T> Compile<T>(Stack program) {
    var s = program.ToRepr();
    var dynMeth = new DynamicMethod("Program" + Regex.Replace(s, @"[^0-9]+", ""),
                                    typeof(T),
                                    new Type[] {},
                                    typeof(Compiler).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    while (! Interpreter.IsHalted(program)) {
      program = Interpreter.Eval(program, new [] { instructions });
      var code = program.Pop();
      var data = program;
      object o = data.Peek();
      if (o is Action<ILStack> a) {
        a(ils);
        data.Pop();
      }
      ils.PushStackContents(data);
      data.Clear();
      data.Push(code);
      program = data;
      // data.Push(code);
      // program = data;
    }
    // ils.PushStackContents(program);
    if (! ils.types.Contains(typeof(T)))
      throw new Exception("No such type on stack.");
    while (ils.types.Any()) {
      var t = ils.types.Peek();
      if (t == typeof(T)) {
        break;
      } else {
        ils.Pop();
      }
    }
    // il.Emit(OpCodes.Ldc_I4, 42);
    il.Emit(OpCodes.Ret);
    return (Func<T>) dynMeth.CreateDelegate(typeof(Func<T>));
  }

}

}
