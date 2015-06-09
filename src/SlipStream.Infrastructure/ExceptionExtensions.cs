﻿/* 此文件来自 Orchard 项目
 * Orchard Project http://www.orchardproject.net
 * 
 * 
 * */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Security;
using System.Runtime.InteropServices;

namespace SlipStream {

    public static class ExceptionExtensions {

        public static bool IsFatal(this Exception ex) {
            return ex is StackOverflowException ||
                ex is OutOfMemoryException ||
                ex is AccessViolationException ||
                ex is AppDomainUnloadedException ||
                ex is ThreadAbortException ||
                ex is SecurityException ||
                ex is SEHException;
        }

    }
}