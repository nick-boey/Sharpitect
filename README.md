# C4SharpAnalyzer

Creates C4 models and diagrams of annotated C# codebases. Contains the annotations and analyzers and uses the C4Sharp project as a backend.

## C4 conventions

C4SharpAnalyzer relies on your codebase being set up according to a relatively strict convention, and is mostly suited to monorepositories.

### Systems

- Systems are solutions.

### Containers

- Containers are the projects that generate an executable.

### Components

- Components cannot be nested, but they can be grouped by namespaces or projects.
- Components may be projects, namespaces or groups.

### Code

- Code are automatically generated class diagrams.

### External connections

- TODO

## Dependencies

C4SharpAnalyzer uses the [C4Sharp](https://github.com/8T4/c4sharp) .NET library as a backend, primarily for the [element abstractions](https://github.com/8T4/c4sharp/tree/main/src/C4Sharp/Elements) and for rendering diagrams.
