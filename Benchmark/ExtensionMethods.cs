﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark
{
    static class ExtensionMethods
    {
        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> a)
        {
            foreach (var e in enumerable)
            {
                a(e);
            }
        }

        public static IEnumerable<T> Random<T>(this IEnumerable<T> enumerable, Random rand)
        {
            return
                enumerable
                    .Select(i => new { i, _ = rand.Next() })
                    .OrderBy(o => o._)
                    .Select(o => o.i)
                    .ToList();
        }

        public static bool IsNullable(this Type t)
        {
            return Nullable.GetUnderlyingType(t) != null;
        }

        public static bool IsList(this Type t)
        {
            return
                (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) ||
                t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
        }

        public static Type GetListInterface(this Type t)
        {
            return
                (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) ?
                t :
                t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
        }

        public static object RandomValue(this Type t, Random rand, int depth = 0)
        {
            if (t.IsPrimitive)
            {
                if (t == typeof(byte))
                {
                    return (byte)(rand.Next(byte.MaxValue - byte.MinValue + 1) + byte.MinValue);
                }

                if (t == typeof(sbyte))
                {
                    return (sbyte)(rand.Next(sbyte.MaxValue - sbyte.MinValue + 1) + sbyte.MinValue);
                }

                if (t == typeof(short))
                {
                    return (short)(rand.Next(short.MaxValue - short.MinValue + 1) + short.MinValue);
                }

                if (t == typeof(ushort))
                {
                    return (ushort)(rand.Next(ushort.MaxValue - ushort.MinValue + 1) + ushort.MinValue);
                }

                if (t == typeof(int))
                {
                    var bytes = new byte[4];
                    rand.NextBytes(bytes);

                    return BitConverter.ToInt32(bytes, 0);
                }

                if (t == typeof(uint))
                {
                    var bytes = new byte[4];
                    rand.NextBytes(bytes);

                    return BitConverter.ToUInt32(bytes, 0);
                }

                if (t == typeof(long))
                {
                    var bytes = new byte[8];
                    rand.NextBytes(bytes);

                    return BitConverter.ToInt64(bytes, 0);
                }

                if (t == typeof(ulong))
                {
                    var bytes = new byte[8];
                    rand.NextBytes(bytes);

                    return BitConverter.ToUInt64(bytes, 0);
                }

                if (t == typeof(float))
                {
                    var bytes = new byte[4];
                    rand.NextBytes(bytes);

                    return BitConverter.ToSingle(bytes, 0);
                }

                if (t == typeof(double))
                {
                    var bytes = new byte[8];
                    rand.NextBytes(bytes);

                    return BitConverter.ToDouble(bytes, 0);
                }

                if (t == typeof(char))
                {
                    // add a bias towards English-y text, it's more realistic
                    int range;
                    if (rand.Next(2) == 0)
                    {
                        range = 0xFFFF + 1;
                    }
                    else
                    {
                        range = 'z' + 1;
                    }

                    var ci = rand.Next(range);
                    var asChar = (char)ci;

                    while
                        (
                            char.IsHighSurrogate(asChar) ||
                            char.IsLowSurrogate(asChar) ||
                            char.IsSurrogate(asChar) ||
                            !(char.IsLetterOrDigit(asChar) || char.IsPunctuation(asChar) || char.IsWhiteSpace(asChar))
                        )
                    {
                        ci = rand.Next(range);
                        asChar = (char)ci;
                    }

                    return asChar;
                }

                if (t == typeof(bool))
                {
                    return (rand.Next(2) == 1);
                }

                throw new InvalidOperationException();
            }

            if (t == typeof(string))
            {
                var len = rand.Next(500);
                var c = new char[len];
                for (var i = 0; i < c.Length; i++)
                {
                    c[i] = (char)typeof(char).RandomValue(rand, depth + 1);
                }

                return new string(c);
            }

            if (t == typeof(DateTime))
            {
                var epoch = new DateTime(1970, 1, 1, 0, 0, 0);

                var bytes = new byte[4];
                rand.NextBytes(bytes);

                var secsOffset = BitConverter.ToInt32(bytes, 0);

                var retDate = epoch.AddSeconds(secsOffset);

                return retDate;
            }

            if (t.IsNullable())
            {
                // leave it unset
                if (rand.Next(2) == 0)
                {
                    // null!
                    return Activator.CreateInstance(t);
                }

                var underlying = Nullable.GetUnderlyingType(t);
                var val = underlying.RandomValue(rand, depth + 1);

                var cons = t.GetConstructor(new[] { underlying });

                return cons.Invoke(new object[] { val });
            }

            if (t.IsEnum)
            {
                var allValues = Enum.GetValues(t);
                var ix = rand.Next(allValues.Length);

                return allValues.GetValue(ix);
            }

            if (t.IsList())
            {
                if (rand.Next(2) == 0 || depth >= 10)
                {
                    return null;
                }

                var listI = t.GetListInterface();

                var valType = listI.GetGenericArguments()[0];

                var retT = typeof(List<>).MakeGenericType(valType);
                var ret = Activator.CreateInstance(retT);
                var add = retT.GetMethod("Add");

                var len = rand.Next(20);
                for (var i = 0; i < len; i++)
                {
                    var elem = valType.RandomValue(rand, depth + 1);
                    add.Invoke(ret, new object[] { elem });
                }

                return ret;
            }

            var retObj = Activator.CreateInstance(t);
            foreach (var p in t.GetProperties())
            {
                if (rand.Next(2) == 0) continue;

                var propType = p.PropertyType;

                p.SetValue(retObj, propType.RandomValue(rand, depth + 1));
            }

            return retObj;
        }

        public static double Median(this IEnumerable<double> e)
        {
            var inOrder = e.OrderBy(_ => _).ToList();

            if (inOrder.Count % 2 == 1)
            {
                return inOrder[inOrder.Count / 2 + 1];
            }

            var a = inOrder[inOrder.Count / 2];
            var b = inOrder[inOrder.Count / 2 + 1];

            return (a + b) / 2.0;
        }
    }
}
