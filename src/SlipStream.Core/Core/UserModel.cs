﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;

using Sandwych.Utility;
using Sandwych;
using NHibernate.SqlCommand;

using SlipStream.Model;
using SlipStream.Data;
using SlipStream.Exceptions;

namespace SlipStream.Core
{

    [Resource]
    public sealed class UserModel : AbstractSqlModel
    {
        public const string ModelName = "core.user";
        public const string PasswordMask = "************";
        public const string RootUserName = "root";
        public const int SaltLength = 16;

        public UserModel()
            : base(ModelName)
        {
            Fields.Chars("login").SetLabel("User Name").SetSize(64).Required().Unique();
            Fields.Chars("password").SetLabel("Password").SetSize(64).Required();
            Fields.Chars("salt").SetLabel("Salt").SetSize(64).Required();
            Fields.Boolean("admin").SetLabel("Administrator?").Required().SetDefaultValueGetter(r => false);
            Fields.Chars("name").SetLabel("Name").Required().SetSize(64);
            Fields.ManyToMany("roles", "core.user_role", "user", "role").SetLabel("Roles");
            Fields.ManyToOne("organization", "core.organization").SetLabel("Organization").NotRequired();
            Fields.ManyToOne("home_action", "core.action").SetLabel("Home Action").NotRequired();
            Fields.Chars("email").SetLabel("Email").NotRequired().SetSize(256);
            Fields.Boolean("active").SetLabel("Active?").Required().SetDefaultValueGetter(r => true);
        }

        public override void Initialize(bool update)
        {
            base.Initialize(update);

            //检测是否有 root 用户
            var isRootUserExisted = UserExists(this.DbDomain.CurrentSession.DataContext, RootUserName);
            if (update && isRootUserExisted)
            {
                this.DbDomain.CurrentSession.BizLogger.Info("Creating the [root] user...");
                this.CreateRootUser(this.DbDomain.CurrentSession.DataContext);
            }
        }

        private static bool UserExists(IDataContext dbctx, string login)
        {
            var sql =
                @"select count(*) from ""core_user"" where ""login"" = ?";

            var rowCount = dbctx.QueryValue(sql, login);
            var isRootUserExisted = rowCount.IsNull() || Convert.ToInt32(rowCount) <= 0;
            return isRootUserExisted;
        }

        private void CreateRootUser(IDataContext dbctx)
        {
            Debug.Assert(dbctx != null);


            var sql =
                @"insert into ""core_user""( 
                    ""_version"", ""name"", ""login"", ""password"", ""admin"", 
                    ""active"", ""_created_time"", ""salt"")
                 values(?,?,?,?,?,?,?,?)";

            //创建 root 用户
            var rootPassword = SlipstreamEnvironment.Settings.ServerPassword;
            var user = new Dictionary<string, object>()
            {
                { "name", "Root User" },
                { "login", RootUserName },
                { "password", rootPassword } ,
                { "admin", true },
                { "active", true },
                { CreatedUserFieldName, DBNull.Value }, //一定要覆盖掉默认设置，因为此时系统里还没有用户，取 Session 里的 UserId 是无意义的
                { CreatedTimeFieldName, DateTime.Now },
                { VersionFieldName, 1 },
            };
            var row = HashPassword(user);

            dbctx.Execute(
                sql, row[VersionFieldName], row["name"], row["login"], row["password"],
                row["admin"], row["active"], row["_created_time"], row["salt"]);
        }


        private static IDictionary<string, object> HashPassword(IDictionary<string, object> record)
        {
            IDictionary<string, object> values2 = record;

            if (record.ContainsKey("password"))
            {
                values2 = new Dictionary<string, object>(record);
                var salt = GenerateSalt();
                values2["salt"] = salt;
                var password = (string)values2["password"];
                values2["password"] = (password + salt).ToSha(); //数据库里要保存 hash 而不是明文
            }
            return values2;
        }


        private static string GenerateSalt()
        {
            var bytes = new byte[SaltLength];
            using (var rng = RNGCryptoServiceProvider.Create())
            {
                rng.GetBytes(bytes);
            }
            return bytes.ToHex();
        }


        private static bool IsPasswordMatched(string hashedPassword, string salt, string password)
        {
            Debug.Assert(!string.IsNullOrEmpty(hashedPassword));
            Debug.Assert(!string.IsNullOrEmpty(salt));
            Debug.Assert(!string.IsNullOrEmpty(password));

            var newHash = (password + salt).ToSha();
            return hashedPassword == newHash;
        }


