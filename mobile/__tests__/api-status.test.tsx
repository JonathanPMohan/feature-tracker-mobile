// The API status line (M2, rules B15 and B16).
//
// These tests never touch the network. `fetch` is replaced with a Jest mock
// that answers however each test needs, so they check the component's
// logic, not your Wi-Fi. The real connection is checked by hand on the phone.

import { render, screen } from "@testing-library/react-native";

import { ApiStatus } from "@/components/ApiStatus";

// A fake fetch, created fresh for each test. jest.fn() records every call,
// so tests can check what URL was requested.
let fetchMock: jest.Mock;

beforeEach(() => {
  process.env.EXPO_PUBLIC_API_URL = "http://192.168.1.20:5080";
  fetchMock = jest.fn();
  // `globalThis` is the global object in any JavaScript environment (the
  // browser calls it `window`, Node calls it `global`). Replacing its fetch
  // makes every `fetch(...)` in the component call the mock instead.
  globalThis.fetch = fetchMock;
});

afterEach(() => {
  delete process.env.EXPO_PUBLIC_API_URL;
});

// A minimal stand-in for fetch's Response: the component only reads .status.
const respondWith = (status: number) => Promise.resolve({ status } as Response);

describe("<ApiStatus />", () => {
  test("says it's checking while the request is in flight", async () => {
    // A promise that never settles: the request stays "in flight" forever.
    fetchMock.mockReturnValue(new Promise(() => {}));

    await render(<ApiStatus />);

    // getBy...: the text must be there right after rendering.
    expect(screen.getByText("API: checking…")).toBeTruthy();
  });

  test("shows connected when /health answers 200", async () => {
    fetchMock.mockReturnValue(respondWith(200));

    await render(<ApiStatus />);

    // findBy... waits (up to a second by default) for the text to appear,
    // because the state updates after the fetch promise resolves.
    expect(await screen.findByText("API: connected")).toBeTruthy();
    // And it called the right URL, built from the env variable.
    expect(fetchMock).toHaveBeenCalledWith(
      "http://192.168.1.20:5080/health",
      expect.anything(), // the options object with the abort signal
    );
  });

  test("shows unreachable when /health answers 503 (database down)", async () => {
    fetchMock.mockReturnValue(respondWith(503));

    await render(<ApiStatus />);

    expect(await screen.findByText("API: unreachable")).toBeTruthy();
  });

  test("shows unreachable when the request fails (no network, wrong IP)", async () => {
    fetchMock.mockReturnValue(Promise.reject(new TypeError("Network request failed")));

    await render(<ApiStatus />);

    expect(await screen.findByText("API: unreachable")).toBeTruthy();
  });

  test("ignores a trailing slash in the configured URL", async () => {
    process.env.EXPO_PUBLIC_API_URL = "http://192.168.1.20:5080/";
    fetchMock.mockReturnValue(respondWith(200));

    await render(<ApiStatus />);

    await screen.findByText("API: connected");
    expect(fetchMock).toHaveBeenCalledWith("http://192.168.1.20:5080/health", expect.anything());
  });

  test("explains what's missing when EXPO_PUBLIC_API_URL isn't set", async () => {
    delete process.env.EXPO_PUBLIC_API_URL;

    await render(<ApiStatus />);

    // A regular expression (/.../): matches if the text contains it.
    expect(screen.getByText(/EXPO_PUBLIC_API_URL/)).toBeTruthy();
    // And it didn't guess an address and call it anyway.
    expect(fetchMock).not.toHaveBeenCalled();
  });
});
