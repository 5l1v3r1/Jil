﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil.DeserializeDynamic
{
    class DynamicDeserializer
    {
        internal static bool UseFastNumberParsing = true;

        public static ObjectBuilder Deserialize(TextReader reader)
        {
            var ret = new ObjectBuilder();

            DeserializeMember(reader, ret);

            Methods.ConsumeWhiteSpace(reader);
            var c = reader.Peek();
            if (c != -1) throw new DeserializationException("Expected end of stream", reader);

            return ret;
        }

        static void DeserializeMember(TextReader reader, ObjectBuilder builder)
        {
            Methods.ConsumeWhiteSpace(reader);

            var c = reader.Read();
            switch (c)
            {
                case -1: throw new DeserializationException("Unexpected end of stream", reader);
                case '"': DeserializeString(reader, builder); return;
                case '[': DeserializeArray(reader, builder); return;
                case '{': DeserializeObject(reader, builder); return;
                case 'n': DeserializeNull(reader, builder); return;
                case 't': DeserializeTrue(reader, builder); return;
                case 'f': DeserializeFalse(reader, builder); return;
                case '-': DeserializeNumber((char)c, reader, builder); return;
            }

            if (c >= '0' && c <= '9')
            {
                DeserializeNumber((char)c, reader, builder);
                return;
            }

            throw new DeserializationException("Expected \", [, {, n, t, f, -, 0, 1, 2, 3, 4, 5, 6, 7, 8, or 9; found " + (char)c, reader);
        }

        static void DeserializeTrue(TextReader reader, ObjectBuilder builder)
        {
            var c = reader.Read();
            if (c != 'r') throw new DeserializationException("Expected r", reader);
            c = reader.Read();
            if (c != 'u') throw new DeserializationException("Expected u", reader);
            c = reader.Read();
            if (c != 'e') throw new DeserializationException("Expected e", reader);

            builder.PutTrue();
        }

        static void DeserializeFalse(TextReader reader, ObjectBuilder builder)
        {
            var c = reader.Read();
            if (c != 'a') throw new DeserializationException("Expected a", reader);
            c = reader.Read();
            if (c != 'l') throw new DeserializationException("Expected l", reader);
            c = reader.Read();
            if (c != 's') throw new DeserializationException("Expected s", reader);
            c = reader.Read();
            if (c != 'e') throw new DeserializationException("Expected e", reader);

            builder.PutFalse();
        }

        static void DeserializeNull(TextReader reader, ObjectBuilder builder)
        {
            var c = reader.Read();
            if (c != 'u') throw new DeserializationException("Expected u", reader);
            c = reader.Read();
            if (c != 'l') throw new DeserializationException("Expected l", reader);
            c = reader.Read();
            if (c != 'l') throw new DeserializationException("Expected l", reader);

            builder.PutNull();
        }

        static void DeserializeString(TextReader reader, ObjectBuilder builder)
        {
            var str = Methods.ReadEncodedStringWithBuffer(reader, builder.CommonCharBuffer, ref builder.CommonStringBuffer);

            builder.PutString(str);
        }

        static void DeserializeArray(TextReader reader, ObjectBuilder builder)
        {
            int c;
            builder.StartArray();

            while(true)
            {
                c = reader.Peek();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", reader);
                if (c == ']')
                {
                    reader.Read();  // skip the ]
                    break;
                }

                DeserializeMember(reader, builder);
                Methods.ConsumeWhiteSpace(reader);
                c = reader.Read();

                if(c == ',') continue;
                if(c == ']') break;

                if(c == -1) throw new DeserializationException("Unexpected end of stream", reader);

                throw new DeserializationException("Expected , or ], found "+(char)c, reader);
            }

            builder.EndArray();
        }

        static void DeserializeObject(TextReader reader, ObjectBuilder builder)
        {
            int c;
            builder.StartObject();

            while (true)
            {
                Methods.ConsumeWhiteSpace(reader);

                c = reader.Peek();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", reader);
                if (c == '}')
                {
                    reader.Read();  // skip }
                    break;
                }

                c = reader.Read();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", reader);
                if (c != '"') throw new DeserializationException("Expected \", found " + (char)c, reader);

                builder.StartObjectMember();
                DeserializeString(reader, builder);

                Methods.ConsumeWhiteSpace(reader);
                c = reader.Read();
                if (c == -1) throw new DeserializationException("Unexpected end of stream", reader);
                if (c != ':') throw new DeserializationException("Expected :, found " + (char)c, reader);

                DeserializeMember(reader, builder);

                builder.EndObjectMember();

                Methods.ConsumeWhiteSpace(reader);
                c = reader.Read();

                if (c == ',') continue;
                if (c == '}') break;

                if (c == -1) throw new DeserializationException("Unexpected end of stream", reader);

                throw new DeserializationException("Expected , or }, found " + (char)c, reader);
            }

            builder.EndObject();
        }

        static void DeserializeNumber(char leadingChar, TextReader reader, ObjectBuilder builder)
        {
            if (!UseFastNumberParsing)
            {
                var number = Methods.ReadDouble(leadingChar, reader, ref builder.CommonStringBuffer);

                builder.PutNumber(number);

                return;
            }

            long beforeDot, afterDot, afterEbeforeDot, afterEafterDot;
            byte afterDotLen, afterEafterDotLen;
            byte ignored;

            beforeDot = Methods.ReadLong(leadingChar, reader, out ignored);
            var c = reader.Peek();
            if (c == '.')
            {
                reader.Read();
                c = reader.Read();
                if (c < '0' && c > '9') throw new DeserializationException("Expected digit", reader);

                afterDot = Methods.ReadLong((char)c, reader, out afterDotLen);

                c = reader.Peek();
            }
            else
            {
                afterDot = afterDotLen = 0;
            }

            if (c == 'e' || c == 'E')
            {
                reader.Read();
                c = reader.Read();
                if (c == '+')
                {
                    reader.Read();
                    c = reader.Read();
                }
                if (c != '-' && !(c >= '0' || c <= '9')) throw new DeserializationException("Expected -, +, or digit", reader);
                afterEbeforeDot = Methods.ReadLong((char)c, reader, out ignored);

                c = reader.Peek();
                if (c == '.')
                {
                    reader.Read();
                    c = reader.Read();
                    if (c < '0' && c > '9') throw new DeserializationException("Expected digit", reader);

                    afterEafterDot = Methods.ReadLong((char)c, reader, out afterEafterDotLen);
                }
                else
                {
                    afterEafterDot = afterEafterDotLen = 0;
                }
            }
            else
            {
                afterEafterDot = afterEbeforeDot = afterEafterDotLen = 0;
            }

            builder.PutFastNumber(beforeDot, afterDot, afterDotLen, afterEbeforeDot, afterEafterDot, afterEafterDotLen);
        }
    }
}
