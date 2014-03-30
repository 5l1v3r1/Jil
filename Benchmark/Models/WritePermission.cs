﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark.Models
{
    [ProtoContract]
    class WritePermission : IGenericEquality<WritePermission>
    {
        [ProtoMember(1)]
        public int? user_id { get; set; }
        [ProtoMember(2)]
        public string object_type { get; set; }
        [ProtoMember(3)]
        public bool? can_add { get; set; }
        [ProtoMember(4)]
        public bool? can_edit { get; set; }
        [ProtoMember(5)]
        public bool? can_delete { get; set; }
        [ProtoMember(6)]
        public int? max_daily_actions { get; set; }
        [ProtoMember(7)]
        public int? min_seconds_between_actions { get; set; }

        public bool Equals(WritePermission obj)
        {
            return
                this.can_add.TrueEquals(obj.can_add) &&
                this.can_delete.TrueEquals(obj.can_delete) &&
                this.can_edit.TrueEquals(obj.can_edit) &&
                this.max_daily_actions.TrueEquals(obj.max_daily_actions) &&
                this.min_seconds_between_actions.TrueEquals(obj.min_seconds_between_actions) &&
                this.object_type.TrueEqualsString(obj.object_type) &&
                this.user_id.TrueEquals(obj.user_id);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.can_add.TrueEquals((bool?)obj.can_add) &&
                this.can_delete.TrueEquals((bool?)obj.can_delete) &&
                this.can_edit.TrueEquals((bool?)obj.can_edit) &&
                this.max_daily_actions.TrueEquals((int?)obj.max_daily_actions) &&
                this.min_seconds_between_actions.TrueEquals((int?)obj.min_seconds_between_actions) &&
                this.object_type.TrueEqualsString((string)obj.object_type) &&
                this.user_id.TrueEquals((int?)obj.user_id);
        }
    }
}
