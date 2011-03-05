﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Reflection;
using System.Diagnostics;

using ObjectServer.Backend;

namespace ObjectServer
{
    /// <summary>
    /// 但凡是需要 RPC 的方法都需要用此 scope 包裹
    /// </summary>
    public sealed class ContextScope : IContext
    {
        private bool ownDb;
        /// <summary>
        /// 安全的创建 Context，会检查 session 等
        /// </summary>
        /// <param name="sessionId"></param>
        public ContextScope(Guid sessionId)
        {
            var sessStore = ObjectServerStarter.SessionStore;
            var session = sessStore.GetSession(sessionId);
            if (session == null || !session.IsActive)
            {
                throw new UnauthorizedAccessException("Not logged!");
            }

            Logger.Info(() =>
                string.Format("ContextScope is opening for sessionId: [{0}]", sessionId));

            this.Session = session;
            this.SessionStore.Pulse(session.Id);

            this.ownDb = true;
            this.Database = ObjectServerStarter.Databases.GetDatabase(session);
            this.Database.DataContext.Open();
        }

        /// <summary>
        /// 直接建立  context，忽略 session 、登录等
        /// </summary>
        /// <param name="dbName"></param>
        internal ContextScope(string dbName)
        {
            Logger.Info(() =>
                string.Format("ContextScope is opening for database: [{0}]", dbName));

            this.Session = new Session(dbName, "system", 0);
            this.SessionStore.PutSession(this.Session);
            this.ownDb = true;
            this.Database = ObjectServerStarter.Databases.GetDatabase(this.Session);
            this.Database.DataContext.Open();
        }

        internal ContextScope(IDatabase db)
        {
            Debug.Assert(db != null);
            Debug.Assert(db.DataContext != null);

            Logger.Info(() =>
                string.Format("ContextScope is opening for DatabaseContext"));

            this.Session = new Session(db.DataContext.DatabaseName, "system", 0);
            this.SessionStore.PutSession(this.Session);
            this.ownDb = false;
            this.Database = db;
            this.Database.DataContext.Open();
        }

        #region IContext 成员

        public IDatabase Database
        {
            get;
            private set;
        }

        public Session Session { get; private set; }

        #endregion

        #region IDisposable 成员

        public void Dispose()
        {
            if (this.ownDb)
            {
                this.Database.DataContext.Close();
            }

            Logger.Info(() => "ScopeContext closed");
        }

        #endregion

        #region IEquatable<IContext> 成员

        public bool Equals(IContext other)
        {
            return this.Session.Id == other.Session.Id;
        }

        #endregion

        public override int GetHashCode()
        {
            return this.Session.Id.GetHashCode();
        }

        private SessionStore SessionStore
        {
            get { return ObjectServerStarter.SessionStore; }
        }
    }
}
