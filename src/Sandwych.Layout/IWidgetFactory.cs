﻿using System;
using System.Collections.Generic;
using System.Text;

using Sandwych.Layout.Models;

namespace Sandwych.Layout
{
    public interface IWidgetFactory
    {
        object CreateInputWidget(Input field);
        ITableLayoutWidget CreateTableLayoutWidget(IContainer container);

        /// <summary>
        /// 创建标签控件
        /// </summary>
        /// <returns></returns>
        object CreateLabelWidget(Models.Label label);

        object CreateNotebookWidget(Models.Notebook notebook);

        object CreatePageWidget(Models.Page page, object parentWidget, object childContent);

        object CreateButtonWidget(Models.Button button);

        /// <summary>
        /// 创建水平线控件
        /// </summary>
        /// <returns></returns>
        object CreateHorizontalLineWidget(Models.HorizontalLine hl);
    }
}
