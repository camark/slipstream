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
    public class ReferenceFieldTests : LocalTestCase
    {
        [Test]
        public void Test_reference_field()
        {
            this.ClearTestModelTable();
            this.ClearMasterAndChildTable();

            //创建 Master 与 Child 测试数据
            dynamic masterRecord1 = new ExpandoObject();
            masterRecord1.name = "master1";
            var masterId1 = this.Service.CreateModel(this.SessionId, "test.master", masterRecord1);

            dynamic childRecord1 = new ExpandoObject();
            childRecord1.name = "child1";
            var childId1 = this.Service.CreateModel(this.SessionId, "test.child", childRecord1);

            //创建测试数据 TestModel
            dynamic testRecord1 = new ExpandoObject();
            testRecord1.name = "test1";
            testRecord1.address = "address1";
            testRecord1.reference_field = new object[] { "test.master", masterId1 };
            var testId1 = this.Service.CreateModel(this.SessionId, "test.test_model", testRecord1);

            dynamic testRecord2 = new ExpandoObject();
            testRecord2.name = "test2";
            testRecord2.address = "address2";
            testRecord2.reference_field = new object[] { "test.child", childId1 };
            var testId2 = this.Service.CreateModel(this.SessionId, "test.test_model", testRecord2);

            var testIds = new object[] { testId1, testId2 };
            var fields = new object[] { "reference_field" };
            var testRecords = this.Service.ReadModel(this.SessionId, "test.test_model", testIds, fields);

            Assert.AreEqual(2, testRecords.Length);
            Assert.IsInstanceOf(typeof(object[]), testRecords[0]["reference_field"]);
            Assert.IsInstanceOf(typeof(object[]), testRecords[1]["reference_field"]);

            var referenceField1 = (object[])testRecords[0]["reference_field"];
            var referenceField2 = (object[])testRecords[1]["reference_field"];

            Assert.AreEqual(3, referenceField1.Length); //必须是三元组
            Assert.AreEqual(3, referenceField2.Length); //必须是三元组

            Assert.AreEqual("test.master", referenceField1[0]); //三元组第一个元素是 model 名称
            Assert.AreEqual(masterId1, referenceField1[1]); //第二个元素是关联的 id
            Assert.AreEqual("master1", referenceField1[2]); //第三个元素是关联的 record 的 name 字段值
            
            Assert.AreEqual("test.child", referenceField2[0]); //三元组第一个元素是 model 名称
            Assert.AreEqual(childId1, referenceField2[1]); //第二个元素是关联的 id
            Assert.AreEqual("child1", referenceField2[2]); //第三个元素是关联的 record 的 name 字段值

            //测试浏览 Reference 字段
            var testModel = (IModel) this.ServiceScope.GetResource("test.test_model");
            dynamic test1 = testModel.Browse(this.ServiceScope, testId1);
            Assert.AreEqual("master1", test1.reference_field.name);
        }
    }
}
