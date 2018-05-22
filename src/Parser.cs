using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sprache;
using OneOf;

namespace SeawispHunter.PushForth {

public class StackParser {

  private static readonly Parser<char> _quotedText =
    Parse.AnyChar.Except(Parse.Char('"'));

  private static readonly Parser<char> escapedChar =
    from _ in Parse.Char('\\')
    from c in Parse.AnyChar
    select c;

  private static readonly Parser<char> quotedChar =
    from open in Parse.Char('\'')
    from c in Parse.CharExcept("'")
    from close in Parse.Char('\'')
    from trailingSpaces in Parse.WhiteSpace.Many()
    select c;

  private static readonly Parser<string> quotedString =
    from open in Parse.Char('"')
    from text in escapedChar.Or(_quotedText).Many().Text()
    from close in Parse.Char('"')
    from trailingSpaces in Parse.WhiteSpace.Many()
    select text;

  public static readonly Parser<string> bareWord = Parse.CharExcept(" \n\t[]•").AtLeastOnce().Text().Token();

  public static readonly Parser<Symbol> symbol = bareWord.Select(s => new Symbol(s));

  // https://stackoverflow.com/questions/21414309/sprache-parse-signed-integer
  private static readonly Parser<int> integer =
    from op in Parse.Optional(Parse.Char('-').Token())
    from num in Parse.Number
    from trailingSpaces in Parse.WhiteSpace.Many()
    select int.Parse(num) * (op.IsDefined ? -1 : 1);

  private static readonly Parser<float> floatRep =
    from op in Parse.Optional(Parse.Char('-').Token())
    from num in Parse.Decimal
    from f in Parse.Char('f')
    from trailingSpaces in Parse.WhiteSpace.Many()
    select float.Parse(num) * (op.IsDefined ? -1 : 1);

  private static readonly Parser<Type> typeofLiteral =
    from _ in Parse.String("typeof")
    from s in Parse.CharExcept(") ").Many().Text()
    .Contained(Parse.Char('(').Token(),
               Parse.Char(')').Token())
    select s.ToType();

  private static readonly Parser<bool> trueLiteral =
    from s in Parse.String("true")
    from trailingSpaces in Parse.WhiteSpace.Many()
    select true;

  private static readonly Parser<bool> falseLiteral =
    from s in Parse.String("false")
    from trailingSpaces in Parse.WhiteSpace.Many()
    select false;

  private static readonly Parser<bool> booleanLiteral =
    trueLiteral.Or(falseLiteral);

  private static readonly Parser<double> doubleRep =
    from op in Parse.Optional(Parse.Char('-').Token())
    from num in Parse.Number
    from dot in Parse.Char('.')
    from frac in Parse.Number
    from trailingSpaces in Parse.WhiteSpace.Many()
    select double.Parse(num + "." + frac) * (op.IsDefined ? -1 : 1);

  // private static readonly Parser<Cell> cell =
  //   // quotedString.Or(bareWord).Or(integer.Select(i => i.ToString()));
  //   quotedString.ToCell().Or(symbol.ToCell());

  private static readonly Parser<object> cell =
    // quotedString.Or(bareWord).Or(integer.Select(i => i.ToString()));
    // quotedString.ToCell().Or(integer.ToCell()).Or(symbol.ToCell());
    quotedString.ToCell().Or(quotedChar.ToCell()).Or(booleanLiteral.ToCell()).Or(floatRep.ToCell()).Or(doubleRep.ToCell()).Or(integer.ToCell()).Or(typeofLiteral.ToCell()).Or(symbol.ToCell());

  internal static readonly Parser<Stack> stackRep =
    from precedingSpaces in Parse.WhiteSpace.Many()
    from lbracket in Parse.Char('[')
    from leadingSpaces in Parse.WhiteSpace.Many()
    from contents in cell.Or(Parse.Ref(() => stackRep)).Many()
    // from contents in cell.Many()
    from rbracket in Parse.Char(']')
    from trailingSpaces in Parse.WhiteSpace.Many()
    select new Stack(contents.Reverse().ToArray());

  private static readonly Parser<Variable> varLiteral =
    from _ in Parse.Char('\'')
    from s in bareWord
    select new Variable(s);

