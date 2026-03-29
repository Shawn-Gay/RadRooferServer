import { Redirect, Stack } from 'expo-router';
import { useAuthStore } from '../../src/store/auth';

export default function AuthLayout() {
  const token = useAuthStore((o) => o.token);

  if (token) return <Redirect href="/(app)" />;

  return <Stack screenOptions={{ headerShown: false }} />;
}
