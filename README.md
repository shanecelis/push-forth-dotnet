Push-forth
==========

Push-forth is a light-weight, strongly-typed, stack-based genetic programming language designed by Maaren Keijzer and detailed in his [2013 paper](https://www.lri.fr/~hansen/proceedings/2013/GECCO/companion/p1635.pdf).  This project is an implementation of a push-forth interpreter and a compiler for .NET by Shane Celis.

Overview
--------

Push-forth is a stack-based language.  It uses the following notation to represent a stack: [a b c ...].  A stack has two principle operations:

* pop([a b c]) -> a, [b c]
* push(z, [b c]) -> void, [z b c]

Here is an example of a push-forth program that adds one plus one.

    [[1 2 +]]

This example works like this.  The evaluator pops "1" off the code stack.  "1" is not an instruction, so the evaluator pushes the "1" onto the data stack.  It does the same thing for the "2".  It pops "+" off the code stack.  The "+" is an instruction, so the evaluator passes the data stack to it.  The "+" instruction pops both "1"s from the data stack, adds them, and pushes the result to the data stack.  When no items are left on the code stack, the interpreter halts.

    [[1 2 +]]
    [[2 +] 1]
    [[+] 2 1]
    [[] 3]

In general there is a stack which holds the code and the data.  The top item of the stack is another stack of code.  The rest of the stack is the data.  The evaluator pops off the code stack, then it pops an item off the code stack.  If it's an instruction, it's executed---pulling its arguments from the data stack and pushing its results to the data stack.  If it's not an instruction, it's pushed onto the data stack so it can be used as an argument.  Finally the code stack is pushed back onto the data stack.  Thus one step of evaluation is completed.


This form of `[[code stack] data stack]` has some nice properties.

* The entire state of a program can be captured by its stack.  Therefore, it's easy to suspend and resume computations.

* There's no return stack as in Forth.

* It's easy to embed and evaluate other programs within it.

```
    [[eval eval eval] [[1 1 +]]]
    [[eval eval] [[1 +] 1]]
    [[eval] [[+] 1 1]]
    [[] [[] 2]]
```

* One can write a push-forth interpreter in push-forth.
```
    [[[eval dup car empty? not] do-while] [[1 2 -]]] -> [[] [[] 1]]
```

* One can write other interpreters in push-forth.

```
    [[[eval2 dup car empty? not] do-while] [[1 2 -]]] -> [[] [[] -1]]
```

Example
-------

To run the one plus two program, here's how one would set it up.

    Stack program = "[[1 2 +]]".ToStack();
    var interpreter = new NonstrictInterpreter();
    program = interpreter.Eval(program); // [[2 +] 1]
    Stack result = interpreter.Run(program); // [[] 3]
    Console.WriteLine(result.ToRepr()); // Prints "[[] 3]" 

Interpreters
------------

There are actually a handful of interpreters in this project.  Most differ only based on their instructions.

The class `Interpreter` is the base interpreter.  It defines the evaluator but does not define any instructions.  It's referred to as the "clean" interpreter within the unit tests.

The `NonstrictInterpreter` is an interpreter that most closely matches Keijzer's description.  Its instructions will be applied most forgivingly such that errors caused by too few arguments on the stack will turn into noops, and wrongly typed arguments will be skipped.

The `StrictInterpreter` is not forgiving.  If there are wrongly typed arguments, exceptions are thrown.  If there are too few arguments, exceptions are thrown.  This interpreter is not intended for genetic programming.

The `ReorderInterpreter` is a special case that takes a program and applies the forgiving approaches of the `NonstrictInterpreter` to produce a new program which may be executed by the `StrictInterpreter`.  This "reordering" is put to good use by La Cava, Helmuth, and Spector in their [2015 paper](https://dl.acm.org/citation.cfm?id=2754763).

Lastly the `TypeInterpreter` takes a program and returns the types it requires as inputs and the types it produces as outputs.

### Instructions Note

Although the instructions differ between each of these interpreters, they are not wholesale re-implementations.  For instance, the `NonstrictInterpreter` uses the same instructions as the `StrictInterpreter` except they are wrapped with the `ReorderWrapper` class.  It handles too few or wrongly typed arguments in general.

Compiler
--------

An additional interpreter is called `Compiler`.  It takes a program and generates Intermediate Language (IL) byte-code.  Since the IL virtual machine is stack-based, it can mirror many of push-forth's operations directly as IL instructions.

The compiler only accepts strict programs, so one can use the `ReorderInterpreter` to take a nonstrict program and make it a strict program.

### Implementation Note

The compiler actually uses the regular push-forth evaluator, but its instructions differ.  It produces `CompilationUnit` objects on the data stack.  Once the program is finished being interpreted, the `CompilationUnit`s left on the data stack are fed an `ILGenerator` to produce IL.

    [[1 1 +]] -> [[] CompilationUnit("ldc.i4 1; ldc.i4 1; add")]

This demonstrates that one can think of a compiler as an interpreter but with a different data type.

### Future Improvements

Since the compiler is an interpreter, it may be easy to implement a mixed-mode compiler that will interpret everything available to it at compile-time.  It's like having an C++'s `constexpr` but automatic.  For evolving programs that may provide a worthwhile heuristic to consider: How much code does anything with data coming in?

* Conventional Interpretation
    [[1 1 +]] -> [[] 2]
* Conventional Compilation
    [[1 1 +]] -> [[] CompilationUnit("ldc.i4 1; ldc.i4 1; add")]
* Mixed Interpretation and Compilation
    [[1 1 +]] -> [[] 2] -> [[] CompilationUnit("ldc.i4 2")]

If there is an external argument "x", the mixed-mode compiler cannot "precompute" its value.

    [[1 1 + x +]] -> [[] [2 x +]] -> [[] CompilationUnit("ldc.i4 2; ldarg.0; add")]

Dependencies
------------

* [Sprache](https://github.com/sprache/Sprache) is used for parsing.
* [xUnit](https://xunit.github.io/docs/getting-started-dotnet-core.html) is used for unit tests.
* [OneOf](https://github.com/mcintyre321/OneOf) is used for discriminated unions (sum types).  (May have fallen out of use, actually.)
* [dotnet toolchain](https://docs.microsoft.com/en-us/dotnet/core/tools/?tabs=netcore2x)

License
-------

This code is licensed under the [MIT License](https://opensource.org/licenses/MIT).

References
----------

* Keijzer, M. (2013). Push-forth: a light-weight, strongly-typed, stack-based genetic programming language. Proceeding of the fifteenth annual conference companion (pp. 1635–1640). New York, New York, USA: ACM. http://doi.org/10.1145/2464576.2482742

* La Cava, W., Helmuth, T., Spector, L., & Danai, K. (2015). Genetic Programming with Epigenetic Local Search (pp. 1055–1062). Presented at the the 2015, New York, New York, USA: ACM Press. http://doi.org/10.1145/2739480.2754763
