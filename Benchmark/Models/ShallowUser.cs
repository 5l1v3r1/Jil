﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Benchmark.Models
{
    public enum UserType : byte
    {
        unregistered = 2,
        registered = 3,
        moderator = 4,
        does_not_exist = 255
    }
    
    [ProtoContract]
    public class ShallowUser : IGenericEquality<ShallowUser>
    {
        [ProtoMember(1)]
        public int? user_id { get; set; }
        [ProtoMember(2)]
        public string display_name { get; set; }
        [ProtoMember(3)]
        public int? reputation { get; set; }
        [ProtoMember(4)]
        public UserType? user_type { get; set; }
        [ProtoMember(5)]
        public string profile_image { get; set; }
        [ProtoMember(6)]
        public string link { get; set; }
        [ProtoMember(7)]
        public int? accept_rate { get; set; }
        [ProtoMember(8)]
        public User.BadgeCount badge_counts { get; set; }

        public bool Equals(ShallowUser obj)
        {
            return
                this.accept_rate.TrueEquals(obj.accept_rate) &&
                this.badge_counts.TrueEquals(obj.badge_counts) &&
                this.display_name.TrueEqualsString(obj.display_name) &&
                this.link.TrueEqualsString(obj.link) &&
                this.profile_image.TrueEqualsString(obj.profile_image) &&
                this.reputation.TrueEquals(obj.reputation) &&
                this.user_id.TrueEquals(obj.user_id) &&
                this.user_type.TrueEquals(obj.user_type);
        }

        public bool EqualsDynamic(dynamic obj)
        {
            return
                this.accept_rate.TrueEquals((int?)obj.accept_rate) &&
                (this.badge_counts == null && obj.badge_counts == null || this.badge_counts.EqualsDynamic(obj.badge_counts)) &&
                this.display_name.TrueEqualsString((string)obj.display_name) &&
                this.link.TrueEqualsString((string)obj.link) &&
                this.profile_image.TrueEqualsString((string)obj.profile_image) &&
                this.reputation.TrueEquals((int?)obj.reputation) &&
                this.user_id.TrueEquals((int?)obj.user_id) &&
                this.user_type.TrueEquals((UserType?)obj.user_type);
        }
    }
}
