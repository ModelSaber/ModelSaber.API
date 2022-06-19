using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ModelSaber.API
{
    public class Program
    {
        public static DateTime CompiledTime { get; private set; }
        public static Version Version { get; private set; } = null!;
        
        public static void Main(string[] args)
        {
            var executingAsm = Assembly.GetExecutingAssembly();
            CompiledTime = executingAsm.GetLinkerTime();
            Version = executingAsm.GetName().Version ?? new Version(0,0,420,69);
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }

    public static class Extensions
    {
        public static DateTime GetLinkerTime(this Assembly assembly)
        {
            const string buildVersionMetadataPrefix = "+build";

            var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (attribute?.InformationalVersion == null) return default;
            var value = attribute.InformationalVersion;
            var index = value.IndexOf(buildVersionMetadataPrefix, StringComparison.Ordinal);
            if (index <= 0) return default;
            value = value[(index + buildVersionMetadataPrefix.Length)..];
            return DateTime.ParseExact(value, "yyyy-MM-ddTHH:mm:ss:fffZ", CultureInfo.InvariantCulture);
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> enumerable)
        {
            foreach (var element in enumerable)
            {
                collection.Add(element);
            }
        }
    }
}
