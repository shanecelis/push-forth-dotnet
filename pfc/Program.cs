using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using PushForth;
using CommandLine;

namespace pfc {

class Options {

  [Option('t', "target", Default = "", HelpText = "exe or library")]
  public string target { get; set; }

  [Option('r', "result-type", Default = "", HelpText = "Provide a type if you want it only that taken from the stack.")]
  public string resultType { get; set; }

  [Value(0, MetaName = "input-file", HelpText = "Provide a type if you want it only that taken from the stack.")]
  public string inputFile { get; set; }

  [Value(1, MetaName = "output-file", HelpText = "Provide a type if you want it only that taken from the stack.")]
  public string outputFile { get; set; }
  // [Option('r', "read", Required = true, HelpText = "Input files to be processed.")]
  // public IEnumerable<string> InputFiles { get; set; }

  // Omitting long name, defaults to name of property, ie "--verbose"
  // [Option(Default = false, HelpText = "Prints all messages to standard output.")]
  // public bool Verbose { get; set; }

  // [Option("stdin", Default = false, HelpText = "Read from stdin")]
  // public bool stdin { get; set; }

  // [Value(0, MetaName = "offset", HelpText = "File offset.")]
  // public long? Offset { get; set; }
}

class Program {

  static void Main(string[] args) {

    CommandLine.Parser.Default.ParseArguments<Options>(args)
      .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
      // .WithNotParsed<Options>((errs) => HandleParseError(errs))
      ;
  }

  public static int RunOptionsAndReturnExitCode(Options opts) {
    var program = File.ReadAllText(opts.inputFile).ToStack();

    var assemblyName = Path.GetFileNameWithoutExtension(opts.outputFile);
    var extension = Path.GetExtension(opts.outputFile);

    if (opts.target == "") {
      switch (extension) {
        case ".exe":
          opts.target = "exe";
          break;
        case ".dll":
          opts.target = "library";
          break;
        default:
          Console.Error.WriteLine($"Please provide a -target option or a '.exe' or '.dll' suffix on output file.");
          return 3;
      }
    }
    switch (opts.target) {
      case "exe":
        if (extension != ".exe")
          Console.Error.WriteLine($"Warning: creating an executable without a '.exe' extension");
        break;
      case "library":
        if (extension != ".dll")
          Console.Error.WriteLine($"Warning: creating a library without a '.dll' extension");
        break;
      default:
        Console.Error.WriteLine($"No such target option '{opts.target}'.");
        return 1;
    }
    var emitter = new Emitter(assemblyName, extension);

    Type resultType;
    if (opts.resultType == "") {
      resultType = typeof(Stack);
    } else {
      resultType = Type.GetType(opts.resultType);
    }

    var methodBuilder = emitter.typeBuilder
      .DefineMethod("Program",
                    MethodAttributes.Public
                    | MethodAttributes.Static
                    | MethodAttributes.HideBySig,
                    CallingConventions.Standard,
                    resultType,
                    Type.EmptyTypes);

    if (opts.target == "exe")
      emitter.EmitCallWriteStack(emitter.GetMain(), methodBuilder);
    // var il = emitter.methodBuilder.GetILGenerator();
    var il = methodBuilder.GetILGenerator();
    var ilStack = new ILStack(il);
    var compiler = new Compiler();
    program = compiler.Compile(program, ilStack);
    if (opts.resultType == "") {
      ilStack.MakeReturnStack(ilStack.count);
      ilStack.ReverseStack();
      // Tack on the rest of the program.
      ilStack.PushPush(program.Peek());
    } else {
      ilStack.FilterStack(Type.GetType(opts.resultType));
    }
    il.Emit(OpCodes.Ret);

    emitter.Close();
    return 0;
  }
}

// http://www.informit.com/articles/article.aspx?p=27368&seqNum=5
public class Emitter {

  private AssemblyName assemblyName;
  private AssemblyBuilder assemblyBuilder;
  private ModuleBuilder moduleBuilder;
  public TypeBuilder typeBuilder;
  // public MethodBuilder methodBuilder;
  string suffix;
  public Emitter(string _assemblyName, string suffix)
  {
    this.suffix = suffix;
    assemblyName = new AssemblyName();
    assemblyName.Name = _assemblyName;

    // Define dynamic assembly
    assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Save);

    // Define dynamic module
    moduleBuilder = assemblyBuilder.DefineDynamicModule("Class1.mod",
                                                        assemblyName.Name + suffix,
                                                        false);
    // Define dynamic type
    typeBuilder = moduleBuilder.DefineType("Class1",
                                           // a static class
                                           // https://stackoverflow.com/questions/33638133/typeattribute-for-static-classes
                                           TypeAttributes.AutoLayout | TypeAttributes.AnsiClass | TypeAttributes.Class
                                           | TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed
                                           | TypeAttributes.BeforeFieldInit);
    // Define dynamic method
  }

  public MethodBuilder GetMain() {
    var methodBuilder = typeBuilder.DefineMethod("Main",
                                             MethodAttributes.Private
                                             | MethodAttributes.Static
                                             | MethodAttributes.HideBySig,
                                             CallingConventions.Standard, typeof(void),
                                             new Type[]{typeof(string[])});
    // Apply attributes
    // methodBuilder.SetCustomAttribute( CreateAttributeBuilder());

    // Establish entry point Main
    assemblyBuilder.SetEntryPoint(methodBuilder);
    return methodBuilder;
  }

  public void Close() {

    // Write the lines of code
    // EmitCode(methodBuilder, toEmit);

    // Create the type
    typeBuilder.CreateType();

    // Save the dynamic assembly
    assemblyBuilder.Save(assemblyName.Name + suffix);
    // var formatter = new BinaryFormatter();
    // var stream = File.Create(name);
    // formatter.Serialize(stream, assemblyBuilder);
    // stream.Close();
  }


  // private CustomAttributeBuilder CreateAttributeBuilder()
  // {
  //   return new CustomAttributeBuilder(
  //                                     typeof(System.STAThreadAttribute).GetConstructor(new Type[]{}),
  //                                     new object[]{});
  // }

  public void EmitCallWriteStack(MethodBuilder builder, MethodInfo callMethod)
  {
    ILGenerator generator = builder.GetILGenerator();
    generator.Emit(OpCodes.Call, callMethod);
    var toRepr = typeof(PushForthExtensions)
      .GetMethod("ToReprQuasiDynamic",
                 BindingFlags.Public | BindingFlags.Static, null,
                 new [] { typeof(object) }, null);
    if (callMethod.ReturnType.IsValueType) {
      generator.Emit(OpCodes.Box, callMethod.ReturnType);
    }
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
