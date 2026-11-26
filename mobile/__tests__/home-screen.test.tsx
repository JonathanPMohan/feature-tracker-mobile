// The first mobile test (M0, rule Z7).
//
// Where it lives: a __tests__ folder at the root of mobile/, NOT in src/app/.
// Expo Router turns every file in src/app/ into a route, so a test there
// would become a (broken) screen. Jest runs every file inside a __tests__
// folder.
//
// What runs it: Jest with the "jest-expo" preset (package.json → "jest").
// The preset makes React Native components render in Node, without a phone,
// by mocking the native parts. It's the same idea as Vitest's jsdom
// environment in your Next.js project: a stand-in for the real platform.
// So this checks what the screen renders, not how it looks.

// `render` draws a component, and `screen` queries what it drew. These are
// the same names as React Testing Library on the web, which your Next.js
// project could have used. The queries work the same way too.
import { render, screen } from "@testing-library/react-native";

// "@/" means mobile/src/ (set up in tsconfig.json "paths"), so this is
// src/app/index.tsx: the home screen's default export.
import HomeScreen from "@/app/index";

// describe/test/expect are Jest globals, just like Vitest's.
describe("<HomeScreen />", () => {
  test("shows the app's title as a heading", async () => {
    // In React Native Testing Library 14 (required by SDK 58), render is
    // async, so it's awaited.
    await render(<HomeScreen />);

    // Find the element by its role and accessible name: the way VoiceOver
    // would describe it ("Feature Tracker, heading"). This is the preferred
    // query because it tests what a user perceives, not how the code is
    // structured. If someone removes accessibilityRole="header", this test
    // fails even though the text is still there.
    //
    // getBy... throws if nothing (or more than one thing) matches, so the
    // query alone is already an assertion. The expect() makes the intent
    // readable.
    expect(
      screen.getByRole("header", { name: "Feature Tracker" }),
    ).toBeTruthy();
  });
});
