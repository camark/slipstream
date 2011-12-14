﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

using NHibernate.SqlCommand;

using ObjectServer.Utility;
using ObjectServer.Data;

namespace ObjectServer
{
    [Serializable]
    public sealed class UserSession
    {
        private static readonly SqlString SelectByIdSql =
            new SqlString(@"select * from ""core_session"" where ""sid"" = ", Parameter.Placeholder);
        private static readonly SqlString SelectByUserIdSql =
            new SqlString(@"select * from ""core_session"" where ""userid"" = ", Parameter.Placeholder);
        private static readonly SqlString UpdateLastActivityTimeSql = SqlString.Parse(
            @"update ""core_session"" set ""last_activity_time"" = ? where ""last_activity_time"" < ? and ""sid"" = ?");

        public const int IdLength = 16;
        public const string SystemUserName = "system";
        public const long SystemUserId = 0;

        public UserSession(IDictionary<string, object> record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            this.Id = (string)record["sid"];
            this.Login = (string)record["login"];
            this.UserId = (long)record["userid"];
            this.LastActivityTime = (DateTime)record["last_activity_time"];
            this.StartTime = (DateTime)record["start_time"];
        }


        public UserSession(string login, long userId)
            : this()
        {

            if (string.IsNullOrEmpty(login))
            {
                throw new ArgumentNullException("login");
            }

            if (userId <= 0)
            {
                throw new ArgumentOutOfRangeException("userId");
            }

            this.Login = login;
            this.UserId = userId;
        }

        private UserSession()
        {
            this.UserId = 0;
            this.Id = GenerateSessionId();
            this.StartTime = DateTime.Now;
            this.LastActivityTime = this.StartTime;
        }

        public static UserSession CreateSystemUserSession()
        {
            var s = new UserSession();
            s.Login = SystemUserName;
            s.UserId = SystemUserId;
            return s;
        }

        private static string GenerateSessionId()
        {
            var bytes = new byte[IdLength];
            using (var rng = RNGCryptoServiceProvider.Create())
            {
                rng.GetBytes(bytes);
            }
            var hash = bytes.ToSha();
            return Convert.ToBase64String(hash);
        }

        public string Id { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime LastActivityTime { get; set; }
        public string Login { get; set; }
        public long UserId { get; set; }

        public DateTime Deadline
        {
            get
            {
                if (!SlipstreamEnvironment.Initialized)
                {
                    throw new InvalidOperationException("Framework uninitialized");
                }
                var timeout = new TimeSpan(0, SlipstreamEnvironment.Configuration.SessionTimeoutMinutes, 0);
                return this.LastActivityTime + timeout;
            }
        }

        public bool IsActive
        {
            get
            {
                return DateTime.Now <= this.Deadline;
            }
        }

        public bool IsSystemUser
        {
            get
            {
                return this.UserId <= 0;
            }
        }
    }
}
