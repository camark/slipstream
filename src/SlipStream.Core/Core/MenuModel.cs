﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SlipStream.Model;

namespace SlipStream.Core
{

    [Resource]
    public sealed class MenuModel : AbstractSqlModel
    {

        public MenuModel()
            : base("core.menu")
        {
            this.Hierarchy = true;

            Fields.Chars("name").SetLabel("Name").Required();
            Fields.Integer("ordinal").SetLabel("Ordinal Number")
                .Required().SetDefaultValueGetter(arg => 0);
            Fields.ManyToOne("parent", "core.menu").SetLabel("Parent Menu").NotRequired();
            Fields.Chars("icon").SetLabel("Icon Name").NotRequired();
            Fields.Boolean("active").SetLabel("Active").Required().SetDefaultValueGetter(arg => true);
            Fields.Reference("action").SetLabel("Action").NotRequired().SetOptions(
                   new Dictionary<string, string>()
                {
                    { "core.action_window", "Window Action" },
                    { "core.action_wizard", "Wizard Action" },
                });
        }

    }
}
