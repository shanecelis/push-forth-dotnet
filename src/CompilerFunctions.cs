using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace SeawispHunter.PushForth {

public static class CompilerFunctions {
  public static object Car(Stack s) => s.Car();
  public static Stack Cdr(Stack s) => s.Cdr(); 
  public static Stack Unit(object o) {
    var s = new Stack();
    s.Push(o);
    return s;
  }

  // The order of the arguments is backwards here.
  public static Stack Cons(Stack s, object o) {
    s.Push(o);
    return s;
  }

}

}
