// A one-line status: can the phone reach the API? (M2, rule B16)
//
// It calls GET /health once when it appears. M5 introduces TanStack Query
// for real data. This plain fetch shows what that library will take care of
// for us: loading state, errors, timeouts and cleanup.

import { useEffect, useState } from "react";
import { StyleSheet, Text } from "react-native";

import { getApiUrl } from "@/api/config";

// A union of string literals: the only three values `status` can hold.
// (C# would use an enum.)
type Status = "checking" | "connected" | "unreachable";

// How long to wait before calling the API unreachable. Without a limit, a
// wrong IP address can leave the request hanging for a minute or more.
const TIMEOUT_MS = 5000;

export function ApiStatus() {
  const apiUrl = getApiUrl();
  const [status, setStatus] = useState<Status>("checking");

  useEffect(() => {
    if (apiUrl === null) {
      return; // nothing to call; the render below explains why
    }

    // `cancelled` is set when the component goes away (or apiUrl changes),
    // so a late answer can't update a screen that's no longer there.
    let cancelled = false;
    // AbortController cancels a fetch: here, after TIMEOUT_MS.
    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), TIMEOUT_MS);

    fetch(`${apiUrl}/health`, { signal: controller.signal })
      .then((response) => {
        if (!cancelled) {
          // Exactly 200 means healthy. 503 (database down) or anything
          // else means the API can't do its job, so: unreachable.
          setStatus(response.status === 200 ? "connected" : "unreachable");
        }
      })
      .catch(() => {
        // No network, wrong address, timeout (the abort lands here too).
        if (!cancelled) {
          setStatus("unreachable");
        }
      })
      .finally(() => clearTimeout(timer));

    // The cleanup function: React runs it when the component unmounts.
    return () => {
      cancelled = true;
      clearTimeout(timer);
      controller.abort();
    };
  }, [apiUrl]);

  if (apiUrl === null) {
    return (
      <Text style={[styles.text, styles.problem]}>
        API address not set: add EXPO_PUBLIC_API_URL to mobile/.env.local
      </Text>
    );
  }

  const label = {
    checking: "API: checking…",
    connected: "API: connected",
    unreachable: "API: unreachable",
  }[status];

  return (
    <Text style={[styles.text, status === "unreachable" && styles.problem]}>{label}</Text>
  );
}

// `style` accepts an array; later entries win, and `false` entries are
// skipped. That's how the red color is added only when there's a problem.
const styles = StyleSheet.create({
  text: {
    marginTop: 16,
    fontSize: 14,
    color: "#666",
    textAlign: "center",
  },
  problem: {
    color: "#b42318",
  },
});
