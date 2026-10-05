using System;

public readonly struct Fix : IEquatable<Fix>, IComparable<Fix>
{
    public const int Shift = 16;
    public const int OneRaw = 1 << Shift;

    public static readonly Fix Zero = new Fix(0);
    public static readonly Fix One = new Fix(OneRaw);
    public static readonly Fix Half = new Fix(OneRaw / 2);

    public readonly int Raw;

    private Fix(int raw)
    {
        Raw = raw;
    }

    public static Fix FromRaw(int raw)
    {
        return new Fix(raw);
    }

    public static Fix FromInt(int value)
    {
        return new Fix(value << Shift);
    }

    public static Fix Ratio(int numerator, int denominator)
    {
        return new Fix((int)(((long)numerator << Shift) / denominator));
    }

    public float ToFloat()
    {
        return Raw / (float)OneRaw;
    }

    public static Fix operator +(Fix a, Fix b) => new Fix(a.Raw + b.Raw);
    public static Fix operator -(Fix a, Fix b) => new Fix(a.Raw - b.Raw);
    public static Fix operator -(Fix a) => new Fix(-a.Raw);
    public static Fix operator *(Fix a, Fix b) => new Fix((int)(((long)a.Raw * b.Raw) >> Shift));
    public static Fix operator *(Fix a, int b) => new Fix(a.Raw * b);
    public static Fix operator /(Fix a, Fix b) => new Fix((int)(((long)a.Raw << Shift) / b.Raw));
    public static Fix operator /(Fix a, int b) => new Fix(a.Raw / b);
    public static bool operator <(Fix a, Fix b) => a.Raw < b.Raw;
    public static bool operator >(Fix a, Fix b) => a.Raw > b.Raw;
    public static bool operator <=(Fix a, Fix b) => a.Raw <= b.Raw;
    public static bool operator >=(Fix a, Fix b) => a.Raw >= b.Raw;
    public static bool operator ==(Fix a, Fix b) => a.Raw == b.Raw;
    public static bool operator !=(Fix a, Fix b) => a.Raw != b.Raw;

    public static Fix Abs(Fix a) => a.Raw < 0 ? new Fix(-a.Raw) : a;
    public static Fix Min(Fix a, Fix b) => a.Raw < b.Raw ? a : b;
    public static Fix Max(Fix a, Fix b) => a.Raw > b.Raw ? a : b;
    public static Fix Clamp(Fix value, Fix min, Fix max) => value.Raw < min.Raw ? min : value.Raw > max.Raw ? max : value;
    public static int Sign(Fix a) => a.Raw < 0 ? -1 : 1;

    public static Fix Sqrt(Fix a)
    {
        if (a.Raw <= 0) return Zero;

        ulong value = (ulong)a.Raw << Shift;
        ulong result = 0;
        ulong bit = 1UL << 62;
        while (bit > value) bit >>= 2;

        while (bit != 0)
        {
            if (value >= result + bit)
            {
                value -= result + bit;
                result = (result >> 1) + bit;
            }
            else result >>= 1;
            bit >>= 2;
        }

        return new Fix((int)result);
    }

    public static Fix Sin(Fix x)
    {
        Fix x2 = x * x;
        Fix x3 = x2 * x;
        Fix x5 = x3 * x2;
        return x - x3 / 6 + x5 / 120;
    }

    public static Fix Cos(Fix x)
    {
        Fix x2 = x * x;
        Fix x4 = x2 * x2;
        return One - x2 / 2 + x4 / 24;
    }

    public bool Equals(Fix other) => Raw == other.Raw;
    public override bool Equals(object obj) => obj is Fix other && Raw == other.Raw;
    public override int GetHashCode() => Raw;
    public int CompareTo(Fix other) => Raw.CompareTo(other.Raw);
    public override string ToString() => ToFloat().ToString("0.####");
}
