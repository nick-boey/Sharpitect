using System;

namespace Sharpitect.Attributes
{
    /// <summary>
    /// Marks an interface or class as a C4 component boundary.
    /// Classes implementing a [Component] interface are automatically grouped into that component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, Inherited = false)]
    public sealed class ComponentAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the component.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets or sets the description of the component.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the component.</param>
        public ComponentAttribute(string name)
        {
            Name = name;
        }
    }
}