using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using PushForth;

namespace pfc {
class Program {
  static void Main(string[] args) {
    var program = File.ReadAllText(args[0]).ToStack();
    var emitter = new Emitter(args[1]);

    var methodBuilder = emitter.typeBuilder
      .DefineMethod("Program",
                    MethodAttributes.Private
                    | MethodAttributes.Static
                    | MethodAttributes.HideBySig,
                    CallingConventions.Standard,
                    typeof(Stack),
                    // new Type[]{ typeof(Stack) });
                    Type.EmptyTypes);

    emitter.EmitCallWriteStack(emitter.methodBuilder, methodBuilder);
    // var il = emitter.methodBuilder.GetILGenerator();
    var il = methodBuilder.GetILGenerator();
    var ilStack = new ILStack(il);
    var compiler = new Compiler();
    program = compiler.Compile(program, ilStack);
    ilStack.MakeReturnStack(ilStack.count);
    ilStack.ReverseStack();
    // Tack on the rest of the program.
    ilStack.PushPush(program.Peek());
    il.Emit(OpCodes.Ret);
    emitter.Close();
    // Emitter.Run(args[0]);
  }
}

// http://www.informit.com/articles/article.aspx?p=27368&seqNum=5
public class Emitter {
  // public static void Run(string toEmit)
  // {
  //   Emitter emitter = new Emitter(toEmit);
  //   emitter.Emit();
  // }

  private string toEmit;
  private AssemblyName assemblyName;
  private AssemblyBuilder assemblyBuilder;
  private ModuleBuilder moduleBuilder;
  public TypeBuilder typeBuilder;
  public MethodBuilder methodBuilder;
  public Emitter(string _assemblyName)
  {
    // const string name = _assemblyName + ".exe";
    assemblyName = new AssemblyName()
;
    assemblyName.Name = _assemblyName;

    // Define dynamic assembly
    assemblyBuilder = CreateAssemblyBuilder(assemblyName,
                                            AssemblyBuilderAccess.Save);

    // Define dynamic module
    moduleBuilder = CreateModuleBuilder(assemblyBuilder);

    // Define dynamic type
    typeBuilder = CreateTypeBuilder(moduleBuilder);

    // Define dynamic method
    methodBuilder = CreateMethodBuilder(typeBuilder);

    // Apply attributes
    methodBuilder.SetCustomAttribute( CreateAttributeBuilder());

    // Establish entry point Main
    assemblyBuilder.SetEntryPoint(methodBuilder);
  }

  public void Close() {

    // Write the lines of code
    // EmitCode(methodBuilder, toEmit);

    // Create the type
    typeBuilder.CreateType();

    // Save the dynamic assembly
    assemblyBuilder.Save(assemblyName.Name + ".exe");
    // var formatter = new BinaryFormatter();
    // var stream = File.Create(name);
    // formatter.Serialize(stream, assemblyBuilder);
    // stream.Close();
  }

  private AssemblyBuilder CreateAssemblyBuilder(
                                                AssemblyName aName, AssemblyBuilderAccess access)
  {
    return AssemblyBuilder.DefineDynamicAssembly(aName, access);
  }

  private ModuleBuilder CreateModuleBuilder(
                                            AssemblyBuilder builder)
  {
    return assemblyBuilder.DefineDynamicModule("Class1.mod",
                                               assemblyName.Name + ".exe",
                                               // "Hello.exe",
                                               false);
    // return assemblyBuilder.DefineDynamicModule("Class1.mod");
  }

  private TypeBuilder CreateTypeBuilder(ModuleBuilder builder)
  {
    return builder.DefineType("Class1");
  }

  private MethodBuilder CreateMethodBuilder(TypeBuilder builder)
  {
    return builder.DefineMethod("Main",
                                MethodAttributes.Private
                                | MethodAttributes.Static
                                | MethodAttributes.HideBySig,
                                CallingConventions.Standard, typeof(void),
                                new Type[]{typeof(string[])});
  }

  private CustomAttributeBuilder CreateAttributeBuilder()
  {
    return new CustomAttributeBuilder(
                                      typeof(System.STAThreadAttribute).GetConstructor(new Type[]{}),
                                      new object[]{});
  }

  public void EmitCallWriteStack(MethodBuilder builder, MethodInfo call)
  {
    ILGenerator generator = builder.GetILGenerator();
    generator.Emit(OpCodes.Call, call);
    var toRepr = typeof(PushForthExtensions)
      .GetMethod("ToRepr",
                 BindingFlags.Public | BindingFlags.Static, null,
                 new [] { typeof(Stack) }, null);
    generator.Emit(OpCodes.Call, toRepr);
    var writeLine = typeof(Console)
      .GetMethod("WriteLine",
                 BindingFlags.Public | BindingFlags.Static, null,
                 new [] { typeof(string) }, null);
    generator.Emit(OpCodes.Call, writeLine);
    generator.Emit(OpCodes.Ret);

    // generator.Emit(OpCodes.Ldstr, text);

    // MethodInfo methodInfo = typeof(System.Console).GetMethod(
    //                                                          "WriteLine",
    //                                                          BindingFlags.Public | BindingFlags.Static, null,
    //                                                          new Type[]{typeof(string)}, null);

    // generator.Emit(OpCodes.Call, methodInfo);
    // generator.Emit(OpCodes.Ret);
  }
}
}
