using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using PushForth;
using Cintio;
using CommandLine;

namespace repl {

class Options {

  [Option('C', "complete-stack", Default = false)]
  public bool completeStack { get; set; }
  [Option('c', "code", HelpText = "Provide a script.")]
  public string program { get; set; }

  [Option('i', "interpreter", Default = "nonstrict",
          HelpText = "Use a 'strict' or 'nonstrict' interpreter.")]
  public string interpreter { get; set; }
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

class Program
{
  static void Main(string[] args)
  {
    CommandLine.Parser.Default.ParseArguments<Options>(args)
      .WithParsed<Options>(opts => RunOptionsAndReturnExitCode(opts))
      // .WithNotParsed<Options>((errs) => HandleParseError(errs))
      ;
  }

  public static int RunOptionsAndReturnExitCode(Options opts) {
    var program = new Stack();
    var code = new Stack();
    program.Push(code);
    Interpreter interpreter;
    switch (opts.interpreter) {
      case "nonstrict":
        interpreter = new NonstrictInterpreter();
        break;
      case "strict":
        interpreter = new StrictInterpreter();
        break;
      default:
        throw new Exception($"No such interpreter '{opts.interpreter}'.");
    }
    var prompt = "> ";
    var trace = true;
    Console.WriteLine($"got a program option '{opts.program}'");
    if (opts.program != null) {
      program = opts.program.ToStack();
      if (! trace) {
        program = interpreter.Run(program);
        Console.WriteLine(program.ToRepr());
      } else {
        Console.WriteLine(program.ToRepr());
        // sb.Append(Environment.NewLine);
        foreach(var stack in interpreter.EvalStream(program)) {
          Console.WriteLine(stack.ToRepr());
          // sb.Append(Environment.NewLine);
          program = stack;
        }
      }
      return 0;
    }
    var startupMsg = "Welcome to push-forth!";
    List<string> completionList = interpreter.instructions.Keys.ToList();
    InteractivePrompt
      .Run(
           ((strCmd, listCmd, listStrings) =>
            {
              if (! opts.completeStack) {
                var newCode = $"[{strCmd}]".ToStack();
                program.Pop();
                program.Push(newCode);
              } else {
                program = strCmd.ToStack();
              }
              string output;
              if (! trace) {
                program = interpreter.Run(program);
                output = program.ToRepr();
              } else {
                var sb = new StringBuilder();
                sb.Append(program.ToRepr());
                sb.Append(Environment.NewLine);
                foreach(var stack in interpreter.EvalStream(program)) {
                  sb.Append(stack.ToRepr());
                  sb.Append(Environment.NewLine);
                  program = stack;
                }
                output = sb.ToString();
              }
              return strCmd
              + Environment.NewLine
              + output
              + Environment.NewLine;
            }), prompt, startupMsg, completionList);
    return 0;
  }
}
}
