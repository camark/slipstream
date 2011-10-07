﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ObjectServer.Model;

namespace ObjectServer.DemoModule
{
    [Resource]
    public sealed class EmployeeModel : AbstractSqlModel
    {
        public EmployeeModel()
            : base("demo.employee")
        {
            Fields.Chars("name").SetLabel("姓名").Required();
            Fields.Chars("address").SetLabel("地址");
            Fields.Float("salary").SetLabel("月薪");
            Fields.Date("birthdate").SetLabel("出生日期");
        }
    }
}
