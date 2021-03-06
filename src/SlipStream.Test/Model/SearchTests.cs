﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;

using NUnit.Framework;

using SlipStream.Model;

namespace SlipStream.Model.Test
{
    [TestFixture]
    public class SearchTests : ServiceContextTestCaseBase
    {
        [Test]
        public void CanSearchWithEmptyConstraint()
        {
            var modelModel = this.GetResource("core.model");
            var constraint = new object[][] { };
            dynamic ids = null;
            Assert.DoesNotThrow(() =>
            {
                ids = modelModel.Search(constraint, null, 0, 0);
            });
            Assert.That(ids != null);
            Assert.That(ids.Length > 1);
        }

        [Test]
        public void CanSearchWithLimit()
        {
            var modelModel = this.GetResource("core.model");

            var constraint = new object[][] { new object[] { "name", "like", "%" } };
            var ids = modelModel.Search(constraint, null, 0, 2);
            Assert.AreEqual(2, ids.Length);

            ids = modelModel.Search(constraint, null, 0, 3);
            Assert.AreEqual(3, ids.Length);
        }

        [Test]
        public void CanSearchWithOffset()
        {
            var modelModel = this.GetResource("core.model");
            var constraint = new object[][] { new object[] { "name", "like", "%" } };
            var ids1 = modelModel.Search(constraint, null, 0, 2);
            var ids2 = modelModel.Search(constraint, null, 1, 2);
            Assert.AreNotEqual(ids1[0], ids2[0]);
            Assert.AreEqual(ids1[1], ids2[0]);

        }

        [Test]
        public void CanUseEqualOperator()
        {
            var modelModel = this.GetResource("core.model");
            var constraint = new object[][] { 
                new object[] {  "name", "=", "core.model" } 
            };

            var ids = modelModel.Search(constraint, null, 0, 0);
            Assert.AreEqual(1, ids.Length);

            constraint = new object[][] {
                new object[] { "name", "=", "a dummy model" }
            };
            ids = modelModel.Search(constraint, null, 0, 0);
            Assert.AreEqual(0, ids.Length);

            //测试 many-to-one 字段的 = 操作符            
        }


        [Test]
        public void CanUseLikeOperator()
        {
            var modelModel = this.GetResource("core.model");

            var constraint = new object[][] { new object[] { "name", "like", "core.modu%" } };
            var ids = modelModel.Search(constraint, null, 0, 0);
            Assert.AreEqual(2, ids.Length);

            constraint = new object[][] { new object[] { "name", "like", "%like dummy%" } };
            ids = modelModel.Search(constraint, null, 0, 0);
            Assert.AreEqual(0, ids.Length);

            constraint = new object[][] { new object[] { "name", "like", "core.modul_" } };
            ids = modelModel.Search(constraint, null, 0, 0);
            Assert.AreEqual(1, ids.Length);
        }

        [Test]
        public void CanUseNotLikeOperator()
        {
            var modelModel = this.GetResource("core.model");

            var constraint = new object[][] { new object[] { "name", "!like", "%.%" } };
            var ids = modelModel.Search(constraint, null, 0, 0);
            Assert.AreEqual(0, ids.Length);
        }

        [Test]
        public void CanUseInOperator()
        {
            var constraint = new object[][] { 
                new object[] { 
                    "name", "in", 
                    new object[] { "core.model", "core.field", "core.module" } 
                } 
            };
            var modelModel = this.GetResource("core.model");
            dynamic ids = modelModel.Search(constraint, null, 0, 0);
            Assert.AreEqual(3, ids.Length);
        }

        [Test]
        public void CanUseNotInOperator()
        {
            dynamic modelModel = this.GetResource("core.model");
            dynamic allIds = modelModel.Search(null, null, 0, 0);

            var notinDomain = new object[][] { 
                new object[] { 
                    "name", "!in", 
                    new object[] { "core.model", "core.field" } 
                } 
            };
            dynamic ids = modelModel.Search(notinDomain, null, 0, 0);
            Assert.AreEqual(allIds.Length, ids.Length + 2);
        }

        [Test]
        public void CanSearchWithOrder()
        {
            var ascOrder = new object[][] { new object[] { "_id", "asc" } };
            var descOrder = new object[][] { new object[] { "_id", "desc" } };
            var modelModel = this.GetResource("core.model");

            var ascIds = (long[])modelModel.Search(null, ascOrder, 0, 0);
            var descIds = (long[])modelModel.Search(null, descOrder, 0, 0);

            Assert.AreEqual(ascIds.Length, descIds.Length);
            Assert.AreNotEqual(ascIds[0], descIds[0]);
            Assert.AreEqual(ascIds.Last(), descIds.First());
        }

        [Test]
        public void CanDoSimpleCount()
        {
            dynamic modelModel = this.GetResource("core.model");
            var ids = modelModel.Search(null, null, 0, 0);
            var count = modelModel.Count(null);
            Assert.AreEqual(ids.Length, count);

            var constraint = new object[][] {
                new object[] { "name", "like", "core.%" },
            };
            ids = modelModel.Search(constraint, null, 0, 0);
            count = modelModel.Count(constraint);
            Assert.AreEqual(ids.Length, count);
        }


        [Test]
        public void CanSearchWithManyToOneFieldConstraints()
        {
            ClearMasterAndChildTable();

            dynamic masterModel = this.GetResource("test.master");
            dynamic childModel = this.GetResource("test.child");

            dynamic master1 = new ExpandoObject();
            master1.name = "master1";
            var master1Id = masterModel.Create(master1);

            dynamic master2 = new ExpandoObject();
            master2.name = "master2";
            var master2Id = masterModel.Create(master2);

            dynamic child1 = new ExpandoObject();
            child1.master = master1Id;
            child1.name = "child1";
            var child1Id = childModel.Create(child1);

            dynamic child2 = new ExpandoObject();
            child2.master = master2Id;
            child2.name = "child2";
            var child2Id = childModel.Create(child2);

            var constraint = new object[][] 
            { 
                new object[] { "master.name", "=", "master1" }
            };
            var childIds = childModel.Search(constraint, null, 0, 0);
            Assert.AreEqual(1, childIds.Length);
            Assert.AreEqual(child1Id, childIds[0]);

            constraint = new object[][]
            {
                new object[] { "master.name", "like", "master%" }
            };
            childIds = childModel.Search(constraint, null, 0, 0);
            Assert.AreEqual(2, childIds.Length);
            Assert.AreEqual(child1Id, childIds[0]);
            Assert.AreEqual(child2Id, childIds[1]);
        }
    }
}
