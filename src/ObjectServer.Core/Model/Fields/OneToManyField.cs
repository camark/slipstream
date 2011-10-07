﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectServer.Model
{
    internal sealed class OneToManyField : AbstractField
    {
        public OneToManyField(IModel model, string name, string childModel, string relatedField)
            : base(model, name, FieldType.OneToMany)
        {
            this.Relation = childModel;
            this.RelatedField = relatedField;
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

            //查询字表
            IModel childModel = (IModel)ctx.GetResource(this.Relation);

            var children = new Dictionary<long, long[]>();
            foreach (var master in records)
            {
                var masterID = (long)master[AbstractModel.IdFieldName];
                var constraint = new List<object[]>();
                constraint.Add(new object[] { this.RelatedField, "=", masterID });
                var childIDs = childModel.SearchInternal(ctx, constraint.ToArray(), null, 0, 0);
                children[masterID] = childIDs;
            }

            var result = new Dictionary<long, object>();
            foreach (var p in records)
            {
                var masterId = (long)p[AbstractModel.IdFieldName];
                result.Add(masterId, children[masterId]);
            }

            return result;
        }

        protected override object OnSetFieldValue(ITransactionContext scope, object value)
        {
            throw new NotSupportedException();
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

            //TODO 重构成跟Many-to-many 一样的
            var targetModelName = this.Relation;
            IModel targetModel = (IModel)scope.GetResource(targetModelName);
            var thisId = record[AbstractModel.IdFieldName];
            //TODO: 下面的条件可能还不够，差 active 等等
            var constraint = new object[][] { new object[] { this.RelatedField, "=", thisId } };
            var destIds = targetModel.SearchInternal(scope, constraint, null, 0, 0);
            var records = (Dictionary<string, object>[])targetModel.ReadInternal(scope, destIds, null);
            return records.Select(r => new BrowsableRecord(scope, targetModel, r)).ToArray();
        }

        public override bool IsColumn { get { return false; } }

        public override bool IsReadonly
        {
            get
            {
                return false;
            }
            set
            {
                throw new NotSupportedException();
            }
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

        public override bool IsScalar
        {
            get { return false; }
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
