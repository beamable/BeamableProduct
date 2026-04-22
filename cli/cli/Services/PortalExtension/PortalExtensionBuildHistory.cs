namespace cli.Services.PortalExtension;

public class PortalExtensionBuild
{
	public string Checksum;

	public bool IsError;
	public string ErrorMessage;

	public string FullBuild;
	public string[] javascriptLines;
	public string[] cssLines;
	public string[] metadataLines;
}

public class PortalExtensionBuildHistory
{
	private readonly int _capacity;
    private readonly LinkedList<PortalExtensionBuild> _orderedList;

    public PortalExtensionBuildHistory(int capacity)
    {
        if (capacity <= 0) throw new ArgumentException("[PortalExtensionBuildHistory] Capacity must be greater than zero.");

        _capacity = capacity;
        _orderedList = new LinkedList<PortalExtensionBuild>();
    }

    public void Add(PortalExtensionBuild value)
    {

        if (_orderedList.Count >= _capacity)
        {
	        _orderedList.RemoveLast();
        }

        _orderedList.AddFirst(value);
    }

    public bool Get(string key, out  PortalExtensionBuild value)
    {
        foreach (var item in _orderedList){
	        if (item.Checksum == key)
	        {
		        value = item;
		        return true;
	        }
        }
        value = null;
        return false;
    }

    public PortalExtensionBuild GetFirst()
    {
	    return _orderedList.First();
    }


    public IEnumerable<PortalExtensionBuild> GetAll()
    {
        foreach (var item in _orderedList)
        {
            yield return item;
        }
    }

    public int Count => _orderedList.Count;
}
