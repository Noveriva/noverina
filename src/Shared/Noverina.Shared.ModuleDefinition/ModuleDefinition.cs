using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Noverina.Shared.ModuleDefinition;

public abstract class ModuleDefinition
{
    internal string ModuleName => GetType().Name;
    
    public abstract string ModulePrefix { get; }

    public abstract void AddDependencies(IServiceCollection services, IConfiguration configuration);
}