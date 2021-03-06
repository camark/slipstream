﻿using System;
using System.Net;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Linq;

using SlipStream.Client.Agos.Models;

namespace SlipStream.Client.Agos.Windows.FormView
{
    public class FieldControlFactory : Sandwych.Layout.IWidgetFactory
    {
        private readonly static IDictionary<String, Type> fieldTypeMapping =
            new Dictionary<String, Type>()
            {
                { "chars", typeof(CharsFieldControl) },
                { "boolean", typeof(BooleanFieldControl) },
                { "enum", typeof(EnumerationFieldControl) },
                { "text", typeof(TextFieldControl) },
                { "int32", typeof(Int32FieldControl) },
                { "many-to-one", typeof(ManyToOneFieldControl) },
                { "one-to-many", typeof(OneToManyFieldControl) },
                { "many-to-many", typeof(ManyToManyFieldControl) },
                { "reference", typeof(ReferenceFieldControl) },
                { "datetime", typeof(DateTimeFieldControl) },
                { "date", typeof(DateFieldControl) },
                { "double", typeof(FloatFieldControl) },
                { "decimal", typeof(DecimalFieldControl) },
            };

        private readonly string model;
        private IDictionary<string, object>[] metaFields;
        private IDictionary<string, IFieldWidget> createdFieldWidgets =
            new Dictionary<string, IFieldWidget>();
        private List<Label> createdLabels = new List<Label>();
        private List<ButtonControl> createdButtons = new List<ButtonControl>();

        public FieldControlFactory(string model, IDictionary<string, object>[] fields)
        {
            this.model = model;
            this.metaFields = fields;
        }

        public object CreateInputWidget(Sandwych.Layout.Models.Input input)
        {
            var metaField = this.metaFields.Where(i => (string)i["name"] == input.Field).Single();
            var fieldType = (string)metaField["type"];

            var t = fieldTypeMapping[fieldType];
            var widget = (IFieldWidget)Activator.CreateInstance(t, metaField);
            this.createdFieldWidgets.Add(input.Field, widget);
            return widget;
        }

        public Sandwych.Layout.ITableLayoutWidget CreateTableLayoutWidget(Sandwych.Layout.Models.IContainer container)
        {
            return new GridLayoutPanelWidget();
        }

        public Object CreateLabelWidget(Sandwych.Layout.Models.Label label)
        {
            var labelWidget = new FieldLabel(label.Field, label.Text);
            if (!String.IsNullOrEmpty(label.Field))
            {
                var metaField = this.metaFields.Where(i => (string)i["name"] == label.Field).Single();
                labelWidget.Text = metaField["label"] as string;
            }
            this.createdLabels.Add(labelWidget);

            return labelWidget;
        }

        public object CreateHorizontalLineWidget(Sandwych.Layout.Models.HorizontalLine hl)
        {
            var hlWidget = new HLine(hl.Text);
            hlWidget.Text = hl.Text;
            return hlWidget;
        }

        public IDictionary<string, IFieldWidget> CreatedFieldWidgets
        {
            get { return this.createdFieldWidgets; }
        }

        public IList<ButtonControl> CreatedButtons
        {
            get { return this.createdButtons; }
        }

        public object CreateNotebookWidget(Sandwych.Layout.Models.Notebook notebook)
        {
            if (notebook == null)
            {
                throw new ArgumentNullException("notebook");
            }

            return new NotebookControl();
        }

        public object CreatePageWidget(Sandwych.Layout.Models.Page page, object parentWidget, object childContent)
        {
            if (page == null)
            {
                throw new ArgumentNullException("page");
            }

            if (childContent == null)
            {
                throw new ArgumentNullException("childContent");
            }

            var tabItem = new PageControl(page.Label);
            tabItem.Content = childContent;
            var tabControl = (TabControl)parentWidget;
            tabControl.Items.Add(tabItem);
            return tabItem;
        }

        public object CreateButtonWidget(Sandwych.Layout.Models.Button button)
        {
            var buttonControl = new ButtonControl(button, this.model);
            this.createdButtons.Add(buttonControl);
            return buttonControl;
        }
    }
}
