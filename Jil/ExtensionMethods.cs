﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil
{
    internal static class ExtensionMethods
    {
        public static bool IsListType(this Type t)
        {
            try
            {
                return
                    (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) ||
                    t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
            }
            catch (Exception) { return false; }
        }

        public static Type GetListInterface(this Type t)
        {
            return
                (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) ?
                t :
                t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));
        }

        public static bool IsDictionaryType(this Type t)
        {
            return
                (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ||
                t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }

        public static Type GetDictionaryInterface(this Type t)
        {
            return
                (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) ?
                t :
                t.GetInterfaces().First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));
        }

        public static void ForEach<T>(this IEnumerable<T> e, Action<T> func)
        {
            foreach (var x in e)
            {
                func(x);
            }
        }

        public static bool IsPrimitiveType(this Type t)
        {
            return
                t == typeof(string) ||
                t == typeof(char) ||
                t == typeof(float) ||
                t == typeof(double) ||
                t == typeof(decimal) ||
                t == typeof(byte) ||
                t == typeof(sbyte) ||
                t == typeof(short) ||
                t == typeof(ushort) ||
                t == typeof(int) ||
                t == typeof(uint) ||
                t == typeof(long) ||
                t == typeof(ulong);
        }

        // From: http://www.ietf.org/rfc/rfc4627.txt?number=4627
        public static string JsonEscape(this string str)
        {
            var ret = "";
            foreach (var c in str)
            {
                switch (c)
                {
                    case '\\': ret += @"\\"; break;
                    case '"': ret += @"\"""; break;
                    case '\u0000': ret += @"\u0000"; break;
                    case '\u0001': ret += @"\u0001"; break;
                    case '\u0002': ret += @"\u0002"; break;
                    case '\u0003': ret += @"\u0003"; break;
                    case '\u0004': ret += @"\u0004"; break;
                    case '\u0005': ret += @"\u0005"; break;
                    case '\u0006': ret += @"\u0006"; break;
                    case '\u0007': ret += @"\u0007"; break;
                    case '\u0008': ret += @"\u0008"; break;
                    case '\u0009': ret += @"\u0009"; break;
                    case '\u000A': ret += @"\u000A"; break;
                    case '\u000B': ret += @"\u000B"; break;
                    case '\u000C': ret += @"\u000C"; break;
                    case '\u000D': ret += @"\u000D"; break;
                    case '\u000E': ret += @"\u000E"; break;
                    case '\u000F': ret += @"\u000F"; break;
                    case '\u0010': ret += @"\u0010"; break;
                    case '\u0011': ret += @"\u0011"; break;
                    case '\u0012': ret += @"\u0012"; break;
                    case '\u0013': ret += @"\u0013"; break;
                    case '\u0014': ret += @"\u0014"; break;
                    case '\u0015': ret += @"\u0015"; break;
                    case '\u0016': ret += @"\u0016"; break;
                    case '\u0017': ret += @"\u0017"; break;
                    case '\u0018': ret += @"\u0018"; break;
                    case '\u0019': ret += @"\u0019"; break;
                    case '\u001A': ret += @"\u001A"; break;
                    case '\u001B': ret += @"\u001B"; break;
                    case '\u001C': ret += @"\u001C"; break;
                    case '\u001D': ret += @"\u001D"; break;
                    case '\u001E': ret += @"\u001E"; break;
                    case '\u001F': ret += @"\u001F"; break;
                    default: ret += c; break;
                }
            }

            return ret;
        }
    }
}
