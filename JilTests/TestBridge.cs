﻿#if XUNIT
using System;
using System.Collections.Generic;

namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    public static class CollectionAssert
    {
        public static void AreEqual<T>(IEnumerable<T> x, IEnumerable<T> y, string message = null)
        {
            Xunit.Assert.Equal<T>(x, y);
        }
    }
    public static class Assert
    {
        public static void AreEqual<T>(T x, T y, string message = null)
        {
            Xunit.Assert.Equal<T>(x, y);
        }
        //public static void AreEqual<T>(IEnumerable<T> x, IEnumerable<T> y)
        //{
        //    Xunit.Assert.Equal<T>(x, y);
        //}
        public static void AreNotEqual<T>(T x, T y)
        {
            Xunit.Assert.NotEqual<T>(x, y);
        }
        //public static void AreNotEqual<T>(IEnumerable<T> x, IEnumerable<T> y)
        //{
        //    Xunit.Assert.NotEqual<T>(x, y);
        //}
        public static void IsFalse(bool value)
        {
            Xunit.Assert.False(value);
        }
        public static void IsTrue(bool value, string message = null)
        {
            Xunit.Assert.True(value);
        }
        public static void Fail()
        {
            Xunit.Assert.Equal("pass", "fail");
        }
        public static void Fail(string message)
        {
            string expected = "pass";
            if (expected == message) expected = "passed"; // unlikely, but!
            Xunit.Assert.Equal(expected, message);
        }
        public static void IsNull(object @object)
        {
            Xunit.Assert.Null(@object);
        }
        public static void IsNotNull(object @object)
        {
            Xunit.Assert.NotNull(@object);
        }
        public static void IsInstanceOfType(object @object, Type type)
        {
            Xunit.Assert.IsType(type, @object);
        }
    }
}
namespace Microsoft.VisualStudio.TestTools.UnitTesting
{
    class TestClassAttribute : Attribute { }
    class TestMethodAttribute : Xunit.FactAttribute { }

    // lazy way of isolating a single test even with limited tools
    //class TestMethodAttribute : Attribute{ }
    //class ActualTestMethodAttribute : Xunit.FactAttribute { }
}

namespace System.Diagnostics.CodeAnalysis
{
    class ExcludeFromCodeCoverageAttribute : Attribute { }
}
#endif