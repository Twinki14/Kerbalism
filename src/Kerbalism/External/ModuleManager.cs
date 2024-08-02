namespace Kerbalism.External
{
    public static class ModuleManager
    {
        public static readonly int mmMajor;

        static ModuleManager()
        {
            foreach (var a in AssemblyLoader.loadedAssemblies)
            {
                if (a.name != "ModuleManager")
                {
                    continue;
                }

                var v = a.assembly.GetName().Version;
                mmMajor = v.Major;
                break;
            }
        }
    }
}
