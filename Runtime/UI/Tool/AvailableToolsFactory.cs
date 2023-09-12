using System;
using System.Collections.Generic;
using System.Linq;

namespace Unity.Muse.Common
{
    public static class AvailableToolsFactory
    {
        static HashSet<Type> s_AvailableTools;

        public static void RegisterTool<T>() where T: ICanvasTool, new()
        {
            s_AvailableTools ??= new HashSet<Type>();
            s_AvailableTools.Add(typeof(T));
        }

        public static IEnumerable<ICanvasTool> GetAvailableTools(Model model)
        {
            var tools = new List<ICanvasTool>();
            foreach (var instance in s_AvailableTools.Select(tool => (ICanvasTool)Activator.CreateInstance(tool)))
            {
                instance.SetModel(model);
                tools.Add(instance);
            }
            return tools;
        }
    }
}
