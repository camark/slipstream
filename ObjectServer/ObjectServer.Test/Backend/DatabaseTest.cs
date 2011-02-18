﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;

using ObjectServer.Backend;

namespace ObjectServer.Test.Backend
{
    public class DatabaseTest
    {

        [Fact]
        void TestQueryAsDictionary()
        {
            using (var db = new DatabaseBase("objectserver"))
            {
                db.Open();

                var dict = db.QueryAsDictionary("select id, name from core_model");
                Assert.NotNull(dict);
                Assert.True(dict.Count > 0);
            }
        }
    }
}