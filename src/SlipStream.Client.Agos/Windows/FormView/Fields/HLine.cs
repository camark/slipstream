﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

using SlipStream.Client.Agos.Models;

namespace SlipStream.Client.Agos.Windows.FormView
{

    [TemplatePart(Name = HLine.ElementRoot, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = HLine.ElementHorizontalLine, Type = typeof(Rectangle))]
    [TemplatePart(Name = HLine.ElementLabel, Type = typeof(TextBlock))]
    public class HLine : Control
    {
        public const string ElementRoot = "Root";
        public const string ElementHorizontalLine = "HorizontalLine";
        public const string ElementLabel = "Label";

        FrameworkElement root;
        Rectangle border;
        TextBlock label;
        private readonly string text;

        public HLine(string text)
        {
            this.text = text;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.root = this.GetTemplateChild(ElementRoot) as FrameworkElement;
            this.border = this.GetTemplateChild(ElementHorizontalLine) as Rectangle;
            this.label = this.GetTemplateChild(ElementLabel) as TextBlock;

            if (this.label != null && !string.IsNullOrEmpty(this.text))
            {
                this.label.Text = this.text;
            }
        }

        public string Text
        {
            get
            {
                if (this.label != null)
                {
                    return (string)this.label.Text;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                if (this.label != null)
                {
                    this.label.Text = value ?? string.Empty;
                }
            }
        }
    }
}
