using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace SeawispHunter.PushForth {

public static class CompilerFunctions {
  public static object Car(Stack s) => s.Peek();
  public static Stack Cdr(Stack s) {
    s = (Stack) s.Clone();
    s.Pop();
    return s;
  }
  public static Stack Unit(object o) {
    var s = new Stack();
    s.Push(o);
    return s;
  }

  public static Stack Cons(object o, Stack s) {
    s.Push(o);
    return s;
  }
}

}
