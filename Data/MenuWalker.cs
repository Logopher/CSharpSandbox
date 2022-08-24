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
                modelValue.Children,
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

            var modelEnumer = model.GetEnumerator();
            var dbEnumer = db.GetEnumerator();

            var modelValue = modelEnumer.Step();

            var dbValue = dbEnumer.Step();

            while (true)
            {
                if (modelValue == null && dbValue == null)
                {
                    break;
                }

                if (modelValue == null || dbValue == null)
                {
                    Recurse(modelParent, dbParent, modelValue, dbValue);
                    modelValue = modelEnumer.Step();
                    dbValue = dbEnumer.Step();
                    continue;
                }

                var direction = modelValue.Header.CompareTo(dbValue.Header);
                if (direction == 0)
                {
                    Recurse(modelParent, dbParent, modelValue, dbValue);
                    modelValue = modelEnumer.Step();
                    dbValue = dbEnumer.Step();
                }
                else if (direction < 0)
                {
                    Recurse(modelParent, dbParent, modelValue, null);
                    modelValue = modelEnumer.Step();
                }
                else if (0 < direction)
                {
                    Recurse(modelParent, dbParent, null, dbValue);
                    dbValue = dbEnumer.Step();
                }
            }
        }
    }
}
