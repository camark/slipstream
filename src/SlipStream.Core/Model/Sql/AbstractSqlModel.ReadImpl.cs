﻿using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Data;
using System.Reflection;
using System.Dynamic;

using NHibernate.SqlCommand;

using SlipStream.Data;

namespace SlipStream.Model
{
    public abstract partial class AbstractSqlModel : AbstractModel
    {
        public override Dictionary<string, object>[] ReadInternal(long[] ids, string[] requiredFields)
        {
            if (ids == null)
            {
                throw new ArgumentNullException("ids");
            }

            if (ids.Length == 0)
            {
                return new Dictionary<string, object>[] { };
            }

            var scope = this.DbDomain.CurrentSession;

            //检查列是否有重复的

            if (ids == null || ids.Count() == 0)
            {
                return new Dictionary<string, object>[] { };
            }

            IList<string> allFields;
            if (requiredFields == null || requiredFields.Count() == 0)
            {
                allFields = this.Fields.Select(p => p.Value.Name).ToList();
            }
            else
            {
                //TODO 检查是否有不存在的列

                var userFields = requiredFields.Select(o => (string)o).ToArray();
                //检查重复的列
                var distinctedFields = userFields.Distinct();

                if (userFields.Length != distinctedFields.Count())
                {
                    throw new ArgumentException("包含重复的字段名", "requiredFields");
                }

                allFields = userFields.ToList();
            }

            if (!allFields.Contains(IdFieldName))
            {
                allFields.Add(IdFieldName);
            }

            //表里的列，也就是可以直接用 SQL 查的列
            var columnFields = from f in allFields
                               let fieldInfo = this.Fields[f]
                               where fieldInfo.IsColumn  //TODO 这里可能要重新考虑下
                               select f;

            columnFields = columnFields.Union(this.Inheritances.Select(i => i.RelatedField));

            var selectStmt = new StringBuilder();
            selectStmt.Append("select ");

            bool commaNeeded = false;
            foreach (var col in columnFields)
            {
                if (commaNeeded)
                {
                    selectStmt.Append(",");
                }
                commaNeeded = true;

                selectStmt.Append('"' + col + '"');
            }

            selectStmt.Append(" from ");
            selectStmt.Append('"' + this.TableName + '"');
            selectStmt.Append(@" where ""_id"" in (");

            commaNeeded = false;
            foreach (var id in ids)
            {
                if (commaNeeded)
                {
                    selectStmt.Append(",");
                }
                commaNeeded = true;

                selectStmt.Append(id.ToString());
            }

            selectStmt.Append(")");

            var sql = selectStmt.ToString();

            //先查找表里的简单字段数据
            var records = scope.DataContext.QueryAsDictionary(sql);

            this.ReadBaseModels(allFields, records);

            this.PostProcessFieldValues(allFields, records);

            return records.ToArray();
        }

        private void PostProcessFieldValues(IList<string> allFields, IList<Dictionary<string, object>> records)
        {
            Debug.Assert(allFields != null);
            Debug.Assert(records != null);

            var ctx = this.DbDomain.CurrentSession;
            //TODO 处理字段读权限
            foreach (var fieldName in allFields)
            {
                var f = this.Fields[fieldName];

                if (this.Inheritances.Any(i => i.RelatedField == f.Name))
                {
                    continue;
                }

                var fieldValues = f.GetFieldValues(records);
                foreach (var record in records)
                {
                    var id = (long)record[IdFieldName];
                    record[f.Name] = fieldValues[id];
                }
            }
        }

        private void ReadBaseModels(IList<string> allFields, Dictionary<string, object>[] records)
        {
            Debug.Assert(allFields != null);
            Debug.Assert(records != null);

            //本尊及各个关联到基类模型的字段已经读出来了，现在读各个基类模型
            foreach (var bm in this.Inheritances)
            {
                var baseModel = (IModel)this.DbDomain.GetResource(bm.BaseModel);
                var baseFieldsToRead = allFields.Intersect(baseModel.Fields.Keys).ToArray();
                var baseIds = records.Select(r => (long)r[bm.RelatedField]).ToArray();
                var baseRecords = baseModel.ReadInternal(baseIds, baseFieldsToRead);
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
