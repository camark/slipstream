﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NUnit.Framework;

namespace ObjectServer.Model.Test
{
    [TestFixture]
    public class BrowseModelTests : LocalTestBase
    {

        [Test]
        public void TestBrowseManyToOneField()
        {
            var masterPropBag = new Dictionary<string, object>()
            {
                { "name", "master-obj" },
            };
            var masterId = this.Service.CreateModel(this.SessionId, "test.master", masterPropBag);
            var childPropBag = new Dictionary<string, object>()
            {
                { "name", "child-obj" },
                { "master", masterId },
            };
            var childId = (long)this.Service.CreateModel(this.SessionId, "test.child", childPropBag);

            using (var ctx = new ContextScope(new Guid(this.SessionId)))
            {
                var childModel = (IModel)ctx.Database.Resources.Resolve("test.child");
                var dynamicChild = childModel.Browse(ctx, childId);
                Assert.AreEqual("master-obj", dynamicChild.master.name);

                var masterModel = (IModel)ctx.Database.Resources.Resolve("test.master");
                var dynamicMaster = masterModel.Browse(ctx, masterId);
                Assert.AreEqual(1, dynamicMaster.children.Length);
                Assert.AreEqual("child-obj", dynamicMaster.children[0].name);
            }
        }
    }
}