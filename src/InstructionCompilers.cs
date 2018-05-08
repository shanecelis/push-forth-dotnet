// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Linq;
// using System.Linq.Expressions;

// namespace SeawispHunter.PushForth {

// public class Compiled: Tuple<Expression> {
//   public Compiled(Expression s) : base(s) { }

//   public Expression expression => Item1;
// }
// public class BinaryInstructionCompiler<X, Y> : Instruction {
//   Action<Stack, X, Y> func;
//   public BinaryInstructionCompiler(Action<Stack, X, Y> func) {
//     this.func = func;
//   }

//   public Stack Apply(Stack stack) {
//     object a, b;
//     a = stack.Pop();
//     b = stack.Pop();
//     func(stack, (X) a, (Y) b);
//     // Expression.Invoke(func, Compile(stack), Compile(a), Compile(b));
//     return stack;
//   }

//   public static Expression Compile(object x) {
//     if (x is Expression e)
//       return e;
//     else
//       return Expression.Constant(x);
//   }

//   public static Instruction WithResult<Z>(Func <X,Y,Z> func) {
//     return new InstructionFunc(stack =>
//         {
//           Expression<Func<Stack, Stack>> f = (_stack) => {
//             object a, b;
//             a = _stack.Pop();
//             b = _stack.Pop();
//             _stack.Push(func((X) a, (Y) b));
//             return _stack;
//           };
//           stack.Push(f);
//           return stack;
//         });
//   }
// }

// }
