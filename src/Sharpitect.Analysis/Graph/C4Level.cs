namespace Sharpitect.Analysis.Graph;

/// <summary>
/// C4 model abstraction levels for annotating declaration nodes.
/// </summary>
public enum C4Level
{
    /// <summary>
    /// No C4 annotation.
    /// </summary>
    None,

    /// <summary>
    /// C4 Level 1: Software System - the highest level of abstraction.
    /// </summary>
    System,

    /// <summary>
    /// C4 Level 2: Container - an application or data store.
    /// </summary>
    Container,

    /// <summary>
    /// C4 Level 3: Component - a logical grouping of functionality.
    /// </summary>
    Component,

    /// <summary>
    /// C4 Level 4: Code - classes, interfaces, and their members.
    /// </summary>
    Code
}