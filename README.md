Push-forth
==========

Push-forth is a light-weight, strongly-typed, stack-based genetic programming language designed by Maaren Keijzer and detailed in his [2013 paper](https://www.lri.fr/~hansen/proceedings/2013/GECCO/companion/p1635.pdf).  This project is an implementation of an interpreter and a compiler in dotnet by Shane Celis.

Overview
--------

Push-forth works like this.  It is a stack-based language.  The top item of the stack defines the code.  The rest of the stack provides the data.  The evaluator pops off an item from the code stack.  If it's an instruction, it's executed pulling its arguments from the stack and pushing its results to the stack.  If it's not an instruction, it's pushed onto the data stack.

    [[1 1 +]] 
    [[1 +] 1] 
    [[+] 1 1] 
    [[] 2]

This form makes it easy to embed and evaluate other programs within the language.

    [[eval eval eval] [[1 1 +]]]
    [[eval eval] [[1 +] 1]]
    [[eval] [[+] 1 1]]
    [[] [[] 2]]

Interpreters
------------

There are actually a handful of Interpreter variants in this project.  The class `Interpreter` is the base class.  It defines the evaluator but does not define any instructions.  It's referred to as the "clean" interpreter within the tests.

The `NonstrictInterpreter` is an interpreter that most closely matches Keijzer's description.  Its instructions will be applied most forgivingly such that errors caused by too few arguments on the stack will turn into noops, and wrongly typed arguments will be skipped.

The `StrictInterpreter` is not forgiving.  If there are wrongly typed arguments, exceptions are thrown.  If there are too few arguments, exceptions are thrown.  This interpreter is not intended for genetic programming.

The `ReorderInterpreter` is a special case that takes a program and applies the forgiving approaches of the `NonstrictInterpreter` to produce a new program which may be executed by the `StrictInterpreter`.  This "reordering" is discussed by La Cava, Helmuth, and Spector in their [2015 paper](https://dl.acm.org/citation.cfm?id=2754763).

Lastly the `TypeInterpreter` takes a program and returns the types it requires as inputs and the types it produces as outputs.

Compiler
--------

An additional interpreter is called `Compiler`.  It takes a program and generates Intermediate Language (IL) byte-code.  Since the IL virtual machine is stack-based, it can mirror many of Push-forth's operations directly as IL instructions.

The compiler only accepts strict programs, so one can use the `ReorderInterpreter` to take a nonstrict program and make it a strict program.

Dependencies
------------

* [Sprache](https://github.com/sprache/Sprache) is used for parsing.
* [xUnit](https://xunit.github.io/docs/getting-started-dotnet-core.html) is used for unit tests.
* [OneOf](https://github.com/mcintyre321/OneOf) is used for discriminated unions (sum types).  (May have fallen out of use, actually.)
* netstandard2.0

References
----------

* Keijzer, M. (2013). Push-forth: a light-weight, strongly-typed, stack-based genetic programming language. Proceeding of the fifteenth annual conference companion (pp. 1635–1640). New York, New York, USA: ACM. http://doi.org/10.1145/2464576.2482742

* La Cava, W., Helmuth, T., Spector, L., & Danai, K. (2015). Genetic Programming with Epigenetic Local Search (pp. 1055–1062). Presented at the the 2015, New York, New York, USA: ACM Press. http://doi.org/10.1145/2739480.2754763
