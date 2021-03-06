﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;

using NUnit.Framework;

using SlipStream.Model;

namespace SlipStream.Model.Test
{

    [TestFixture]
    public class TableModelTests : ServiceContextTestCaseBase
    {
        [Test]
        public void CheckCrud()
        {
            var modelName = "test.test_model";
            var dbName = this.SessionToken;

            dynamic testModel = this.GetResource(modelName);

            //Delete all records first
            var allIds = testModel.Search(null, null, 0, 0);
            if (allIds.Length > 0)
            {
                testModel.Delete(allIds);
            }

            var values = new Dictionary<string, object>
            {
                { "name", "sweet_name" },
                { "address", "my address" },
                { "field1", 123 },
                { "field2", 100 },
            };
            dynamic id = testModel.Create(values);
            Assert.True(id > 0);

            var domain1 = new object[][] { new object[] { "name", "=", "sweet_name" } };
            dynamic foundIds = testModel.Search(domain1, null, 0, 100);
            Assert.AreEqual(1, foundIds.Length);
            Assert.AreEqual(id, foundIds[0]);

            var newValues = new Dictionary<string, object> {
                { "name", "changed_name" },
            };
            testModel.Write(id, newValues);

            var ids = new object[] { id };
            dynamic data = testModel.Read(ids, null);
            Assert.AreEqual(1, data.Length);
            Assert.AreEqual("changed_name", data[0]["name"]);
            Assert.AreEqual(223, data[0]["field3"]); //检测函数字段的计算是否正确

            testModel.Delete(ids);

            foundIds = testModel.Search(domain1, null, 0, 100);
            Assert.AreEqual(0, foundIds.Length);
        }

        [Test]
        public void CanCreateAndReadRelatedFields()
        {
            dynamic masterModel = this.GetResource("test.master");
            dynamic childModel = this.GetResource("test.child");

            this.ClearMasterAndChildTable();

            var masterPropBag = new Dictionary<string, object>()
            {
                { "name", "master-obj" },
            };
            var masterId = masterModel.Create(masterPropBag);

            var childPropBag = new Dictionary<string, object>()
            {
                { "name", "child-obj" },
                { "master", masterId },
            };

            var childId = (long)childModel.Create(childPropBag);

            var ids = new object[] { childId };
            dynamic rows = childModel.Read(ids, null);
            var masterField = rows[0]["master"];
            Assert.AreEqual(typeof(object[]), masterField.GetType());
            var one2ManyField = (object[])masterField;
            Assert.AreEqual(one2ManyField[0], masterId);
            Assert.AreEqual(one2ManyField[1], "master-obj");

            var masterFieldNames = new string[] { "name", "children" };
            var masterRows = masterModel.Read(new object[] { masterId }, masterFieldNames);
            var master = masterRows[0];
            var children = (long[])master["children"];

            Assert.AreEqual(1, children.Length);
            Assert.AreEqual(childId, children[0]);

            //更新
            var masterId2 = (long)masterModel.Create(masterPropBag);
            childPropBag["master"] = masterId2;
            childPropBag[AbstractModel.VersionFieldName] = AbstractModel.FirstVersion;
            childModel.Write(childId, childPropBag);

            dynamic children2 = childModel.Read(new object[] { childId }, new object[] { "master" });
            var masterField3 = (object[])children2[0]["master"];
            Assert.AreEqual(masterId2, masterField3[0]);
        }


        [Test]
        public void ReadNullableOneToManyField()
        {
            var masterFields = new object[] { "name", "children" };
            var master = new Dictionary<string, object>();
            dynamic masterModel = this.GetResource("test.master");

            var id = masterModel.Create(master);

            dynamic masterRecords = masterModel.Read(new object[] { id }, masterFields);
            var record = masterRecords[0];

            Assert.IsInstanceOf<long[]>(record["children"]);
            Assert.AreEqual(0, ((long[])record["children"]).Length);
            Assert.True(record["name"] == DBNull.Value);
        }

    }
}
