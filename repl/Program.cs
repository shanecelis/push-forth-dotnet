using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using PushForth;
using Cintio;

namespace repl
{
    class Program
    {
        static void Main(string[] args)
        {
          var program = new Stack();
          var code = new Stack();
          program.Push(code);
          var interpreter = new NonstrictInterpreter();
          var prompt = "> ";
          var trace = true;
          var startupMsg = "Welcome to push-forth!";
          List<string> completionList = interpreter.instructions.Keys.ToList();
          InteractivePrompt
            .Run(
                 ((strCmd, listCmd, listStrings) =>
                   {
                     var newCode = $"[{strCmd}]".ToStack();
                     program.Pop();
                     program.Push(newCode);
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
        }
    }
}
