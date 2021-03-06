﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Diagnostics;

using NUnit.Framework;

using SlipStream.Model;

namespace SlipStream.Model.Fields
{
    [TestFixture]
    public class ManyToManyFieldTests : ServiceContextTestCaseBase
    {
        private void ClearManyToManyModels()
        {
            Debug.Assert(this.Context != null);
            this.ClearModel("test.department_employee");
            this.ClearModel("test.department");
            this.ClearModel("test.employee");
        }

        [Test]
        public void CanBrowseManyToManyField()
        {
            this.ClearManyToManyModels();
            dynamic ids = this.GenerateTestData();

            dynamic employeeModel = this.GetResource("test.employee");
            dynamic e1 = employeeModel.Browse(ids.eid1);

            //TODO: 这里要排序再比较
            Assert.AreEqual(3, e1.departments.Length);
            Assert.AreEqual(e1.departments[0]._id, ids.did2);
            Assert.AreEqual(e1.departments[1]._id, ids.did3);
            Assert.AreEqual(e1.departments[2]._id, ids.did4);

            dynamic e2 = employeeModel.Browse(ids.eid2);
            Assert.AreEqual(2, e2.departments.Length);
            Assert.AreEqual(e2.departments[0]._id, ids.did3);
            Assert.AreEqual(e2.departments[1]._id, ids.did4);
        }

        [Test]
        public void Test_CRUD_m2m_field()
        {
            this.ClearManyToManyModels();
            dynamic employeeModel = this.GetResource("test.employee");

            dynamic data = this.GenerateTestData();
            var ids = new object[] { data.eid1, data.eid2 };

            var employees = employeeModel.Read(ids, new object[] { "name", "departments" });

            Assert.AreEqual(2, employees.Length);
            var employee1 = employees[0];
            var employee2 = employees[1];

            Assert.IsInstanceOf<long[]>(employee1["departments"]);
            var departments1 = (long[])employee1["departments"];
            Assert.AreEqual(3, departments1.Length);

            var originDeptIds = new long[] { data.did2, data.did3, data.did4 };
            Array.Sort(originDeptIds);
            Array.Sort(departments1);
            Assert.AreEqual(originDeptIds[0], departments1[0]);
            Assert.AreEqual(originDeptIds[1], departments1[1]);
            Assert.AreEqual(originDeptIds[2], departments1[2]);
        }

        [Test]
        public void CanCreateManyToManyField()
        {
            this.ClearManyToManyModels();
            dynamic employeeModel = this.GetResource("test.employee");

            dynamic data = this.GenerateTestData();

            dynamic e = new ExpandoObject();
            e.name = "test-employee";
            e.departments = new long[] { data.did1, data.did2, data.did3 };
            var eid = employeeModel.Create(e);

            var fields = new string[] { "name", "departments" };
            var record = employeeModel.Read(new object[] { eid }, fields)[0];

            var departments = (long[])record["departments"];

            Assert.AreEqual(3, departments.Length);

            var originDeptIds = new long[] { data.did1, data.did2, data.did3 };
            Array.Sort(originDeptIds);
            Array.Sort(departments);
            Assert.AreEqual(originDeptIds[0], departments[0]);
            Assert.AreEqual(originDeptIds[1], departments[1]);
            Assert.AreEqual(originDeptIds[2], departments[2]);
        }

        [Test]
        public void CanWriteManyToManyField()
        {
            this.ClearManyToManyModels();
            dynamic employeeModel = this.GetResource("test.employee");
            dynamic data = this.GenerateTestData();

            dynamic r1 = employeeModel.Read(new long[] { data.eid1 }, null)[0];
            dynamic e1 = new ExpandoObject();
            e1.departments = new long[] { data.did1, data.did2 };
            e1._version = r1["_version"];
            employeeModel.Write(data.eid1, e1);

            var fields = new string[] { "name", "departments" };
            var record = employeeModel.Read(new object[] { data.eid1 }, fields)[0];

            var departments = (long[])record["departments"];
            Assert.AreEqual(2, departments.Length);
            var originDeptIds = new long[] { data.did1, data.did2 };
            Array.Sort(originDeptIds);
            Array.Sort(departments);
            Assert.AreEqual(originDeptIds[0], departments[0]);
            Assert.AreEqual(originDeptIds[1], departments[1]);
        }

        private dynamic GenerateTestData()
        {
            dynamic ids = new ExpandoObject();
            dynamic e = new ExpandoObject();
            dynamic employeeModel = this.GetResource("test.employee");
            dynamic depModel = this.GetResource("test.department");
            dynamic depEmployeeModel = this.GetResource("test.department_employee");

            e.name = "employee";
            ids.eid1 = employeeModel.Create(e);
            ids.eid2 = employeeModel.Create(e);
            ids.eid3 = employeeModel.Create(e);
            ids.eid4 = employeeModel.Create(e);
            ids.eid5 = employeeModel.Create(e);

            dynamic dept = new ExpandoObject();
            dept.name = "department";
            ids.did1 = depModel.Create(dept);
            ids.did2 = depModel.Create(dept);
            ids.did3 = depModel.Create(dept);
            ids.did4 = depModel.Create(dept);
            ids.did5 = depModel.Create(dept);

            //设置e1 对应 did2, did3, did4
            depEmployeeModel.Create(
                new Dictionary<string, object>() { { "eid", ids.eid1 }, { "did", ids.did2 }, });
            depEmployeeModel.Create(
                new Dictionary<string, object>() { { "eid", ids.eid1 }, { "did", ids.did3 }, });
            depEmployeeModel.Create(
                new Dictionary<string, object>() { { "eid", ids.eid1 }, { "did", ids.did4 }, });

            //设置 e2  对应 did3 did4
            depEmployeeModel.Create(
                new Dictionary<string, object>() { { "eid", ids.eid2 }, { "did", ids.did3 }, });
            depEmployeeModel.Create(
                new Dictionary<string, object>() { { "eid", ids.eid2 }, { "did", ids.did4 }, });

            return ids;
        }

    }
}
