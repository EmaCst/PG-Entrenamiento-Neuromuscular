using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// Typed contracts for the five authenticated athlete-only endpoints.
// Never cache a response across account/server changes; the cloud service
// checks the login generation both before and after every network request.
[Serializable] public class PraxenApiMetric
{
    public string key, label, unit, direction;
    public float average, min, max;
    public int samples;
}
[Serializable] public class PraxenApiExerciseSummary
{
    public string exerciseType;
    public int sessions, athletes, results;
    public PraxenApiMetricFields metrics;
}
[Serializable] public class PraxenApiMetricFields
{
    public PraxenApiMetric accuracyPercent, hits, misses, score;
    public PraxenApiMetric completedTurns, cuesIssued, averageResponseSeconds;
    public PraxenApiMetric averageReactionSeconds, bestReactionSeconds;
}
[Serializable] public class PraxenApiSummary
{
    public int totalSessions, athleteCount;
    public float averageDurationSeconds;
    public string firstSessionAt, lastSessionAt, aggregationBasis;
    public PraxenApiExerciseSummary[] exercises;
}
[Serializable] public class PraxenApiSummaryResponse
{
    public PraxenApiSummary summary;
}
[Serializable] public class PraxenApiLastResponse
{
    public SessionResultData latest;
    public PraxenApiSessionMetrics metrics;
}
[Serializable] public class PraxenApiSessionMetrics
{
    public string sessionId;
    public int athleteId;
    public string startedAt, endedAt, mode;
    public float durationSeconds;
}
[Serializable] public class PraxenApiEvolutionPoint
{
    public string sessionId, startedAt, period;
    public int athleteId;
    public float value, change, improvement, improvementPercent;
    public bool improved;
}
[Serializable] public class PraxenApiEvolutionMetric
{
    public string key, label, unit, direction;
}
[Serializable] public class PraxenApiEvolutionSeries
{
    public string exerciseType;
    public PraxenApiEvolutionMetric metric;
    public PraxenApiEvolutionPoint[] points;
}
[Serializable] public class PraxenApiEvolution
{
    public string granularity, aggregationBasis;
    public PraxenApiEvolutionSeries[] series;
}
[Serializable] public class PraxenApiEvolutionResponse
{
    public PraxenApiEvolution evolution;
}
[Serializable] public class PraxenApiSessionsResponse
{
    public int total, limit, offset;
    public SessionResultData[] sesiones;
}
[Serializable] public class PraxenApiMetadataResponse
{
    public string[] exerciseTypes;
}

public sealed class PraxenResultsApi
{
    private readonly PraxenCloud cloud;

    public PraxenResultsApi(PraxenCloud owner) { cloud = owner; }

    // The API identifies the athlete using the JWT. No caller can inject
    // another athleteId into these URLs.
    public IEnumerator GetLatest(Action<PraxenApiLastResponse, string> done,
        string exerciseType = null)
    {
        yield return Request("ultima", exerciseType, null,
            text => JsonUtility.FromJson<PraxenApiLastResponse>(text), done);
    }
    public IEnumerator GetSummary(Action<PraxenApiSummaryResponse, string> done,
        string exerciseType = null)
    {
        yield return Request("resumen", exerciseType, null,
            text => JsonUtility.FromJson<PraxenApiSummaryResponse>(text), done);
    }
    public IEnumerator GetEvolution(Action<PraxenApiEvolutionResponse, string> done,
        string exerciseType = null, string metric = null)
    {
        yield return Request("evolucion", exerciseType, metric,
            text => JsonUtility.FromJson<PraxenApiEvolutionResponse>(text), done);
    }
    public IEnumerator GetSessions(Action<PraxenApiSessionsResponse, string> done,
        string exerciseType = null, int offset = 0)
    {
        yield return Request("sesiones", exerciseType, null,
            text => JsonUtility.FromJson<PraxenApiSessionsResponse>(text), done, offset);
    }
    public IEnumerator GetMetadata(Action<PraxenApiMetadataResponse, string> done)
    {
        yield return Request("metricas", null, null,
            text => JsonUtility.FromJson<PraxenApiMetadataResponse>(text), done);
    }

    private IEnumerator Request<T>(string endpoint, string exerciseType, string metric,
        Func<string, T> parse, Action<T, string> done, int offset = 0) where T : class
    {
        if (!cloud.TryGetAthleteCredentials(out string server, out string jwt,
            out int athleteId, out int revision))
        {
            done?.Invoke(null, "Inicia sesión para consultar tu historial.");
            yield break;
        }
        string url = server + "/api/resultados/me/" + endpoint;
        var options = new List<string>();
        if (!string.IsNullOrEmpty(exerciseType))
            options.Add("exerciseType=" + UnityWebRequest.EscapeURL(exerciseType));
        if (!string.IsNullOrEmpty(metric))
            options.Add("metric=" + UnityWebRequest.EscapeURL(metric));
        if (endpoint == "evolucion") options.Add("limit=30");
        if (endpoint == "sesiones")
        { options.Add("limit=20"); options.Add("offset=" + Math.Max(0, offset)); }
        if (options.Count > 0) url += "?" + string.Join("&", options);

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.timeout = 20;
            request.SetRequestHeader("Authorization", "Bearer " + jwt);
            yield return request.SendWebRequest();

            // A login/logout or account switch while loading MUST NOT expose
            // the other account's result to the currently displayed profile.
            if (!cloud.MatchesAthleteCredentials(server, athleteId, revision))
                yield break;

            if (request.result != UnityWebRequest.Result.Success)
            {
                string message = request.responseCode == 401
                    ? "Sesión caducada. Vuelve a iniciar sesión."
                    : request.responseCode == 404
                        ? "Los resultados todavía no están disponibles."
                        : request.responseCode == 0
                            ? "Sin conexión. Puedes volver a intentarlo."
                            : request.responseCode >= 500
                                ? "El servidor no está disponible ahora."
                                : "No fue posible consultar los resultados (" + request.responseCode + ").";
                done?.Invoke(null, message);
                yield break;
            }
            try
            {
                T parsed = parse(request.downloadHandler.text);
                done?.Invoke(parsed, parsed == null ? "Respuesta vacía del servidor." : null);
            }
            catch (Exception)
            {
                done?.Invoke(null, "No se pudieron interpretar los resultados.");
            }
        }
    }
}

