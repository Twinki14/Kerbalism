using System;
using System.Collections.Generic;
using System.Reflection;
using Kerbalism.Modules;

// TODO - Kerbalism - Forked Science
// TODO - This is likely no longer required, EC is the only thing we'll be truely simulating in the background
// TODO - and this appears to be for all other types of resources (Maybe CommNet?)

namespace Kerbalism.Database
{
    public class ResourceUpdateDelegate
    {
        private readonly PartModule _module;
        private readonly MethodInfo _methodInfo;

        private static readonly Dictionary<Type, MethodInfo> SupportedModules = new Dictionary<Type, MethodInfo>();
        private static readonly List<Type> UnsupportedModules = new List<Type>();

        private ResourceUpdateDelegate(MethodInfo methodInfo, PartModule module)
        {
            _methodInfo = methodInfo;
            _module = module;
        }

        public string invoke(Dictionary<string, double> availableResources, List<KeyValuePair<string, double>> resourceChangeRequest)
        {
            var km = _module as IKerbalismModule;
            if (km != null)
            {
                return km.ResourceUpdate(availableResources, resourceChangeRequest);
            }

            var title = _methodInfo.Invoke(_module, new object[] {availableResources, resourceChangeRequest});
            return title == null ? _module.moduleName : title.ToString();
        }

        public static ResourceUpdateDelegate Instance(PartModule module)
        {
            var type = module.GetType();

            SupportedModules.TryGetValue(type, out var methodInfo);
            if (methodInfo != null)
            {
                return new ResourceUpdateDelegate(methodInfo, module);
            }

            if (UnsupportedModules.Contains(type))
            {
                return null;
            }

            methodInfo = module.GetType().GetMethod("ResourceUpdate", BindingFlags.Instance | BindingFlags.Public);
            if (methodInfo == null)
            {
                UnsupportedModules.Add(type);
                return null;
            }

            SupportedModules[type] = methodInfo;
            return new ResourceUpdateDelegate(methodInfo, module);
        }
    }
}
