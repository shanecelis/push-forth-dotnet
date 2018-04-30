using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sprache;
using OneOf;

namespace SeawispHunter.PushForth {

internal class StackParser {

  private static readonly Parser<char> _quotedText =
    Parse.AnyChar.Except(Parse.Char('"'));

  private static readonly Parser<char> escapedChar =
    from _ in Parse.Char('\\')
    from c in Parse.AnyChar
    select c;

  private static readonly Parser<string> quotedString =
    from open in Parse.Char('"')
    from text in escapedChar.Or(_quotedText).Many().Text()
    from close in Parse.Char('"')
    from trailingSpaces in Parse.Char(' ').Many()
    select text;

  public static readonly Parser<string> bareWord = Parse.CharExcept(" []").AtLeastOnce().Text().Token();

  public static readonly Parser<Symbol> symbol = bareWord.Select(s => new Symbol(s));

  // https://stackoverflow.com/questions/21414309/sprache-parse-signed-integer
  private static readonly Parser<int> integer =
    from op in Parse.Optional(Parse.Char('-').Token())
    from num in Parse.Number
    from trailingSpaces in Parse.Char(' ').Many()
    select int.Parse(num) * (op.IsDefined ? -1 : 1);

  private static readonly Parser<float> floatRep =
    from op in Parse.Optional(Parse.Char('-').Token())
    from num in Parse.Decimal
    from f in Parse.Char('f')
    from trailingSpaces in Parse.Char(' ').Many()
    select float.Parse(num) * (op.IsDefined ? -1 : 1);

  private static readonly Parser<double> doubleRep =
    from op in Parse.Optional(Parse.Char('-').Token())
    from num in Parse.Number
    from dot in Parse.Char('.')
    from frac in Parse.Number
    from trailingSpaces in Parse.Char(' ').Many()
    select double.Parse(num + "." + frac) * (op.IsDefined ? -1 : 1);

  // private static readonly Parser<Cell> cell =
  //   // quotedString.Or(bareWord).Or(integer.Select(i => i.ToString()));
  //   quotedString.ToCell().Or(symbol.ToCell());

  private static readonly Parser<object> cell =
    // quotedString.Or(bareWord).Or(integer.Select(i => i.ToString()));
    // quotedString.ToCell().Or(integer.ToCell()).Or(symbol.ToCell());
    quotedString.ToCell().Or(floatRep.ToCell()).Or(doubleRep.ToCell()).Or(integer.ToCell()).Or(symbol.ToCell());

  internal static readonly Parser<Stack> stackRep =
    from lbracket in Parse.Char('[')
    from leadingSpaces in Parse.Char(' ').Many()
    from contents in cell.Or(Parse.Ref(() => stackRep)).Many()
    // from contents in cell.Many()
    from rbracket in Parse.Char(']')
    from trailingSpaces in Parse.Char(' ').Many()
    select new Stack(contents.Reverse().ToArray());

}
}
