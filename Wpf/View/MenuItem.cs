using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CSharpSandbox.Wpf.View
{
    using Model = Data.Model.MenuItem;

    public class MenuItem : System.Windows.Controls.MenuItem
    {
        internal Model Model { get; }

        internal MenuItem(Model model, Func<string, ICommand> commandResolver)
        {
            Model = model;

            if (Model.AccessCharacter != null)
            {
                var index = Model.Header.IndexOf(Model.AccessCharacter!.Value);

                if (index == -1)
                {
                    throw new Exception("Access character not found in header.");
                }

                Header = $"{Model.Header[..index]}_{Model.Header[index..]}";

                if (Model.CommandName != null)
                {
                    Command = commandResolver(Model.CommandName);
                }
            }

            ItemsSource = Model.Children.Select(i => new MenuItem(i, commandResolver));
        }
    }
}