        public override long CreateInternal(IDictionary<string, object> values)
        {
            if (values == null || values.Count == 0)
            {
                throw new ArgumentNullException("values");
            }

            IDictionary<string, object> values2 = HashPassword(values);
            return base.CreateInternal(values2);
        }


        public override void WriteInternal(long id, IDictionary<string, object> record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException("id");
            }

            //更新用户记录业务是不能修改密码与 Salt 的
            var values2 = new Dictionary<string, object>(record);
            if (record.ContainsKey("password"))
            {
                values2.Remove("password");
            }
            if (record.ContainsKey("salt"))
            {
                values2.Remove("salt");
            }

            base.WriteInternal(id, values2);

            //TODO 通知 Session 缓存
        }

        public override Dictionary<string, object>[] ReadInternal(long[] ids, string[] fields)
        {
            var records = base.ReadInternal(ids, fields);

            //"salt" "password" 是敏感字段，不要让客户端获取
            foreach (var record in records)
            {
                if (record.ContainsKey("salt"))
                {
                    record["salt"] = null;
                }

                if (record.ContainsKey("password"))
                {
                    record["password"] = PasswordMask;
                }
            }


            return records;
        }


        public UserSession LogOn(string database, string login, string password)
        {
            var constraint = new object[][] { new object[] { "login", "=", login } };

            var userIds = base.SearchInternal(constraint, null, 0, 0);
            if (userIds.Length != 1)
            {
                throw new UserDoesNotExistException("Cannot found user: " + login, login);
            }

            var user = base.ReadInternal(
                new long[] { userIds[0] },
                new string[] { "password", "salt" })[0];

            var hashedPassword = (string)user["password"];
            var salt = (string)user["salt"];

            var ctx = this.DbDomain.CurrentSession;
            if (IsPasswordMatched(hashedPassword, salt, password))
            {
                var session = this.FetchOrCreateSession(ctx.UserSessionService, database, login, user);

                LoggerProvider.EnvironmentLogger.Info(() =>
                    String.Format("User[{0}.{1}] logged.", ctx.DataContext.DatabaseName, login));
                return session;
            }
            else
            {
                LoggerProvider.EnvironmentLogger.Warn(() =>
                    String.Format("Failed to log on user: [{0}.{1}]", ctx.DataContext.DatabaseName, login));
                throw new Exceptions.SecurityException("Failed to log on");
            }
        }


        public void LogOff(string sessionToken)
        {
            var ctx = this.DbDomain.CurrentSession;
            var session = ctx.UserSessionService.GetByToken(sessionToken);

            if (session != null)
            {
                ctx.UserSessionService.Remove(sessionToken);

                LoggerProvider.EnvironmentLogger.Info(() =>
                    String.Format("User[{0}.{1}] logged out.", ctx.DataContext.DatabaseName, session.Login));
            }
            else
            {
                LoggerProvider.EnvironmentLogger.Warn(() =>
                    String.Format("One connection try to log off a unexisted session: {0}", sessionToken));
            }
        }

        [ServiceMethod("ChangePassword")]
        public static void ChangePassword(
            IModel model, IServiceContext ctx, string newPassword)
        {
            if (model == null)
            {
                throw new ArgumentNullException("model");
            }

            if (ctx == null)
            {
                throw new ArgumentNullException("session");
            }

            if (String.IsNullOrEmpty(newPassword))
            {
                throw new ArgumentNullException("newPassword");
            }

            var record = new Dictionary<string, object>()
            {
                { "password", newPassword },
            };
            HashPassword(record);
            model.WriteInternal(ctx.UserSession.UserId, record);
        }

        public static Dictionary<string, object>[] GetAllModelAccessEntries(long userId)
        {
            throw new NotImplementedException();
        }


        private UserSession FetchOrCreateSession(
            IUserSessionStore sessionService, string dbName, string login, IDictionary<string, object> userFields)
        {
            Debug.Assert(sessionService != null);
            Debug.Assert(userFields.ContainsKey("password"));

            var uid = (long)userFields[IdFieldName];

            var oldSession = sessionService.GetByUserId(uid);

            if (oldSession == null)
            {
                var newSession = new UserSession(login, uid);
                sessionService.Put(newSession);
                return newSession;
            }
            else if (!oldSession.IsActive)
            {
                sessionService.Remove(oldSession.Token);
                var newSession = new UserSession(login, uid);
                sessionService.Put(newSession);
                return newSession;
            }
            else
            {
                sessionService.Pulse(oldSession.Token);
                return oldSession;
            }
        }

        /// <summary>
        /// 凡是 init.sql 里建立的表都不应该返回引用对象
        /// </summary>
        /// <returns></returns>
        public override string[] GetReferencedObjects()
        {
            return new string[] { };
        }
    }





}
