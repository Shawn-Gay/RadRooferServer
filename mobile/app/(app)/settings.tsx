import { useState, useEffect } from 'react';
import {
  View, Text, TouchableOpacity, StyleSheet, Alert,
  TextInput, ScrollView, ActivityIndicator,
} from 'react-native';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useAuthStore } from '../../src/store/auth';
import { api } from '../../src/api/client';
import type { Location } from '../../src/types/api';

function IntegrationsSection({ location }: { location: Location }) {
  const queryClient = useQueryClient();
  const [calendarId, setCalendarId] = useState(location.calendarId ?? '');

  useEffect(() => {
    setCalendarId(location.calendarId ?? '');
  }, [location.calendarId]);

  const { mutate: save, isPending } = useMutation({
    mutationFn: () => api.updateIntegrations(location.id, calendarId),
    onSuccess: (result) => {
      queryClient.setQueryData<{ items: Location[] }>(['locations'], (old) => {
        if (!old) return old;
        return {
          items: old.items.map((o) =>
            o.id === result.id ? { ...o, calendarId: result.calendarId } : o
          ),
        };
      });
      Alert.alert('Saved', 'Integration settings updated.');
    },
  });

  const isDirty = calendarId !== (location.calendarId ?? '');

  return (
    <View style={styles.card}>
      <Text style={styles.cardTitle}>Integrations — {location.name}</Text>

      <Text style={styles.fieldLabel}>Google Calendar ID</Text>
      <TextInput
        style={styles.input}
        value={calendarId}
        onChangeText={setCalendarId}
        placeholder="e.g. primary or abc123@group.calendar.google.com"
        placeholderTextColor="#94A3B8"
        autoCapitalize="none"
        autoCorrect={false}
      />
      <Text style={styles.hint}>
        Leave blank to use the platform default calendar.
      </Text>

      <TouchableOpacity
        style={[styles.saveButton, !isDirty && styles.saveButtonDisabled]}
        onPress={() => save()}
        disabled={!isDirty || isPending}
      >
        {isPending
          ? <ActivityIndicator size="small" color="#FFFFFF" />
          : <Text style={styles.saveButtonText}>Save</Text>}
      </TouchableOpacity>
    </View>
  );
}

export default function SettingsScreen() {
  const { user, logout, activeLocationId } = useAuthStore();
  const queryClient = useQueryClient();

  const { data: locationsData } = useQuery({
    queryKey: ['locations'],
    queryFn: () => api.getLocations(),
  });

  const activeLocation: Location | null =
    locationsData?.items.find((o) => o.id === activeLocationId) ?? null;

  function confirmLogout() {
    Alert.alert('Sign Out', 'Are you sure you want to sign out?', [
      { text: 'Cancel', style: 'cancel' },
      {
        text: 'Sign Out',
        style: 'destructive',
        onPress: async () => {
          queryClient.clear();
          await logout();
        },
      },
    ]);
  }

  return (
    <ScrollView style={styles.container} contentContainerStyle={styles.content}>
      <Text style={styles.heading}>Settings</Text>

      <View style={styles.card}>
        <Text style={styles.cardTitle}>Account</Text>
        <Text style={styles.label}>Name</Text>
        <Text style={styles.value}>{user?.fullName}</Text>
        <View style={styles.divider} />
        <Text style={styles.label}>Email</Text>
        <Text style={styles.value}>{user?.email}</Text>
        <View style={styles.divider} />
        <Text style={styles.label}>Company</Text>
        <Text style={styles.value}>{user?.tenantName}</Text>
        <View style={styles.divider} />
        <Text style={styles.label}>Role</Text>
        <Text style={styles.value}>{user?.role}</Text>
      </View>

      {activeLocation ? (
        <IntegrationsSection location={activeLocation} />
      ) : (
        <View style={styles.card}>
          <Text style={styles.cardTitle}>Integrations</Text>
          <Text style={styles.noLocation}>
            Select a location in the Locations tab to configure integrations.
          </Text>
        </View>
      )}

      <TouchableOpacity style={styles.logoutButton} onPress={confirmLogout}>
        <Text style={styles.logoutText}>Sign Out</Text>
      </TouchableOpacity>
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F8FAFC' },
  content: { padding: 20, paddingBottom: 40 },
  heading: { fontSize: 26, fontWeight: '700', color: '#1E293B', marginBottom: 20 },
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
    marginBottom: 16,
  },
  cardTitle: {
    fontSize: 13,
    fontWeight: '600',
    color: '#94A3B8',
    textTransform: 'uppercase',
    letterSpacing: 0.5,
    marginBottom: 12,
  },
  label: { fontSize: 11, fontWeight: '600', color: '#94A3B8', textTransform: 'uppercase', letterSpacing: 0.5 },
  value: { fontSize: 15, color: '#1E293B', marginTop: 3 },
  divider: { height: 1, backgroundColor: '#F1F5F9', marginVertical: 12 },
  fieldLabel: { fontSize: 12, fontWeight: '600', color: '#475569', marginBottom: 6 },
  input: {
    height: 44,
    borderWidth: 1,
    borderColor: '#E2E8F0',
    borderRadius: 8,
    paddingHorizontal: 12,
    fontSize: 14,
    color: '#1E293B',
    backgroundColor: '#F8FAFC',
  },
  hint: { fontSize: 11, color: '#94A3B8', marginTop: 5, marginBottom: 14 },
  saveButton: {
    height: 40,
    backgroundColor: '#2563EB',
    borderRadius: 8,
    justifyContent: 'center',
    alignItems: 'center',
  },
  saveButtonDisabled: { backgroundColor: '#BFDBFE' },
  saveButtonText: { color: '#FFFFFF', fontSize: 14, fontWeight: '600' },
  noLocation: { fontSize: 13, color: '#94A3B8', textAlign: 'center', paddingVertical: 8 },
  logoutButton: {
    height: 48,
    backgroundColor: '#FEF2F2',
    borderRadius: 10,
    justifyContent: 'center',
    alignItems: 'center',
    borderWidth: 1,
    borderColor: '#FECACA',
    marginTop: 8,
  },
  logoutText: { color: '#DC2626', fontSize: 15, fontWeight: '600' },
});
