﻿using System;
using System.Collections;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using SeawispHunter.PushForth;

namespace MyBenchmarks
{
public class Md5VsSha256
{
  private const int N = 10000;
  private readonly byte[] data;

  private readonly SHA256 sha256 = SHA256.Create();
  private readonly MD5 md5 = MD5.Create();

  public Md5VsSha256()
  {
    data = new byte[N];
    new Random(42).NextBytes(data);
  }

  [Benchmark]
  public byte[] Sha256() => sha256.ComputeHash(data);

  [Benchmark]
  public byte[] Md5() => md5.ComputeHash(data);
}

public class InterpreterVsCompiler {

  public string _programString = "[[]]";
  // public string programString = "[[2 1 +]]";
  [Params("[[]]", "[[2 1 +]]", "[[2 1 + 2 5 + *]]")]
  public string programString {
    get => _programString;
    set {
      _programString = value;
      _program = null;
    }
  }
  // public string programString = "[[2 1 + 2 5 + *]]";
  public Interpreter interpreter;
  public Compiler compiler;
  Func<Stack> compiledProgram;

  Stack _program;
  Stack program {
    get {
      if (_program == null) {
        _program = programString.ToStack();
      }
      return _program;
    }
  }
  public InterpreterVsCompiler() {
    interpreter = new Interpreter();
    compiler = new Compiler();
    compiledProgram = compiler.Compile(programString.ToStack());
  }
  Stack Copy(Stack s) {
    // XXX Not a generic clone.
    var code = (Stack) s.Peek();
    return new Stack(new [] { code.Clone() });
  }
  [Benchmark]
  public Stack Parse() => programString.ToStack();

  [Benchmark]
  public string ToRepr() => program.ToRepr();

  [Benchmark]
  public Stack CopyProgram() => Copy(program);

  [Benchmark]
  public Stack Interpreter() => interpreter.Run(Copy(program));

  [Benchmark]
  public Func<Stack> Compile() {
    return compiler.Compile(Copy(program));
  }

  [Benchmark]
  public Stack CompileAndRun() {
    var f = compiler.Compile(Copy(program));
    return f();
  }

  [Benchmark]
  public Stack Run() {
    return compiledProgram();
  }
}

public class Program
{
  public static void Main(string[] args)
  {
    // var summary = BenchmarkRunner.Run<Md5VsSha256>();
    var summary = BenchmarkRunner.Run<InterpreterVsCompiler>();
  }
}
}
