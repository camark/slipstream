﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Dynamic;

using Autofac;
using NUnit.Framework;

namespace SlipStream {

    public abstract class ServiceContextTestCaseBase : ServiceTestCaseBase {
        private IDbDomain _dbDomain;

        private void ClearTestUsers() {
            dynamic userModel = this.GetResource("core.user");

            var constraints = new object[] { new object[] { "login", "=", "test" } };
            var ids = userModel.Search(this.Context, constraints, null, 0, 0);
            if (ids.Length > 0) {
                userModel.Delete(this.Context, ids);
            }
        }

        public ISlipstreamService Service { get; private set; }

        public IServiceContext Context { get; private set; }

        protected dynamic GetResource(string resName) {
            return this._dbDomain.GetResource(resName);
        }

        public IDbDomain DbDomain { get { return this._dbDomain; } }

        [SetUp]
        public void BeforeTest() {
            Debug.Assert(this.Context == null);
            Debug.Assert(!string.IsNullOrEmpty(this.SessionToken));

            this._dbDomain = SlipstreamEnvironment.DbDomains.GetDbDomain(TestingDatabaseName);
            this.Context = _dbDomain.OpenSession(this.SessionToken);
        }

        [TearDown]
        public void AfterTest() {
            Debug.Assert(this.Context != null);
            this.Context.Dispose();
            this.Context = null;
        }

        protected void ClearTestModelTable() {
            Debug.Assert(this.Context != null);
            this.ClearModel("test.test_model");
        }


        protected void ClearMasterAndChildTable() {
            Debug.Assert(this.Context != null);
            this.ClearModel("test.child");
            this.ClearModel("test.master");
        }

        protected void ClearModel(string modelName) {
            dynamic model = this.GetResource(modelName);
            var ids = model.Search(null, null, 0, 0);
            if (ids.Length > 0) {
                model.Delete(ids);
            }
        }

    }
}
