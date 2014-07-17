﻿using System;
using Jil.Common;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using System.Runtime.CompilerServices;

namespace Jil.SerializeDynamic
{
    class DynamicSerializer
    {
        static void SerializeDynamicObject(IDynamicMetaObjectProvider dyn, TextWriter stream, Options opts)
        {
            stream.Write("{");

            var dynType = dyn.GetType();
            var param = Expression.Parameter(typeof(object));
            var metaObj = dyn.GetMetaObject(param);

            var first = true;

            foreach (var memberName in metaObj.GetDynamicMemberNames())
            {
                if (!first)
                {
                    stream.Write(",");
                }

                first = false;

                var binder = (GetMemberBinder)Binder.GetMember(0, memberName, dynType, new[] { CSharpArgumentInfo.Create(0, null) });
                var callSite = CallSite<Func<CallSite, object, object>>.Create(binder);

                var val = callSite.Target.Invoke(callSite, dyn);

                stream.Write("\"" + memberName.JsonEscape(jsonp: true) + "\":");
                Serialize(val, stream, opts);
            }

            stream.Write("}");
        }

        static void SerializePrimitive(Type type, object val, TextWriter stream, Options opts)
        {
            // TODO: some that doesn't suck
            var staticMtd = typeof(JSON).GetMethods().Single(m => m.Name == "Serialize" && m.GetParameters().Length == 3);
            var genericMtd = staticMtd.MakeGenericMethod(type);
            genericMtd.Invoke(null, new[] { val, stream, opts });
        }

        public static void Serialize(object obj, TextWriter stream, Options opts)
        {
            if (obj == null)
            {
                stream.Write("null");
                return;
            }

            var objType = obj.GetType();

            if(objType.IsPrimitiveType())
            {
                SerializePrimitive(objType, obj, stream, opts);
                return;
            }

            var dynObject = obj as IDynamicMetaObjectProvider;
            if (dynObject != null)
            {
                SerializeDynamicObject(dynObject, stream, opts);
                return;
            }

            throw new NotImplementedException();
        }
    }
}
