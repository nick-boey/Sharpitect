using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers;

namespace Sharpitect.Analysis.Embedding;

/// <summary>
/// ONNX-based embedding service using sentence transformer models.
/// Default model: all-MiniLM-L6-v2 (384 dimensions).
/// </summary>
public sealed class OnnxEmbeddingService : IEmbeddingService
{
    private readonly InferenceSession _session;
    private readonly BertTokenizer _tokenizer;
    private readonly int _maxSequenceLength;
    private bool _disposed;

    /// <summary>
    /// Default embedding dimension for all-MiniLM-L6-v2.
    /// </summary>
    public const int DefaultDimension = 384;

    /// <summary>
    /// Default maximum sequence length.
    /// </summary>
    public const int DefaultMaxSequenceLength = 256;

    /// <inheritdoc />
    public int Dimension { get; }

    /// <summary>
    /// Creates a new ONNX embedding service.
    /// </summary>
    /// <param name="modelPath">Path to the ONNX model file.</param>
    /// <param name="vocabPath">Path to the tokenizer vocabulary file.</param>
    /// <param name="dimension">The embedding dimension. Default: 384.</param>
    /// <param name="maxSequenceLength">Maximum sequence length. Default: 256.</param>
    public OnnxEmbeddingService(
        string modelPath,
        string vocabPath,
        int dimension = DefaultDimension,
        int maxSequenceLength = DefaultMaxSequenceLength)
    {
        ArgumentException.ThrowIfNullOrEmpty(modelPath);
        ArgumentException.ThrowIfNullOrEmpty(vocabPath);

        if (!File.Exists(modelPath))
        {
            throw new FileNotFoundException($"ONNX model not found: {modelPath}", modelPath);
        }

        if (!File.Exists(vocabPath))
        {
            throw new FileNotFoundException($"Vocabulary file not found: {vocabPath}", vocabPath);
        }

        Dimension = dimension;
        _maxSequenceLength = maxSequenceLength;

        var sessionOptions = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL
        };

        _session = new InferenceSession(modelPath, sessionOptions);
        _tokenizer = BertTokenizer.Create(vocabPath);
    }

    /// <inheritdoc />
    public Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(text);

        cancellationToken.ThrowIfCancellationRequested();

        var embedding = GenerateEmbedding(text);
        return Task.FromResult(embedding);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(texts);

        var textList = texts.ToList();
        var embeddings = new List<float[]>(textList.Count);

        foreach (var text in textList)
        {
            cancellationToken.ThrowIfCancellationRequested();
            embeddings.Add(GenerateEmbedding(text));
        }

        return Task.FromResult<IReadOnlyList<float[]>>(embeddings);
    }

    private float[] GenerateEmbedding(string text)
    {
        // Tokenize the input text
        var encoding = _tokenizer.EncodeToIds(
            text,
            _maxSequenceLength,
            out _,
            out _);

        var inputIds = encoding.ToArray();
        var attentionMask = new long[inputIds.Length];
        var tokenTypeIds = new long[inputIds.Length];

        for (int i = 0; i < inputIds.Length; i++)
        {
            attentionMask[i] = 1;
            tokenTypeIds[i] = 0;
        }

        // Create tensors
        var inputIdsTensor = new DenseTensor<long>(
            inputIds.Select(x => (long)x).ToArray(),
            [1, inputIds.Length]);

        var attentionMaskTensor = new DenseTensor<long>(
            attentionMask,
            [1, attentionMask.Length]);

        var tokenTypeIdsTensor = new DenseTensor<long>(
            tokenTypeIds,
            [1, tokenTypeIds.Length]);

        // Create input container
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids", inputIdsTensor),
            NamedOnnxValue.CreateFromTensor("attention_mask", attentionMaskTensor),
            NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIdsTensor)
        };

        // Run inference
        using var results = _session.Run(inputs);

        // Get the output tensor (last hidden state or sentence embedding)
        var outputTensor = results.First().AsTensor<float>();

        // Mean pooling over the sequence dimension
        var embedding = MeanPooling(outputTensor, attentionMask);

        // Normalize the embedding
        return Normalize(embedding);
    }

    private float[] MeanPooling(Tensor<float> lastHiddenState, long[] attentionMask)
    {
        var dimensions = lastHiddenState.Dimensions;
        var sequenceLength = dimensions[1];
        var hiddenSize = dimensions[2];

        var pooled = new float[hiddenSize];
        var maskSum = attentionMask.Sum();

        if (maskSum == 0)
        {
            return pooled;
        }

        for (int h = 0; h < hiddenSize; h++)
        {
            float sum = 0;
            for (int s = 0; s < sequenceLength; s++)
            {
                if (attentionMask[s] == 1)
                {
                    sum += lastHiddenState[0, s, h];
                }
            }
            pooled[h] = sum / maskSum;
        }

        return pooled;
    }

    private static float[] Normalize(float[] vector)
    {
        var magnitude = (float)Math.Sqrt(vector.Sum(x => x * x));

        if (magnitude == 0)
        {
            return vector;
        }

        var normalized = new float[vector.Length];
        for (int i = 0; i < vector.Length; i++)
        {
            normalized[i] = vector[i] / magnitude;
        }

        return normalized;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.Dispose();
        _disposed = true;
    }
}
