using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sprache;
using OneOf;

namespace SeawispHunter.PushForth {

public static class PushForthExtensions {
  public static bool Any(this Stack s) {
    return s.Count != 0;
  }

  public static Parser<object> ToCell<T>(this Parser<T> parser) {
    return parser.Select(t => (object) t);
  }

  public static Parser<OneOf<Type,Variable>> ToTypeOrVariable(this Parser<Variable> parser) => parser.Select(t => (OneOf<Type,Variable>) t);

  public static Parser<OneOf<Type,Variable>> ToTypeOrVariable(this Parser<Type> parser) => parser.Select(t => (OneOf<Type,Variable>) t);

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
    return StackParser.ParseStack(repr);
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
      else if (x is Type t)
        sb.Append(t.PrettyName());
      else if (x is IDictionary d)
        sb.Append(d.ToRepr());
      else if (x is string str)
        sb.Append($"\"{str}\"");
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
      else if (x is Type t)
        sb.Append(t.PrettyName());
      else if (x is IDictionary d)
        sb.Append(d.ToRepr());
      else if (x is string str)
        sb.Append($"\"{str}\"");
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
      else if (x is Type t)
        sb.Append(t.PrettyName());
      else if (x is IDictionary d)
        sb.Append(d.ToRepr());
      else if (x is string str)
        sb.Append($"\"{str}\"");
      else
        sb.Append(x.ToString());
      if (s.Any())
        sb.Append(" ");
    }
    sb.Append("]");
    return sb.ToString();
  }

  public static Stack Map(this Stack s, Func<object, object> f) {
    // Queue q = new Queue();
    s = (Stack) s.Clone();
    Stack q = new Stack();
    while (s.Any()) {
      object o = s.Pop();
      if (o is Stack r) {
        o = r.Map(f);
      } else {
        o = f(o);
      }
      // q.Enqueue(o);
      q.Push(o);
    }
    return new Stack(q);
  }

  public static string PrettyName(this Type type) {
    if (type.GetGenericArguments().Length == 0) {
      if (type.IsNumericType()) {
        return type.NumericTypeAsString();
      } else {
        return type.Name;
      }
    }
    var genericArguments = type.GetGenericArguments();
    var typeDef = type.Name;
    if (typeDef.Contains("`")) {
      var unmangledName = typeDef.Substring(0, typeDef.IndexOf("`"));
      return unmangledName
        + "<" + string.Join(",", genericArguments
                                   .Select(t => t.PrettyName())
                                   .ToArray())
        + ">";
    } else {
      return typeDef;
    }
  }

  // public static string PrettySignature(this MethodInfo mi,
  //                                      bool includeClassName = false) {
  //   var ps = mi.GetParameters()
  //     .Select(p => String.Format("{0} {1}",
  //                                p.ParameterType.PrettyName(),
  //                                p.Name));

  //   return String.Format("{4}{0} {3}{1}({2})", mi.ReturnType.PrettyName(),
  //                        mi.Name,
  //                        string.Join(", ", ps.ToArray()),
  //                        includeClassName
  //                          ? mi.DeclaringType.PrettyName() + "."
  //                          : "",
  //                        mi.IsStatic ? "static " : "");
  // }
  public static Type ToType(this string s) {
    switch (s) {
      case "bool":
        return typeof(bool);
      case "byte":
        return typeof(byte);
      case "sbyte":
        return typeof(sbyte);
      case "char":
        return typeof(char);
      case "ushort":
        return typeof(ushort);
      case "uint":
        return typeof(uint);
      case "ulong":
        return typeof(ulong);
      case "short":
        return typeof(short);
      case "int":
        return typeof(int);
      case "long":
        return typeof(long);
      case "decimal":
        return typeof(decimal);
      case "double":
        return typeof(double);
      case "float":
        return typeof(float);
      default:
        return Type.GetType(s, true);
    }
  }

  public static string NumericTypeAsString(this Type t) {
    if (t.IsEnum)
      return null;
    switch (Type.GetTypeCode(t)) {
      case TypeCode.Boolean:
        return "bool";
      case TypeCode.Byte:
        return "byte";
      case TypeCode.SByte:
        return "sbyte";
      case TypeCode.Char:
        return "char";
      case TypeCode.UInt16:
        return "ushort";
      case TypeCode.UInt32:
        return "uint";
      case TypeCode.UInt64:
        return "ulong";
      case TypeCode.Int16:
        return "short";
      case TypeCode.Int32:
        return "int";
      case TypeCode.Int64:
        return "long";
      case TypeCode.Decimal:
        return "decimal";
      case TypeCode.Double:
        return "double";
      case TypeCode.Single:
        return "float";
      default:
        return null;
    }
  }


  // public static CompleterEntity ToEntity(this ICoercer coercer) {
  //   return new CompleterEntity(coercer);
  // }

  public static bool IsNumericType(this Type t) {
    if (t.IsEnum)
      return false;
    switch (Type.GetTypeCode(t)) {
      case TypeCode.Boolean: // XXX This is a lie!
      case TypeCode.Byte:
      case TypeCode.SByte:
      case TypeCode.Char:
      case TypeCode.UInt16:
      case TypeCode.UInt32:
      case TypeCode.UInt64:
      case TypeCode.Int16:
      case TypeCode.Int32:
      case TypeCode.Int64:
      case TypeCode.Decimal:
      case TypeCode.Double:
      case TypeCode.Single:
        return true;
      default:
        return false;
    }
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

  // https://stackoverflow.com/questions/3537657/c-sharp-how-to-create-an-array-from-an-enumerator
  public static IEnumerable<T> ToEnumerable<T>(this IEnumerator<T> enumerator) {
    while(enumerator.MoveNext())
      yield return enumerator.Current;
  }

  public static IEnumerable ToEnumerable(this IEnumerator enumerator) {
    while(enumerator.MoveNext())
      yield return enumerator.Current;
  }

  // public static Type GetReprType(this IReprType repr) {
  //   return repr.type;
  // }

  public static Type GetReprType(this object repr) {
    if (repr is IReprType rt)
      return rt.type;
    else
      return repr.GetType();
  }

  public static Stack Conj(this Stack s, object o) {
    s.Push(o);
    return s;
  }

  public static Queue Conj(this Queue s, object o) {
    s.Enqueue(o);
    return s;
  }

  public static object Sepr(this Stack s) {
    return s.Pop();
  }

  public static object Sepr(this Queue s) {
    return s.Dequeue();
  }

  // https://stackoverflow.com/questions/7391348/c-sharp-clone-a-stack
  public static Stack<T> Clone<T>(this Stack<T> original) {
    var arr = new T[original.Count];
    original.CopyTo(arr, 0);
    Array.Reverse(arr);
    return new Stack<T>(arr);
  }

  public static Stack Cons(object o, Stack s) {
    s.Push(o);
    return s;
  }

  public static object Car(this Stack s) => s.Peek();

  public static Stack Cdr(this Stack s) {
    s = (Stack) s.Clone();
    s.Pop();
    return s;
  }

  // public static String ToRepr(this object o) => o.ToString();
  // public static String ToRepr(this int o) => o.ToString();

  public static String ToRepr(this IDictionary dict) {
    var sb = new StringBuilder();
    sb.Append("{");
    foreach(DictionaryEntry kv in dict) {
      sb.Append(" ");
      sb.Append(kv.Key.ToString());
      sb.Append(" -> ");
      // This requires System.DynamicRuntime and Microsoft.CSharp
      // dynamic v = kv.Value;
      // sb.Append(v.ToRepr());
      object v = kv.Value;
      sb.Append(v.ToReprQuasiDynamic());
      // if (v is Stack s)
      //   sb.Append(s.ToRepr());
      // else if (v is Dictionary<K,V> d)
      //   sb.Append(d.ToRepr());
      // else
      //   sb.Append(v.ToString());
      sb.Append(",");
    }
    if (sb.Length > 1)
      sb.Remove(sb.Length - 1, 1);
    sb.Append(" }");
    return sb.ToString();
  }

  public static string ToReprQuasiDynamic(this object v) {
    if (v is Stack s)
      return s.ToRepr();
    else if (v is IEnumerable e)
      return e.ToRepr();
    else if (v is IDictionary d)
      return d.ToRepr();
    else if (v is Type t)
      return t.PrettyName();
    else
      return v.ToString();
  }

  public static string ToRepr(this IEnumerable e) {
    var sb = new StringBuilder();
    sb.Append("E(");
    foreach(object x in e) {
      sb.Append(x.ToReprQuasiDynamic());
      sb.Append(", ");
    }
    if (sb.Length > 2)
      sb.Remove(sb.Length - 2, 2);
    sb.Append(")");
    return sb.ToString();
  }

  public static object Peek(this IEnumerable e) {
    return e.Cast<object>().First();
  }

  // public static Stack Cons(this Stack s, object o) {
  //   s.Push(o);
  //   return s;
  // }

  public static IEnumerable Cdr(this IEnumerable e) {
    return e.Cast<object>().Skip(1).CastBack();
  }

  public static IEnumerable CastBack<T>(this IEnumerable<T> e) {
    foreach(T x in e)
      yield return x;
  }

  public static bool Any(this IEnumerable e) {
    foreach (var x in e)
      return true;
    return false;
  }

  public static Stack ToStack(this IEnumerable e) {
    var s = new Stack();
    foreach (var x in e)
      s.Push(x);
    return s;
  }

  public static Parser<T> FailOnThrow<T>(this Parser<T> parser) {
    if (parser == null) throw new ArgumentNullException("parser");

    return i => {
        try {
          var result = parser(i);
          return result;
        } catch (Exception e) {
          return Result.Failure<T>(i, $"Got exception {e}.", new string[0]);
        }
      };
  }

  public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TKey, TValue> func) {
    TValue val;
    if (! dict.TryGetValue(key, out val))
        dict.Add(key, val = func(key));
    return val;
  }
}
}
