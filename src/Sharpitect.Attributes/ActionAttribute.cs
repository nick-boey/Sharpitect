using System;

namespace Sharpitect.Attributes
{
    /// <summary>
    /// Defines a custom relationship name for method calls between components.
    /// The relationship name must match an entry in the relationships registry in the .sln.yml file.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class ActionAttribute : Attribute
    {
        /// <summary>
        /// Gets the relationship name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActionAttribute"/> class.
        /// </summary>
        /// <param name="name">The relationship name (must match an entry in the relationships registry).</param>
        public ActionAttribute(string name)
        {
            Name = name;
        }
    }
}