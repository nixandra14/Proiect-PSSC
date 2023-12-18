using LanguageExt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static LanguageExt.Prelude;
using LanguageExt.ClassInstances.Pred;

namespace Proiect2.Domain.Models
{
    public record OrderId
    {
        public int Value { get; }

        public OrderId(int value)
        {

            if (IsValid(value))
            {
                Value = value;
            }
            else
            {
                throw new Exception($"{value} is invalid");
            }
        }

        public static Option<OrderId> TryParseOrderId(int intValue)
        {
            if (IsValid(intValue))
            {
                return Some<OrderId>(new(intValue));
            }
            else
            {
                return None;
            }

        }
        private static bool IsValid(int numericID) => numericID != -1;
    }
}
