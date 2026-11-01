namespace TutorMatch.Api.Features.SemanticSearch.Services;

/// <summary>
/// Utilitar matematic pentru calculul similaritatii vectoriale si serializarea/deserializarea vectorilor de embeddings.
/// </summary>
public static class VectorMath
{
    private const float Epsilon = 1e-9f;

    /// <summary>
    /// Calculeaza similaritatea cosinus intre doi vectori de numere in virgula mobila:
    /// dot_product(a, b) / (norm(a) * norm(b)).
    /// </summary>
    /// <param name="a">Primul vector.</param>
    /// <param name="b">Al doilea vector.</param>
    /// <returns>Scorul de similaritate in intervalul [-1, 1], sau 0 daca unul dintre vectori este nul, gol, de lungimi diferite sau cu norma zero.</returns>
    public static float CosineSimilarity(float[]? a, float[]? b)
    {
        if (a is null || b is null || a.Length == 0 || b.Length == 0 || a.Length != b.Length)
        {
            return 0f;
        }

        float dotProduct = 0f;
        float normA = 0f;
        float normB = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        if (normA <= Epsilon || normB <= Epsilon)
        {
            return 0f;
        }

        float similarity = dotProduct / (MathF.Sqrt(normA) * MathF.Sqrt(normB));

        // Clampare pentru prevenirea erorilor de precizie in virgula mobila (ex. 1.0000001f sau -1.0000001f)
        return Math.Clamp(similarity, -1.0f, 1.0f);
    }

    /// <summary>
    /// Serializeaza un vector float[] intr-o reprezentare binara byte[] (BLOB).
    /// </summary>
    /// <param name="vector">Vectorul de numere float.</param>
    /// <returns>Tabloul de octeti corespunzator.</returns>
    public static byte[] Serialize(float[]? vector)
    {
        if (vector is null || vector.Length == 0)
        {
            return Array.Empty<byte>();
        }

        byte[] bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>
    /// Deserializeaza o reprezentare binara byte[] (BLOB) intr-un vector float[].
    /// </summary>
    /// <param name="bytes">Tabloul de octeti.</param>
    /// <returns>Vectorul float[] corespunzator.</returns>
    public static float[] Deserialize(byte[]? bytes)
    {
        if (bytes is null || bytes.Length == 0)
        {
            return Array.Empty<float>();
        }

        int floatCount = bytes.Length / sizeof(float);
        float[] vector = new float[floatCount];
        Buffer.BlockCopy(bytes, 0, vector, 0, floatCount * sizeof(float));
        return vector;
    }
}
