using System;

namespace StructureMap.Pipeline
{
    public class DefaultInstance : Instance
    {
        public DefaultInstance()
        {
            int x = 1;
        }

        protected override object build(Type pluginType, IBuildSession session)
        {
            return session.CreateInstance(pluginType);
        }

        protected override string getDescription()
        {
            return "Default";
        }
    }
}