﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Transactions;
using System.Data;

using log4net;

using ObjectServer.Backend;
using ObjectServer.Model;
using ObjectServer.Utility;

namespace ObjectServer
{

    public sealed class ObjectPool
    {
        private static readonly ILog Log = LogManager.GetLogger(
            MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<string, IServiceObject> objects =
            new Dictionary<string, IServiceObject>();

        public ObjectPool(IDatabaseContext db, string dbName)
        {
            this.Database = dbName;
            this.LoadAllObjects(db);

            this.InitializeAllObjects(db);
        }

        private void LoadAllObjects(IDatabaseContext db)
        {
            this.RegisterAllCoreObjects();

            ObjectServerStarter.ModulePool.LoadActivedModules(db, this);
        }

        private void InitializeAllObjects(IDatabaseContext db)
        {
            //一次性初始化所有对象
            //obj.Initialize(db, pool);
            //TODO: 初始化非 IModel 对象
            var models =
                (from o in this.objects.Values
                 where o is IModel
                 select o as IModel).ToArray();

            var sortedModels =
                DependencySorter
                .Sort(models, m => m.Name, m => m.ReferencedObjects);

            foreach (var m in sortedModels)
            {
                m.Initialize(db, this);
            }
        }

        public string Database { get; private set; }

        public void AddModelsWithinAssembly(Assembly assembly)
        {
            if (Log.IsInfoEnabled)
            {
                Log.InfoFormat("Start to register all models for assembly [{0}]...", assembly.FullName);
            }

            var types = GetModelsFromAssembly(assembly);

            using (var db = Backend.DataProvider.OpenDatabase(this.Database))
            {
                db.Open();
                foreach (var t in types)
                {
                    var obj = ObjectPool.CreateStaticObjectInstance(t);
                    this.objects[obj.Name] = obj;
                }
            }

            if (Log.IsInfoEnabled)
            {
                Log.InfoFormat("Done");
            }
        }

        public void AddServiceObject(IServiceObject obj)
        {
            //TODO 处理对象已经存在的问题，继承等

            this.objects.Add(obj.Name, obj);
        }

        public IServiceObject this[string objName]
        {
            get
            {
                IServiceObject so;
                if (!this.objects.TryGetValue(objName, out so))
                {
                    var msg = string.Format("Cannot found service object: {0} ", objName);
                    throw new ServiceObjectNotFoundException(msg, objName);
                }
                return this.objects[objName];
            }
        }

        /// <summary>
        /// 检查是否包含对象 objName
        /// </summary>
        /// <param name="objName"></param>
        /// <returns></returns>
        public bool Contains(string objName)
        {
            return this.objects.ContainsKey(objName);
        }


        private void RegisterAllCoreObjects()
        {
            var a = typeof(ObjectPool).Assembly;
            this.AddModelsWithinAssembly(a);
        }


        private static Type[] GetModelsFromAssembly(Assembly assembly)
        {
            var types = assembly.GetTypes();
            var result = new List<Type>();
            foreach (var t in types)
            {
                var assemblies = t.GetCustomAttributes(typeof(ServiceObjectAttribute), false);
                if (assemblies.Length > 0)
                {
                    result.Add(t);
                }
            }
            return result.ToArray();
        }

        #region ServiceObject(s) factory methods

        public static IServiceObject CreateStaticObjectInstance(Type t)
        {
            var obj = Activator.CreateInstance(t) as IServiceObject;
            if (obj == null)
            {
                var msg = string.Format("类型 '{0}' 没有实现 IServiceObject 接口", t.FullName);
                throw new InvalidCastException(msg);
            }
            return obj;
        }

        public static T CreateStaticObjectInstance<T>()
            where T : class, IServiceObject
        {
            return CreateStaticObjectInstance(typeof(T)) as T;
        }

        //以后要支持 DLR，增加  CreateDynamicObjectInstance

        #endregion

    }
}
