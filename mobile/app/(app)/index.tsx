import {
  View, Text, ScrollView, StyleSheet,
  ActivityIndicator, RefreshControl, Switch,
} from 'react-native';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../src/api/client';
import { useAuthStore } from '../../src/store/auth';
import type { Location } from '../../src/types/api';

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <View style={styles.statCard}>
      <Text style={styles.statValue}>{value}</Text>
      <Text style={styles.statLabel}>{label}</Text>
    </View>
  );
}

function VapiToggle({ location }: { location: Location }) {
  const queryClient = useQueryClient();

  const { mutate, isPending } = useMutation({
    mutationFn: (enabled: boolean) => api.toggleAssistant(location.id, enabled),
    onSuccess: (result) => {
      queryClient.setQueryData<{ items: Location[] }>(['locations'], (old) => {
        if (!old) return old;
        return {
          items: old.items.map((o) =>
            o.id === result.id ? { ...o, vapiEnabled: result.vapiEnabled } : o
          ),
        };
      });
    },
  });

  const isOn = location.vapiEnabled;

  return (
    <View style={[styles.toggleCard, isOn ? styles.toggleCardOn : styles.toggleCardOff]}>
      <View style={styles.toggleLeft}>
        <View style={[styles.statusDot, isOn ? styles.statusDotOn : styles.statusDotOff]} />
        <View style={styles.toggleInfo}>
          <Text style={styles.toggleLabel}>AI Receptionist</Text>
          <Text style={[styles.toggleSub, isOn ? styles.toggleSubOn : styles.toggleSubOff]}>
            {isOn ? 'Active — answering calls' : 'Off — not answering calls'}
          </Text>
        </View>
      </View>
      <Switch
        value={isOn}
        onValueChange={(v) => mutate(v)}
        disabled={isPending}
        trackColor={{ false: '#E2E8F0', true: '#86EFAC' }}
        thumbColor={isOn ? '#16A34A' : '#94A3B8'}
      />
    </View>
  );
}

export default function DashboardScreen() {
  const { user, activeLocationId } = useAuthStore();

  const { data: stats, isLoading: statsLoading, refetch: refetchStats } = useQuery({
    queryKey: ['appt-stats', activeLocationId],
    queryFn: () => api.getAppointmentStats(activeLocationId),
  });

  const { data: locationsData, refetch: refetchLocations } = useQuery({
    queryKey: ['locations'],
    queryFn: () => api.getLocations(),
  });

  const allLocations = locationsData?.items ?? [];
  const visibleLocations = activeLocationId
    ? allLocations.filter((o) => o.id === activeLocationId)
    : allLocations;

  async function onRefresh() {
    await Promise.all([refetchStats(), refetchLocations()]);
  }

  return (
    <ScrollView
      style={styles.container}
      contentContainerStyle={styles.content}
      refreshControl={<RefreshControl refreshing={false} onRefresh={onRefresh} />}
    >
      <View style={styles.header}>
        <Text style={styles.heading}>Dashboard</Text>
        <Text style={styles.tenant}>{user?.tenantName}</Text>
      </View>

      {visibleLocations.map((o) => (
        <VapiToggle key={o.id} location={o} />
      ))}

      {statsLoading ? (
        <ActivityIndicator style={{ marginTop: 32 }} color="#2563EB" />
      ) : (
        <View style={styles.statsGrid}>
          <StatCard label="Appts Today" value={stats?.today ?? 0} />
          <StatCard label="Appts This Week" value={stats?.thisWeek ?? 0} />
        </View>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F8FAFC' },
  content: { padding: 20 },
  header: { marginBottom: 20 },
  heading: { fontSize: 26, fontWeight: '700', color: '#1E293B' },
  tenant: { fontSize: 13, color: '#64748B', marginTop: 2 },
  toggleCard: {
    borderRadius: 14,
    padding: 18,
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 24,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 2 },
    shadowOpacity: 0.08,
    shadowRadius: 6,
    elevation: 3,
  },
  toggleCardOn: { backgroundColor: '#F0FDF4', borderWidth: 1, borderColor: '#86EFAC' },
  toggleCardOff: { backgroundColor: '#FFF5F5', borderWidth: 1, borderColor: '#FCA5A5' },
  toggleLeft: { flex: 1, flexDirection: 'row', alignItems: 'center', gap: 12 },
  statusDot: { width: 10, height: 10, borderRadius: 5 },
  statusDotOn: { backgroundColor: '#16A34A' },
  statusDotOff: { backgroundColor: '#EF4444' },
  toggleInfo: { flex: 1 },
  toggleLabel: { fontSize: 15, fontWeight: '700', color: '#1E293B' },
  toggleSub: { fontSize: 12, marginTop: 2 },
  toggleSubOn: { color: '#16A34A' },
  toggleSubOff: { color: '#EF4444' },
  statsGrid: { flexDirection: 'row', gap: 12 },
  statCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 10,
    padding: 16,
    flex: 1,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
  },
  statValue: { fontSize: 32, fontWeight: '700', color: '#2563EB' },
  statLabel: { fontSize: 12, color: '#64748B', marginTop: 4 },
});
