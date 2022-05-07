using System;

namespace CometPeak.SerializableKrakenIoc
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Parameter)]
    public class InjectAttribute : Attribute
    {
        public object Category;

        public InjectAttribute(string category = null)
        {
            Category = string.IsNullOrEmpty(category) ? null : category;
        }

        public InjectAttribute(object category)
        {
            Category = category;
        }
    }
}