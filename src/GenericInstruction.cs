/* Original code Copyright (c) 2018 Shane Celis[1]
   Licensed under the MIT License[2]

   Original code posted here[3].

   This comment generated by code-cite[4].

   [1]: https://github.com/shanecelis
   [2]: https://opensource.org/licenses/MIT
   [3]: https://github.com/shanecelis/push-forth-dotnet/
   [4]: https://github.com/shanecelis/code-cite
*/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OneOf;

namespace PushForth {

/**
 */
public class GenericMethodInstruction<T> : GenericInstruction, TypedInstruction {
  MethodInfo methodInfo;
  FuncFactory<T> factory;
  public Instruction innerInstruction = null;
  internal IEnumerable<Type> _inputTypes = Type.EmptyTypes;
  internal IEnumerable<Type> _outputTypes = Type.EmptyTypes;
  public IEnumerable<Type> inputTypes => _inputTypes;
  public IEnumerable<Type> outputTypes => _outputTypes;

  public GenericMethodInstruction(FuncFactory<T> factory,
                                  MethodInfo methodInfo) {
    this.factory = factory;
    this.methodInfo = methodInfo;
    if (! methodInfo.IsGenericMethodDefinition)
      throw new Exception($"Must provide a generic method; {methodInfo} isn't.");
  }

  public Instruction GetInstruction(IEnumerable<Type> types) {
    var mi = methodInfo.MakeGenericMethod(types.ToArray());
    return (Instruction) factory.FromMethod(mi);
  }

  public Stack Apply(Stack s) {
    if (innerInstruction == null)
      throw new Exception("NYI");
    else
      return innerInstruction.Apply(s);
  }
}

}