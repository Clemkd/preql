using Microsoft.Extensions.DependencyInjection;

namespace Preql;

/// <summary>
/// Extension methods for configuring Preql services.
/// </summary>
public static class PreqlServiceCollectionExtensions
{
    /// <summary>
    /// Adds Preql services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="dialect">The SQL dialect to use.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddPreql(this IServiceCollection services, SqlDialect dialect)
    {
        services.AddSingleton<IPreqlContext>(sp => new PreqlContext(dialect));
        return services;
    }

    /// <summary>
    /// Adds Preql services with a specific context type to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <typeparam name="TContext">The type of the context interface. Must have a constructor that accepts <see cref="SqlDialect"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="dialect">The SQL dialect to use.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="InvalidOperationException">Thrown if TContext doesn't have an appropriate constructor.</exception>
    public static IServiceCollection AddPreql<TContext>(this IServiceCollection services, SqlDialect dialect)
        where TContext : class, IPreqlContext
    {
        services.AddSingleton<TContext>(sp =>
        {
            try
            {
                return (TContext)Activator.CreateInstance(typeof(TContext), dialect)!;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to create instance of {typeof(TContext).Name}. " +
                    $"Ensure the type has a public constructor that accepts SqlDialect parameter.", ex);
            }
        });
        return services;
    }
}
