// The home screen. With Expo Router, every file in src/app/ is a screen, and
// its path is its route: this file is "/", like app/page.tsx in Next.js.
// M5 replaces this placeholder with the board.

// React Native has no HTML. Instead of <div> and <p>, you use components that
// map to native iOS and Android views:
//   <View>  ≈ <div>   a box for layout
//   <Text>  ≈ <p>     ALL text must be inside <Text>; a bare string in a
//                     <View> is an error, unlike in a <div>
import { StyleSheet, Text, View } from "react-native";

// A route file's default export is the screen, as in Next.js.
export default function HomeScreen() {
  return (
    <View style={styles.container}>
      {/* accessibilityRole="header" tells VoiceOver this is a heading, the
          way <h1> does on the web. The test finds the title by this role,
          so the test also proves the screen is accessible. */}
      <Text accessibilityRole="header" style={styles.title}>
        Feature Tracker
      </Text>
      <Text style={styles.subtitle}>Feature requests, on your phone.</Text>
    </View>
  );
}

// Styles are JavaScript objects, not CSS. There's no cascade and no class
// names: each component gets its style through the `style` prop.
// StyleSheet.create checks the keys and gives you autocompletion.
// Numbers are density-independent points, so there's no "px".
// Layout is flexbox by default, but with flexDirection: "column", the
// opposite of the web's default.
const styles = StyleSheet.create({
  container: {
    flex: 1, // fill the whole screen (like height: 100%)
    alignItems: "center",
    justifyContent: "center", // centered content stays clear of the notch
    padding: 24,
    gap: 8,
  },
  title: {
    fontSize: 32,
    fontWeight: "700",
  },
  subtitle: {
    fontSize: 16,
    color: "#666",
  },
});
