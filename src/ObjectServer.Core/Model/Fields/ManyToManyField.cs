﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using NHibernate.SqlCommand;

using ObjectServer.Data;
using ObjectServer.Utility;

namespace ObjectServer.Model
{
    internal sealed class ManyToManyField : AbstractField
    {
        public ManyToManyField(IModel model, string name,
            string refModel, string originField, string targetField)
            : base(model, name, FieldType.ManyToMany)
        {

            this.Relation = refModel;
            this.OriginField = originField;
            this.RelatedField = targetField;
            this.Lazy = true;
        }

        protected override Dictionary<long, object> OnGetFieldValues(
           ITransactionContext ctx, ICollection<Dictionary<string, object>> records)
        {
            if (ctx == null)
            {
                throw new ArgumentNullException("ctx");
            }

            if (records == null)
            {
                throw new ArgumentNullException("records");
            }

            //中间表模型
            var relationModel = (IModel)ctx.GetResource(this.Relation);
            var result = new Dictionary<long, object>();
            foreach (var rec in records)
            {
                var selfId = (long)rec[AbstractModel.IdFieldName];
                //中间表没有记录，返回空
                var sql = new SqlString(
                    "select ",
                    DataProvider.Dialect.QuoteForColumnName(this.RelatedField),
                    " from ",
                    DataProvider.Dialect.QuoteForTableName(relationModel.TableName),
                    " where ",
                    DataProvider.Dialect.QuoteForColumnName(this.OriginField),
                    "=", Parameter.Placeholder);
                var targetIds = ctx.DBContext.QueryAsArray<object>(sql, selfId);
                result[selfId] = targetIds.Select(o => (long)o).ToArray();
            }

            return result;
        }

        protected override object OnSetFieldValue(ITransactionContext scope, object value)
        {
            if (!(value is long[]))
            {
                throw new ArgumentException("'value' must be long[]", "value");
            }

            return value;
        }

        public override object BrowseField(ITransactionContext scope, IDictionary<string, object> record)
        {
            if (scope == null)
            {
                throw new ArgumentNullException("scope");
            }

            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            long[] targetIds = null;
            if (record.ContainsKey(this.Name))
            {
                var targetFields = (object[][])record[this.Name];
            }
            else //Lazy 的字段，我们重新读取
            {
                var id = (long)record[AbstractModel.IdFieldName];
                var fields = new string[] { this.Name };
                var newRecord = ((Dictionary<string, object>[])this.Model.ReadInternal(scope, new long[] { id }, fields))[0];
                targetIds = (long[])newRecord[this.Name];
            }

            var relationModel = (IModel)scope.GetResource(this.Relation);
            var targetModelName = relationModel.Fields[this.RelatedField].Relation;
            var targetModel = (IModel)scope.GetResource(targetModelName);
            var targetRecords = targetModel.ReadInternal(scope, targetIds, null);
            return targetRecords.Select(tr => new BrowsableRecord(scope, targetModel, tr)).ToArray();
        }

        public override bool IsColumn { get { return false; } }

        public override bool IsReadonly
        {
            get { return false; }
            set { throw new NotSupportedException(); }
        }

        public override bool IsScalar
        {
            get { return false; }
        }

        public override int Size
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

        public override ObjectServer.Model.OnDeleteAction OnDeleteAction
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public override IDictionary<string, string> Options
        {
            get
            {
                throw new NotSupportedException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }

    }
}
