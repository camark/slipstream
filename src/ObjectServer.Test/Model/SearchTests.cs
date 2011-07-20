﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;

using NUnit.Framework;

using ObjectServer.Model;

namespace ObjectServer.Model.Test
{
    [TestFixture]
    public class SearchTests : LocalTestCase
    {
        [Test]
        public void Test_empty_domain()
        {
            var domain = new object[][] { };
            var ids = new long[] { };
            Assert.DoesNotThrow(() =>
                {
                    ids = this.Service.SearchModel(this.SessionId, "core.model", domain);
                });
            Assert.That(ids.Length > 1);
        }

        [Test]
        public void Test_search_limit()
        {
            var domain = new object[][] { new object[] { "name", "like", "%" } };
            var ids = this.Service.SearchModel(this.SessionId, "core.model",
                domain, null, 0, 2);
            Assert.AreEqual(2, ids.Length);

            ids = this.Service.SearchModel(this.SessionId, "core.model",
                domain, null, 0, 3);
            Assert.AreEqual(3, ids.Length);
        }

        [Test]
        public void Test_search_offset()
        {
            var domain = new object[][] { new object[] { "name", "like", "%" } };
            var ids1 = this.Service.SearchModel(this.SessionId, "core.model",
                domain, null, 0, 2);
            var ids2 = this.Service.SearchModel(this.SessionId, "core.model",
                domain, null, 1, 2);
            Assert.AreNotEqual(ids1[0], ids2[0]);
            Assert.AreEqual(ids1[1], ids2[0]);

        }

        [Test]
        public void Test_domain_equal_operator()
        {
            var domain = new object[][] { 
                new object[] {  "name", "=", "core.model" } 
            };

            var ids = this.Service.SearchModel(this.SessionId, "core.model", domain);
            Assert.AreEqual(1, ids.Length);

            domain = new object[][] {
                new object[] { "name", "=", "a dummy model" }
            };
            ids = this.Service.SearchModel(this.SessionId, "core.model", domain);
            Assert.AreEqual(0, ids.Length);

            //测试 many-to-one 字段的 = 操作符            
        }


        [Test]
        public void Test_domain_like_operator()
        {
            var domain = new object[][] { new object[] { "name", "like", "core.modu%" } };
            var ids = this.Service.SearchModel(this.SessionId, "core.model", domain);
            Assert.AreEqual(1, ids.Length);

            domain = new object[][] { new object[] { "name", "like", "%like dummy%" } };
            ids = this.Service.SearchModel(this.SessionId, "core.model", domain);
            Assert.AreEqual(0, ids.Length);

            domain = new object[][] { new object[] { "name", "like", "core.modul_" } };
            ids = this.Service.SearchModel(this.SessionId, "core.model", domain);
            Assert.AreEqual(1, ids.Length);
        }

        [Test]
        public void Test_domain_in_operator()
        {
            var domain = new object[][] { 
                new object[] { 
                    "name", "in", 
                    new object[] { "core.model", "core.field", "core.module" } 
                } 
            };
            var ids = this.Service.SearchModel(this.SessionId, "core.model", domain, null, 0, 0);
            Assert.AreEqual(3, ids.Length);
        }

        [Test]
        public void Test_domain_notin_operator()
        {
            var allIds = this.Service.SearchModel(this.SessionId, "core.model", null, null, 0, 0);

            var notinDomain = new object[][] { 
                new object[] { 
                    "name", "!in", 
                    new object[] { "core.model", "core.field" } 
                } 
            };
            var ids = this.Service.SearchModel(this.SessionId, "core.model", notinDomain, null, 0, 0);

            Assert.AreEqual(allIds.Length, ids.Length + 2);
        }

        [Test]
        public void Test_simple_searching_order()
        {
            var ascOrder = new object[][] { new object[] { "_id", "asc" } };
            var descOrder = new object[][] { new object[] { "_id", "desc" } };

            var ascIds = this.Service.SearchModel(this.SessionId, "core.model", null, ascOrder, 0, 0);
            var descIds = this.Service.SearchModel(this.SessionId, "core.model", null, descOrder, 0, 0);

            Assert.AreEqual(ascIds.Length, descIds.Length);
            Assert.AreNotEqual(ascIds[0], descIds[0]);
            Assert.AreEqual(ascIds.Last(), descIds.First());
        }


        [Test]
        public void Test_many_to_one_field_domain()
        {
            ClearMasterAndChildTable();

            var masterModel = this.ServiceScope.GetResource("test.master");
            var childModel = this.ServiceScope.GetResource("test.child");

            dynamic master1 = new ExpandoObject();
            master1.name = "master1";
            var master1Id = this.Service.CreateModel(this.SessionId, "test.master", master1);

            dynamic master2 = new ExpandoObject();
            master2.name = "master2";
            var master2Id = this.Service.CreateModel(this.SessionId, "test.master", master2);

            dynamic child1 = new ExpandoObject();
            child1.master = master1Id;
            child1.name = "child1";
            var child1Id = this.Service.CreateModel(this.SessionId, "test.child", child1);

            dynamic child2 = new ExpandoObject();
            child2.master = master2Id;
            child2.name = "child2";
            var child2Id = this.Service.CreateModel(this.SessionId, "test.child", child2);

            var domain = new object[][] 
            { 
                new object[] { "master.name", "=", "master1" }
            };
            var childIds = this.Service.SearchModel(this.SessionId, "test.child", domain);
            Assert.AreEqual(1, childIds.Length);
            Assert.AreEqual(child1Id, childIds[0]);

            domain = new object[][]
            {
                new object[] { "master.name", "like", "master%" }
            };
            childIds = this.Service.SearchModel(this.SessionId, "test.child", domain);
            Assert.AreEqual(2, childIds.Length);
            Assert.AreEqual(child1Id, childIds[0]);
            Assert.AreEqual(child2Id, childIds[1]);

        }
    }
}
