﻿using Jil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JilTests
{
    [TestClass]
    public class Serialize
    {
        public class _SimpleObject
        {
            public int Foo;
        }

        [TestMethod]
        public void SimpleObject()
        {
            using (var str = new StringWriter())
            {
                JSON.Serialize(new _SimpleObject { Foo = 123 }, str);

                var res = str.ToString();

                Assert.AreEqual("{\"Foo\":123}", res);
            }
        }

        public class _Cyclical
        {
            public int Foo;

            public _Cyclical Next;
        }

        [TestMethod]
        public void Cyclical()
        {
            using (var str = new StringWriter())
            {
                JSON.Serialize(new _Cyclical { Foo = 123, Next = new _Cyclical { Foo = 456 } }, str);

                var res = str.ToString();

                Assert.AreEqual("{\"Next\":{\"Next\":null,\"Foo\":456},\"Foo\":123}", res);
            }
        }

        [TestMethod]
        public void Primitive()
        {
            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<byte>(123, str);

                    var res = str.ToString();

                    Assert.AreEqual("123", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<sbyte>(-123, str);

                    var res = str.ToString();

                    Assert.AreEqual("-123", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<short>(-1000, str);

                    var res = str.ToString();

                    Assert.AreEqual("-1000", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<ushort>(5000, str);

                    var res = str.ToString();

                    Assert.AreEqual("5000", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<int>(-123, str);

                    var res = str.ToString();

                    Assert.AreEqual("-123", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<uint>(123456789, str);

                    var res = str.ToString();

                    Assert.AreEqual("123456789", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<long>(-5000000000, str);

                    var res = str.ToString();

                    Assert.AreEqual("-5000000000", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<ulong>(8000000000, str);

                    var res = str.ToString();

                    Assert.AreEqual("8000000000", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<string>("hello world", str);

                    var res = str.ToString();

                    Assert.AreEqual("\"hello world\"", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<char>('c', str);

                    var res = str.ToString();

                    Assert.AreEqual("\"c\"", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<float>(1.234f, str);

                    var res = str.ToString();

                    Assert.AreEqual("1.234", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<double>(1.1, str);

                    var res = str.ToString();

                    Assert.AreEqual("1.1", res);
                }
            }

            {
                using (var str = new StringWriter())
                {
                    JSON.Serialize<decimal>(4.56m, str);

                    var res = str.ToString();

                    Assert.AreEqual("4.56", res);
                }
            }
        }

#pragma warning disable 0649
        public class _StringsAndChars
        {
            public class _One
            {
                public string Single;
            }
            
            public class _Two
            {
                public int _;
                public string Trailing;
            }

            public class _Three
            {
                public string Leading;
                public int _;
            }

            public _One One;
            public _Two Two;
            public _Three Three;
        }
#pragma warning restore 0649

        [TestMethod]
        public void StringsAndChars()
        {
            using (var str = new StringWriter())
            {
                JSON.Serialize(
                    new _StringsAndChars
                    {
                        One = new _StringsAndChars._One
                        {
                            Single = "Hello World"
                        },
                        Two = new _StringsAndChars._Two
                        {
                            _ = 123,
                            Trailing = "Fizz Buzz"
                        },
                        Three = new _StringsAndChars._Three
                        {
                            Leading = "Foo Bar",
                            _ = 456
                        }
                    },
                    str
                );

                var res = str.ToString();

                Assert.AreEqual("{\"One\":{\"Single\":\"Hello World\"},\"Two\":{\"Trailing\":\"Fizz Buzz\",\"_\":123},\"Three\":{\"Leading\":\"Foo Bar\",\"_\":456}}", res);
            }
        }

        [TestMethod]
        public void Dictionary()
        {
            using (var str = new StringWriter())
            {
                JSON.Serialize(
                    new Dictionary<string, int>
                    {
                        { "hello world", 123 }
                    },
                    str
                );

                var res = str.ToString();

                Assert.AreEqual("{\"hello world\":123}", res);
            }
        }
    }
}
