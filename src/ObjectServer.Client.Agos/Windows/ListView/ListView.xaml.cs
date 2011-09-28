﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Diagnostics;
using System.Threading;

using ObjectServer.Client.Agos.Models;
using ObjectServer.Client.Agos;
using ObjectServer.Client.Agos.Windows.ListView.ValueConverters;

namespace ObjectServer.Client.Agos.Windows.ListView
{
    public partial class ListView : UserControl
    {
        private static Dictionary<string, Tuple<Type, IValueConverter>> COLUMN_TYPE_MAPPING
            = new Dictionary<string, Tuple<Type, IValueConverter>>()
        {
            {"ID", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), null) },
            {"Integer", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), null) },
            {"Float", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), null) },
            {"Decimal", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), null) },
            {"Chars", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), null) },
            {"Text", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), null) },
            {"Boolean", new Tuple<Type, IValueConverter>(typeof(DataGridCheckBoxColumn), null) },
            {"DateTime", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), null) },
            {"Date", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new DateFieldConverter()) },
            {"Time", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new TimeFieldConverter()) },
            {"ManyToOne", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new ManyToOneFieldConverter()) },
            {"Enumeration", new Tuple<Type, IValueConverter>(typeof(DataGridTextColumn), new EnumFieldConverter()) },
        };

        private IDictionary<string, object> viewRecord;
        private IDictionary<string, object> actionRecord;
        private readonly IList<string> fields = new List<string>();
        private string modelName;
        private IDictionary<string, IQueryField> createdQueryFields =
            new Dictionary<string, IQueryField>();

        public ListView(long actionID)
            : this()
        {
            this.ActionID = actionID;

            this.Init();
        }

        public ListView()
        {
            this.InitializeComponent();
        }

        public void Query()
        {
            this.LoadData();
        }

        private void Init()
        {
            var app = (App)Application.Current;
            var actionIds = new long[] { this.ActionID };
            var fields = new string[] { "_id", "name", "view", "model", "views" };
            var sc = SynchronizationContext.Current;
            app.ClientService.ReadModel("core.action_window", actionIds, fields, actionRecords =>
            {
                sc.Send(delegate
                {
                    this.actionRecord = actionRecords[0];
                    var view = (object[])actionRecords[0]["view"];
                    var viewIds = new long[] { (long)view[0] };
                    app.ClientService.ReadModel("core.view", viewIds, null, viewRecords =>
                    {
                        sc.Send(delegate
                        {
                            this.viewRecord = viewRecords[0];
                            this.LoadInternal();
                        }, null);
                    });
                }, null);
            });
        }

        #region IWindowAction Members

        public long ActionID { get; set; }

        #endregion

        private void LoadData()
        {
            Debug.Assert(this.createdQueryFields != null);

            var app = (App)Application.Current;
            //加载数据
            var offset = 0;// long.Parse(this.textOffset.Text);
            var limit = 2000;// long.Parse(this.textLimit.Text);

            //生成条件
            var constraints = new List<object[]>();
            foreach (var p in this.createdQueryFields)
            {
                if (!p.Value.IsEmpty)
                {
                    foreach (var c in p.Value.GetConstraints())
                    {
                        constraints.Add(c.ToConstraint());
                    }
                }
            }

            var sc = SynchronizationContext.Current;
            app.ClientService.SearchModel(this.modelName, constraints.ToArray(), null, offset, limit, ids =>
            {
                app.ClientService.ReadModel(this.modelName, ids, this.fields, records =>
                {
                    sc.Send(delegate
                    {
                        //我们需要一个唯一的字符串型 ID
                        this.gridList.ItemsSource = DataSourceCreator.ToDataSource(records, this.modelName, fields.ToArray());
                    }, null);
                });
            });
        }

        private void LoadInternal()
        {
            var app = (App)Application.Current;

            var layout = (string)this.viewRecord["layout"];
            var layoutDocument = XDocument.Parse(layout);
            this.modelName = (string)this.actionRecord["model"];

            this.InitializeColumns(layoutDocument);

            this.LoadData();
        }

        private void InitializeColumns(XDocument layoutDocument)
        {
            var app = (App)Application.Current;
            var args = new object[] { this.modelName };
            app.ClientService.Execute("core.model", "GetFields", args, result =>
            {
                var fields = ((object[])result).Select(r => (Dictionary<string, object>)r).ToArray();
                var viewFields = layoutDocument.Elements("tree").Elements();

                IList<DataGridBoundColumn> cols = new List<DataGridBoundColumn>();
                cols.Add(this.MakeColumn("_id", "ID", "ID", System.Windows.Visibility.Collapsed));

                foreach (var f in viewFields)
                {
                    var fieldName = f.Attribute("name").Value;
                    var metaField = fields.Single(i => (string)i["name"] == fieldName);
                    cols.Add(this.MakeColumn(fieldName, (string)metaField["type"], (string)metaField["label"]));
                }

                this.gridList.Columns.Clear();
                foreach (var col in cols)
                {
                    this.gridList.Columns.Add(col);
                }

                this.CreateQueryForm(fields, viewFields);
            });
        }

        private void CreateQueryForm(Dictionary<string, object>[] fields, IEnumerable<XElement> viewFields)
        {
            //生成基本查询条件表单
            var columns = (int)Math.Round(this.BasicConditions.ActualWidth / 150.00) * 2;
            if (columns % 2 != 0) columns--;
            var basicQueryForm = new Malt.Layout.Models.Form()
            {
                ColumnCount = columns,
            };

            var basicFields = viewFields.Where(ele => ele.Attribute("where").Value == "basic");
            var basicQueryFormChildren = new List<Malt.Layout.Models.Placable>();
            var factory = new QueryFieldControlFactory(fields);
            var createdFieldControls = new Dictionary<string, IQueryField>();
            foreach (var fieldLayout in basicFields)
            {
                var fieldName = fieldLayout.Attribute("name").Value;
                var metaField = fields.Single(i => (string)i["name"] == fieldName);
                var label = new Malt.Layout.Models.Label()
                {
                    Text = (string)metaField["label"],
                };
                basicQueryFormChildren.Add(label);

                var field = new Malt.Layout.Models.Field()
                {
                    Name = (string)metaField["name"],
                };
                basicQueryFormChildren.Add(field);
            }
            basicQueryForm.ChildElements = basicQueryFormChildren.ToArray();

            var layoutEngine = new Malt.Layout.LayoutEngine(factory);
            var basicQueryGrid = layoutEngine.CreateLayoutTable(basicQueryForm);
            this.BasicConditions.Child = (Grid)basicQueryGrid;
            this.createdQueryFields = factory.CreatedQueryField;

            this.ClearAllConstraints();
        }


        private DataGridBoundColumn MakeColumn(
            string fieldName, string fieldType, string fieldLabel, Visibility visibility = Visibility.Visible)
        {
            this.fields.Add(fieldName);
            var tuple = COLUMN_TYPE_MAPPING[fieldType];
            var col = Activator.CreateInstance(tuple.Item1) as DataGridBoundColumn;
            col.Visibility = visibility;
            col.Header = fieldLabel;
            col.Binding = new System.Windows.Data.Binding(fieldName);
            if (tuple.Item2 != null)
            {
                col.Binding.Converter = tuple.Item2;
            }
            return col;
        }

        private void buttonQuery_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(this.ActionID > 0);
            this.LoadData();
        }

        private void buttonDelete_Click(object sender, RoutedEventArgs e)
        {
            var sb = new System.Text.StringBuilder();
            var ids = new List<long>();
            foreach (dynamic item in this.gridList.SelectedItems)
            {
                var id = (long)item._id;
                ids.Add(id);
            }

            var msg = String.Format("您确定要永久删除 {0} 条记录吗？", ids.Count);
            var dlgResult = MessageBox.Show(msg, "删除确认", MessageBoxButton.OKCancel);

            var sc = SynchronizationContext.Current;
            if (dlgResult == MessageBoxResult.OK)
            {
                //执行删除
                var app = (App)Application.Current;
                app.IsBusy = true;

                var args = new object[] { ids };
                app.ClientService.Execute(this.modelName, "Delete", args, result =>
                {
                    sc.Send(delegate
                    {
                        this.LoadData();
                        app.IsBusy = false;
                    }, null);
                });
            }
        }

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new FormView.FormDialog(this.modelName, -1, this.actionRecord);
            dlg.ShowDialog();
            //先看看有没有已经打开同样的动作标签页了，如果有就跳转过去
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.gridList.SelectedItems.Count != 1)
            {
                return;
            }

            dynamic item = this.gridList.SelectedItems[0];
            var recordID = (long)item._id;

            var dlg = new FormView.FormDialog(this.modelName, recordID, this.actionRecord);
            dlg.ShowDialog();
        }

        private void ClearConstraintsButton_Click(object sender, RoutedEventArgs e)
        {
            this.ClearAllConstraints();
            this.LoadData();
        }

        private void ClearAllConstraints()
        {
            System.Diagnostics.Debug.Assert(this.createdQueryFields != null);

            foreach (var p in this.createdQueryFields)
            {
                p.Value.Empty();
            }
        }

    }
}
