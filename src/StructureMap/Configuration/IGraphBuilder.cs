using System;
using StructureMap.Graph;

namespace StructureMap.Configuration
{
    public interface IProfileBuilder
    {
        void AddProfile(string profileName);
        void OverrideProfile(TypePath typePath, string instanceKey);
        void SetDefaultProfileName(string profileName);
    }


    public interface IGraphBuilder
    {
        PluginGraph PluginGraph { get; }
        void AddRegistry(string registryTypeName);

        void FinishFamilies();

        IProfileBuilder GetProfileBuilder();

        void ConfigureFamily(TypePath pluginTypePath, Action<PluginFamily> action);

        [Obsolete("Just not gonna be necessary anymore")]
        void WithSystemObject<T>(InstanceMemento memento, string context, Action<T> action);
        void WithType(TypePath path, string context, Action<Type> action);
    }
}