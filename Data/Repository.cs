using CSharpSandbox.Common;
using CSharpSandbox.Shells;
using Microsoft.Extensions.Logging;

namespace Data;

public class Repository : IDisposable
{
    private readonly ILogger _logger;
    private readonly Database.Context _context;

    private readonly Dictionary<string, Script> _scripts;
    private readonly List<Model.MenuItem> _menuItems;

    public IReadOnlyDictionary<string, Script> Scripts => _scripts;

    public IReadOnlyList<Model.MenuItem> MenuItems => _menuItems;

    public Repository(ILogger logger)
    {
        _logger = logger;

        _context = new();

        _scripts = _context.Commands
            .ToDictionary(
                c => c.Name,
                c =>
                {
                    var source = Mundane.EmptyString;

                    try
                    {
                        source = File.ReadAllText(c.FilePath);
                    }
                    catch (IOException e)
                    {
                        _logger.LogError("{Message}", e.Message);
                    }

                    return Script.Create(Enum.Parse<Language>(c.Language), source);
                });

        _menuItems = new List<Model.MenuItem>();
        MenuWalker.WalkMenu2(_menuItems, _context.MenuItems, (mP, dP, m, d) =>
        {
            if (d == null)
            {
                throw new Exception();
            }

            if (m == null)
            {
                m = new Model.MenuItem(d.Header, d.AccessCharacter, d.CommandName, d.IsReadOnly);

                mP?.Add(m);
            }
            else if (!m.IsReadOnly)
            {
                m.Header = d.Header;
                m.AccessCharacter = d.AccessCharacter;
                m.CommandName = d.CommandName;
            }

            return (m, d);
        });
    }

    public void SaveAll()
    {
        MenuWalker.WalkMenu2(_menuItems, _context.MenuItems, (mP, dP, m, d) =>
        {
            if (m == null)
            {
                throw new Exception();
            }

            if (d == null)
            {
                d = new Database.MenuItem
                {
                    Parent = dP,
                    Header = m.Header,
                    AccessCharacter = m.AccessCharacter,
                    CommandName = m.CommandName,
                    IsReadOnly = m.IsReadOnly,
                };

                _context.MenuItems.Add(d);
            }
            else
            {
                d.Header = m.Header;
                d.AccessCharacter = m.AccessCharacter;
                d.CommandName = m.CommandName;
                d.IsReadOnly = m.IsReadOnly;
            }

            return (m, d);
        });

        _context.SaveChanges();
    }

    public void Dispose() => SaveAll();
}
