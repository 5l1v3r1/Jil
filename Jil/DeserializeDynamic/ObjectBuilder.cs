﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jil.DeserializeDynamic
{
    sealed class ObjectBuilder
    {
        public StringBuilder CommonStringBuffer;

        char[] _CommonCharBuffer;
        public char[] CommonCharBuffer
        {
            get
            {
                if (_CommonCharBuffer != null) return _CommonCharBuffer;

                _CommonCharBuffer = new char[Methods.CharBufferSize];
                return _CommonCharBuffer;
            }
        }

        public JsonObject BeingBuilt;

        public void PutNull()
        {
            if (BeingBuilt != null)
            {
                BeingBuilt.Put(null);
            }
        }

        public void PutTrue()
        {
            if (BeingBuilt == null)
            {
                BeingBuilt = JsonObject.True;
            }
            else
            {
                BeingBuilt.Put(JsonObject.True);
            }
        }

        public void PutFalse()
        {
            if (BeingBuilt == null)
            {
                BeingBuilt = JsonObject.False;
            }
            else
            {
                BeingBuilt.Put(JsonObject.False);
            }
        }

        public void PutString(string str)
        {
            if (BeingBuilt == null)
            {
                BeingBuilt = JsonObject.ForString(str);
            }
            else
            {
                BeingBuilt.Put(JsonObject.ForString(str));
            }
        }

        public void StartArray()
        {
            var arr = JsonObject.NewArray(BeingBuilt);

            if (BeingBuilt == null)
            {
                BeingBuilt = arr;
            }
            else
            {
                BeingBuilt.Put(arr);
                BeingBuilt = arr;
            }
        }

        public void EndArray()
        {
            BeingBuilt = BeingBuilt.Pop();
        }

        public void StartObject()
        {
            var obj = JsonObject.NewObject(BeingBuilt);

            if (BeingBuilt == null)
            {
                BeingBuilt = obj;
            }
            else
            {
                BeingBuilt.Put(obj);
                BeingBuilt = obj;
            }
        }

        public void EndObject()
        {
            BeingBuilt = BeingBuilt.Pop();
        }

        public void StartObjectMember()
        {
            var member = JsonObject.NewObjectMember(BeingBuilt);

            //BeingBuilt.Put(member);
            BeingBuilt = member;
        }

        public void EndObjectMember()
        {
            BeingBuilt = BeingBuilt.Pop();
        }

        public void PutNumber(double number)
        {
            var num = JsonObject.ForNumber(number);
            if (BeingBuilt == null)
            {
                BeingBuilt = num;
            }
            else
            {
                BeingBuilt.Put(num);
            }
        }
    }
}
