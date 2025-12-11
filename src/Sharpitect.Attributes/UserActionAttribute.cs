using System;

namespace Sharpitect.Attributes
{
    /// <summary>
    /// Marks a method as a user entry point, creating a relationship from a person to the component.
    /// The person name must match an entry in the people section of the .sln.c4 file.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class UserActionAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the person/actor performing the action.
        /// </summary>
        public string Person { get; }

        /// <summary>
        /// Gets the description of the action being performed.
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserActionAttribute"/> class.
        /// </summary>
        /// <param name="person">The name of the person/actor (must match an entry in the people section).</param>
        /// <param name="action">The description of the action being performed.</param>
        public UserActionAttribute(string person, string action)
        {
            Person = person;
            Action = action;
        }
    }
}
