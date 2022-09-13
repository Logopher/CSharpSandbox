using CSharpSandbox.Common;
using CSharpSandbox.Shells;
using NLog;

namespace Data;

public class Repository : IDisposable
{
    static readonly Logger CurrentLogger = LogManager.GetCurrentClassLogger();

    private readonly Database.Context _context;

    private readonly Dictionary<string, Script> _scripts;
    private List<Model.MenuItem> _menuItems;

    public IReadOnlyDictionary<string, Script> Scripts => _scripts;

    public IReadOnlyList<Model.MenuItem> MenuItems => _menuItems;

    public Repository(Database.Context context)
    {
        _context = context;

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
                        CurrentLogger.Error("{Message}", e.Message);
                    }

                    return Script.Create(Enum.Parse<Language>(c.Language), source);
                });

        _menuItems = _context.MenuItems
            .Select(i => new Model.MenuItem(i))
            .ToList();
    }

    public void Add(params Model.MenuItem[] items)
    {
        foreach (var item in items)
        {
            Add(item.Children.ToArray());
            _context.Add(item.Record);
        }
    }

    public void Delete(params Model.MenuItem[] items)
    {
        foreach (var item in items)
        {
            Delete(item.Children.ToArray());
            item.Parent?.Remove(item);
            _context.Remove(item.Record);
        }
    }

    public void SetHeader(Model.MenuItem item, string header)
    {
        if (item.Id != null)
        {
            var record = _context.MenuItems.First(i => i.Id == item.Id);
            var siblings = _context.MenuItems
                .Where(i => i.Parent == record.Parent)
                .Except(new[] { record });

            if (siblings.Any(i => i.Header == header))
            {
                throw new InvalidOperationException("Another item in this menu already has this header.");
            }

            record.Header = header;
            _context.SaveChanges();
        }

        item.Header = header;
    }

    public void Save()
    {
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.SaveChanges();
        GC.SuppressFinalize(this);
    }
}
