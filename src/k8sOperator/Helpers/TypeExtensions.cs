namespace k8s.Operator.Helpers;

internal static class TypeExtensions
{
    extension(Type type)
    {
        public string GetFriendlyName()
        {
            if (type.IsGenericType)
            {
                // Get the generic type name without the `1, `2, etc.
                string typeName = type.Name.Substring(0, type.Name.IndexOf('`'));

                // Recursively get friendly names for generic arguments
                string genericArgs = string.Join(", ",
                    type.GetGenericArguments().Select(t => t.GetFriendlyName()));

                return $"{typeName}<{genericArgs}>";
            }
            else
            {
                return type.Name;
            }
        }
    }
}

