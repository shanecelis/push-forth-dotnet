using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SeawispHunter.PushForth;

public class Variable : Tuple<string> {
  public Variable(string name) : base(name) { }
  public string name => Item1;
}

/*
  Eh, just rewrote it using page 328 from Russell and Norvig.
 */
public static class Unifier {

  public static Dictionary<string, object>
    UnifyVar(Variable v, object x, Dictionary<string, object> theta) {
    if (theta.ContainsKey(v.name))
      return Unify(theta[v.name], x, theta);
    else if (x is Variable w && theta.ContainsKey(w.name))
      return Unify(v, theta[w.name], theta);
    else if (OccurCheck(v, x))
      throw new Exception("Cyclical binding");
      // return null;
    else {
      theta[v.name] = x;
      return theta;
    }
  }

  public static bool OccurCheck(Variable v, object x) {
    if (x is string s) {
      return v.name == s;
    } else if (x is IEnumerable e)
      return e.Cast<object>().Any(o => OccurCheck(v, o));
    else
      return false;
  }

  public static Dictionary<string, object>
    Unify(object a, object b) => Unify(a, b, new Dictionary<string, object>());

  public static Dictionary<string, object>
    Unify(object a, object b, Dictionary<string, object> theta) {

    if (a.Equals(b))
      return theta;
    else if (a is Variable v)
      return UnifyVar(v, b, theta);
    else if (b is Variable w)
      return UnifyVar(w, a, theta);
    else if (a is IEnumerable e && b is IEnumerable f) {
      if (! e.Any() || ! f.Any()) {
        if (e.Any() || f.Any())
          throw new Exception("Lists are not the same length");
        else
          return theta;
      }
      return Unify(e.Cdr(), f.Cdr(), Unify(e.Peek(), f.Peek(), theta));
    } else
      throw new Exception($"Expected either list, string, or constant arguments; not e1 {a.ToReprQuasiDynamic()} and e2 {b.ToReprQuasiDynamic()}");
  }

  public static object Substitute(Dictionary<string, dynamic> bindings,
                                  object x) {
    if (x is string str && bindings.ContainsKey(str))
      return bindings[str];
    if (x is IEnumerable s)
      return s.Cast<object>().Select(o => Substitute(bindings, o)).CastBack();
    return x;
  }
}

public class Unifier2 {

  public static bool IsConstant(object e) {
    return !(e is IEnumerable) && !(e is string);
  }

  public static bool Occurs(string s, IEnumerable e) {
    // e.Cast<object>.Any(x => 
    foreach(var x in e) {
      if (x is string t) {
        if (s == t)
          return true;
      } else if (x is IEnumerable f) {
        // XXX This looks wrong.
        // return Occurs(s, f);
        if (Occurs(s, f))
          return true;
      }
    }
    return false;
  }

  public static object Substitute(Dictionary<string, dynamic> bindings,
                                  object x) {
    if (x is string str && bindings.ContainsKey(str))
      return bindings[str];
    if (x is IEnumerable s)
      return s.Cast<object>().Select(o => Substitute(bindings, o)).CastBack();
      // return PushForthExtensions.Cons(Substitute(bindings, s.Peek()),
      //                                 (Stack) Substitute(bindings, s.Cdr()));
    return x;
  }

  public static void Unify(Variable v, object o, Dictionary<string, object> theta) {
    if (o is IEnumerable e && Occurs(v.name, e))
      throw new Exception("Cyclical binding");
    else
      theta[v.name] = o;
  }

  public static Dictionary<string, object> Unify(object e1, object e2) {
    if (IsConstant(e1) && IsConstant(e2)) {
      if (e1.Equals(e2))
        return new Dictionary<string,object>();
      else
        throw new Exception($"Unification failed for e1 {e1} and e2 {e2}.");
    }

    if (e1 is string s1) {
      if (e2 is IEnumerable st2 && Occurs(s1, st2))
        throw new Exception("Cyclical binding");
      else
        return new Dictionary<string, object>() { { s1, e2 } };
    }

    if (e2 is string s2) {
      if (e1 is IEnumerable st1 && Occurs(s2, st1))
        throw new Exception("Cyclical binding");
      else
        return new Dictionary<string, object>() { { s2, e1 } };
    }

    if (e1 is IEnumerable st1c && e2 is IEnumerable st2c) {
      if (! st1c.Any() || ! st2c.Any()) {
        if (st1c.Any() || st2c.Any())
          throw new Exception("Lists are not the same length");
        else
          return new Dictionary<string, object>();
      }
      var b1 = Unify(st1c.Peek(), st2c.Peek());
      var b2 = Unify(Substitute(b1, st1c.Cdr()),
                     Substitute(b1, st2c.Cdr()));

      // Merge b2 into b1.
      foreach (var kv in b2)
        b1.Add(kv.Key, kv.Value);

      return b1;
    } else {
      throw new Exception($"Expected either list, string, or constant arguments; not e1 {e1.ToReprQuasiDynamic()} and e2 {e2.ToReprQuasiDynamic()}");
    }
  }
}
