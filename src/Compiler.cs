using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace SeawispHunter.PushForth {

public class Compiler {

  public Dictionary<string, Instruction> instructions
    = new Dictionary<string, Instruction>();
  Dictionary<string, Func<Stack>> memoizedPrograms
    = new Dictionary<string, Func<Stack>>();

  public Compiler() {
    foreach (var op in new [] { "+", "-", "*", "/", ">", "<", ">=", "<=", "==" })
      instructions[op] = new MathOpCompiler(op);
    instructions["pop"] = new InstructionCompiler(1, ilStack => {
        ilStack.Pop();
      },
      stack => stack.Peek().GetReprType());
    instructions["dup"] = new InstructionCompiler(1, ilStack => {
        // XXX This probably doesn't work well with stacks.
        ilStack.il.Emit(OpCodes.Dup);
        if (ilStack.types.Peek() == typeof(Stack))
            ilStack.stackTypes.Push(ilStack.stackTypes.Peek());
        ilStack.types.Push(ilStack.types.Peek());
      },
      stack => stack.Peek().GetReprType());
    foreach(var method in typeof(CompilerFunctions).GetMethods())
      instructions[method.Name.ToLower()] = new InstructionCompiler(method);
    instructions["swap"] = new InstructionCompiler(2, ilStack => {
        var t1 = ilStack.GetTemp(ilStack.types.Peek());
        ilStack.il.Emit(OpCodes.Stloc, t1.LocalIndex);
        ilStack.types.Pop();
        LocalBuilder t2;
        if (t1.LocalType == ilStack.types.Peek())
          t2 = ilStack.il.DeclareLocal(ilStack.types.Peek());
        else
          t2 = ilStack.GetTemp(ilStack.types.Peek());
        ilStack.il.Emit(OpCodes.Stloc, t2.LocalIndex);
        ilStack.types.Pop();
        ilStack.il.Emit(OpCodes.Ldloc, t1.LocalIndex);
        ilStack.types.Push(t1.LocalType);
        ilStack.il.Emit(OpCodes.Ldloc, t2.LocalIndex);
        ilStack.types.Push(t2.LocalType);
      },
      stack => { var a = stack.Pop();
        var b = stack.Peek();
        stack.Push(a);
        return b.GetReprType();
      });
    instructions["split"] = new InstructionCompiler(1, ilStack => {
        ilStack.ReverseStack();
        ilStack.UnrollStack();
      },
      stack => ((Stack) stack.Peek()).Peek().GetReprType());
    instructions["cat"] = new InstructionCompiler(2, ilStack => {
        ilStack.MakeReturnStack(2);
        ilStack.ReverseStack();
      },
      typeof(Stack));
    instructions["if"] = new InstructionFunc(stack => {
        if (stack.Peek() is bool) {
          var condition = (bool) stack.Pop();
          var consequent = (Stack) stack.Pop();
          var otherwise = (Stack) stack.Pop();

          stack.Push(new CompilationUnit(ilStack => {
                Compile(condition ? consequent : otherwise,
                        ilStack,
                        new [] { instructions });
              },
              typeof(int)));
      } else {
        var condition = (CompilationUnit) stack.Pop();
        var consequent = (Stack) stack.Pop();
        var otherwise = (Stack) stack.Pop();

        var data1 = (Stack) stack.Clone();
        var data2 = (Stack) stack.Clone();
        Action<ILStack> _emitter;
        _emitter = ilStack => {
          // if (ilStack.types.Peek() != typeof(bool))
          //   throw new Exception($"Expected a bool not {ilStack.types.Peek().PrettyName()}");
          // ilStack.types.Pop();
          // if (ilStack.types.Peek() != typeof(Stack))
          //   throw new Exception($"Expected a Stack for consequent not {ilStack.types.Peek().PrettyName()}");
          // ilStack.types.Pop();
          // if (ilStack.types.Peek() != typeof(Stack))
          //   throw new Exception($"Expected a Stack for consequent not {ilStack.types.Peek().PrettyName()}");
          var ifnot = ilStack.il.DefineLabel();
          var end = ilStack.il.DefineLabel();
          // ilStack.Push(true);
          condition.emitter(ilStack);
          ilStack.il.Emit(OpCodes.Brfalse_S, ifnot);
          ilStack.types.Pop(); // consume the bool
          // Compile if possible. Interpret ifnot.
          data1.Push(consequent);
          Console.WriteLine("Consequent " + data1.ToRepr());
          Compile(data1, ilStack, new [] { instructions });
          // ilStack.il.Emit(OpCodes.Ldc_I4_1);
          // ilStack.Push(1);
          ilStack.types.Pop(); // In reality only type gets put on the stack.
          ilStack.il.Emit(OpCodes.Br, end);
          ilStack.il.MarkLabel(ifnot);
          // ilStack.Push(0);
          data2.Push(otherwise);
          Compile(data2, ilStack, new [] { instructions });
          ilStack.il.MarkLabel(end);
        };
        stack.Push(new CompilationUnit(_emitter, typeof(int)));
        }
      });
  }

  internal static Stack
    Compile(Stack program,
            ILStack ils,
            IEnumerable<Dictionary<string, Instruction>> instructionSets) {
    object code;
    Stack data;
    while (! Interpreter.IsHalted(program)) {
      program = Interpreter.Eval(program, instructionSets);
    }
    // Console.WriteLine("Compiled Program " + program.ToRepr());
    // Compile after it's all done.
    code = program.Pop();
    data = program;
    var reversedData = new Stack(data);
    foreach(object d in reversedData) {
      if (d is CompilationUnit cu)
        cu.emitter(ils);
      else
        ils.Push(d);
    }
    program.Push(code);
    return program;
  }

  public Func<Stack> Compile(Stack program) {
    // var s = program.ToRepr();
    var dynMeth = new DynamicMethod("Program", // + Regex.Replace(s, @"[^0-9]+", ""),
                                    typeof(Stack),
                                    new Type[] {},
                                    typeof(Compiler).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    Compile(program, ils, new [] { instructions });
    // Turn the IL stack into a Stack.
    ils.MakeReturnStack(ils.count);
    ils.ReverseStack();
    // Tack on the rest of the program.
    ils.PushPush(program.Peek());
    il.Emit(OpCodes.Ret);
    return (Func<Stack>) dynMeth.CreateDelegate(typeof(Func<Stack>));
  }

  public Func<X, Stack> Compile<X>(Stack program, string argxName) {
    // var s = program.ToRepr();
    var dynMeth = new DynamicMethod("Program", // + Regex.Replace(s, @"[^0-9]+", ""),
                                    typeof(Stack),
                                    new Type[] {typeof(X)},
                                    typeof(Compiler).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var arguments = new Dictionary<string, Instruction>();
    arguments[argxName] = new InstructionCompiler(0, _ilStack => {
        _ilStack.il.Emit(OpCodes.Ldarg_0);
        _ilStack.types.Push(typeof(X));
      },
      typeof(X));
    var ils = new ILStack(il);
    Compile(program, ils, new [] { arguments, instructions });
    // Turn the IL stack into a Stack.
    ils.MakeReturnStack(ils.count);
    ils.ReverseStack();
    // Tack on the rest of the program.
    ils.PushPush(program.Peek());
    il.Emit(OpCodes.Ret);
    return (Func<X,Stack>) dynMeth.CreateDelegate(typeof(Func<X,Stack>));
  }

  public Func<Stack> CompileStack(Stack program) {
    // var s = program.ToRepr();
    var dynMeth = new DynamicMethod("Program", // + Regex.Replace(s, @"[^0-9]+", ""),
                                    typeof(Stack),
                                    new Type[] {},
                                    typeof(Compiler).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    ils.PushStackContents(program);
    ils.MakeReturnStack(ils.count);
    il.Emit(OpCodes.Ret);
    return (Func<Stack>) dynMeth.CreateDelegate(typeof(Func<Stack>));
  }

  public Func<T> CompileStack<T>(Stack program) {
    // var s = program.ToRepr();
    var dynMeth = new DynamicMethod("Program", // + Regex.Replace(s, @"[^0-9]+", ""),
                                    typeof(T),
                                    new Type[] {},
                                    typeof(Compiler).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    var result = il.DeclareLocal(typeof(T));
    ils.PushStackContents(new Stack(program));
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
    il.Emit(OpCodes.Ret);
    return (Func<T>) dynMeth.CreateDelegate(typeof(Func<T>));
  }


  public Func<T> Compile<T>(Stack program) {
    // var s = program.ToRepr();
    var dynMeth = new DynamicMethod("Program",// + Regex.Replace(s, @"[^0-9]+", ""),
                                    typeof(T),
                                    new Type[] {},
                                    typeof(Compiler).Module);
    ILGenerator il = dynMeth.GetILGenerator(256);
    var ils = new ILStack(il);
    Compile(program, ils, new [] { instructions });
    // ils.PushStackContents(program);
    if (! ils.types.Contains(typeof(T)))
      throw new Exception($"No such type {typeof(T).PrettyName()} on stack.");
    while (ils.types.Any()) {
      var t = ils.types.Peek();
      if (t == typeof(T)) {
        break;
      } else {
        ils.Pop();
      }
    }
    il.Emit(OpCodes.Ret);
    return (Func<T>) dynMeth.CreateDelegate(typeof(Func<T>));
  }

}

}
