using System;
using System.Linq;
using Loki.Bot;

namespace CommunityLib
{
    public class Communication
    {
        /// <summary>
        /// This function calls a given plugin's Execute() implementation (generic)
        /// </summary>
        /// <typeparam name="T">Desired returned type (needs to match with plugin's return type)</typeparam>
        /// <param name="pluginName">Plugin's name</param>
        /// <param name="method">Method's name to call</param>
        /// <param name="param">Objects or variables to pass to the destination plugin</param>
        /// <returns></returns>
        public static T GenericExecute<T>(string pluginName, string method, dynamic[] param)
        {
            // We gather the plugin considering the name of this one
            var plugin = PluginManager.Plugins.FirstOrDefault(x => x.Name == pluginName);
            // If the plugin is null, return the default value of the desired type
            if (plugin == null) return (T)Convert.ChangeType(default(T), typeof(T));
            // Else, grab execute from the plugin, wit method and params
            var value = plugin.Execute(method, param);
            // Return the gathered value
            return (T)Convert.ChangeType(value, typeof(T));
        }
    }
}
