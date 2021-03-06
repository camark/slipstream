﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using System.Reflection;
using System.Diagnostics;

using Sandwych.Utility;
using Sandwych;
using SlipStream.Data;
using SlipStream.Core;

namespace SlipStream
{
    using IRecord = IDictionary<string, object>;
    using Record = Dictionary<string, object>;

    internal sealed class SlipstreamService : ISlipstreamService
    {
        private readonly IDataProvider _dataProvider;
        private readonly IDbDomainManager _dbDomainManager;

        public SlipstreamService(IDataProvider dataProvider, IDbDomainManager dbDomainManager)
        {
            this._dataProvider = dataProvider;
            this._dbDomainManager = dbDomainManager;
        }

        public string LogOn(string dbName, string username, string password)
        {
            if (string.IsNullOrEmpty(dbName))
            {
                throw new ArgumentNullException("_dbName");
            }

            if (string.IsNullOrEmpty(username))
            {
                throw new ArgumentNullException("username");
            }

            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentNullException("password");
            }

            var dbDomain = _dbDomainManager.GetDbDomain(dbName);
            using (var ctx = dbDomain.OpenSystemSession())
            {
                dynamic userModel = dbDomain.GetResource(UserModel.ModelName);
                UserSession session = userModel.LogOn(dbName, username, password);
                Debug.Assert(session != null);
                return session.Token.ToString();
            }
        }


        public void LogOff(string db, string sessionToken)
        {
            if (string.IsNullOrEmpty(db))
            {
                throw new ArgumentNullException("db");
            }

            if (string.IsNullOrEmpty(sessionToken))
            {
                throw new ArgumentNullException("sessionToken");
            }

            var dbDomain = _dbDomainManager.GetDbDomain(db);
            using (var ctx = dbDomain.OpenSession(sessionToken))
            {
                dynamic userModel = dbDomain.GetResource(UserModel.ModelName);
                userModel.LogOff(sessionToken);
            }
        }

        public string GetVersion()
        {
            return StaticSettings.Version.ToString();
        }

        public object Execute(string db, string sessionToken, string resource, string method, params object[] args)
        {
            if (string.IsNullOrEmpty(db))
            {
                throw new ArgumentNullException("session");
            }

            if (string.IsNullOrEmpty(sessionToken))
            {
                throw new ArgumentNullException("userSessionId");
            }

            if (string.IsNullOrEmpty(resource))
            {
                throw new ArgumentNullException("resource");
            }

            //加入事务
            //REVIEW
            var dbDomain = _dbDomainManager.GetDbDomain(db);
            using (var ctx = dbDomain.OpenSession(sessionToken))
            {
                dynamic res = dbDomain.GetResource(resource);
                var svc = res.GetService(method);
                var result = svc.Invoke(res, args);
                return result;
            }
        }

        #region Database handling methods

        public string[] ListDatabases()
        {
            return this._dataProvider.ListDatabases();
        }

        public void CreateDatabase(string rootPasswordHash, string dbName, string adminPassword)
        {
            VerifyRootPassword(rootPasswordHash);

            this._dataProvider.CreateDatabase(dbName);
            SlipstreamEnvironment.DbDomains.Register(dbName, true);
        }

        public void DeleteDatabase(string rootPasswordHash, string dbName)
        {
            VerifyRootPassword(rootPasswordHash);

            this._dataProvider.DeleteDatabase(dbName); //删除实际数据库
            SlipstreamEnvironment.DbDomains.Remove(dbName); //删除数据库上下文
        }

        private static void VerifyRootPassword(string rootPasswordHash)
        {
            var cfgRootPasswordHash = SlipstreamEnvironment.Settings.ServerPassword.ToSha();
            if (rootPasswordHash != cfgRootPasswordHash)
            {
                throw new SlipStream.Exceptions.SecurityException("Invalid password of root user");
            }
        }

        #endregion


        #region Model methods

        public long CreateModel(string db, string sessionToken, string modelName, IRecord record)
        {
            return (long)Execute(db, sessionToken, modelName, "Create", new object[] { record });
        }

        public long CountModel(string db, string sessionToken, string modelName, object[] constraints)
        {
            return (long)Execute(db, sessionToken, modelName, "Count", new object[] { constraints });
        }

        public long[] SearchModel(
            string db, string sessionToken, string modelName, object[] constraints, object[] order, long offset, long limit)
        {
            return (long[])Execute(db, sessionToken, modelName, "Search", new object[] { constraints, order, offset, limit });
        }

        public Record[] ReadModel(string db, string sessionToken, string modelName, object[] ids, object[] fields)
        {
            return (Record[])Execute(db, sessionToken, modelName, "Read", new object[] { ids, fields });
        }

        public void WriteModel(
            string db, string sessionToken, string modelName, object id, IRecord record)
        {
            Execute(db, sessionToken, modelName, "Write", new object[] { id, record });
        }

        public void DeleteModel(string db, string sessionToken, string modelName, object[] ids)
        {
            Execute(db, sessionToken, modelName, "Delete", new object[] { ids });
        }

        #endregion

    }
}
