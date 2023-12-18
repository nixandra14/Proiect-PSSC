using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt;
using static LanguageExt.Prelude;

namespace Proiect2.Domain.Models
{
    public record Total
    {
        public decimal Value { get; }

        internal Total(decimal value)
        {
            if (IsValid(value))
            {
                Value = value;
            }
            else
            {
                throw new Exception($"{value:0.##} is an invalid total value.");
            }


        }
        public static Option<Total> TryParse(decimal decimalValue)
        {
            if (IsValid(decimalValue))
            {
                return Some<Total>(new(decimalValue));
            }
            else
            {
                return None;
            }
        }
        private static bool IsValid(decimal numericTotal) => numericTotal != -1;
    }
    }

