using Microsoft.Data.Sqlite;
using Sharpitect.Analysis.Graph;

namespace Sharpitect.Analysis.Persistence;

/// <summary>
/// SQLite implementation of the graph repository.
/// </summary>
public sealed class SqliteGraphRepository : IGraphRepository
{
    private readonly string _connectionString;
    private SqliteConnection? _connection;

    /// <summary>
    /// Creates a new SQLite graph repository.
    /// </summary>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    public SqliteGraphRepository(string databasePath)
    {
        _connectionString = $"Data Source={databasePath}";
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _connection = new SqliteConnection(_connectionString);
        await _connection.OpenAsync(cancellationToken);

        await ExecuteNonQueryAsync(CreateTablesScript, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveNodeAsync(DeclarationNode node, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        const string sql = """
                           INSERT OR REPLACE INTO nodes
                           (id, name, kind, file_path, start_line, start_column, end_line, end_column, c4_level, c4_description, metadata)
                           VALUES ($id, $name, $kind, $file_path, $start_line, $start_column, $end_line, $end_column, $c4_level, $c4_description, $metadata)
                           """;

        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        AddNodeParameters(command, node);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveNodesAsync(IEnumerable<DeclarationNode> nodes, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        await using var transaction = await _connection!.BeginTransactionAsync(cancellationToken);

        const string sql = $"""
                            INSERT OR REPLACE INTO nodes
                            (id, name, kind, file_path, start_line, start_column, end_line, end_column, c4_level, c4_description, metadata)
                            VALUES ($id, $name, $kind, $file_path, $start_line, $start_column, $end_line, $end_column, $c4_level, $c4_description, $metadata)
                            """;

        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = (SqliteTransaction)transaction;

        foreach (var node in nodes)
        {
            command.Parameters.Clear();
            AddNodeParameters(command, node);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveEdgeAsync(RelationshipEdge edge, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        const string sql = """
                           INSERT OR REPLACE INTO edges
                           (id, source_id, target_id, kind, source_file_path, source_line, metadata)
                           VALUES ($id, $source_id, $target_id, $kind, $source_file_path, $source_line, $metadata)
                           """;

        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        AddEdgeParameters(command, edge);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveEdgesAsync(IEnumerable<RelationshipEdge> edges, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        await using var transaction = await _connection!.BeginTransactionAsync(cancellationToken);

        const string sql = """
                           INSERT OR REPLACE INTO edges
                           (id, source_id, target_id, kind, source_file_path, source_line, metadata)
                           VALUES ($id, $source_id, $target_id, $kind, $source_file_path, $source_line, $metadata)
                           """;

        await using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.Transaction = (SqliteTransaction)transaction;

        foreach (var edge in edges)
        {
            command.Parameters.Clear();
            AddEdgeParameters(command, edge);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    /// <inheritdoc /> 
    public async Task<IEnumerable<DeclarationNode>> GetAllNodesAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        const string sql = "SELECT * FROM nodes";

        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var nodes = new List<DeclarationNode>();
        while (await reader.ReadAsync(cancellationToken))
        {
            nodes.Add(ReadNode(reader));
        }

        return nodes;
    }

    /// <inheritdoc />
    public async Task<DeclarationNode?> GetNodeAsync(string id, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        const string sql = "SELECT * FROM nodes WHERE id = $id";

        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$id", id);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            return ReadNode(reader);
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<DeclarationNode?> GetNodeByFullyQualifiedNameAsync(string fullyQualifiedName,
        CancellationToken cancellationToken = default)
    {
        // Since Id = FullyQualifiedName, we can just query by id
        return await GetNodeAsync(fullyQualifiedName, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeclarationNode>> GetNodesByKindAsync(DeclarationKind kind,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        const string sql = "SELECT * FROM nodes WHERE kind = $kind";

        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$kind", (int)kind);

        return await ReadNodesAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DeclarationNode>> GetNodesByFileAsync(string filePath,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        const string sql = "SELECT * FROM nodes WHERE file_path = $file_path";

        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$file_path", filePath);

        return await ReadNodesAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RelationshipEdge>> GetOutgoingEdgesAsync(string nodeId,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        const string sql = "SELECT * FROM edges WHERE source_id = $source_id";

        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$source_id", nodeId);

        return await ReadEdgesAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RelationshipEdge>> GetIncomingEdgesAsync(string nodeId,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        const string sql = "SELECT * FROM edges WHERE target_id = $target_id";

        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$target_id", nodeId);

        return await ReadEdgesAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RelationshipEdge>> GetEdgesByKindAsync(RelationshipKind kind,
        CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        const string sql = "SELECT * FROM edges WHERE kind = $kind";

        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$kind", (int)kind);

        return await ReadEdgesAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        const string sql = """
                           DELETE FROM edges;
                           DELETE FROM nodes;
                           """;

        await ExecuteNonQueryAsync(sql, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DeclarationGraph> LoadGraphAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        var graph = new DeclarationGraph();

        // Load all nodes
        await using (var command = _connection!.CreateCommand())
        {
            command.CommandText = "SELECT * FROM nodes";
            var nodes = await ReadNodesAsync(command, cancellationToken);
            graph.AddNodes(nodes);
        }

        // Load all edges
        await using (var command = _connection.CreateCommand())
        {
            command.CommandText = "SELECT * FROM edges";
            var edges = await ReadEdgesAsync(command, cancellationToken);
            graph.AddEdges(edges);
        }

        return graph;
    }

    /// <inheritdoc />
    public async Task<int> GetNodeCountAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        await using var command = _connection!.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM nodes";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    /// <inheritdoc />
    public async Task<int> GetEdgeCountAsync(CancellationToken cancellationToken = default)
    {
        EnsureInitialized();

        await using var command = _connection!.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM edges";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }

    private void EnsureInitialized()
    {
        if (_connection == null)
        {
            throw new InvalidOperationException("Repository has not been initialized. Call InitializeAsync first.");
        }
    }

    private async Task ExecuteNonQueryAsync(string sql, CancellationToken cancellationToken)
    {
        await using var command = _connection!.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddNodeParameters(SqliteCommand command, DeclarationNode node)
    {
        command.Parameters.AddWithValue("$id", node.Id);
        command.Parameters.AddWithValue("$name", node.Name);
        command.Parameters.AddWithValue("$kind", (int)node.Kind);
        command.Parameters.AddWithValue("$file_path", node.FilePath);
        command.Parameters.AddWithValue("$start_line", node.StartLine);
        command.Parameters.AddWithValue("$start_column", node.StartColumn);
        command.Parameters.AddWithValue("$end_line", node.EndLine);
        command.Parameters.AddWithValue("$end_column", node.EndColumn);
        command.Parameters.AddWithValue("$c4_level", (int)node.C4Level);
        command.Parameters.AddWithValue("$c4_description", (object?)node.C4Description ?? DBNull.Value);
        command.Parameters.AddWithValue("$metadata", (object?)node.Metadata ?? DBNull.Value);
    }

    private static void AddEdgeParameters(SqliteCommand command, RelationshipEdge edge)
    {
        command.Parameters.AddWithValue("$id", edge.Id);
        command.Parameters.AddWithValue("$source_id", edge.SourceId);
        command.Parameters.AddWithValue("$target_id", edge.TargetId);
        command.Parameters.AddWithValue("$kind", (int)edge.Kind);
        command.Parameters.AddWithValue("$source_file_path", (object?)edge.SourceFilePath ?? DBNull.Value);
        command.Parameters.AddWithValue("$source_line", (object?)edge.SourceLine ?? DBNull.Value);
        command.Parameters.AddWithValue("$metadata", (object?)edge.Metadata ?? DBNull.Value);
    }

    private static async Task<List<DeclarationNode>> ReadNodesAsync(SqliteCommand command,
        CancellationToken cancellationToken)
    {
        var nodes = new List<DeclarationNode>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            nodes.Add(ReadNode(reader));
        }

        return nodes;
    }

    private static DeclarationNode ReadNode(SqliteDataReader reader)
    {
        return new DeclarationNode
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            Name = reader.GetString(reader.GetOrdinal("name")),
            Kind = (DeclarationKind)reader.GetInt32(reader.GetOrdinal("kind")),
            FilePath = reader.GetString(reader.GetOrdinal("file_path")),
            StartLine = reader.GetInt32(reader.GetOrdinal("start_line")),
            StartColumn = reader.GetInt32(reader.GetOrdinal("start_column")),
            EndLine = reader.GetInt32(reader.GetOrdinal("end_line")),
            EndColumn = reader.GetInt32(reader.GetOrdinal("end_column")),
            C4Level = (C4Level)reader.GetInt32(reader.GetOrdinal("c4_level")),
            C4Description = reader.IsDBNull(reader.GetOrdinal("c4_description"))
                ? null
                : reader.GetString(reader.GetOrdinal("c4_description")),
            Metadata = reader.IsDBNull(reader.GetOrdinal("metadata"))
                ? null
                : reader.GetString(reader.GetOrdinal("metadata"))
        };
    }

    private static async Task<List<RelationshipEdge>> ReadEdgesAsync(SqliteCommand command,
        CancellationToken cancellationToken)
    {
        var edges = new List<RelationshipEdge>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            edges.Add(ReadEdge(reader));
        }

        return edges;
    }

    private static RelationshipEdge ReadEdge(SqliteDataReader reader)
    {
        return new RelationshipEdge
        {
            Id = reader.GetString(reader.GetOrdinal("id")),
            SourceId = reader.GetString(reader.GetOrdinal("source_id")),
            TargetId = reader.GetString(reader.GetOrdinal("target_id")),
            Kind = (RelationshipKind)reader.GetInt32(reader.GetOrdinal("kind")),
            SourceFilePath = reader.IsDBNull(reader.GetOrdinal("source_file_path"))
                ? null
                : reader.GetString(reader.GetOrdinal("source_file_path")),
            SourceLine = reader.IsDBNull(reader.GetOrdinal("source_line"))
                ? null
                : reader.GetInt32(reader.GetOrdinal("source_line")),
            Metadata = reader.IsDBNull(reader.GetOrdinal("metadata"))
                ? null
                : reader.GetString(reader.GetOrdinal("metadata"))
        };
    }

    private const string CreateTablesScript = """
                                              CREATE TABLE IF NOT EXISTS nodes (
                                                  id TEXT PRIMARY KEY,
                                                  name TEXT NOT NULL,
                                                  kind INTEGER NOT NULL,
                                                  file_path TEXT NOT NULL,
                                                  start_line INTEGER NOT NULL,
                                                  start_column INTEGER NOT NULL,
                                                  end_line INTEGER NOT NULL,
                                                  end_column INTEGER NOT NULL,
                                                  c4_level INTEGER NOT NULL DEFAULT 0,
                                                  c4_description TEXT,
                                                  metadata TEXT
                                              );

                                              CREATE TABLE IF NOT EXISTS edges (
                                                  id TEXT PRIMARY KEY,
                                                  source_id TEXT NOT NULL,
                                                  target_id TEXT NOT NULL,
                                                  kind INTEGER NOT NULL,
                                                  source_file_path TEXT,
                                                  source_line INTEGER,
                                                  metadata TEXT,
                                                  FOREIGN KEY (source_id) REFERENCES nodes(id) ON DELETE CASCADE,
                                                  FOREIGN KEY (target_id) REFERENCES nodes(id) ON DELETE CASCADE
                                              );

                                              CREATE INDEX IF NOT EXISTS idx_nodes_kind ON nodes(kind);
                                              CREATE INDEX IF NOT EXISTS idx_nodes_file ON nodes(file_path);
                                              CREATE INDEX IF NOT EXISTS idx_nodes_name ON nodes(name);

                                              CREATE INDEX IF NOT EXISTS idx_edges_source ON edges(source_id);
                                              CREATE INDEX IF NOT EXISTS idx_edges_target ON edges(target_id);
                                              CREATE INDEX IF NOT EXISTS idx_edges_kind ON edges(kind);
                                              CREATE INDEX IF NOT EXISTS idx_edges_source_kind ON edges(source_id, kind);
                                              CREATE INDEX IF NOT EXISTS idx_edges_target_kind ON edges(target_id, kind);
                                              """;
}