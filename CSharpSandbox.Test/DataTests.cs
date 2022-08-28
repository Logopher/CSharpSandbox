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
            var m = new Model.MenuItem("About", 'A', "About");
            var m2 = new Model.MenuItem("Foo", 'F', "Foo");
            var d = new Database.MenuItem
            {
                Header = "About",
                AccessCharacter = 'A',
                CommandName = "About",
                Children = new List<Database.MenuItem>(),
            };
            var d2 = new Database.MenuItem
            {
                Header = "Foo",
                AccessCharacter = 'F',
                CommandName = "Foo",
                Children = new List<Database.MenuItem>(),
            };

            var walker = new MenuWalker((mP, dP, m, d) =>
            {
                if (m == null || d == null)
                { throw new Exception(); }

                return (m, d);
            });

            walker.Walk(new List<Model.MenuItem> { m, m2 }, new List<Database.MenuItem> { d, d2 });
        }

        [TestMethod]
        public void MenuWalker()
        {


            var walker = new MenuWalker((mP, dP, m, d) =>
            {
                m ??= new Model.MenuItem("foo", 'f', "Foo");
                d ??= new Database.MenuItem();
                return (m, d);
            });
        }
    }
}