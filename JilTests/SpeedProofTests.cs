﻿using Jil.Serialize;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Sigil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JilTests
{
    // These tests make *no sense* in debug
#if !DEBUG
    [TestClass]
    public class SpeedProofTests
    {
        private static char _RandChar(Random rand)
        {
            var lower = rand.Next(2) == 0;

            var ret = (char)('A' + rand.Next('Z' - 'A'));

            if (lower) ret = char.ToLower(ret);

            return ret;
        }

        private static string _RandString(Random rand)
        {
            var len = rand.Next(20);
            var ret = new char[len];

            for (var i = 0; i < len; i++)
            {
                ret[i] = _RandChar(rand);
            }

            return new string(ret);
        }

        private static DateTime _RandDateTime(Random rand)
        {
            var year = 1 + rand.Next(3000);
            var month = 1 + rand.Next(12);
            var day = 1 + rand.Next(28);
            var hour = rand.Next(24);
            var minute = rand.Next(60);
            var second = rand.Next(60);

            return new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
        }

        private static void CompareTimes<T>(List<T> toSerialize, Action<TextWriter, T> a, Action<TextWriter, T> b, out double aTimeMS, out double bTimeMS, bool checkCorrectness = true)
        {
            var aTimer = new Stopwatch();
            var bTimer = new Stopwatch();

            // Some of our optimizations change the produced string, so we can conditionally suppress this
            if (checkCorrectness)
            {
                foreach (var item in toSerialize)
                {
                    using (var aStr = new StringWriter())
                    using (var bStr = new StringWriter())
                    {
                        a(aStr, item);
                        b(bStr, item);

                        Assert.AreEqual(aStr.ToString(), bStr.ToString());
                    }
                }
            }

            Action timeA =
                () =>
                {
                    aTimer.Start();
                    for (var i = 0; i < toSerialize.Count; i++)
                    {
                        using (var str = new StringWriter())
                        {
                            a(str, toSerialize[i]);
                        }
                    }
                    aTimer.Stop();
                };

            Action timeB =
                () =>
                {
                    bTimer.Start();
                    for (var i = 0; i < toSerialize.Count; i++)
                    {
                        using (var str = new StringWriter())
                        {
                            b(str, toSerialize[i]);
                        }
                    }
                    bTimer.Stop();
                };

            for (var i = 0; i < 5; i++)
            {
                timeA();
                timeB();
            }

            bTimer.Reset();
            aTimer.Reset();

            for (var i = 0; i < 100; i++)
            {
                var order = (i % 2) == 0;

                if (order)
                {
                    timeA();
                    timeB();
                }
                else
                {
                    timeB();
                    timeA();
                }
            }

            aTimeMS = aTimer.ElapsedMilliseconds;
            bTimeMS = bTimer.ElapsedMilliseconds;
        }

        public class _ReorderMembers
        {
            public int Foo;
            public string Bar;
            public double Fizz;
            public decimal Buzz;
            public char Hello;
            public string[] World;
        }

        [TestMethod]
        public void ReorderMembers()
        {
            Action<TextWriter, _ReorderMembers> memoryOrder;
            Action<TextWriter, _ReorderMembers> normalOrder;

            try
            {
                {
                    InlineSerializer.ReorderMembers = true;

                    // Build the *actual* serializer method
                    memoryOrder = InlineSerializer.Build<_ReorderMembers>();
                }

                {
                    InlineSerializer.ReorderMembers = false;

                    // Build the *actual* serializer method
                    normalOrder = InlineSerializer.Build<_ReorderMembers>();
                }
            }
            finally
            {
                InlineSerializer.ReorderMembers = true;
            }

            var rand = new Random(1160428);

            var toSerialize = new List<_ReorderMembers>();
            for (var i = 0; i < 10000; i++)
            {
                toSerialize.Add(
                    new _ReorderMembers
                    {
                        Bar = _RandString(rand),
                        Buzz = ((decimal)rand.NextDouble()) * decimal.MaxValue,
                        Fizz = rand.NextDouble() * double.MaxValue,
                        Foo = rand.Next(int.MaxValue),
                        Hello = _RandChar(rand),
                        World = Enumerable.Range(0, rand.Next(100)).Select(s => _RandString(rand)).ToArray()
                    }
                );
            }

            toSerialize = toSerialize.Select(_ => new { _ = _, Order = rand.Next() }).OrderBy(o => o.Order).Select(o => o._).Where((o, ix) => ix % 2 == 0).ToList();

            double reorderedTime, normalOrderTime;
            CompareTimes(toSerialize, memoryOrder, normalOrder, out reorderedTime, out normalOrderTime, checkCorrectness: false);

            Assert.IsTrue(reorderedTime < normalOrderTime, "reorderedTime = " + reorderedTime + ", normalOrderTime = " + normalOrderTime);
        }

        public class _SkipNumberFormatting
        {
            public byte A;
            public sbyte B;
            public short C;
            public ushort D;
            public int E;
        }

        [TestMethod]
        public void SkipNumberFormatting()
        {
            Action<TextWriter, _SkipNumberFormatting> skipping;
            Action<TextWriter, _SkipNumberFormatting> normal;

            try
            {
                {
                    InlineSerializer.SkipNumberFormatting = true;

                    // Build the *actual* serializer method
                    skipping = InlineSerializer.Build<_SkipNumberFormatting>();
                }

                {
                    InlineSerializer.SkipNumberFormatting = false;

                    // Build the *actual* serializer method
                    normal = InlineSerializer.Build<_SkipNumberFormatting>();
                }
            }
            finally
            {
                InlineSerializer.SkipNumberFormatting = true;
            }

            var rand = new Random(141090045);

            var toSerialize = new List<_SkipNumberFormatting>();
            for (var i = 0; i < 10000; i++)
            {
                toSerialize.Add(
                    new _SkipNumberFormatting
                    {
                        A = (byte)rand.Next(101),
                        B = (sbyte)rand.Next(101),
                        C = (short)rand.Next(101),
                        D = (ushort)rand.Next(101),
                        E = rand.Next(101),
                    }
                );
            }

            toSerialize = toSerialize.Select(_ => new { _ = _, Order = rand.Next() }).OrderBy(o => o.Order).Select(o => o._).Where((o, ix) => ix % 2 == 0).ToList();

            double skippingTime, normalTime;
            CompareTimes(toSerialize, skipping, normal, out skippingTime, out normalTime);

            Assert.IsTrue(skippingTime < normalTime, "skippingTime = " + skippingTime + ", normalTime = " + normalTime);
        }

        public class _UseCustomIntegerToString
        {
            public byte A;
            public sbyte B;
            public short C;
            public ushort D;
            public int E;
            public uint F;
            public long G;
            public ulong H;
        }

        [TestMethod]
        public void UseCustomIntegerToString()
        {
            Action<TextWriter, _UseCustomIntegerToString> custom;
            Action<TextWriter, _UseCustomIntegerToString> normal;

            try
            {
                {
                    InlineSerializer.UseCustomIntegerToString = true;

                    // Build the *actual* serializer method
                    custom = InlineSerializer.Build<_UseCustomIntegerToString>();
                }

                {
                    InlineSerializer.UseCustomIntegerToString = false;

                    // Build the *actual* serializer method
                    normal = InlineSerializer.Build<_UseCustomIntegerToString>();
                }
            }
            finally
            {
                InlineSerializer.UseCustomIntegerToString = true;
            }

            var rand = new Random(139426720);

            var toSerialize = new List<_UseCustomIntegerToString>();
            for (var i = 0; i < 10000; i++)
            {
                toSerialize.Add(
                    new _UseCustomIntegerToString
                    {
                        A = (byte)(101 + rand.Next(155)),
                        B = (sbyte)(101 + rand.Next(27)),
                        C = (short)(101 + rand.Next(1000)),
                        D = (ushort)(101 + rand.Next(1000)),
                        E = 101 + rand.Next(int.MaxValue - 101),
                        F = (uint)(101 + rand.Next(int.MaxValue - 101)),
                        G = (long)(101 + rand.Next(int.MaxValue)),
                        H = (ulong)(101 + rand.Next(int.MaxValue))
                    }
                );
            }

            toSerialize = toSerialize.Select(_ => new { _ = _, Order = rand.Next() }).OrderBy(o => o.Order).Select(o => o._).Where((o, ix) => ix % 2 == 0).ToList();

            double customTime, normalTime;
            CompareTimes(toSerialize, custom, normal, out customTime, out normalTime);

            Assert.IsTrue(customTime < normalTime, "customTime = " + customTime + ", normalTime = " + normalTime);
        }

        public class _SkipDateTimeMathMethods
        {
            public DateTime[] Dates;
        }

        [TestMethod]
        public void SkipDateTimeMathMethods()
        {
            Action<TextWriter, _SkipDateTimeMathMethods> skipped;
            Action<TextWriter, _SkipDateTimeMathMethods> normal;

            try
            {
                {
                    InlineSerializer.SkipDateTimeMathMethods = true;

                    // Build the *actual* serializer method
                    skipped = InlineSerializer.Build<_SkipDateTimeMathMethods>();
                }

                {
                    InlineSerializer.SkipDateTimeMathMethods = false;

                    // Build the *actual* serializer method
                    normal = InlineSerializer.Build<_SkipDateTimeMathMethods>();
                }
            }
            finally
            {
                InlineSerializer.SkipDateTimeMathMethods = true;
            }

            var rand = new Random(66262484);

            var toSerialize = new List<_SkipDateTimeMathMethods>();
            for (var i = 0; i < 1000; i++)
            {
                var numDates = new DateTime[5 + rand.Next(10)];

                for (var j = 0; j < numDates.Length; j++)
                {
                    numDates[j] = _RandDateTime(rand);
                }

                toSerialize.Add(
                    new _SkipDateTimeMathMethods
                    {
                        Dates = numDates
                    }
                );
            }

            toSerialize = toSerialize.Select(_ => new { _ = _, Order = rand.Next() }).OrderBy(o => o.Order).Select(o => o._).Where((o, ix) => ix % 2 == 0).ToList();

            double skippedTime, normalTime;
            CompareTimes(toSerialize, skipped, normal, out skippedTime, out normalTime);

            Assert.IsTrue(skippedTime < normalTime, "skippedTime = " + skippedTime + ", normalTime = " + normalTime);
        }

        public class _SkipSimplePropertyAccess
        {
            public byte A { get; set; }
            public sbyte B { get; set; }
            public short C { get; set; }
            public ushort D { get; set; }
            public int E { get; set; }
            public uint F { get; set; }
            public long G { get; set; }
            public ulong H { get; set; }
            public float I { get; set; }
            public double J { get; set; }
            public decimal K { get; set; }
            public DateTime L { get; set; }
            public string M { get; set; }
            public char N { get; set; }
        }

        [TestMethod]
        public void SkipSimplePropertyAccess()
        {
            Action<TextWriter, _SkipSimplePropertyAccess> skipped;
            Action<TextWriter, _SkipSimplePropertyAccess> normal;

            try
            {
                {
                    InlineSerializer.SkipSimplePropertyAccess = true;

                    // Build the *actual* serializer method
                    skipped = InlineSerializer.Build<_SkipSimplePropertyAccess>();
                }

                {
                    InlineSerializer.SkipSimplePropertyAccess = false;

                    // Build the *actual* serializer method
                    normal = InlineSerializer.Build<_SkipSimplePropertyAccess>();
                }
            }
            finally
            {
                InlineSerializer.SkipSimplePropertyAccess = true;
            }

            var rand = new Random(94093827);

            var toSerialize = new List<_SkipSimplePropertyAccess>();
            for (var i = 0; i < 10000; i++)
            {
                toSerialize.Add(
                    new _SkipSimplePropertyAccess
                    {
                        A = (byte)rand.Next(byte.MaxValue),
                        B = (sbyte)rand.Next(sbyte.MaxValue),
                        C = (short)rand.Next(short.MaxValue),
                        D = (ushort)rand.Next(ushort.MaxValue),
                        E = rand.Next(int.MaxValue),
                        F = (uint)rand.Next(),
                        G = rand.Next(),
                        H = (ulong)rand.Next(),
                        I = (float)rand.NextDouble(),
                        J = rand.NextDouble(),
                        K = (decimal)rand.NextDouble(),
                        L = _RandDateTime(rand),
                        M = _RandString(rand),
                        N = _RandChar(rand)
                    }
                );
            }

            toSerialize = toSerialize.Select(_ => new { _ = _, Order = rand.Next() }).OrderBy(o => o.Order).Select(o => o._).Where((o, ix) => ix % 2 == 0).ToList();

            double skippedTime, normalTime;
            CompareTimes(toSerialize, skipped, normal, out skippedTime, out normalTime);

            Assert.IsTrue(skippedTime < normalTime, "skippedTime = " + skippedTime + ", normalTime = " + normalTime);
        }

        public class _UseCustomDoubleToString
        {
            public double[] Doubles;
        }

        [TestMethod]
        public void UseCustomDoubleToString()
        {
            Action<TextWriter, _UseCustomDoubleToString> custom;
            Action<TextWriter, _UseCustomDoubleToString> normal;

            try
            {
                {
                    InlineSerializer.UseCustomDoubleToString = true;

                    // Build the *actual* serializer method
                    custom = InlineSerializer.Build<_UseCustomDoubleToString>();
                }

                {
                    InlineSerializer.UseCustomDoubleToString = false;

                    // Build the *actual* serializer method
                    normal = InlineSerializer.Build<_UseCustomDoubleToString>();
                }
            }
            finally
            {
                InlineSerializer.UseCustomDoubleToString = true;
            }

            var rand = new Random(126052078);

            var toSerialize = new List<_UseCustomDoubleToString>();
            for (var i = 0; i < 1000; i++)
            {
                var num = 10 + rand.Next(10);
                var doubles = Enumerable.Range(0, num).Select(_ => rand.NextDouble() * rand.Next()).ToArray();

                toSerialize.Add(
                    new _UseCustomDoubleToString
                    {
                        Doubles = doubles
                    }
                );
            }

            toSerialize = toSerialize.Select(_ => new { _ = _, Order = rand.Next() }).OrderBy(o => o.Order).Select(o => o._).Where((o, ix) => ix % 2 == 0).ToList();

            double customTime, normalTime;
            CompareTimes(toSerialize, custom, normal, out customTime, out normalTime, checkCorrectness: false);

            Assert.IsTrue(customTime < normalTime, "customTime = " + customTime + ", normalTime = " + normalTime);
        }
    }
#endif
}
