// The root layout: it wraps every screen, like app/layout.tsx in Next.js.
// <Stack> is a navigator. Opening a screen pushes it on top with the iOS
// slide-in animation, and the back button or swipe pops it.
import { Stack } from "expo-router";

export default function RootLayout() {
  return (
    <Stack>
      {/* Without this line, the home screen's header bar would show the
          file name, "index". The screen draws its own title, so the header
          is hidden. Screens added later (a request's page) will show it,
          with a back button. */}
      <Stack.Screen name="index" options={{ headerShown: false }} />
    </Stack>
  );
}
