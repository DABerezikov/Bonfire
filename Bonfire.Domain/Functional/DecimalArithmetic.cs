using System.Numerics;

namespace Bonfire.Domain.Functional;

/// <summary>Точные промежуточные значения без переполнения; преобразование в decimal с округлением ToEven.</summary>
public static class DecimalArithmetic
{
    private const string RangeError = "Результат расчёта выходит за допустимый диапазон";
    private static readonly BigInteger MaxCoefficient = (BigInteger.One << 96) - 1;

    public static Result<decimal> Add(decimal left, decimal right) => Combine(left, right, false);

    public static Result<decimal> Subtract(decimal left, decimal right) => Combine(left, right, true);

    private static Result<decimal> Combine(decimal left, decimal right, bool subtract)
    {
        var a = Parts(left);
        var b = Parts(right);
        var scale = Math.Max(a.Scale, b.Scale);
        var first = a.Coefficient * BigInteger.Pow(10, scale - a.Scale);
        var second = b.Coefficient * BigInteger.Pow(10, scale - b.Scale);
        return FromScaled(subtract ? first - second : first + second, scale);
    }

    public static Result<decimal> Multiply(decimal left, decimal right)
    {
        var a = Parts(left);
        var b = Parts(right);
        return FromScaled(a.Coefficient * b.Coefficient, a.Scale + b.Scale);
    }

    public static Result<decimal> Divide(decimal dividend, decimal divisor)
    {
        if (divisor == 0) return Result.Failure<decimal>("Делитель не может быть нулём");
        var a = Parts(dividend);
        var b = Parts(divisor);
        var numerator = BigInteger.Abs(a.Coefficient) * BigInteger.Pow(10, b.Scale);
        var denominator = BigInteger.Abs(b.Coefficient) * BigInteger.Pow(10, a.Scale);
        for (var scale = 28; scale >= 0; scale--)
        {
            var coefficient = Round(numerator * BigInteger.Pow(10, scale), denominator);
            if (coefficient <= MaxCoefficient)
            {
                while (scale > 0 && coefficient % 10 == 0)
                {
                    coefficient /= 10;
                    scale--;
                }

                return Result.Success(Create(coefficient, scale, a.Coefficient.Sign * b.Coefficient.Sign < 0));
            }
        }

        return Result.Failure<decimal>(RangeError);
    }

    public static Result<int> ToInt32(decimal value) => value < int.MinValue || value > int.MaxValue
        ? Result.Failure<int>("Количество выходит за допустимый диапазон")
        : Result.Success((int)value);

    public static Result<decimal> Sum(IEnumerable<decimal> values)
    {
        var result = Result.Success(0m);
        foreach (var value in values)
        {
            result = result.Bind(total => Add(total, value));
            if (result.IsFailure) return result;
        }

        return result;
    }

    private static (BigInteger Coefficient, int Scale) Parts(decimal value)
    {
        var bits = decimal.GetBits(value);
        var magnitude = (uint)bits[0] + ((BigInteger)(uint)bits[1] << 32) + ((BigInteger)(uint)bits[2] << 64);
        return (value < 0 ? -magnitude : magnitude, (bits[3] >> 16) & 0xff);
    }

    private static Result<decimal> FromScaled(BigInteger coefficient, int scale)
    {
        for (var removed = Math.Max(0, scale - 28); removed <= scale; removed++)
        {
            var rounded = Round(BigInteger.Abs(coefficient), BigInteger.Pow(10, removed));
            if (rounded <= MaxCoefficient)
                return Result.Success(Create(rounded, scale - removed, coefficient.Sign < 0));
        }

        return Result.Failure<decimal>(RangeError);
    }

    private static BigInteger Round(BigInteger numerator, BigInteger denominator)
    {
        var quotient = BigInteger.DivRem(numerator, denominator, out var remainder);
        var half = (remainder * 2).CompareTo(denominator);
        return half > 0 || (half == 0 && !quotient.IsEven) ? quotient + 1 : quotient;
    }

    private static decimal Create(BigInteger coefficient, int scale, bool negative)
        // unchecked здесь только перепаковывает проверенные 32-битные слова мантиссы в формат конструктора decimal.
        => new(unchecked((int)(uint)(coefficient & uint.MaxValue)),
            unchecked((int)(uint)((coefficient >> 32) & uint.MaxValue)),
            unchecked((int)(uint)((coefficient >> 64) & uint.MaxValue)), negative, (byte)scale);
}