  private static readonly Parser<Type> varTypeLiteral =
    from _ in Parse.Char('\'')
    from c in Parse.Chars("abcd")
    from trailingSpaces in Parse.WhiteSpace.Many()
    select Variable.TypeFromChar(c);

  private static readonly Parser<Type> typeLiteral =
    from s in bareWord
    select s.ToType();

  internal static readonly Parser<Stack> typeRep =
    from lbracket in Parse.Char('[')
    from leadingSpaces in Parse.WhiteSpace.Many()
    from contents in varLiteral.ToCell().Or(typeLiteral.FailOnThrow().ToCell()).Many()
    from rbracket in Parse.Char(']')
    from trailingSpaces in Parse.WhiteSpace.Many()
    select new Stack(contents.Reverse().ToArray());

  internal static readonly Parser<Stack<OneOf<Type, Variable>>> typeRep2 =
    from lbracket in Parse.Char('[')
    from leadingSpaces in Parse.WhiteSpace.Many()
    from contents in varLiteral.ToTypeOrVariable().Or(typeLiteral.FailOnThrow().ToTypeOrVariable()).Many()
    from rbracket in Parse.Char(']')
    from trailingSpaces in Parse.WhiteSpace.Many()
    select new Stack<OneOf<Type, Variable>>(contents.Reverse().ToArray());

  internal static readonly Parser<Stack<Type>> typeRep3 =
    from lbracket in Parse.Char('[')
    from leadingSpaces in Parse.WhiteSpace.Many()
    from contents in varTypeLiteral.Or(typeLiteral.FailOnThrow()).Many()
    from rbracket in Parse.Char(']')
    from trailingSpaces in Parse.WhiteSpace.Many()
    select new Stack<Type>(contents.Reverse().ToArray());

  private static readonly Parser<char> pivotChar = Parse.Char('•');

  internal static readonly Parser<Stack> pivotRep =
    from precedingSpaces in Parse.WhiteSpace.Many()
    from lbracket in Parse.Char('[')
    from leadingSpaces in Parse.WhiteSpace.Many()
    from code in cell.Or(Parse.Ref(() => stackRep)).Many()
    // from code in Parse.Not(pivotChar).Then(c => cell.Or(Parse.Ref(() => stackRep)).Many())
    from pivot in pivotChar
    from pivotSpaces in Parse.WhiteSpace.Many()
    from data in cell.Or(Parse.Ref(() => stackRep)).Many()
    // from contents in cell.Many()
    from rbracket in Parse.Char(']')
    from trailingSpaces in Parse.WhiteSpace.Many()
    select Interpreter.Cons(new Stack(code.ToArray()), new Stack(data.Reverse().ToArray()));


  public static Stack ParseStack(string s) => stackRep.Parse(s);

  public static Stack ParsePivot(string s) => pivotRep.Parse(s);

  public static Stack ParseTypeSignature(string s) => typeRep.Parse(s);

  public static Stack<OneOf<Type, Variable>>
    ParseTypeSignature2(string s) => typeRep2.Parse(s);

  public static Stack<Type>
    ParseTypeSignature3(string s) => typeRep3.Parse(s);

  // XXX Rename to ParseWithSubstitution
  public static Stack ParseWithResolution<T>(string s,
                                             Dictionary<string, T> dict) {
    Parser<object> cell =
      quotedString.ToCell().Or(booleanLiteral.ToCell()).Or(floatRep.ToCell()).Or(doubleRep.ToCell()).Or(integer.ToCell()).Or(typeofLiteral.ToCell()).Or(symbol.Resolve(dict));
    Parser<Stack> stackRep = null;
    stackRep =
      from lbracket in Parse.Char('[')
      from leadingSpaces in Parse.WhiteSpace.Many()
      from contents in cell.Or(Parse.Ref(() => stackRep)).Many()
      // from contents in cell.Many()
      from rbracket in Parse.Char(']')
      from trailingSpaces in Parse.WhiteSpace.Many()
      select new Stack(contents.Reverse().ToArray());
    return stackRep.Parse(s);
  }
}
}
