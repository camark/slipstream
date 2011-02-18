﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;

namespace ObjectServer.Backend
{
    public interface IDatabase : IDisposable
    {

        void Open();
        void Close();

        string[] List();

        DbConnection Connection { get; }

        bool Connected { get; }

        string DatabaseName { get; }

        object QueryValue(string commandText, params object[] args);

        int Execute(string commandText, params object[] args);

        DataTable QueryAsDataTable(string commandText, params object[] args);

        List<Dictionary<string, object>> QueryAsDictionary(string commandText, params object[] args);
    }
}