import { create } from 'zustand';
import * as SecureStore from 'expo-secure-store';
import { Platform } from 'react-native';
import type { User } from '../types/api';

const TOKEN_KEY = 'auth_token';
const USER_KEY = 'auth_user';
const LOCATION_KEY = 'active_location_id';

const storage = {
  getItem: (key: string) =>
    Platform.OS === 'web'
      ? Promise.resolve(localStorage.getItem(key))
      : SecureStore.getItemAsync(key),
  setItem: (key: string, value: string) =>
    Platform.OS === 'web'
      ? Promise.resolve(void localStorage.setItem(key, value))
      : SecureStore.setItemAsync(key, value),
  deleteItem: (key: string) =>
    Platform.OS === 'web'
      ? Promise.resolve(void localStorage.removeItem(key))
      : SecureStore.deleteItemAsync(key),
};

interface AuthState {
  token: string | null;
  user: User | null;
  activeLocationId: string | null;
  isHydrated: boolean;
  setAuth: (token: string, user: User) => Promise<void>;
  setActiveLocation: (locationId: string) => Promise<void>;
  logout: () => Promise<void>;
  hydrate: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set) => ({
  token: null,
  user: null,
  activeLocationId: null,
  isHydrated: false,

  setAuth: async (token, user) => {
    await storage.setItem(TOKEN_KEY, token);
    await storage.setItem(USER_KEY, JSON.stringify(user));
    set({ token, user });
  },

  setActiveLocation: async (locationId) => {
    await storage.setItem(LOCATION_KEY, locationId);
    set({ activeLocationId: locationId });
  },

  logout: async () => {
    await storage.deleteItem(TOKEN_KEY);
    await storage.deleteItem(USER_KEY);
    await storage.deleteItem(LOCATION_KEY);
    set({ token: null, user: null, activeLocationId: null });
  },

  hydrate: async () => {
    const token = await storage.getItem(TOKEN_KEY);
    const userJson = await storage.getItem(USER_KEY);
    const locationId = await storage.getItem(LOCATION_KEY);
    if (token && userJson) {
      set({
        token,
        user: JSON.parse(userJson) as User,
        activeLocationId: locationId,
      });
    }
    set({ isHydrated: true });
  },
}));
