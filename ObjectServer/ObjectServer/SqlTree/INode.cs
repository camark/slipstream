﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectServer.SqlTree
{
    public interface INode : ICloneable
    {
        void Traverse(IVisitor visitor);
    }
}
