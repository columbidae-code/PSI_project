using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace beveikZinojau.lt.Services.ServiceHelpers
{

    public static class StringExtensions
    {
        public static bool IsMissing([NotNullWhen(false)] this string? value)
        {
            return string.IsNullOrWhiteSpace(value);
        }
    }
}
