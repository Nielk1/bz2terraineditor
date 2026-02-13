using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

public readonly struct UInt4 :
    IComparable<UInt4>,
    IEquatable<UInt4>,
    INumber<UInt4>,
    IConvertible
{
    private readonly byte _value;

    public UInt4(byte value)
    {
        if (value > 15)
            throw new ArgumentOutOfRangeException(nameof(value), "UInt4 value must be between 0 and 15.");
        _value = value;
    }

    public static readonly UInt4 MinValue = new UInt4(0);
    public static readonly UInt4 MaxValue = new UInt4(15);

    public byte Value => _value;

    // Implicit and explicit conversions
    public static implicit operator UInt4(byte value) => new UInt4((byte)(value & 0xF));
    public static explicit operator byte(UInt4 n) => n._value;
    public static implicit operator int(UInt4 n) => n._value;
    public static explicit operator UInt4(int value) => new UInt4((byte)(value & 0xF));

    // Arithmetic operators
    public static UInt4 operator +(UInt4 a, UInt4 b) => new UInt4((byte)((a._value + b._value) & 0xF));
    public static UInt4 operator -(UInt4 a, UInt4 b) => new UInt4((byte)((a._value - b._value) & 0xF));
    public static UInt4 operator *(UInt4 a, UInt4 b) => new UInt4((byte)((a._value * b._value) & 0xF));
    public static UInt4 operator /(UInt4 a, UInt4 b) => new UInt4((byte)(b._value == 0 ? 0 : (a._value / b._value) & 0xF));
    public static UInt4 operator %(UInt4 a, UInt4 b) => new UInt4((byte)(b._value == 0 ? 0 : (a._value % b._value) & 0xF));

    // Comparison operators
    public static bool operator ==(UInt4 a, UInt4 b) => a._value == b._value;
    public static bool operator !=(UInt4 a, UInt4 b) => a._value != b._value;
    public static bool operator <(UInt4 a, UInt4 b) => a._value < b._value;
    public static bool operator >(UInt4 a, UInt4 b) => a._value > b._value;
    public static bool operator <=(UInt4 a, UInt4 b) => a._value <= b._value;
    public static bool operator >=(UInt4 a, UInt4 b) => a._value >= b._value;

    // Unary operators
    public static UInt4 operator --(UInt4 value) => new UInt4((byte)((value._value - 1) & 0xF));
    public static UInt4 operator ++(UInt4 value) => new UInt4((byte)((value._value + 1) & 0xF));
    public static UInt4 operator -(UInt4 value) => new UInt4((byte)((16 - value._value) & 0xF)); // Negation in modular arithmetic
    public static UInt4 operator +(UInt4 value) => value; // Unary plus, just returns the value

    // IComparable and IEquatable implementations
    public int CompareTo(UInt4 other) => _value.CompareTo(other._value);
    public bool Equals(UInt4 other) => _value == other._value;
    public override bool Equals(object? obj) => obj is UInt4 n && Equals(n);
    public override int GetHashCode() => _value;

    public override string ToString() => _value.ToString();

    // INumber<UInt4> implementation (required methods)
    public static UInt4 Zero => new UInt4(0);
    public static UInt4 One => new UInt4(1);

    public static UInt4 Add(UInt4 left, UInt4 right) => left + right;
    public static UInt4 Subtract(UInt4 left, UInt4 right) => left - right;
    public static UInt4 Multiply(UInt4 left, UInt4 right) => left * right;
    public static UInt4 Divide(UInt4 left, UInt4 right) => left / right;
    public static UInt4 Modulus(UInt4 left, UInt4 right) => left % right;

    public static UInt4 CreateChecked<TOther>(TOther value) where TOther : INumberBase<TOther>
        => new UInt4((byte)(Convert.ToByte(value) & 0xF));

    public static UInt4 CreateSaturating<TOther>(TOther value) where TOther : INumberBase<TOther>
    {
        var v = Convert.ToInt32(value);
        if (v < 0) return MinValue;
        if (v > 15) return MaxValue;
        return new UInt4((byte)v);
    }

    public static UInt4 CreateTruncating<TOther>(TOther value) where TOther : INumberBase<TOther>
        => new UInt4((byte)(Convert.ToInt32(value) & 0xF));

    public static bool TryCreate<TOther>(TOther value, out UInt4 result) where TOther : INumberBase<TOther>
    {
        var v = Convert.ToInt32(value);
        if (v >= 0 && v <= 15)
        {
            result = new UInt4((byte)v);
            return true;
        }
        result = default;
        return false;
    }

    public static bool IsZero(UInt4 value) => value._value == 0;
    public static bool IsOne(UInt4 value) => value._value == 1;
    public static int Sign(UInt4 value) => value._value == 0 ? 0 : 1;


    public static UInt4 Abs(UInt4 value) => value; // Always positive in range 0-15
    public static UInt4 Max(UInt4 x, UInt4 y) => x._value > y._value ? x : y;
    public static UInt4 Min(UInt4 x, UInt4 y) => x._value < y._value ? x : y;
    public static UInt4 Clamp(UInt4 value, UInt4 min, UInt4 max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }

    public static UInt4 Parse(string s, NumberStyles style, IFormatProvider? provider)
    {
        var v = byte.Parse(s, style, provider);
        if (v > 15) throw new OverflowException("Value exceeds UInt4 range.");
        return new UInt4(v);
    }
    public static bool TryParse(string? s, NumberStyles style, IFormatProvider? provider, out UInt4 result)
    {
        result = default;
        if (byte.TryParse(s, style, provider, out var v) && v <= 15)
        {
            result = new UInt4(v);
            return true;
        }
        return false;
    }

    public static bool IsNegative(UInt4 value) => false;
    public static bool IsPositive(UInt4 value) => value._value > 0;
    public static bool IsEven(UInt4 value) => (value._value & 1) == 0;
    public static bool IsOdd(UInt4 value) => (value._value & 1) == 1;

    public static UInt4 Negate(UInt4 value) => value; // No negative UInt4

    public static UInt4 Reciprocal(UInt4 value) => value._value == 0 ? Zero : One; // Not meaningful, but required

    public static UInt4 Pow(UInt4 value, UInt4 exponent)
    {
        int result = 1;
        for (int i = 0; i < exponent._value; i++)
            result *= value._value;
        return new UInt4((byte)(result & 0xF));
    }

    public static UInt4 ToInt32(UInt4 value) => value;
    public static UInt4 ToInt64(UInt4 value) => value;
    public static UInt4 ToUInt32(UInt4 value) => value;
    public static UInt4 ToUInt64(UInt4 value) => value;

    public static int Radix => 10;

    public static UInt4 AdditiveIdentity => throw new NotImplementedException();

    public static UInt4 MultiplicativeIdentity => throw new NotImplementedException();

    public static bool IsCanonical(UInt4 value) => true;
    public static UInt4 Canonicalize(UInt4 value) => value;

    public int CompareTo(object? obj)
    {
        if (obj is UInt4 n) return CompareTo(n);
        throw new ArgumentException("Object must be of type UInt4.");
    }

    public static bool IsComplexNumber(UInt4 value) => false;
    public static bool IsEvenInteger(UInt4 value) => (value._value & 1) == 0;
    public static bool IsFinite(UInt4 value) => true;
    public static bool IsImaginaryNumber(UInt4 value) => false;
    public static bool IsInfinity(UInt4 value) => false;
    public static bool IsInteger(UInt4 value) => true;
    public static bool IsNaN(UInt4 value) => false;
    public static bool IsNegativeInfinity(UInt4 value) => false;
    public static bool IsNormal(UInt4 value) => value._value != 0;
    public static bool IsOddInteger(UInt4 value) => (value._value & 1) == 1;
    public static bool IsPositiveInfinity(UInt4 value) => false;
    public static bool IsRealNumber(UInt4 value) => true;
    public static bool IsSubnormal(UInt4 value) => false;
    public static UInt4 MaxMagnitude(UInt4 x, UInt4 y) => Max(x, y); // Both are non-negative, so max magnitude is just max
    public static UInt4 MaxMagnitudeNumber(UInt4 x, UInt4 y) => Max(x, y); // Same as above
    public static UInt4 MinMagnitude(UInt4 x, UInt4 y) => Min(x, y); // Both are non-negative, so min magnitude is just min
    public static UInt4 MinMagnitudeNumber(UInt4 x, UInt4 y) => Min(x, y); // Same as above
    public static UInt4 Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider) => Parse(s.ToString(), style, provider);
    public static bool TryConvertFromChecked<TOther>(TOther value, [MaybeNullWhen(false)] out UInt4 result) where TOther : INumberBase<TOther>
    {
        if (TryCreate(value, out result)) return true;
        result = default;
        return false;
    }
    public static bool TryConvertFromSaturating<TOther>(TOther value, [MaybeNullWhen(false)] out UInt4 result) where TOther : INumberBase<TOther>
    {
        if (TryCreate(value, out result)) return true;
        var v = Convert.ToInt32(value);
        if (v < 0) result = MinValue;
        else if (v > 15) result = MaxValue;
        else result = new UInt4((byte)v);
        return true;
    }

    public static bool TryConvertFromTruncating<TOther>(TOther value, [MaybeNullWhen(false)] out UInt4 result) where TOther : INumberBase<TOther>
    {
        if (TryCreate(value, out result)) return true;
        var v = Convert.ToInt32(value);
        result = new UInt4((byte)(v & 0xF));
        return true;
    }
    public static bool TryConvertToChecked<TOther>(UInt4 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
    {
        if (TryCreate(value, out var temp))
        {
            result = (TOther)Convert.ChangeType(temp.Value, typeof(TOther));
            return true;
        }
        result = default;
        return false;
    }
    public static bool TryConvertToSaturating<TOther>(UInt4 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
    {
        if (TryCreate(value, out var temp))
        {
            result = (TOther)Convert.ChangeType(temp.Value, typeof(TOther));
            return true;
        }
        var v = value.Value;
        if (v < 0) result = (TOther)Convert.ChangeType(0, typeof(TOther));
        else if (v > 15) result = (TOther)Convert.ChangeType(15, typeof(TOther));
        else result = (TOther)Convert.ChangeType(v, typeof(TOther));
        return true;
    }
    public static bool TryConvertToTruncating<TOther>(UInt4 value, [MaybeNullWhen(false)] out TOther result) where TOther : INumberBase<TOther>
    {
        if (TryCreate(value, out var temp))
        {
            result = (TOther)Convert.ChangeType(temp.Value, typeof(TOther));
            return true;
        }
        var v = value.Value;
        result = (TOther)Convert.ChangeType(v & 0xF, typeof(TOther));
        return true;
    }
    public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, [MaybeNullWhen(false)] out UInt4 result) => TryParse(s.ToString(), style, provider, out result);
    public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format, IFormatProvider? provider) => _value.TryFormat(destination, out charsWritten, format, provider);
    public string ToString(string? format, IFormatProvider? formatProvider) => _value.ToString(format, formatProvider);
    public static UInt4 Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s.ToString(), NumberStyles.Integer, provider);
    public static bool TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out UInt4 result) => TryParse(s.ToString(), NumberStyles.Integer, provider, out result);
    public static UInt4 Parse(string s, IFormatProvider? provider) => Parse(s, NumberStyles.Integer, provider);
    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out UInt4 result) => TryParse(s, NumberStyles.Integer, provider, out result);

    public TypeCode GetTypeCode() => TypeCode.Byte;
    public bool ToBoolean(IFormatProvider? provider) => _value != 0;
    public byte ToByte(IFormatProvider? provider) => _value;
    public char ToChar(IFormatProvider? provider) => (char)_value;
    public DateTime ToDateTime(IFormatProvider? provider) => throw new InvalidCastException("UInt4 cannot be converted to DateTime.");
    public decimal ToDecimal(IFormatProvider? provider) => _value;
    public double ToDouble(IFormatProvider? provider) => _value;
    public short ToInt16(IFormatProvider? provider) => _value;
    public int ToInt32(IFormatProvider? provider) => _value;
    public long ToInt64(IFormatProvider? provider) => _value;
    public sbyte ToSByte(IFormatProvider? provider) => (sbyte)_value;
    public float ToSingle(IFormatProvider? provider) => _value;
    public string ToString(IFormatProvider? provider) => _value.ToString(provider);
    public object ToType(Type conversionType, IFormatProvider? provider) => Convert.ChangeType(_value, conversionType, provider);
    public ushort ToUInt16(IFormatProvider? provider) => _value;
    public uint ToUInt32(IFormatProvider? provider) => _value;
    public ulong ToUInt64(IFormatProvider? provider) => _value;
}