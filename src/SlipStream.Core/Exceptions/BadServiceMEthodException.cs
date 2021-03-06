﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SlipStream.Exceptions
{
    [Serializable]
    public sealed class BadServiceMethodException : ResourceException
    {
        public BadServiceMethodException(string msg, string objName, string methodName) :
            base(msg)
        {
            this.ObjectName = objName;
            this.MethodName = methodName;
        }

        public string ObjectName { get; private set; }
        public string MethodName { get; private set; }
    }
}
