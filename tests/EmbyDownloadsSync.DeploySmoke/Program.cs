using System.Reflection;
using System.Runtime.Loader;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: EmbyDownloadsSync.DeploySmoke <merged-plugin-path>");
    return 2;
}

var temporaryDirectory = Path.Combine(Path.GetTempPath(), "emby-downloads-sync-smoke-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(temporaryDirectory);
var isolatedPlugin = Path.Combine(temporaryDirectory, "EmbyDownloadsSync.dll");
File.Copy(Path.GetFullPath(args[0]), isolatedPlugin);
var context = new AssemblyLoadContext("EmbyDownloadsSync.DeploySmoke." + Guid.NewGuid().ToString("N"), true);
context.Resolving += (_, name) => AssemblyLoadContext.Default.Assemblies.FirstOrDefault(assembly => assembly.GetName().Name == name.Name)
    ?? TryLoad(name);

try
{
    Assembly assembly;
    using (var stream = File.OpenRead(isolatedPlugin)) assembly = context.LoadFromStream(stream);
    _ = assembly.GetTypes();
    if (assembly.GetType("EmbyDownloadsSync.Plugin.Plugin", true) == null ||
        assembly.GetType("EmbyDownloadsSync.Core.Planning.SyncPlanner", true) == null)
        throw new InvalidOperationException("Merged plugin types were not found.");
    Console.WriteLine("Merged Emby Downloads Sync plugin loaded from an isolated directory.");
    return 0;
}
finally
{
    context.Unload();
    GC.Collect();
    GC.WaitForPendingFinalizers();
    Directory.Delete(temporaryDirectory, true);
}

static Assembly? TryLoad(AssemblyName name)
{
    try { return AssemblyLoadContext.Default.LoadFromAssemblyName(name); }
    catch (FileNotFoundException) { return null; }
}
