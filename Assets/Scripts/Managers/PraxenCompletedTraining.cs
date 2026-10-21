using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Passes only the just-finished training session to the normal phone menu.
/// It is deliberately transient: no account history or headset view is kept.
/// Results are captured from the same object saved by PraxenCloud.Submit,
/// so the screen works offline without requesting or waiting for the API.
/// </summary>
public static class PraxenCompletedTraining
{
    private static SessionResultData pending;

    public static void Present(SessionResultData session)
    {
        pending = session;
    }

    public static bool TryTake(out SessionResultData session)
    {
        session = pending;
        pending = null;
        return session != null;
    }

    public static string Duration(SessionResultData session)
    {
        if (session == null ||
            !DateTimeOffset.TryParse(session.startedAt, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTimeOffset started) ||
            !DateTimeOffset.TryParse(session.endedAt, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateTimeOffset ended))
            return "Duración no disponible";

        double seconds = (ended - started).TotalSeconds;
        if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
            return "Duración no disponible";

        int totalSeconds = Math.Max(0, (int)Math.Round(seconds));
        return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00} min",
            totalSeconds / 60, totalSeconds % 60);
    }

    public static List<PraxenResultCard> Cards(SessionResultData session)
    {
        var cards = new List<PraxenResultCard>();
        if (session == null) return cards;

        if (session.exercises != null && session.exercises.Count > 0)
        {
            int hits = 0, misses = 0;
            double weightedReaction = 0;
            int timedHits = 0;
            foreach (AimLabResult entry in session.exercises)
            {
                if (entry == null || entry.metrics == null) continue;
                int count = Math.Max(0, entry.metrics.hits);
                hits += count;
                misses += Math.Max(0, entry.metrics.misses);
                if (count > 0 && entry.metrics.averageReactionSeconds > 0)
                {
                    weightedReaction += count * entry.metrics.averageReactionSeconds;
                    timedHits += count;
                }
            }

            cards.Add(new PraxenResultCard(
                "MANOS", hits.ToString(CultureInfo.InvariantCulture), "Aciertos",
                "Precisión " + Accuracy(hits, misses) + "   ·   " + misses + " fallos",
                timedHits > 0
                    ? "Reacción media " + DecimalSeconds(weightedReaction / timedHits)
                    : "Sin tiempo de reacción registrado"));
        }

        if (session.footExercises != null && session.footExercises.Count > 0)
        {
            int hits = 0, misses = 0;
            double weightedReaction = 0;
            int timedHits = 0;
            foreach (FootExerciseResult entry in session.footExercises)
            {
                if (entry == null || entry.metrics == null) continue;
                int count = Math.Max(0, entry.metrics.hits);
                hits += count;
                misses += Math.Max(0, entry.metrics.misses);
                if (count > 0 && entry.metrics.averageReactionSeconds > 0)
                {
                    weightedReaction += count * entry.metrics.averageReactionSeconds;
                    timedHits += count;
                }
            }

            cards.Add(new PraxenResultCard(
                "PIES", hits.ToString(CultureInfo.InvariantCulture), "Aciertos",
                "Precisión " + Accuracy(hits, misses) + "   ·   " + misses + " fallos",
                timedHits > 0
                    ? "Reacción media " + DecimalSeconds(weightedReaction / timedHits)
                    : "Sin tiempo de reacción registrado"));
        }

        if (session.runningExercises != null && session.runningExercises.Count > 0)
        {
            int completed = 0, cues = 0, misses = 0;
            double weightedTime = 0;
            int timedTurns = 0;
            foreach (RunningExerciseResult entry in session.runningExercises)
            {
                if (entry == null || entry.metrics == null) continue;
                int count = Math.Max(0, entry.metrics.completedTurns);
                completed += count;
                cues += Math.Max(0, entry.metrics.cuesIssued);
                misses += Math.Max(0, entry.metrics.misses);
                if (count > 0 && entry.metrics.averageResponseSeconds > 0)
                {
                    weightedTime += count * entry.metrics.averageResponseSeconds;
                    timedTurns += count;
                }
            }

            cards.Add(new PraxenResultCard(
                "CORRER", completed.ToString(CultureInfo.InvariantCulture), "Giros completados",
                cues + " indicaciones   ·   " + misses + " fallos",
                timedTurns > 0
                    ? "Respuesta media " + DecimalSeconds(weightedTime / timedTurns)
                    : "Sin tiempos de respuesta registrados"));
        }

        return cards;
    }

    private static string Accuracy(int hits, int misses)
    {
        int attempts = hits + misses;
        return attempts > 0
            ? (100d * hits / attempts).ToString("0.#", CultureInfo.InvariantCulture) + "%"
            : "sin intentos";
    }

    private static string DecimalSeconds(double seconds)
    {
        return seconds.ToString("0.00", CultureInfo.InvariantCulture) + " s";
    }
}

public sealed class PraxenResultCard
{
    public readonly string Title;
    public readonly string PrimaryValue;
    public readonly string PrimaryLabel;
    public readonly string DetailOne;
    public readonly string DetailTwo;

    public PraxenResultCard(string title, string primaryValue, string primaryLabel,
        string detailOne, string detailTwo)
    {
        Title = title;
        PrimaryValue = primaryValue;
        PrimaryLabel = primaryLabel;
        DetailOne = detailOne;
        DetailTwo = detailTwo;
    }
}
