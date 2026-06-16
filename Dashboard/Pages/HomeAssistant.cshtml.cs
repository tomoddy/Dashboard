using Dashboard.Models;
using Dashboard.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Dashboard.Pages;

/// <summary>
/// Page model for the Home Assistant iframe view.
/// </summary>
/// <param name="statusStore">The singleton status store.</param>
/// <param name="configuration">The app configuration.</param>
public class HomeAssistantModel(StatusStore statusStore, IConfiguration configuration) : PageModel
{
    private readonly StatusStore StatusStore = statusStore;
    private readonly IConfiguration Configuration = configuration;

    /// <summary>
    /// All services ordered alphabetically by name.
    /// </summary>
    public List<ServiceEntry> Services { get; set; } = [];

    /// <summary>
    /// Latest cached status for all services, keyed by service name.
    /// </summary>
    public IReadOnlyDictionary<string, ServiceStatus> Statuses { get; set; } = new Dictionary<string, ServiceStatus>();

    /// <summary>
    /// Loads services from config and reads cached statuses from the store.
    /// </summary>
    public void OnGet()
    {
        Services = Configuration.GetSection("Services").Get<List<ServiceEntry>>() ?? [];
        Services = [.. Services.OrderBy(s => s.Name)];
        Statuses = StatusStore.GetAll();
    }
}