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
  I tried to use this code:

  https://raw.githubusercontent.com/elkdanger/jigsaw-library/da2019975dc85863695e1ee8d64a0b4be7e4eb84/Utilities/Unifier.cs

  But I ended up just rewriting it using page 328 from Russell and Norvig.
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
