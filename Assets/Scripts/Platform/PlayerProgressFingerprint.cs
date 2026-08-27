using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ThreeDoorsOfFate.Platform
{
    public static class PlayerProgressFingerprint
    {
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        public static string ComputeContentHash(string json)
        {
            PlayerProgressSnapshot snapshot = JsonUtility.FromJson<PlayerProgressSnapshot>(json);
            if (snapshot == null || snapshot.schemaVersion != PlayerProgressSnapshot.CurrentSchemaVersion)
            {
                throw new InvalidOperationException("Unsupported player progress schema.");
            }

            StringBuilder canonical = new();
            canonical.Append("schema:").Append(snapshot.schemaVersion).Append('\n');
            AppendToken(canonical, "active-run-id", "runId", snapshot.activeRunId ?? string.Empty);
            AppendToken(
                canonical,
                "active-run-schema",
                "version",
                snapshot.activeRunSchemaVersion.ToString());
            AppendToken(
                canonical,
                "active-run-cursor",
                "cursor",
                snapshot.activeRunRandomCursor.ToString());
            foreach (ProgressIntValue entry in (snapshot.integers ?? new List<ProgressIntValue>())
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.key))
                .OrderBy(entry => entry.key, StringComparer.Ordinal))
            {
                AppendToken(canonical, "int", entry.key, entry.value.ToString());
            }

            foreach (ProgressStringValue entry in (snapshot.strings ?? new List<ProgressStringValue>())
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.key))
                .OrderBy(entry => entry.key, StringComparer.Ordinal))
            {
                AppendToken(canonical, "string", entry.key, entry.value ?? string.Empty);
            }

            ulong hash = FnvOffsetBasis;
            foreach (byte value in Encoding.UTF8.GetBytes(canonical.ToString()))
            {
                hash ^= value;
                hash *= FnvPrime;
            }

            return hash.ToString("x16");
        }

        private static void AppendToken(StringBuilder builder, string kind, string key, string value)
        {
            builder.Append(kind)
                .Append(':')
                .Append(key.Length)
                .Append(':')
                .Append(key)
                .Append(':')
                .Append(value.Length)
                .Append(':')
                .Append(value)
                .Append('\n');
        }
    }
}
