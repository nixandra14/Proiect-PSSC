using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LanguageExt;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static LanguageExt.Prelude;

namespace Proiect2.Domain.Models
{
    public record CardNumber
    {
        public const string Pattern = "^[0-9]{16}$";
        private static readonly Regex ValidPattern = new(Pattern);

        public string Value { get; }

        public CardNumber(string value)
        {

            if (IsValid(value))
            {
                Value = value;
            }
            else
            {
                throw new InvalidCardNumberException($"{value} is invalid");
            }
        }

        public static bool IsValid(string stringValue) => ValidPattern.IsMatch(stringValue);

        public override string ToString()
        {
            return Value;
        }

        public static Option<CardNumber> TryParseCardNumber(string stringValue)
        {
            if (IsValid(stringValue))
            {
                return Some<CardNumber>(new(stringValue));
            }
            else
            {
                return None;
            }
        }
    }
}
