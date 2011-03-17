﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Data;
using System.Reflection;
using System.Dynamic;

using ObjectServer.Backend;
using ObjectServer.Utility;
using ObjectServer.SqlTree;

namespace ObjectServer.Model
{
    public abstract partial class AbstractTableModel : AbstractModel
    {
        public override Dictionary<string, object>[] ReadInternal(
                 IResourceScope scope, IEnumerable<long> ids, IEnumerable<string> requiredFields = null)
        {
            if (scope == null)
            {
                throw new ArgumentNullException("ctx");
            }

            if (!this.CanRead)
            {
                throw new NotSupportedException();
            }

            if (ids == null || ids.Count() == 0)
            {
                return new Dictionary<string, object>[] { };
            }

            IList<string> allFields;
            if (requiredFields == null || requiredFields.Count() == 0)
            {
                allFields = this.Fields.Where(p => !p.Value.Lazy)
                    .Select(p => p.Value.Name).ToList();
            }
            else
            {
                //检查是否有不存在的列
                var userFields = requiredFields.Select(o => (string)o);
                allFields = userFields.ToList();
            }

            if (!allFields.Contains("id"))
            {
                allFields.Add("id");
            }

            //表里的列，也就是可以直接用 SQL 查的列
            var columnFields = from f in allFields
                               where this.Fields[f].IsColumn()
                               select f;

            columnFields = columnFields.Union(this.Inheritances.Select(i => i.RelatedField));

            var sql = string.Format("SELECT {0} FROM \"{1}\" WHERE \"id\" IN ({2})",
                ToColumnList(columnFields),
                this.TableName,
                ids.ToCommaList());

            //先查找表里的简单字段数据
            var records = scope.DatabaseProfile.DataContext.QueryAsDictionary(sql);

            this.ReadBaseModels(scope, allFields, records);

            this.PostProcessFieldValues(scope, allFields, records);

            return records.ToArray();
        }

        private void PostProcessFieldValues(
            IResourceScope scope, IList<string> allFields, List<Dictionary<string, object>> records)
        {
            Debug.Assert(scope != null);
            Debug.Assert(allFields != null);
            Debug.Assert(records != null);

            foreach (var fieldName in allFields)
            {
                var f = this.Fields[fieldName];
                if (f.Name == "id")
                {
                    continue;
                }

                var fieldValues = f.GetFieldValues(scope, records);
                foreach (var record in records)
                {
                    var id = (long)record["id"];
                    record[f.Name] = fieldValues[id];
                }
            }
        }

        private void ReadBaseModels(
            IResourceScope scope, IList<string> allFields, List<Dictionary<string, object>> records)
        {
            Debug.Assert(scope != null);
            Debug.Assert(allFields != null);
            Debug.Assert(records != null);

            //本尊及各个关联到基类模型的字段已经读出来了，现在读各个基类模型
            foreach (var bm in this.Inheritances)
            {
                var baseModel = (IMetaModel)scope.DatabaseProfile.GetResource(bm.BaseModel);
                var baseFieldsToRead = allFields.Intersect(baseModel.Fields.Keys);
                var baseIds = records.Select(r => (long)r[bm.RelatedField]);
                var baseRecords = baseModel.ReadInternal(scope, baseIds, baseFieldsToRead);
                //合并到结果中
                for (int i = 0; i < baseRecords.Length; i++)
                {
                    foreach (var baseField in baseRecords[i])
                    {
                        if (!records[i].ContainsKey(baseField.Key))
                        {
                            records[i].Add(baseField.Key, baseField.Value);
                        }
                    }
                }
            }
        }


    } //class
}
