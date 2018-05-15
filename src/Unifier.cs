// https://raw.githubusercontent.com/elkdanger/jigsaw-library/da2019975dc85863695e1ee8d64a0b4be7e4eb84/Utilities/Unifier.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SeawispHunter.PushForth;

public class Unifier {

  public static bool IsConstant(object e) {
    return !(e is Stack) && !(e is string);
  }

  public static bool Occurs(string s, Stack e) {
    if (e.Peek() is string str)
      if (str == s)
        return true;
    if (e.Peek() is Stack st)
      return Occurs(s, st);
    var tail = e.Cdr();
    return ! tail.Any() ? false : Occurs(s, tail);
  }

  public static object Substitute(Dictionary<string, dynamic> bindings,
                                  object x) {
    if (x is String str && bindings.ContainsKey(str))
      return bindings[str];
    if (x is Stack s && s.Any())
      return PushForthExtensions.Cons(Substitute(bindings, s.Peek()),
                                      (Stack) Substitute(bindings, s.Cdr()));
    return x;
  }

  public static Dictionary<string, object> Unify(object e1, object e2) {
    if ((IsConstant(e1) && IsConstant(e2))) {
      if (e1 == e2)
        return new Dictionary<string,object>();
      throw new Exception("Unification failed");
    }

    if (e1 is string s1) {
      if (e2 is Stack st2 && Occurs(s1, st2))
        throw new Exception("Cyclical binding");
      return new Dictionary<string, object>() { { s1, e2 } };
    }

    if (e2 is string _s2) {
      if (e1 is Stack _st1 && Occurs(_s2, _st1))
        throw new Exception("Cyclical binding");
      return new Dictionary<string, object>() { { _s2, e1 } };
    }

    // if (!(e1 is Stack) || !(e2 is Stack))
    //   throw new Exception("Expected either list, string, or constant arguments");

    if (e1 is Stack st1c && e2 is Stack st2c) {
      if (! st1c.Any() || ! st2c.Any()) {
        if (st1c.Any() || st2c.Any())
          throw new Exception("Stacks are not the same length");
        return new Dictionary<string, object>();
      }
      var b1 = Unify(st1c.Peek(), st2c.Peek());
      var b2 = Unify(Substitute(b1, st1c.Cdr()), Substitute(b1, st2c.Cdr()));

      foreach (var kv in b2)
        b1.Add(kv.Key, kv.Value);
      return b1;
    } else {
      throw new Exception("Expected either list, string, or constant arguments");
    }

  }
}
