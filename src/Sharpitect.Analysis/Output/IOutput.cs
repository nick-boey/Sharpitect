using Sharpitect.Analysis.Model;

namespace Sharpitect.Analysis.Output;

/// <summary>
/// Interface for outputting an architecture model to various formats.
/// </summary>
public interface IOutput
{
    /// <summary>
    /// Writes the architecture model to the specified writer.
    /// </summary>
    /// <param name="model">The architecture model to output.</param>
    /// <param name="writer">The text writer to write to.</param>
    void Write(ArchitectureModel model, TextWriter writer);
}
