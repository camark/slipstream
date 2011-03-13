﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ObjectServer.Model
{
    internal sealed class EnumerationMetaField : AbstractMetaField
    {
        private Dictionary<string, string> options = new Dictionary<string, string>();

        public const int DefaultSize = 16;

        public EnumerationMetaField(IMetaModel model, string name, IDictionary<string, string> options)
            : base(model, name, FieldType.Enumeration)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            if (options == null || options.Count() <= 0)
            {
                throw new ArgumentNullException("options");
            }

            foreach (var p in options)
            {
                this.options.Add(p.Key, p.Value);
            }

            var maxLength = options.Max(p => p.Key.Length);
            this.Size = Math.Max(maxLength, DefaultSize);
        }


        protected override Dictionary<long, object> OnGetFieldValues(
            IResourceScope session, ICollection<Dictionary<string, object>> records)
        {
            return records.ExtractFieldValues(this.Name);
        }

        protected override Dictionary<long, object> OnSetFieldValues(
            IResourceScope scope, ICollection<Dictionary<string, object>> records)
        {
            //TODO 检查是否在范围内

            return records.ExtractFieldValues(this.Name);
        }

        public override object BrowseField(IResourceScope scope, IDictionary<string, object> record)
        {
            return record[this.Name];
        }

        public override bool IsColumn()
        {
            return !this.IsFunctional;
        }

        public override bool IsScalar
        {
            get { return true; }
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
                return this.options;
            }
        }
    }
}
