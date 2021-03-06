﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

using SlipStream.Model;

namespace SlipStream.Test
{

    [Resource]
    public sealed class EmployeeModel : AbstractSqlModel
    {
        public EmployeeModel()
            : base("test.employee")
        {
            Fields.Chars("name").SetLabel("Name").Required().SetSize(64);
            Fields.Integer("age").SetLabel("Age").NotRequired();
            Fields.ManyToMany("departments", "test.department_employee", "eid", "did")
                .SetLabel("Departments");
        }
    }

    [Resource]
    public sealed class DepartmentModel : AbstractSqlModel
    {
        public DepartmentModel()
            : base("test.department")
        {
            Fields.Chars("name").SetLabel("Name").Required().SetSize(64);
            Fields.ManyToMany("employees", "test.department_employee", "did", "eid")
                .SetLabel("Employees");
        }
    }

    [Resource]
    public sealed class DepartmentEmployeeModel : AbstractSqlModel
    {

        public DepartmentEmployeeModel()
            : base("test.department_employee")
        {
            this.TableName = "test_department_employee_rel";

            Fields.ManyToOne("did", "test.department").SetLabel("Department").Required();
            Fields.ManyToOne("eid", "test.employee").SetLabel("Employee").Required();
        }
    }


}
