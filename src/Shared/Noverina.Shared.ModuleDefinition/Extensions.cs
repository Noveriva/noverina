using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Noverina.Shared.ModuleDefinition;

public static class Extensions
{
    private static Dictionary<string, ModuleDefinition> _registeredModules = new();
    
    public static void RegisterModule<TModule>(Func<TModule>? moduleFactory = null) where TModule : ModuleDefinition
    {
        var moduleDefinition = moduleFactory is not null
            ? moduleFactory()
            : Activator.CreateInstance<TModule>();
        
        _registeredModules.Add(moduleDefinition.ModuleName, moduleDefinition);
    }
    
    public static void AddModules(this IServiceCollection services, IConfiguration configuration)
    {
        foreach (var module in _registeredModules.Values)
        {
            module.AddDependencies(services, configuration);
        }
    }
}