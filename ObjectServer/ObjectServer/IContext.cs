﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;

using ObjectServer.Backend;

namespace ObjectServer
{
    public interface IContext : IDisposable
    {
        IDatabase Database { get; }
    }
}