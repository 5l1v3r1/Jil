﻿using Jil.Serialize;
using Jil.SerializeDynamic;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil
{
    /// <summary>
    /// Fast JSON serializer.
    /// </summary>
    public sealed class JSON
    {
        /// <summary>
        /// Serializes the given data to the provided TextWriter.
        /// 
        /// Pass an Options object to configure the particulars (such as whitespace, and DateTime formats) of
        /// the produced JSON.  If omitted, Options.Default is used.
        /// 
        /// Unlike Serialize, this method will inspect the Type of data to determine what serializer to invoke.
        /// This is not as fast as calling Serialize with a known type.
        /// 
        /// Objects with participate in the DLR will be serialized appropriately, all other types
        /// will be serialized via reflection.
        /// </summary>
        public static void SerializeDynamic(dynamic data, TextWriter output, Options options = null)
        {
            DynamicSerializer.Serialize(output, (object)data, options ?? Options.Default, 0);
        }

        /// <summary>
        /// Serializes the given data, returning it as a string.
        /// 
        /// Pass an Options object to configure the particulars (such as whitespace, and DateTime formats) of
        /// the produced JSON.  If omitted, Options.Default is used.
        /// 
        /// Unlike Serialize, this method will inspect the Type of data to determine what serializer to invoke.
        /// This is not as fast as calling Serialize with a known type.
        /// 
        /// Objects with participate in the DLR will be serialized appropriately, all other types
        /// will be serialized via reflection.
        /// </summary>
        public static string SerializeDynamic(object data, Options options = null)
        {
            using (var str = new StringWriter())
            {
                SerializeDynamic(data, str, options);
                return str.ToString();
            }
        }

        /// <summary>
        /// Serializes the given data to the provided TextWriter.
        /// 
        /// Pass an Options object to configure the particulars (such as whitespace, and DateTime formats) of
        /// the produced JSON.  If omitted, Options.Default is used.
        /// </summary>
        public static void Serialize<T>(T data, TextWriter output, Options options = null)
        {
            if (output == null)
            {
                throw new ArgumentNullException("output");
            }

            if (typeof(T) == typeof(object))
            {
                SerializeDynamic(data, output, options);
                return;
            }

            var asStr = Serialize(data, options);
            output.Write(asStr);

            // TODO: Uncomment this stuff when the time comes; 
            //       Memory pressure is a lot worse if we actually
            //       spin everything out into a string, TextWriter
            //       is an all around better interface

            /*options = options ?? Options.Default;

            switch (options.UseDateTimeFormat)
            {
                case DateTimeFormat.ISO8601:
                    ISO8601(data, output, options);
                    return;

                case DateTimeFormat.MillisecondsSinceUnixEpoch:
                    Milliseconds(data, output, options);
                    return;

                case DateTimeFormat.SecondsSinceUnixEpoch:
                    Seconds(data, output, options);
                    return;

                case DateTimeFormat.NewtonsoftStyleMillisecondsSinceUnixEpoch:
                    NewtonsoftStyle(data, output, options);
                    return;

                default: throw new InvalidOperationException("Unexpected Options: " + options);
            }*/
        }

        /// <summary>
        /// Serializes the given data, returning the output as a string.
        /// 
        /// Pass an Options object to configure the particulars (such as whitespace, and DateTime formats) of
        /// the produced JSON.  If omitted, Options.Default is used.
        /// </summary>
        public static string Serialize<T>(T data, Options options = null)
        {
            if (typeof(T) == typeof(object))
            {
                return SerializeDynamic(data, options);
            }

            options = options ?? Options.Default;

            switch (options.UseDateTimeFormat)
            {
                case DateTimeFormat.ISO8601:
                    return ISO8601ToString(data, options);

                case DateTimeFormat.MillisecondsSinceUnixEpoch:
                    return MillisecondsToString(data, options);

                case DateTimeFormat.SecondsSinceUnixEpoch:
                    return SecondsToString(data, options);

                case DateTimeFormat.NewtonsoftStyleMillisecondsSinceUnixEpoch:
                    return NewtonsoftStyleToString(data, options);

                default: throw new InvalidOperationException("Unexpected Options: " + options);
            }
        }

        static string ReadToString<T>(StringThunkDelegate<T> del, T data)
        {
            var writer = new ThunkWriter();
            writer.Init();
            del(ref writer, data, 0);

            return writer.StaticToString();
        }

        static void NewtonsoftStyle<T>(T data, TextWriter output, Options options)
        {
            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<NewtonsoftStylePrettyPrintExcludeNullsJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP)
            {
                TypeCache<NewtonsoftStylePrettyPrintExcludeNullsJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<NewtonsoftStyleExcludeNullsJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<NewtonsoftStylePrettyPrintJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                TypeCache<NewtonsoftStylePrettyPrintExcludeNullsInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldIncludeInherited)
            {
                TypeCache<NewtonsoftStyleExcludeNullsInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.IsJSONP)
            {
                TypeCache<NewtonsoftStyleExcludeNullsJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.IsJSONP)
            {
                TypeCache<NewtonsoftStylePrettyPrintJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint)
            {
                TypeCache<NewtonsoftStylePrettyPrintExcludeNulls, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                TypeCache<NewtonsoftStylePrettyPrintInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<NewtonsoftStyleJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls)
            {
                TypeCache<NewtonsoftStyleExcludeNulls, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint)
            {
                TypeCache<NewtonsoftStylePrettyPrint, T>.Get()(output, data, 0);
                return;
            }

            if (options.IsJSONP)
            {
                TypeCache<NewtonsoftStyleJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldIncludeInherited)
            {
                TypeCache<NewtonsoftStyleInherited, T>.Get()(output, data, 0);
                return;
            }

            TypeCache<NewtonsoftStyle, T>.Get()(output, data, 0);
        }

        static string NewtonsoftStyleToString<T>(T data, Options options)
        {
            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<NewtonsoftStylePrettyPrintExcludeNullsJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP)
            {
                return ReadToString(TypeCache<NewtonsoftStylePrettyPrintExcludeNullsJSONP, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<NewtonsoftStyleExcludeNullsJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<NewtonsoftStylePrettyPrintJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<NewtonsoftStylePrettyPrintExcludeNullsInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<NewtonsoftStyleExcludeNullsInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.IsJSONP)
            {
                return ReadToString(TypeCache<NewtonsoftStyleExcludeNullsJSONP, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.IsJSONP)
            {
                return ReadToString(TypeCache<NewtonsoftStylePrettyPrintJSONP, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint)
            {
                return ReadToString(TypeCache<NewtonsoftStylePrettyPrintExcludeNulls, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<NewtonsoftStylePrettyPrintInherited, T>.GetToString(), data);
            }

            if (options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<NewtonsoftStyleJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls)
            {
                return ReadToString(TypeCache<NewtonsoftStyleExcludeNulls, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint)
            {
                return ReadToString(TypeCache<NewtonsoftStylePrettyPrint, T>.GetToString(), data);
            }

            if (options.IsJSONP)
            {
                return ReadToString(TypeCache<NewtonsoftStyleJSONP, T>.GetToString(), data);
            }

            if (options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<NewtonsoftStyleInherited, T>.GetToString(), data);
            }

            return ReadToString(TypeCache<NewtonsoftStyle, T>.GetToString(), data);
        }

        static void Milliseconds<T>(T data, TextWriter output, Options options)
        {
            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<MillisecondsPrettyPrintExcludeNullsJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP)
            {
                TypeCache<MillisecondsPrettyPrintExcludeNullsJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<MillisecondsExcludeNullsJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<MillisecondsPrettyPrintJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                TypeCache<MillisecondsPrettyPrintExcludeNullsInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldIncludeInherited)
            {
                TypeCache<MillisecondsExcludeNullsInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.IsJSONP)
            {
                TypeCache<MillisecondsExcludeNullsJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.IsJSONP)
            {
                TypeCache<MillisecondsPrettyPrintJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint)
            {
                TypeCache<MillisecondsPrettyPrintExcludeNulls, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                TypeCache<MillisecondsPrettyPrintInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<MillisecondsJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls)
            {
                TypeCache<MillisecondsExcludeNulls, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint)
            {
                TypeCache<MillisecondsPrettyPrint, T>.Get()(output, data, 0);
                return;
            }

            if (options.IsJSONP)
            {
                TypeCache<MillisecondsJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldIncludeInherited)
            {
                TypeCache<MillisecondsInherited, T>.Get()(output, data, 0);
                return;
            }

            TypeCache<Milliseconds, T>.Get()(output, data, 0);
        }

        static string MillisecondsToString<T>(T data, Options options)
        {
            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<MillisecondsPrettyPrintExcludeNullsJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP)
            {
                return ReadToString(TypeCache<MillisecondsPrettyPrintExcludeNullsJSONP, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<MillisecondsExcludeNullsJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<MillisecondsPrettyPrintJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<MillisecondsPrettyPrintExcludeNullsInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<MillisecondsExcludeNullsInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.IsJSONP)
            {
                return ReadToString(TypeCache<MillisecondsExcludeNullsJSONP, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.IsJSONP)
            {
                return ReadToString(TypeCache<MillisecondsPrettyPrintJSONP, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint)
            {
                return ReadToString(TypeCache<MillisecondsPrettyPrintExcludeNulls, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<MillisecondsPrettyPrintInherited, T>.GetToString(), data);
            }

            if (options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<MillisecondsJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls)
            {
                return ReadToString(TypeCache<MillisecondsExcludeNulls, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint)
            {
                return ReadToString(TypeCache<MillisecondsPrettyPrint, T>.GetToString(), data);
            }

            if (options.IsJSONP)
            {
                return ReadToString(TypeCache<MillisecondsJSONP, T>.GetToString(), data);
            }

            if (options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<MillisecondsInherited, T>.GetToString(), data);
            }

            return ReadToString(TypeCache<Milliseconds, T>.GetToString(), data);
        }

        static void Seconds<T>(T data, TextWriter output, Options options)
        {
            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<SecondsPrettyPrintExcludeNullsJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP)
            {
                TypeCache<SecondsPrettyPrintExcludeNullsJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<SecondsExcludeNullsJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<SecondsPrettyPrintJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                TypeCache<SecondsPrettyPrintExcludeNullsInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldIncludeInherited)
            {
                TypeCache<SecondsExcludeNullsInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.IsJSONP)
            {
                TypeCache<SecondsExcludeNullsJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.IsJSONP)
            {
                TypeCache<SecondsPrettyPrintJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint)
            {
                TypeCache<SecondsPrettyPrintExcludeNulls, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                TypeCache<SecondsPrettyPrintInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<SecondsJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls)
            {
                TypeCache<SecondsExcludeNulls, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint)
            {
                TypeCache<SecondsPrettyPrint, T>.Get()(output, data, 0);
                return;
            }

            if (options.IsJSONP)
            {
                TypeCache<SecondsJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldIncludeInherited)
            {
                TypeCache<SecondsInherited, T>.Get()(output, data, 0);
                return;
            }

            TypeCache<Seconds, T>.Get()(output, data, 0);
        }

        static string SecondsToString<T>(T data, Options options)
        {
            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<SecondsPrettyPrintExcludeNullsJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP)
            {
                return ReadToString(TypeCache<SecondsPrettyPrintExcludeNullsJSONP, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<SecondsExcludeNullsJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<SecondsPrettyPrintJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<SecondsPrettyPrintExcludeNullsInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<SecondsExcludeNullsInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.IsJSONP)
            {
                return ReadToString(TypeCache<SecondsExcludeNullsJSONP, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.IsJSONP)
            {
                return ReadToString(TypeCache<SecondsPrettyPrintJSONP, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint)
            {
                return ReadToString(TypeCache<SecondsPrettyPrintExcludeNulls, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<SecondsPrettyPrintInherited, T>.GetToString(), data);
            }

            if (options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<SecondsJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls)
            {
                return ReadToString(TypeCache<SecondsExcludeNulls, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint)
            {
                return ReadToString(TypeCache<SecondsPrettyPrint, T>.GetToString(), data);
            }

            if (options.IsJSONP)
            {
                return ReadToString(TypeCache<SecondsJSONP, T>.GetToString(), data);
            }

            if (options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<SecondsInherited, T>.GetToString(), data);
            }

            return ReadToString(TypeCache<Seconds, T>.GetToString(), data);
        }

        static void ISO8601<T>(T data, TextWriter output, Options options)
        {
            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<ISO8601PrettyPrintExcludeNullsJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP)
            {
                TypeCache<ISO8601PrettyPrintExcludeNullsJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<ISO8601ExcludeNullsJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<ISO8601PrettyPrintJSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                TypeCache<ISO8601PrettyPrintExcludeNullsInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldIncludeInherited)
            {
                TypeCache<ISO8601ExcludeNullsInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.IsJSONP)
            {
                TypeCache<ISO8601ExcludeNullsJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.IsJSONP)
            {
                TypeCache<ISO8601PrettyPrintJSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint)
            {
                TypeCache<ISO8601PrettyPrintExcludeNulls, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                TypeCache<ISO8601PrettyPrintInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.IsJSONP && options.ShouldIncludeInherited)
            {
                TypeCache<ISO8601JSONPInherited, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldExcludeNulls)
            {
                TypeCache<ISO8601ExcludeNulls, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldPrettyPrint)
            {
                TypeCache<ISO8601PrettyPrint, T>.Get()(output, data, 0);
                return;
            }

            if (options.IsJSONP)
            {
                TypeCache<ISO8601JSONP, T>.Get()(output, data, 0);
                return;
            }

            if (options.ShouldIncludeInherited)
            {
                TypeCache<ISO8601Inherited, T>.Get()(output, data, 0);
                return;
            }

            TypeCache<ISO8601, T>.Get()(output, data, 0);
        }

        static string ISO8601ToString<T>(T data, Options options)
        {
            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<ISO8601PrettyPrintExcludeNullsJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.IsJSONP)
            {
                return ReadToString(TypeCache<ISO8601PrettyPrintExcludeNullsJSONP, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<ISO8601ExcludeNullsJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<ISO8601PrettyPrintJSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<ISO8601PrettyPrintExcludeNullsInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<ISO8601ExcludeNullsInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.IsJSONP)
            {
                return ReadToString(TypeCache<ISO8601ExcludeNullsJSONP, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.IsJSONP)
            {
                return ReadToString(TypeCache<ISO8601PrettyPrintJSONP, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls && options.ShouldPrettyPrint)
            {
                return ReadToString(TypeCache<ISO8601PrettyPrintExcludeNulls, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<ISO8601PrettyPrintInherited, T>.GetToString(), data);
            }

            if (options.IsJSONP && options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<ISO8601JSONPInherited, T>.GetToString(), data);
            }

            if (options.ShouldExcludeNulls)
            {
                return ReadToString(TypeCache<ISO8601ExcludeNulls, T>.GetToString(), data);
            }

            if (options.ShouldPrettyPrint)
            {
                return ReadToString(TypeCache<ISO8601PrettyPrint, T>.GetToString(), data);
            }

            if (options.IsJSONP)
            {
                return ReadToString(TypeCache<ISO8601JSONP, T>.GetToString(), data);
            }

            if (options.ShouldIncludeInherited)
            {
                return ReadToString(TypeCache<ISO8601Inherited, T>.GetToString(), data);
            }

            return ReadToString(TypeCache<ISO8601, T>.GetToString(), data);
        }

        /// <summary>
        /// Deserializes JSON from the given TextReader as the passed type.
        /// 
        /// This is equivalent to calling Deserialize&lt;T&gt;(TextReader, Options), except
        /// without requiring a generic parameter.  For true dynamic deserialization, you 
        /// should use DeserializeDynamic instead.
        /// 
        /// Pass an Options object to specify the particulars (such as DateTime formats) of
        /// the JSON being deserialized.  If omitted, Options.Default is used.
        /// </summary>
        public static object Deserialize(TextReader reader, Type type, Options options = null)
        {
            if(reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if(type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (type == typeof(object))
            {
                return DeserializeDynamic(reader, options);
            }

            return Jil.Deserialize.DeserializeIndirect.Deserialize(reader, type, options);
        }

        /// <summary>
        /// Deserializes JSON from the given string as the passed type.
        /// 
        /// This is equivalent to calling Deserialize&lt;T&gt;(string, Options), except
        /// without requiring a generic parameter.  For true dynamic deserialization, you 
        /// should use DeserializeDynamic instead.
        /// 
        /// Pass an Options object to specify the particulars (such as DateTime formats) of
        /// the JSON being deserialized.  If omitted, Options.Default is used.
        /// </summary>
        public static object Deserialize(string text, Type type, Options options = null)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            using (var reader = new StringReader(text))
            {
                return Deserialize(reader, type, options);
            }
        }

        /// <summary>
        /// Deserializes JSON from the given TextReader.
        /// 
        /// Pass an Options object to specify the particulars (such as DateTime formats) of
        /// the JSON being deserialized.  If omitted, Options.Default is used.
        /// </summary>
        public static T Deserialize<T>(TextReader reader, Options options = null)
        {
            if (reader == null)
            {
                throw new ArgumentNullException("reader");
            }

            if (typeof(T) == typeof(object))
            {
                return DeserializeDynamic(reader, options);
            }

            try
            {
                options = options ?? Options.Default;

                switch (options.UseDateTimeFormat)
                {
                    case DateTimeFormat.NewtonsoftStyleMillisecondsSinceUnixEpoch:
                        return Jil.Deserialize.TypeCache<Jil.Deserialize.NewtonsoftStyle, T>.Get()(reader, 0);
                    case DateTimeFormat.MillisecondsSinceUnixEpoch:
                        return Jil.Deserialize.TypeCache<Jil.Deserialize.MillisecondStyle, T>.Get()(reader, 0);
                    case DateTimeFormat.SecondsSinceUnixEpoch:
                        return Jil.Deserialize.TypeCache<Jil.Deserialize.SecondStyle, T>.Get()(reader, 0);
                    case DateTimeFormat.ISO8601:
                        return Jil.Deserialize.TypeCache<Jil.Deserialize.ISO8601Style, T>.Get()(reader, 0);
                    default: throw new InvalidOperationException("Unexpected Options: " + options);
                }

            }
            catch (Exception e)
            {
                if (e is DeserializationException) throw;

                throw new DeserializationException(e, reader);
            }
        }

        /// <summary>
        /// Deserializes JSON from the given string.
        /// 
        /// Pass an Options object to specify the particulars (such as DateTime formats) of
        /// the JSON being deserialized.  If omitted, Options.Default is used.
        /// </summary>
        public static T Deserialize<T>(string text, Options options = null)
        {
            if (text == null)
            {
                throw new ArgumentNullException("text");
            }

            if (typeof(T) == typeof(object))
            {
                return DeserializeDynamic(text, options);
            }

            using (var reader = new StringReader(text))
            {
                return Deserialize<T>(reader, options);
            }
        }

        /// <summary>
        /// Deserializes JSON from the given TextReader, inferring types from the structure of the JSON text.
        /// 
        /// For the best performance, use the strongly typed Deserialize method when possible.
        /// </summary>
        public static dynamic DeserializeDynamic(TextReader reader, Options options = null)
        {
            options = options ?? Options.Default;

            var built = Jil.DeserializeDynamic.DynamicDeserializer.Deserialize(reader, options);

            return built.BeingBuilt;
        }

        /// <summary>
        /// Deserializes JSON from the given string, inferring types from the structure of the JSON text.
        /// 
        /// For the best performance, use the strongly typed Deserialize method when possible.
        /// </summary>
        public static dynamic DeserializeDynamic(string str, Options options = null)
        {
            using (var reader = new StringReader(str))
            {
                return DeserializeDynamic(reader, options);
            }
        }
    }
}
