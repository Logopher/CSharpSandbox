using CSharpSandbox.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    internal class MenuWalker
    {
        public static void Walk(
            IEnumerable<Model.MenuItem> modelMenuItems,
            IEnumerable<Database.MenuItem> dbMenuItems,
            Func<Model.MenuItem?, Database.MenuItem?, Model.MenuItem?, Database.MenuItem?, (Model.MenuItem, Database.MenuItem)> body)
        {
            var walker = new MenuWalker(body);
            walker.Walk(modelMenuItems, dbMenuItems);
        }

        private readonly Func<Model.MenuItem?, Database.MenuItem?, Model.MenuItem?, Database.MenuItem?, (Model.MenuItem, Database.MenuItem)> _body;

        public MenuWalker(Func<Model.MenuItem?, Database.MenuItem?, Model.MenuItem?, Database.MenuItem?, (Model.MenuItem, Database.MenuItem)> body)
        {
            _body = body;
        }

        public void Recurse(Model.MenuItem? modelParent, Database.MenuItem? dbParent,
            Model.MenuItem? modelValue, Database.MenuItem? dbValue)
        {
            if ((modelParent == null) != (dbParent == null))
            {
                throw new Exception();
            }

            (modelValue, dbValue) = _body(modelParent, dbParent, modelValue, dbValue);

            Walk(
                modelValue,
                dbValue,
                modelValue.Children.ToList(),
                dbValue.Children);
        }

        public void Walk(
            IEnumerable<Model.MenuItem> modelMenuItems,
            IEnumerable<Database.MenuItem> dbMenuItems)
            => Walk(null, null, modelMenuItems, dbMenuItems);

        public void Walk(
            Model.MenuItem? modelParent,
            Database.MenuItem? dbParent,
            IEnumerable<Model.MenuItem> modelMenuItems,
            IEnumerable<Database.MenuItem> dbMenuItems)
        {
            var model = modelMenuItems.OrderBy(i => i.Header).ToList();
            var db = dbMenuItems.OrderBy(i => i.Header).ToList();

            Model.MenuItem? modelValue;
            Database.MenuItem? dbValue;

            var i = 0;

            void stepModel()
            {
                modelValue = i < model.Count ? model![i] : null;
            }

            void stepDb()
            {
                dbValue = i < db.Count ? db![i] : null;
            }

            stepModel();
            stepDb();

            for (i = 1; i < model.Count || i < db.Count; i++)
            {
                if (modelValue == null || dbValue == null)
                {
                    Recurse(modelParent, dbParent, modelValue, dbValue);
                    stepModel();
                    stepDb();
                }
                else
                {
                    var direction = modelValue.Header.CompareTo(dbValue.Header);
                    if (direction == 0)
                    {
                        Recurse(modelParent, dbParent, modelValue, dbValue);
                        stepModel();
                        stepDb();
                    }
                    else if (direction < 0)
                    {
                        Recurse(modelParent, dbParent, modelValue, null);
                        stepModel();
                        stepDb();
                    }
                    else if (0 < direction)
                    {
                        Recurse(modelParent, dbParent, null, dbValue);
                        stepDb();
                    }
                }
            }
        }
    }
}
