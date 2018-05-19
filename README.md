Push-forth
==========

Push-forth is a light-weight, strongly-typed, stack-based genetic programming language designed by Maaren Keijzer and detailed in his [2013 paper](https://www.lri.fr/~hansen/proceedings/2013/GECCO/companion/p1635.pdf).  This project is an implementation of an interpreter and a compiler in dotnet by Shane Celis.

Overview
--------

Push-forth works like this.  It is a stack-based language.  The top item on the stack defines the code.  The rest of the stack provides the data.  The evaluator pops off an item from the code stack.  If it's an instruction, it's executed.  If it's not, it's pushed onto the data stack.

    [[1 1 +]] 
    [[1 +] 1] 
    [[+] 1 1] 
    [[] 2]

This form makes it very easy to embed and evaluate other stacks within the language.

Interpreters
------------

There are actually several variants of the interpreter contained in this project.  The class `Interpreter` is the base class.  It defines the evaluator but does not define any instructions.  It's referred to as the "clean" interpreter within the tests.

The `NonstrictInterpreter` is an interpreter that most closely matches Keijzer's description.  Its instructions will be applied most forgivingly such that errors caused by too few arguments on the stack will turn into noops, wrongly typed arguments will be skipped.

The `StrictInterpreter` is not forgiving.  If there are wrongly typed arguments, exceptions are thrown.  If there are too few arguments, exceptions are thrown.  This interpreter is not intended for genetic programming.

The `ReorderInterpreter` is a special case that takes a program and applies the forgiving approaches of the `NonstrictInterpreter` to produce a new program which may be executed by the `StrictInterpreter`.

Lastly the `TypeInterpreter` takes a program and returns the types it requires as inputs and the types it produces as outputs.

Compiler
--------

An additional interpreter is called `Compiler`.  It takes a program and generates Intermediate Language (IL) byte-code.  Since the IL virtual machine is stack-based, it can mirror many of Push-forth's operations directly as IL instructions.

The compiler only accepts strict programs, so one can use the `ReorderInterpreter` to take a nonstrict program and make it strict.
