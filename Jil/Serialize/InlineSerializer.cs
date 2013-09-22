﻿using Sigil.NonGeneric;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Jil.Serialize
{
    class InlineSerializer
    {
        public static bool ReorderMembers = true;
        public static bool SkipNumberFormatting = true;
        public static bool UseCustomIntegerToString = true;
        public static bool SkipDateTimeMathMethods = true;

        static string CharBuffer = "char_buffer";
        const int CharBufferSize = 20;

        static Dictionary<char, string> CharacterEscapes = 
            new Dictionary<char, string>{
                { '\\',  @"\\" },
                { '"', @"\""" },
                { '\u0000', @"\u0000" },
                { '\u0001', @"\u0001" },
                { '\u0002', @"\u0002" },
                { '\u0003', @"\u0003" },
                { '\u0004', @"\u0004" },
                { '\u0005', @"\u0005" },
                { '\u0006', @"\u0006" },
                { '\u0007', @"\u0007" },
                { '\u0008', @"\b" },
                { '\u0009', @"\t" },
                { '\u000A', @"\n" },
                { '\u000B', @"\u000B" },
                { '\u000C', @"\f" },
                { '\u000D', @"\r" },
                { '\u000E', @"\u000E" },
                { '\u000F', @"\u000F" },
                { '\u0010', @"\u0010" },
                { '\u0011', @"\u0011" },
                { '\u0012', @"\u0012" },
                { '\u0013', @"\u0013" },
                { '\u0014', @"\u0014" },
                { '\u0015', @"\u0015" },
                { '\u0016', @"\u0016" },
                { '\u0017', @"\u0017" },
                { '\u0018', @"\u0018" },
                { '\u0019', @"\u0019" },
                { '\u001A', @"\u001A" },
                { '\u001B', @"\u001B" },
                { '\u001C', @"\u001C" },
                { '\u001D', @"\u001D" },
                { '\u001E', @"\u001E" },
                { '\u001F', @"\u001F" }
        };

        static MethodInfo TextWriter_WriteString = typeof(TextWriter).GetMethod("Write", new[] { typeof(string) });
        static void WriteString(string str, Emit emit)
        {
            emit.LoadArgument(0);
            emit.LoadConstant(str);
            emit.CallVirtual(TextWriter_WriteString);
        }

        static List<MemberInfo> OrderMembersForAccess(Type forType)
        {
            var members = forType.GetProperties().Where(p => p.GetMethod != null).Cast<MemberInfo>().Concat(forType.GetFields());

            if (forType.IsValueType)
            {
                return members.ToList();
            }

            var fields = Utils.FieldOffsetsInMemory(forType);
            var props = Utils.PropertyFieldUsage(forType);

            // This order appears to be the "best" for access speed purposes
            var ret =
                !ReorderMembers ?
                    members :
                    members.OrderBy(
                         m =>
                         {
                             var asField = m as FieldInfo;
                             if (asField != null)
                             {
                                 return fields[asField];
                             }

                             var asProp = m as PropertyInfo;
                             if (asProp != null)
                             {
                                 List<FieldInfo> usesFields;
                                 if (!props.TryGetValue(asProp, out usesFields))
                                 {
                                     return int.MaxValue;
                                 }

                                 if (usesFields.Count == 0) return int.MaxValue;

                                 return usesFields.Select(f => fields[f]).Min();
                             }

                             return int.MaxValue;
                         }
                    ).ThenBy(
                        m => m is FieldInfo ? 0 : 1
                    ).Reverse();

            return ret.ToList();
        }

        static HashSet<Type> FindRecursiveTypes(Type forType)
        {
            var alreadySeen = new HashSet<Type>();
            var ret = new HashSet<Type>();

            var pending = new List<Type>();
            pending.Add(forType);

            while (pending.Count > 0)
            {
                var curType = pending[0];
                pending.RemoveAt(0);

                if (curType.IsPrimitiveType()) continue;

                if (curType.IsListType())
                {
                    var listI = curType.GetListInterface();
                    var valType = listI.GetGenericArguments()[0];
                    pending.Add(valType);
                    continue;
                }

                if (curType.IsDictionaryType())
                {
                    var dictI = curType.GetDictionaryInterface();
                    var valType = dictI.GetGenericArguments()[1];
                    pending.Add(valType);
                    continue;
                }

                if (alreadySeen.Contains(curType))
                {
                    ret.Add(curType);
                    continue;
                }

                alreadySeen.Add(curType);

                foreach (var field in curType.GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    pending.Add(field.FieldType);
                }

                foreach (var prop in curType.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetMethod != null))
                {
                    pending.Add(prop.PropertyType);
                }
            }

            return ret;
        }

        static void WriteMember(MemberInfo member, Emit emit, Dictionary<Type, Sigil.Local> recursiveTypes, bool isValueType, Sigil.Local inLocal = null)
        {
            var asField = member as FieldInfo;
            var asProp = member as PropertyInfo;

            if (asField == null && asProp == null) throw new Exception("Wha?");

            var serializingType = asField != null ? asField.FieldType : asProp.PropertyType;

            // It's a list, go and build that code
            if (serializingType.IsListType())
            {
                if (inLocal != null)
                {
                    emit.LoadLocal(inLocal);
                }
                else
                {
                    emit.LoadArgument(1);
                }

                if (asField != null)
                {
                    emit.LoadField(asField);
                }

                if (asProp != null)
                {
                    var getMtd = asProp.GetMethod;
                    if (getMtd.IsVirtual)
                    {
                        emit.CallVirtual(getMtd);
                    }
                    else
                    {
                        emit.Call(getMtd);
                    }
                }

                using (var loc = emit.DeclareLocal(serializingType))
                {
                    emit.StoreLocal(loc);

                    if (serializingType.IsListType())
                    {
                        WriteList(serializingType, emit, recursiveTypes, loc);
                        return;
                    }

                    if (serializingType.IsDictionaryType())
                    {
                        WriteDictionary(serializingType, emit, recursiveTypes, loc);
                        return;
                    }

                    WriteList(serializingType, emit, recursiveTypes, loc);
                }

                return;
            }

            var isRecursive = recursiveTypes.ContainsKey(serializingType);

            if (isRecursive)
            {
                emit.LoadLocal(recursiveTypes[serializingType]);    // Action<TextWriter, serializingType>
            }

            // Only put this on the stack if we'll need it
            var preloadTextWriter = serializingType.IsPrimitiveType() || isRecursive || serializingType.IsNullableType();
            if (preloadTextWriter)
            {
                emit.LoadArgument(0);   // TextWriter
            }

            if (inLocal == null)
            {
                emit.LoadArgument(1);       // TextWriter obj
            }
            else
            {
                emit.LoadLocal(inLocal);    // TextWriter obj
            }

            if (asField != null)
            {
                emit.LoadField(asField);    // TextWriter field
            }

            if (asProp != null)
            {
                var getMtd = asProp.GetMethod;
                if(getMtd.IsVirtual)
                {
                    emit.CallVirtual(getMtd);   // TextWriter prop
                }
                else
                {
                    emit.Call(getMtd);          // TextWriter prop
                }
            }

            if (isRecursive)
            {
                // Stack is:
                //  - serializingType
                //  - TextWriter
                //  - Action<TextWriter, serializingType>

                var recursiveAct = typeof(Action<,>).MakeGenericType(typeof(TextWriter), serializingType);
                var invoke = recursiveAct.GetMethod("Invoke");

                emit.Call(invoke);

                return;
            }

            if (serializingType.IsPrimitiveType())
            {
                WritePrimitive(serializingType, emit);
                return;
            }

            if (serializingType.IsNullableType())
            {
                WriteNullable(serializingType, emit, recursiveTypes);
                return;
            }

            using (var loc = emit.DeclareLocal(serializingType))
            {
                emit.StoreLocal(loc);   // TextWriter;

                WriteObject(serializingType, emit, recursiveTypes, loc);
            }
        }

        static void WriteNullable(Type nullableType, Emit emit, Dictionary<Type, Sigil.Local> recursiveTypes)
        {
            // Top of stack is
            //  - nullable
            //  - TextWriter

            var hasValue = nullableType.GetProperty("HasValue").GetMethod;
            var value = nullableType.GetProperty("Value").GetMethod;
            var underlyingType = nullableType.GetUnderlyingType();
            var done = emit.DefineLabel();

            using (var loc = emit.DeclareLocal(nullableType))
            {
                var notNull = emit.DefineLabel();

                emit.StoreLocal(loc);       // TextWriter
                emit.LoadLocalAddress(loc); // TextWriter nullableType*
                emit.Call(hasValue);        // TextWriter bool
                emit.BranchIfTrue(notNull); // TextWriter

                emit.Pop();                 // --empty--
                WriteString("null", emit);  // --empty--
                emit.Branch(done);          // --empty--

                emit.MarkLabel(notNull);    // TextWriter
                emit.LoadLocalAddress(loc); // TextWriter nullableType*
                emit.Call(value);           // TextWriter value
            }

            if (underlyingType.IsPrimitiveType())
            {
                WritePrimitive(underlyingType, emit);
            }
            else
            {
                using (var loc = emit.DeclareLocal(underlyingType))
                {
                    emit.StoreLocal(loc);   // TextWriter

                    if (recursiveTypes.ContainsKey(underlyingType))
                    {
                        var act = typeof(Action<,>).MakeGenericType(typeof(TextWriter), underlyingType);
                        var invoke = act.GetMethod("Invoke");

                        emit.Pop();                                     // --empty--
                        emit.LoadLocal(recursiveTypes[underlyingType]); // Action<TextWriter, underlyingType>
                        emit.LoadArgument(0);                           // Action<,> TextWriter
                        emit.LoadLocal(loc);                            // Action<,> TextWriter value
                        emit.Call(invoke);                              // --empty--
                    }
                    else
                    {
                        if (underlyingType.IsListType())
                        {
                            WriteList(underlyingType, emit, recursiveTypes, loc);
                        }
                        else
                        {
                            if (underlyingType.IsDictionaryType())
                            {
                                WriteList(underlyingType, emit, recursiveTypes, loc);
                            }
                            else
                            {
                                emit.Pop();

                                WriteObject(underlyingType, emit, recursiveTypes, loc);
                            }
                        }
                    }
                }
            }

            emit.MarkLabel(done);
        }

        static void WriteDateTime(Emit emit)
        {
            // top of stack:
            //   - DateTime
            //   - TextWriter

            using (var loc = emit.DeclareLocal<DateTime>())
            {
                emit.StoreLocal(loc);       // TextWriter
                emit.LoadLocalAddress(loc); // TextWriter DateTime*
            }

            if (!SkipDateTimeMathMethods)
            {
                var subtractMtd = typeof(DateTime).GetMethod("Subtract", new[] { typeof(DateTime) });
                var totalMs = typeof(TimeSpan).GetProperty("TotalMilliseconds").GetMethod;
                var dtCons = typeof(DateTime).GetConstructor(new[] { typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(DateTimeKind) });

                emit.LoadConstant(1970);                    // TextWriter DateTime* 1970
                emit.LoadConstant(1);                       // TextWriter DateTime* 1970 1
                emit.LoadConstant(1);                       // TextWriter DateTime* 1970 1 1
                emit.LoadConstant(0);                       // TextWriter DateTime* 1970 1 1 0
                emit.LoadConstant(0);                       // TextWriter DateTime* 1970 1 1 0 0
                emit.LoadConstant(0);                       // TextWriter DateTime* 1970 1 1 0 0 0 
                emit.LoadConstant((int)DateTimeKind.Utc);   // TextWriter DateTime* 1970 1 1 0 0 0 Utc
                emit.NewObject(dtCons);                     // TextWriter DateTime* DateTime*
                emit.Call(subtractMtd);                     // TextWriter TimeSpan

                using (var loc = emit.DeclareLocal<TimeSpan>())
                {
                    emit.StoreLocal(loc);                   // TextWriter
                    emit.LoadLocalAddress(loc);             // TextWriter TimeSpan*
                }

                emit.Call(totalMs);                         // TextWriter double
                emit.Convert<long>();                       // TextWriter int

                WriteString("\"\\/Date(", emit);            // TextWriter int
                WritePrimitive(typeof(long), emit);         // --empty--
                WriteString(")\\/\"", emit);                  // --empty--

                return;
            }

            var getTicks = typeof(DateTime).GetProperty("Ticks").GetMethod;

            emit.Call(getTicks);                            // TextWriter long
            emit.LoadConstant(621355968000000000L);         // TextWriter long (Unix Epoch Ticks long)
            emit.Subtract();                                // TextWriter long
            emit.LoadConstant(10000L);                      // TextWriter long 10000
            emit.Divide();                                  // TextWriter long

            WriteString("\"\\/Date(", emit);            // TextWriter int
            WritePrimitive(typeof(long), emit);         // --empty--
            WriteString(")\\/\"", emit);                  // --empty--
        }

        static void WritePrimitive(Type primitiveType, Emit emit)
        {
            if (primitiveType == typeof(char))
            {
                WriteEncodedChar(emit);

                return;
            }

            if (primitiveType == typeof(string))
            {
                WriteEncodedString(emit);
                return;
            }

            if (primitiveType == typeof(DateTime))
            {
                WriteDateTime(emit);
                return;
            }

            if(primitiveType == typeof(bool))
            {
                var trueLabel = emit.DefineLabel();
                var done = emit.DefineLabel();

                emit.BranchIfTrue(trueLabel);   // TextWriter
                emit.Pop();                     // --empty--
                WriteString("false", emit);     // --empty--
                emit.Branch(done);

                emit.MarkLabel(trueLabel);      // TextWriter
                emit.Pop();                     // --empty--
                WriteString("true", emit);      // --empty--

                emit.MarkLabel(done);

                return;
            }

            var needsIntCoersion = primitiveType == typeof(byte) || primitiveType == typeof(sbyte) || primitiveType == typeof(short) || primitiveType == typeof(ushort);

            if (needsIntCoersion)
            {
                emit.Convert<int>();            // TextWriter int
                primitiveType = typeof(int);
            }

            if (primitiveType == typeof(int) && SkipNumberFormatting)
            {
                var writeInt = typeof(TextWriter).GetMethod("Write", new[] { typeof(int) });
                var done = emit.DefineLabel();

                emit.Duplicate();               // TextWriter int int

                var labels = Enumerable.Range(0, 100).Select(l => emit.DefineLabel()).ToArray();

                emit.Switch(labels);            // TextWriter int

                // default case

                if (UseCustomIntegerToString)
                {
                    emit.LoadLocal(CharBuffer);          // TextWriter int (ref char[])
                    emit.Call(InlineSerializer_CustomWriteInt); // --empty--
                }
                else
                {
                    emit.CallVirtual(writeInt);     // --empty--
                }

                emit.Branch(done);              // --empty--

                for (var i = 0; i < labels.Length; i++)
                {
                    var label = labels[i];

                    emit.MarkLabel(label);      // TextWriter int
                    emit.Pop();                 // TextWriter
                    emit.Pop();                 // --empty--
                    WriteString("" + i, emit);  // --empty--
                    emit.Branch(done);          // --empty--
                }

                emit.MarkLabel(done);           // --empty--

                return;
            }

            var builtInMtd = typeof(TextWriter).GetMethod("Write", new[] { primitiveType });

            if (UseCustomIntegerToString)
            {
                if (primitiveType == typeof(uint))
                {
                    emit.LoadLocal(CharBuffer);          // TextWriter int (ref char[])
                    emit.Call(InlineSerializer_CustomWriteUInt); // --empty--

                    return;
                }

                if (primitiveType == typeof(long))
                {
                    emit.LoadLocal(CharBuffer);          // TextWriter int (ref char[])
                    emit.Call(InlineSerializer_CustomWriteLong); // --empty--

                    return;
                }

                if (primitiveType == typeof(ulong))
                {
                    emit.LoadLocal(CharBuffer);          // TextWriter int (ref char[])
                    emit.Call(InlineSerializer_CustomWriteULong); // --empty--

                    return;
                }

                emit.CallVirtual(builtInMtd);       // --empty--
                return;
            }

            emit.CallVirtual(builtInMtd);       // --empty--
        }

        static MethodInfo InlineSerializer_CustomWriteInt = typeof(InlineSerializer).GetMethod("CustomWriteInt", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CustomWriteInt(TextWriter writer, int number, char[] buffer)
        {
            // Gotta special case this, we can't negate it
            if (number == int.MinValue)
            {
                writer.Write("-2147483648");
                return;
            }

            var ptr = CharBufferSize - 1;

            var copy = number;
            if (copy < 0)
            {
                copy = -copy;
            }

            do
            {
                var ix = copy % 10;
                copy /= 10;

                buffer[ptr] = (char)('0' + ix);
                ptr--;
            } while (copy != 0);

            if (number < 0)
            {
                buffer[ptr] = '-';
                ptr--;
            }

            writer.Write(buffer, ptr + 1, CharBufferSize - 1 - ptr);
        }

        static MethodInfo InlineSerializer_CustomWriteUInt = typeof(InlineSerializer).GetMethod("CustomWriteUInt", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CustomWriteUInt(TextWriter writer, uint number, char[] buffer)
        {
            var ptr = CharBufferSize - 1;

            var copy = number;
            
            do
            {
                var ix = copy % 10;
                copy /= 10;

                buffer[ptr] = (char)('0' + ix);
                ptr--;
            } while (copy != 0);

            writer.Write(buffer, ptr + 1, CharBufferSize - 1 - ptr);
        }

        static MethodInfo InlineSerializer_CustomWriteLong = typeof(InlineSerializer).GetMethod("CustomWriteLong", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CustomWriteLong(TextWriter writer, long number, char[] buffer)
        {
            // Gotta special case this, we can't negate it
            if (number == long.MinValue)
            {
                writer.Write("-9223372036854775808");
                return;
            }

            var ptr = CharBufferSize - 1;

            var copy = number;
            if (copy < 0)
            {
                copy = -copy;
            }

            do
            {
                var ix = (int)(copy % 10);
                copy /= 10;

                buffer[ptr] = (char)('0' + ix);
                ptr--;
            } while (copy != 0);

            if (number < 0)
            {
                buffer[ptr] = '-';
                ptr--;
            }

            writer.Write(buffer, ptr + 1, CharBufferSize - 1 - ptr);
        }

        static MethodInfo InlineSerializer_CustomWriteULong = typeof(InlineSerializer).GetMethod("CustomWriteULong", BindingFlags.Static | BindingFlags.NonPublic);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void CustomWriteULong(TextWriter writer, ulong number, char[] buffer)
        {
            var ptr = CharBufferSize - 1;

            var copy = number;

            do
            {
                var ix = (int)(copy % 10);
                copy /= 10;

                buffer[ptr] = (char)('0' + ix);
                ptr--;
            } while (copy != 0);

            writer.Write(buffer, ptr + 1, CharBufferSize - 1 - ptr);
        }

        static void WriteEncodedChar(Emit emit)
        {
            // top of stack is:
            //  - char
            //  - TextWriter

            var writeChar = typeof(TextWriter).GetMethod("Write", new[] { typeof(char) });

            var lowestCharNeedingEncoding = (int)CharacterEscapes.Keys.OrderBy(c => (int)c).First();

            var needLabels = CharacterEscapes.OrderBy(kv => kv.Key).Select(kv => Tuple.Create(kv.Key - lowestCharNeedingEncoding, kv.Value)).ToList();

            var labels = new List<Tuple<Sigil.Label, string>>();

            int? prev = null;
            foreach (var pair in needLabels)
            {
                if (prev != null && pair.Item1 - prev != 1) break;

                var label = emit.DefineLabel();

                labels.Add(Tuple.Create(label, pair.Item2));
                
                prev = pair.Item1;
            }

            var done = emit.DefineLabel();
            var slash = emit.DefineLabel();
            var quote = emit.DefineLabel();

            emit.Duplicate();                               // TextWriter char char
            emit.Convert<int>();
            emit.LoadConstant(lowestCharNeedingEncoding);   // TextWriter char char int
            emit.Subtract();                                // TextWriter char int

            emit.Switch(labels.Select(s => s.Item1).ToArray()); // TextWriter char

            // this is the fall-through (default) case

            emit.Duplicate();               // TextWriter char char
            emit.LoadConstant('\\');        // TextWriter char char \
            emit.BranchIfEqual(slash);      // TextWriter char

            emit.Duplicate();               // TextWriter char char
            emit.LoadConstant('"');         // TextWriter char char "
            emit.BranchIfEqual(quote);      // TextWriter clear

            emit.CallVirtual(writeChar);    // --empty--
            emit.Branch(done);              // --empty--

            emit.MarkLabel(slash);          // TextWriter char

            emit.Pop();                     // TextWriter
            emit.Pop();                     // --empty--
            WriteString(@"\\", emit);       // --empty--
            emit.Branch(done);              // --empty--

            emit.MarkLabel(quote);
            emit.Pop();                     // TextWriter
            emit.Pop();                     // --empty--
            WriteString(@"\""", emit);      // --empty--
            emit.Branch(done);              // --empty--

            foreach (var label in labels)
            {
                emit.MarkLabel(label.Item1);    // TextWriter char

                emit.Pop();                     // TextWriter
                emit.Pop();                     // --empty--
                WriteString(label.Item2, emit); // --empty-- 
                emit.Branch(done);              // --empty--
            }

            emit.MarkLabel(done);
        }

        static void WriteEncodedString(Emit emit)
        {
            // top of stack is:
            //  - string
            //  - TextWriter

            var writeChar = typeof(TextWriter).GetMethod("Write", new[] { typeof(char) });
            var strLength = typeof(string).GetProperty("Length").GetMethod;
            var strCharsIx = typeof(string).GetProperty("Chars").GetMethod;

            var done = emit.DefineLabel();

            using (var str = emit.DeclareLocal<string>())
            using (var i = emit.DeclareLocal<int>())
            {
                var loop = emit.DefineLabel();

                emit.LoadConstant(0);   // TextWriter string 0
                emit.StoreLocal(i);     // TextWriter string
                emit.StoreLocal(str);   // TextWriter
                emit.Duplicate();       // TextWriter TextWriter

                emit.MarkLabel(loop);               // TextWriter TextWriter
                emit.LoadLocal(i);                  // TextWriter TextWriter i
                emit.LoadLocal(str);                // TextWriter TextWriter i string
                emit.Call(strLength);               // TextWriter TextWriter i str.Length
                emit.BranchIfGreaterOrEqual(done);  // TextWriter TextWriter

                emit.LoadLocal(str);    // TextWriter TextWriter string
                emit.LoadLocal(i);      // TextWriter TextWriter string i
                emit.Call(strCharsIx);  // TextWriter TextWriter char

                WriteEncodedChar(emit); // TextWriter

                emit.LoadLocal(i);      // TextWriter i
                emit.LoadConstant(1);   // TextWriter i 1
                emit.Add();             // TextWriter (i+1)
                emit.StoreLocal(i);     // TextWriter
                emit.Duplicate();       // TextWriter TextWriter
                emit.Branch(loop);      // TextWriter TextWriter
            }

            emit.MarkLabel(done);       // TextWriter TextWriter
            emit.Pop();
            emit.Pop();
        }

        internal static void WriteObject(Type forType, Emit emit, Dictionary<Type, Sigil.Local> recursiveTypes, Sigil.Local inLocal = null)
        {
            var writeOrder = OrderMembersForAccess(forType);

            var notNull = emit.DefineLabel();

            var isValueType = forType.IsValueType;

            if (inLocal != null)
            {
                emit.LoadLocal(inLocal);
            }
            else
            {
                emit.LoadArgument(1);
            }

            if (isValueType)
            {
                using (var temp = emit.DeclareLocal(forType))
                {
                    emit.StoreLocal(temp);
                    emit.LoadLocalAddress(temp);
                }
            }

            var end = emit.DefineLabel();

            emit.BranchIfTrue(notNull);
            WriteString("null", emit);
            emit.Branch(end);

            emit.MarkLabel(notNull);
            WriteString("{", emit);

            var firstPass = true;
            var previousMemberWasStringy = false;
            foreach (var member in writeOrder)
            {
                string keyString;
                if (firstPass)
                {
                    keyString = "\"" + member.Name.JsonEscape() + "\":";
                    firstPass = false;
                }
                else
                {
                    keyString = ",\"" + member.Name.JsonEscape() + "\":";
                }

                if (previousMemberWasStringy)
                {
                    keyString = "\"" + keyString;
                }

                var isStringy = member.IsStringyType();

                if (isStringy)
                {
                    keyString = keyString + "\"";
                }

                WriteString(keyString, emit);
                WriteMember(member, emit, recursiveTypes, isValueType, inLocal);

                previousMemberWasStringy = isStringy;
            }

            if (previousMemberWasStringy)
            {
                WriteString("\"}", emit);
            }
            else
            {
                WriteString("}", emit);
            }

            emit.MarkLabel(end);
        }

        static void WriteList(Type listType, Emit emit, Dictionary<Type, Sigil.Local> recursiveTypes, Sigil.Local inLocal = null)
        {
            var elementType = listType.GetListInterface().GetGenericArguments()[0];

            var iEnumerable = typeof(IEnumerable<>).MakeGenericType(elementType);
            var iEnumerableGetEnumerator = iEnumerable.GetMethod("GetEnumerator");
            var enumeratorMoveNext = typeof(System.Collections.IEnumerator).GetMethod("MoveNext");
            var enumeratorCurrent = iEnumerableGetEnumerator.ReturnType.GetProperty("Current").GetMethod;

            var iList = typeof(IList<>).MakeGenericType(elementType);

            var isRecursive = recursiveTypes.ContainsKey(elementType);
            var preloadTextWriter = elementType.IsPrimitiveType() || isRecursive || elementType.IsNullableType();

            var notNull = emit.DefineLabel();

            if (inLocal != null)
            {
                emit.LoadLocal(inLocal);
            }
            else
            {
                emit.LoadArgument(1);
            }

            var end = emit.DefineLabel();

            emit.BranchIfTrue(notNull);
            WriteString("null", emit);
            emit.Branch(end);

            emit.MarkLabel(notNull);
            WriteString("[", emit);

            var done = emit.DefineLabel();

            using (var e = emit.DeclareLocal(iEnumerableGetEnumerator.ReturnType))
            {
                if (inLocal != null)
                {
                    emit.LoadLocal(inLocal);
                }
                else
                {
                    emit.LoadArgument(1);
                }

                emit.CastClass(iList);                        // IList<>
                emit.CallVirtual(iEnumerableGetEnumerator);   // IEnumerator<>
                emit.StoreLocal(e);                           // --empty--

                // Do the whole first element before the loop starts, so we don't need a branch to emit a ','
                {
                    emit.LoadLocal(e);                      // IEnumerator<>
                    emit.CallVirtual(enumeratorMoveNext);   // bool
                    emit.BranchIfFalse(done);               // --empty--

                    if (isRecursive)
                    {
                        var loc = recursiveTypes[elementType];

                        emit.LoadLocal(loc);                // Action<TextWriter, elementType>
                    }

                    if (preloadTextWriter)
                    {
                        emit.LoadArgument(0);               // Action<>? TextWriter
                    }

                    emit.LoadLocal(e);                      // Action<>? TextWriter? IEnumerator<>
                    emit.CallVirtual(enumeratorCurrent);    // Action<>? TextWriter? type

                    WriteElement(elementType, emit, recursiveTypes);   // --empty--
                }

                var loop = emit.DefineLabel();

                emit.MarkLabel(loop);

                emit.LoadLocal(e);                      // IEnumerator<>
                emit.CallVirtual(enumeratorMoveNext);   // bool
                emit.BranchIfFalse(done);               // --empty--

                if (isRecursive)
                {
                    var loc = recursiveTypes[elementType];

                    emit.LoadLocal(loc);                // Action<TextWriter, elementType>
                }

                if (preloadTextWriter)
                {
                    emit.LoadArgument(0);               // Action<>? TextWriter
                }

                emit.LoadLocal(e);                      // Action<>? TextWriter? IEnumerator<>
                emit.CallVirtual(enumeratorCurrent);    // Action<>? TextWriter? type

                WriteString(",", emit);

                WriteElement(elementType, emit, recursiveTypes);   // --empty--

                emit.Branch(loop);
            }

            emit.MarkLabel(done);

            WriteString("]", emit);

            emit.MarkLabel(end);
        }

        static void WriteElement(Type elementType, Emit emit, Dictionary<Type, Sigil.Local> recursiveTypes)
        {
            if (elementType.IsPrimitiveType())
            {
                WritePrimitive(elementType, emit);
                return;
            }

            if (elementType.IsNullableType())
            {
                WriteNullable(elementType, emit, recursiveTypes);
                return;
            }

            var isRecursive = recursiveTypes.ContainsKey(elementType);
            if (isRecursive)
            {
                // Stack is:
                //  - serializingType
                //  - TextWriter
                //  - Action<TextWriter, serializingType>

                var recursiveAct = typeof(Action<,>).MakeGenericType(typeof(TextWriter), elementType);
                var invoke = recursiveAct.GetMethod("Invoke");

                emit.Call(invoke);

                return;
            }

            using(var loc = emit.DeclareLocal(elementType))
            {
                emit.StoreLocal(loc);

                if (elementType.IsListType())
                {
                    WriteList(elementType, emit, recursiveTypes, loc);
                    return;
                }

                if (elementType.IsDictionaryType())
                {
                    WriteDictionary(elementType, emit, recursiveTypes, loc);
                    return;
                }

                WriteObject(elementType, emit, recursiveTypes, loc);
            }
        }

        static void WriteDictionary(Type dictType, Emit emit, Dictionary<Type, Sigil.Local> recursiveTypes, Sigil.Local inLocal = null)
        {
            var dictI = dictType.GetDictionaryInterface();

            var keyType = dictI.GetGenericArguments()[0];
            var elementType = dictI.GetGenericArguments()[1];

            if (keyType != typeof(string))
            {
                throw new InvalidOperationException("JSON dictionaries must have strings as keys, found: " + keyType);
            }

            var kvType = typeof(KeyValuePair<,>).MakeGenericType(typeof(string), elementType);

            var iEnumerable = typeof(IEnumerable<>).MakeGenericType(kvType);
            var iEnumerableGetEnumerator = iEnumerable.GetMethod("GetEnumerator");
            var enumeratorMoveNext = typeof(System.Collections.IEnumerator).GetMethod("MoveNext");
            var enumeratorCurrent = iEnumerableGetEnumerator.ReturnType.GetProperty("Current").GetMethod;

            var iDictionary = typeof(IDictionary<,>).MakeGenericType(typeof(string), elementType);

            var isRecursive = recursiveTypes.ContainsKey(elementType);
            var preloadTextWriter = elementType.IsPrimitiveType() || isRecursive || elementType.IsNullableType();

            var notNull = emit.DefineLabel();

            if (inLocal != null)
            {
                emit.LoadLocal(inLocal);
            }
            else
            {
                emit.LoadArgument(1);
            }

            var end = emit.DefineLabel();

            emit.BranchIfTrue(notNull);
            WriteString("null", emit);
            emit.Branch(end);

            emit.MarkLabel(notNull);
            WriteString("{", emit);

            var done = emit.DefineLabel();

            using (var e = emit.DeclareLocal(iEnumerableGetEnumerator.ReturnType))
            using(var kvpLoc = emit.DeclareLocal(kvType))
            {
                if (inLocal != null)
                {
                    emit.LoadLocal(inLocal);
                }
                else
                {
                    emit.LoadArgument(1);
                }

                emit.CastClass(iDictionary);                  // IDictionary<,>
                emit.CallVirtual(iEnumerableGetEnumerator);   // IEnumerator<KeyValuePair<,>>
                emit.StoreLocal(e);                           // --empty--

                // Do the whole first element before the loop starts, so we don't need a branch to emit a ','
                {
                    emit.LoadLocal(e);                      // IEnumerator<KeyValuePair<,>>
                    emit.CallVirtual(enumeratorMoveNext);   // bool
                    emit.BranchIfFalse(done);               // --empty--

                    if (isRecursive)
                    {
                        var loc = recursiveTypes[elementType];

                        emit.LoadLocal(loc);                // Action<TextWriter, elementType>
                    }

                    if (preloadTextWriter)
                    {
                        emit.LoadArgument(0);               // Action<>? TextWriter
                    }

                    emit.LoadLocal(e);                      // Action<>? TextWriter? IEnumerator<>
                    emit.CallVirtual(enumeratorCurrent);    // Action<>? TextWriter? KeyValuePair<,>

                    emit.StoreLocal(kvpLoc);                // Action<>? TextWriter?
                    emit.LoadLocalAddress(kvpLoc);          // Action<>? TextWriter? KeyValuePair<,>*

                    WriteKeyValue(elementType, emit, recursiveTypes);   // --empty--
                }

                var loop = emit.DefineLabel();

                emit.MarkLabel(loop);

                emit.LoadLocal(e);                      // IEnumerator<KeyValuePair<,>>
                emit.CallVirtual(enumeratorMoveNext);   // bool
                emit.BranchIfFalse(done);               // --empty--

                if (isRecursive)
                {
                    var loc = recursiveTypes[elementType];

                    emit.LoadLocal(loc);                // Action<TextWriter, elementType>
                }

                if (preloadTextWriter)
                {
                    emit.LoadArgument(0);               // Action<>? TextWriter
                }

                emit.LoadLocal(e);                      // Action<>? TextWriter? IEnumerator<>
                emit.CallVirtual(enumeratorCurrent);    // Action<>? TextWriter? KeyValuePair<,>

                emit.StoreLocal(kvpLoc);                // Action<>? TextWriter?
                emit.LoadLocalAddress(kvpLoc);          // Action<>? TextWriter? KeyValuePair<,>*

                WriteString(",", emit);

                WriteKeyValue(elementType, emit, recursiveTypes);   // --empty--
            }

            emit.MarkLabel(done);

            WriteString("}", emit);

            emit.MarkLabel(end);
        }

        static void WriteKeyValue(Type elementType, Emit emit, Dictionary<Type, Sigil.Local> recursiveTypes)
        {
            // top of the stack is a KeyValue<string, elementType>

            var keyValuePair = typeof(KeyValuePair<,>).MakeGenericType(typeof(string), elementType);
            var key = keyValuePair.GetProperty("Key").GetMethod;
            var value = keyValuePair.GetProperty("Value").GetMethod;

            WriteString("\"", emit);

            emit.Duplicate();   // kvp kvp
            emit.Call(key);     // kvp string

            using (var str = emit.DeclareLocal<string>())
            {
                emit.StoreLocal(str);   // kvp
                emit.LoadArgument(0);   // kvp TextWriter
                emit.LoadLocal(str);    // kvp TextWriter string

                emit.CallVirtual(typeof(TextWriter).GetMethod("Write", new [] { typeof(string) })); // kvp
            }

            WriteString("\":", emit);

            emit.Call(value);   // elementType

            if (elementType.IsPrimitiveType())
            {
                WritePrimitive(elementType, emit);
                return;
            }

            if (elementType.IsNullableType())
            {
                WriteNullable(elementType, emit, recursiveTypes);
                return;
            }

            var isRecursive = recursiveTypes.ContainsKey(elementType);
            if (isRecursive)
            {
                // Stack is:
                //  - serializingType
                //  - TextWriter
                //  - Action<TextWriter, serializingType>

                var recursiveAct = typeof(Action<,>).MakeGenericType(typeof(TextWriter), elementType);
                var invoke = recursiveAct.GetMethod("Invoke");

                emit.Call(invoke);

                return;
            }

            using (var loc = emit.DeclareLocal(elementType))
            {
                emit.StoreLocal(loc);

                if (elementType.IsListType())
                {
                    WriteList(elementType, emit, recursiveTypes, loc);
                    return;
                }

                if (elementType.IsDictionaryType())
                {
                    WriteList(elementType, emit, recursiveTypes, loc);
                    return;
                }

                WriteObject(elementType, emit, recursiveTypes, loc);
            }
        }

        static Dictionary<Type, Sigil.Local> PreloadRecursiveTypes(HashSet<Type> recursiveTypes, Emit emit)
        {
            var ret = new Dictionary<Type, Sigil.Local>();

            foreach (var type in recursiveTypes)
            {
                var cacheType = typeof(TypeCache<>).MakeGenericType(type);
                var thunk = cacheType.GetField("Thunk", BindingFlags.Public | BindingFlags.Static);

                var loc = emit.DeclareLocal(thunk.FieldType);

                emit.LoadField(thunk);  // Action<TextWriter, type>
                emit.StoreLocal(loc);   // --empty--

                ret[type] = loc;
            }

            return ret;
        }

        static void AddCharBuffer(Emit emit)
        {
            if (UseCustomIntegerToString)
            {
                emit.DeclareLocal<char[]>(CharBuffer);
                emit.LoadConstant(CharBufferSize);
                emit.NewArray<char>();
                emit.StoreLocal(CharBuffer);
            }
        }

        static Action<TextWriter, ForType> BuildObjectWithNewDelegate<ForType>()
        {
            var recursiveTypes = FindRecursiveTypes(typeof(ForType));

            var emit = Emit.NewDynamicMethod(typeof(void), new[] { typeof(TextWriter), typeof(ForType) });

            AddCharBuffer(emit);

            var preloaded = PreloadRecursiveTypes(recursiveTypes, emit);

            WriteObject(typeof(ForType), emit, preloaded);
            emit.Return();

            return emit.CreateDelegate<Action<TextWriter, ForType>>();
        }

        static Action<TextWriter, ListType> BuildListWithNewDelegate<ListType>()
        {
            var recursiveTypes = FindRecursiveTypes(typeof(ListType));

            var emit = Emit.NewDynamicMethod(typeof(void), new[] { typeof(TextWriter), typeof(ListType) });

            AddCharBuffer(emit);

            var preloaded = PreloadRecursiveTypes(recursiveTypes, emit);

            WriteList(typeof(ListType), emit, preloaded);
            emit.Return();

            return emit.CreateDelegate<Action<TextWriter, ListType>>();
        }

        static Action<TextWriter, DictType> BuildDictionaryWithNewDelegate<DictType>()
        {
            var recursiveTypes = FindRecursiveTypes(typeof(DictType));

            var emit = Emit.NewDynamicMethod(typeof(void), new[] { typeof(TextWriter), typeof(DictType) });

            AddCharBuffer(emit);

            var preloaded = PreloadRecursiveTypes(recursiveTypes, emit);

            WriteDictionary(typeof(DictType), emit, preloaded);
            emit.Return();

            return emit.CreateDelegate<Action<TextWriter, DictType>>();
        }

        static Action<TextWriter, PrimitiveType> BuildPrimitiveWithNewDelegate<PrimitiveType>()
        {
            var primitiveType = typeof(PrimitiveType);
            var isString = primitiveType == typeof(string) || primitiveType == typeof(char);

            var emit = Emit.NewDynamicMethod(typeof(void), new[] { typeof(TextWriter), typeof(PrimitiveType) });

            AddCharBuffer(emit);

            emit.LoadArgument(0);
            emit.LoadArgument(1);

            if (isString)
            {
                WriteString("\"", emit);
            }

            WritePrimitive(typeof(PrimitiveType), emit);

            if (isString)
            {
                WriteString("\"", emit);
            }

            emit.Return();

            return emit.CreateDelegate<Action<TextWriter, PrimitiveType>>();
        }

        static Action<TextWriter, NullableType> BuildNullableWithNewDelegate<NullableType>()
        {
            var recursiveTypes = FindRecursiveTypes(typeof(NullableType));

            var emit = Emit.NewDynamicMethod(typeof(void), new[] { typeof(TextWriter), typeof(NullableType) });

            AddCharBuffer(emit);

            var preloaded = PreloadRecursiveTypes(recursiveTypes, emit);

            emit.LoadArgument(0);
            emit.LoadArgument(1);

            WriteNullable(typeof(NullableType), emit, preloaded);
            
            emit.Return();

            return emit.CreateDelegate<Action<TextWriter, NullableType>>();
        }

        public static Action<TextWriter, ForType> Build<ForType>()
        {
            var forType = typeof(ForType);

            if (forType.IsNullableType())
            {
                return BuildNullableWithNewDelegate<ForType>();
            }

            if (forType.IsPrimitiveType())
            {
                return BuildPrimitiveWithNewDelegate<ForType>();
            }

            if (forType.IsDictionaryType())
            {
                return BuildDictionaryWithNewDelegate<ForType>();
            }

            if (forType.IsListType())
            {
                return BuildListWithNewDelegate<ForType>();
            }

            return BuildObjectWithNewDelegate<ForType>();
        }
    }
}
