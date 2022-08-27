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
            var d = new Database.MenuItem
            {
                Header = "About",
                AccessCharacter = 'A',
                CommandName = "About",
                Children = new List<Database.MenuItem>(),
            };

            var walker = new MenuWalker((mP, dP, m, d) =>
            {
                if (m == null || d == null)
                { throw new Exception(); }

                return (m, d);
            });

            walker.Walk(new[] { m }, new[] { d });
        }

        [TestMethod]
        public void MenuWalker()
        {


            var walker = new MenuWalker((m, d, mP, dP) =>
            {
                m ??= new Model.MenuItem("foo", 'f', "Foo");
                d ??= new Database.MenuItem();
                return (m, d);
            });
        }
    }
}