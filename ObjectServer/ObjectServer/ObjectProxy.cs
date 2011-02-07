﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Transactions;
using Npgsql;

using ObjectServer.Backend;

namespace ObjectServer
{
    public class ObjectProxy : MarshalByRefObject
    {

        public object Execute(string dbName, string objectName, string name, object[] args)
        {
            using (var session = new Session(dbName))
            {
                var obj = session.Pool.LookupObject(objectName);
                var method = obj.GetServiceMethod(name);
                var internalArgs = new object[args.Length + 1];
                internalArgs[0] = session;
                args.CopyTo(internalArgs, 1);

                var tx = session.Connection.BeginTransaction();
                try
                {
                    var result = method.Invoke(obj, internalArgs);
                    tx.Commit();
                    return result;
                }
                catch (Exception ex)
                {
                    tx.Rollback();
                    throw ex;
                }
            }
        }

    }
}
