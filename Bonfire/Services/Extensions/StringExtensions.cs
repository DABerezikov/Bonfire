﻿using System.Globalization;
using System.Text.RegularExpressions;

namespace Bonfire.Services.Extensions;

public static class StringExtension
{
    public static double DoubleParseAdvanced(this string strToParse, char decimalSymbol = ',')
    {
        string tmp = Regex.Match(strToParse, @"([-]?[0-9]+)([\s])?([0-9]+)?[." + decimalSymbol + "]?([0-9 ]+)?([0-9]+)?").Value;

        if (tmp.Length > 0 && strToParse.Contains(tmp))
        {
            var currDecSeparator = NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator;

            tmp = tmp.Replace(".", currDecSeparator).Replace(decimalSymbol.ToString(), currDecSeparator);
            tmp = tmp.Replace(",", currDecSeparator).Replace(decimalSymbol.ToString(), currDecSeparator);

            return double.Parse(tmp);
        }

        return 0;
    }

    public static decimal DecimalParseAdvanced(this string strToParse, char decimalSymbol = ',')
    {
        string tmp = Regex.Match(strToParse, @"([-]?[0-9]+)([\s])?([0-9]+)?[." + decimalSymbol + "]?([0-9 ]+)?([0-9]+)?").Value;

        if (tmp.Length > 0 && strToParse.Contains(tmp))
        {
            var currDecSeparator = NumberFormatInfo.CurrentInfo.CurrencyDecimalSeparator;

            tmp = tmp.Replace(".", currDecSeparator).Replace(decimalSymbol.ToString(), currDecSeparator);
            tmp = tmp.Replace(",", currDecSeparator).Replace(decimalSymbol.ToString(), currDecSeparator);

            return decimal.Parse(tmp);
        }

        return 0;
    }
}