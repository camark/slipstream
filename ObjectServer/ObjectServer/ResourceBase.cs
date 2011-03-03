﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Diagnostics;

using ObjectServer.Backend;

namespace ObjectServer
{
    /// <summary>
    /// 适用于静态语言（非DLR）的服务对象基类
    /// </summary>
    public abstract class ResourceBase : IResource
    {
        private readonly IDictionary<string, MethodInfo> serviceMethods =
            new SortedList<string, MethodInfo>();

        protected ResourceBase(string name)
        {

            this.SetName(name);

            this.RegisterAllServiceMethods();
        }

        private void SetName(string name)
        {
            if (!NamingRule.IsValidServiceName(name))
            {
                var msg = string.Format("Invalid service object name: '{0}'", name);
                Logger.Error(() => msg);
                throw new BadServiceObjectNameException(msg, name);
            }

            this.Name = name;

            if (string.IsNullOrEmpty(this.Label))
            {
                this.Label = name;
            }

            this.Module = name.Split('.')[0];
        }

        public MethodInfo GetServiceMethod(string name)
        {
            return this.serviceMethods[name];
        }

        /// <summary>
        /// 为动态语言预留的
        /// </summary>
        protected void RegisterServiceMethod(MethodInfo mi)
        {
            this.VerifyMethod(mi);

            this.serviceMethods.Add(mi.Name, mi);
        }

        private void VerifyMethod(MethodInfo mi)
        {
            var parameters = mi.GetParameters();
            if (parameters.Length < 1
                || parameters[0].ParameterType != typeof(IContext)
                || !mi.IsPublic)
            {
                var msg = string.Format(
                    "The method '{1}' of object {0} must have an IContext parameter at first.",
                    this.Name, mi.Name);
                Logger.Error(() => msg);
                throw new BadServiceMethodException(msg, this.Name, mi.Name);
            }
        }

        private void RegisterAllServiceMethods()
        {
            var t = this.GetType();
            var methods = t.GetMethods();
            foreach (var m in methods)
            {
                var attrs = m.GetCustomAttributes(typeof(ServiceMethodAttribute), false);
                if (attrs.Length > 0)
                {
                    this.RegisterServiceMethod(m);
                }
            }
        }


        public virtual void Initialize(IDatabase db)
        {
            this.Pool = db.Resources;
        }

        public string Name { get; private set; }

        public string Label { get; protected set; }

        public string Module { get; private set; }

        public abstract bool DatabaseRequired { get; }

        public abstract string[] GetReferencedObjects();

        public IResourceContainer Pool { get; private set; }


        #region ServiceObject(s) factory methods

        internal static IResource CreateStaticObjectInstance(Type t)
        {
            var obj = Activator.CreateInstance(t) as IResource;
            if (obj == null)
            {
                var msg = string.Format("类型 '{0}' 没有实现 IServiceObject 接口", t.FullName);
                throw new InvalidCastException(msg);
            }
            return obj;
        }

        internal static T CreateStaticObjectInstance<T>()
            where T : class, IResource
        {
            return CreateStaticObjectInstance(typeof(T)) as T;
        }

        //以后要支持 DLR，增加  CreateDynamicObjectInstance

        #endregion
    }
}