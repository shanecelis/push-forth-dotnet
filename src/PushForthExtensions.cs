using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sprache;

namespace SeawispHunter.PushForth {

public static class PushForthExtensions {
  public static bool Any(this Stack s) {
    return s.Count != 0;
  }

  public static Parser<object> ToCell<T>(this Parser<T> parser) {
    return parser.Select(t => (object) t);
  }

  public static Parser<object> Resolve<T>(this Parser<Symbol> parser, Dictionary<string, T> dict) {
    return parser.Select(s => {
        T obj;
        if (dict.TryGetValue(s.name, out obj))
          return (object) obj;
        else
          return (object) s;
      });
  }

  public static Stack ToStack(this string repr) {
    return Interpreter.ParseString(repr);
  }

  public static string ToRepr(this Stack s) {
    var sb = new StringBuilder();
    ToReprHelper(s, sb);
    return sb.ToString();
  }

  private static void ToReprHelper(Stack s, StringBuilder sb) {
    sb.Append("[");
    var a = s.ToArray();
    Array.Reverse(a);
    s = new Stack(a);
    while (s.Any()) {
      object x = s.Pop();
      if (x is Stack substack)
        ToReprHelper(substack, sb);
      // else if (x is Instruction i)
      //   sb.Append(instructions.First(kv => kv.Value == i).Key);
      else
        sb.Append(x.ToString());
      if (s.Any())
        sb.Append(" ");
    }
    sb.Append("]");
  }

  public static string ToPivot(this Stack s) {
    var sb = new StringBuilder();
    s = (Stack) s.Clone();
    var code = (Stack) s.Pop();
    var data = s;
    sb.Append("[");
    s = new Stack(code); // Write the code stack backwards.
    while (s.Any()) {
      object x = s.Pop();
      if (x is Stack substack)
        ToReprHelper(substack, sb);
      // else if (x is Instruction i)
      //   sb.Append(instructions.First(kv => kv.Value == i).Key);
      else
        sb.Append(x.ToString());
      sb.Append(" ");
    }
    sb.Append("• ");

    var a = data.ToArray();
    Array.Reverse(a);
    s = new Stack(a);
    while (s.Any()) {
      object x = s.Pop();
      if (x is Stack substack)
        ToReprHelper(substack, sb);
      // else if (x is Instruction i)
      //   sb.Append(instructions.First(kv => kv.Value == i).Key);
      else
        sb.Append(x.ToString());
      if (s.Any())
        sb.Append(" ");
    }
    sb.Append("]");
    return sb.ToString();
  }

  // public static Parser<Cell> ToCell(this Parser<string> parser) {
  //   return parser.Select(t => (Cell) t);
  // }

  // public static Parser<Cell> ToCell(this Parser<int> parser) {
  //   return parser.Select(t => (Cell) t);
  // }

  // public static Parser<Cell> ToCell(this Parser<Symbol> parser) {
  //   return parser.Select(t => (Cell) t);
  // }
}
}
