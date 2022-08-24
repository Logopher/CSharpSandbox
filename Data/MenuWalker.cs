﻿using CSharpSandbox.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data
{
    internal class MenuWalker
    {
        public static void WalkMenu(
            IEnumerable<Model.MenuItem> modelMenuItems,
            IEnumerable<Database.MenuItem> dbMenuItems,
            Action<Model.MenuItem?, Database.MenuItem?> body)
        {
            var model = modelMenuItems.OrderBy(i => i.Header).ToList();
            var db = dbMenuItems.OrderBy(i => i.Header).ToList();

            var modelEnumer = model.GetEnumerator();
            var dbEnumer = db.GetEnumerator();

            int compare(Model.MenuItem modelValue, Database.MenuItem dbValue)
                => modelValue.Header.CompareTo(dbValue.Header);

            void recurse(Model.MenuItem? modelValue, Database.MenuItem? dbValue)
            {
                body(modelValue, dbValue);

                WalkMenu(
                    modelValue?.Children ?? Array.Empty<Model.MenuItem>().AsEnumerable(),
                    dbValue?.Children ?? Array.Empty<Database.MenuItem>().AsEnumerable(),
                    body);
            }

            void recurse2(Model.MenuItem? modelValue, Database.MenuItem? dbValue)
            {
                if (modelValue == null || dbValue == null)
                {
                    recurse(modelValue, dbValue);
                }
                else
                {
                    var direction = compare(modelValue, dbValue);
                    if (direction == 0)
                    {
                        recurse(modelValue, dbValue);
                    }
                    else if (direction < 0)
                    {
                        do
                        {
                            recurse(modelValue, null);
                            modelValue = null;
                        }
                        while (modelEnumer.MoveNext() && (modelValue = modelEnumer.Current).Header.CompareTo(dbValue.Header) < 0);

                        recurse2(modelValue, dbValue);
                    }
                    else if (0 < direction)
                    {
                        do
                        {
                            recurse(null, dbValue);
                            dbValue = null;
                        }
                        while (dbEnumer.MoveNext() && 0 < modelValue.Header.CompareTo((dbValue = dbEnumer.Current).Header));

                        recurse2(modelValue, dbValue);
                    }
                }
            }

            var modelEnumerDone = false;
            var dbEnumerDone = false;

            while ((modelEnumerDone || (modelEnumerDone = !modelEnumer.MoveNext()))
                && (dbEnumerDone || (dbEnumerDone = !dbEnumer.MoveNext()))
                && (!modelEnumerDone || !dbEnumerDone))
            {
                var modelValue = modelEnumerDone ? null : modelEnumer.Current;
                var dbValue = dbEnumerDone ? null : dbEnumer.Current;

                recurse2(modelValue, dbValue);
            }
        }
        public static void WalkMenu2(
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

            var helper = new MenuWalkerHelper(this, modelParent, dbParent, modelEnumer, dbEnumer);

            var modelEnumerMore = true;
            var dbEnumerMore = true;

            Model.MenuItem? modelValue = null;
            Database.MenuItem? dbValue = null;

            while (modelEnumerMore || dbEnumerMore)
            {
                modelEnumerMore = modelEnumerMore && modelEnumer.Step(out modelValue);

                dbEnumerMore = dbEnumerMore && dbEnumer.Step(out dbValue);

                helper.Recurse(modelValue, dbValue);
            }
        }
    }

    internal class MenuWalkerHelper
    {
        private readonly MenuWalker _walker;
        private readonly Model.MenuItem? _modelParent;
        private readonly Database.MenuItem? _dbParent;
        private readonly List<Model.MenuItem>.Enumerator _modelEnumer;
        private readonly List<Database.MenuItem>.Enumerator _dbEnumer;

        public MenuWalkerHelper(MenuWalker walker, Model.MenuItem? modelParent, Database.MenuItem? dbParent, List<Model.MenuItem>.Enumerator modelEnumer, List<Database.MenuItem>.Enumerator dbEnumer)
        {
            _walker = walker;
            _modelParent = modelParent;
            _dbParent = dbParent;
            _modelEnumer = modelEnumer;
            _dbEnumer = dbEnumer;
        }

        public void Recurse(Model.MenuItem? modelValue, Database.MenuItem? dbValue)
        {
            if (modelValue == null || dbValue == null)
            {
                _walker.Recurse(_modelParent, _dbParent, modelValue, dbValue);
            }
            else
            {
                int compare() => modelValue!.Header.CompareTo(dbValue!.Header);
                if (compare() == 0)
                {
                    _walker.Recurse(_modelParent, _dbParent, modelValue, dbValue);
                }
                else
                {
                    while (compare() != 0)
                    {
                        if (compare() < 0)
                        {
                            _walker.Recurse(_modelParent, _dbParent, modelValue, null);
                            if (_modelEnumer.Step(out modelValue))
                            {
                                break;
                            }
                        }
                        else
                        {
                            _walker.Recurse(_modelParent, _dbParent, null, dbValue);
                            if (_dbEnumer.Step(out dbValue))
                            {
                                break;
                            }
                        }
                    }

                    Recurse(modelValue, dbValue);
                }
            }
        }
    }
}
