using Data;

using Model = Data.Model;
using Database = Data.Database;

namespace CSharpSandbox.Tests
{
    [TestClass]
    public class DataTests
    {
        [TestMethod]
        public void Reconciliation()
        {
            /*
            var model = new List<Model.MenuItem>
            {
                new Model.MenuItem("About", 'A', "About"),
                new Model.MenuItem("Foo", 'F', "Foo"),
                new Model.MenuItem("Baz", 'Z', "Baz"),
            };
            var db = new List<Model.MenuItem>
            {
                new Model.MenuItem("About", 'A', "About"),
                new Model.MenuItem("Foo", 'F', "Foo"),
                new Model.MenuItem("Bar", 'B', "Bar"),
            };
            var menu = Repository.Reconcile(model, db);

            Assert.IsNotNull(menu);
            Assert.AreEqual(4, menu.Count);
            //*/
        }
    }
}