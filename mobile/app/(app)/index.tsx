import {
  View, Text, ScrollView, StyleSheet,
  ActivityIndicator, RefreshControl, Switch,
} from 'react-native';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { api } from '../../src/api/client';
import { useAuthStore } from '../../src/store/auth';
import type { Appointment, Location } from '../../src/types/api';

function StatCard({ label, value }: { label: string; value: number }) {
  return (
    <View style={styles.statCard}>
      <Text style={styles.statValue}>{value}</Text>
      <Text style={styles.statLabel}>{label}</Text>
    </View>
  );
}

function AppointmentRow({ appt }: { appt: Appointment }) {
  const start = new Date(appt.startTime).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  });
  const firstLine = appt.notes?.split('\n')[0] ?? 'No details';
  const subLines = appt.notes?.split('\n').slice(1, 3).join(' · ') ?? '';
  return (
    <View style={styles.row}>
      <View style={styles.rowMain}>
        <Text style={styles.rowTitle}>{firstLine}</Text>
        {!!subLines && <Text style={styles.rowSub} numberOfLines={1}>{subLines}</Text>}
      </View>
      <View style={styles.rowRight}>
        <Text style={styles.rowMeta}>{start}</Text>
        <Text style={[styles.badge, appt.status === 'Scheduled' && styles.badgeActive]}>
          {appt.status}
        </Text>
      </View>
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

  return (
    <View style={styles.toggleCard}>
      <View style={styles.toggleInfo}>
        <Text style={styles.toggleLabel}>AI Receptionist</Text>
        <Text style={styles.toggleSub}>
          {location.vapiEnabled ? 'Active — taking calls' : 'Off — not taking calls'}
        </Text>
      </View>
      <Switch
        value={location.vapiEnabled}
        onValueChange={(v) => mutate(v)}
        disabled={isPending}
        trackColor={{ false: '#E2E8F0', true: '#BFDBFE' }}
        thumbColor={location.vapiEnabled ? '#2563EB' : '#94A3B8'}
      />
    </View>
  );
}

export default function DashboardScreen() {
  const { user, activeLocationId } = useAuthStore();
  const queryClient = useQueryClient();

  const { data: stats, isLoading: statsLoading, refetch: refetchStats } = useQuery({
    queryKey: ['appt-stats', activeLocationId],
    queryFn: () => api.getAppointmentStats(activeLocationId),
  });

  const { data: appts, isLoading: apptsLoading, refetch: refetchAppts } = useQuery({
    queryKey: ['appointments', activeLocationId, 1],
    queryFn: () => api.getAppointments(activeLocationId),
  });

  const { data: locationsData } = useQuery({
    queryKey: ['locations'],
    queryFn: () => api.getLocations(),
  });

  const activeLocation = locationsData?.items.find((o) => o.id === activeLocationId) ?? null;
  const isLoading = statsLoading || apptsLoading;

  async function onRefresh() {
    await Promise.all([refetchStats(), refetchAppts()]);
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

      {activeLocation ? (
        <VapiToggle location={activeLocation} />
      ) : (
        <View style={styles.noLocationHint}>
          <Text style={styles.noLocationText}>
            Select a location in the Locations tab to control the AI receptionist.
          </Text>
        </View>
      )}

      {isLoading ? (
        <ActivityIndicator style={{ marginTop: 32 }} color="#2563EB" />
      ) : (
        <>
          <View style={styles.statsGrid}>
            <StatCard label="Appts Today" value={stats?.today ?? 0} />
            <StatCard label="Appts This Week" value={stats?.thisWeek ?? 0} />
          </View>

          <Text style={styles.sectionTitle}>Recent Appointments</Text>
          {appts?.items.length === 0 && (
            <Text style={styles.empty}>No appointments yet.</Text>
          )}
          {appts?.items.slice(0, 5).map((appt) => (
            <AppointmentRow key={appt.id} appt={appt} />
          ))}
        </>
      )}
    </ScrollView>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F8FAFC' },
  content: { padding: 20 },
  header: { marginBottom: 16 },
  heading: { fontSize: 26, fontWeight: '700', color: '#1E293B' },
  tenant: { fontSize: 13, color: '#64748B', marginTop: 2 },
  toggleCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 16,
    flexDirection: 'row',
    alignItems: 'center',
    marginBottom: 20,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
  },
  toggleInfo: { flex: 1 },
  toggleLabel: { fontSize: 15, fontWeight: '600', color: '#1E293B' },
  toggleSub: { fontSize: 12, color: '#64748B', marginTop: 2 },
  noLocationHint: {
    backgroundColor: '#F1F5F9',
    borderRadius: 10,
    padding: 14,
    marginBottom: 20,
  },
  noLocationText: { fontSize: 13, color: '#64748B', textAlign: 'center' },
  statsGrid: { flexDirection: 'row', gap: 12, marginBottom: 28 },
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
  sectionTitle: { fontSize: 16, fontWeight: '600', color: '#1E293B', marginBottom: 12 },
  empty: { color: '#94A3B8', textAlign: 'center', marginTop: 20 },
  row: {
    backgroundColor: '#FFFFFF',
    borderRadius: 10,
    padding: 14,
    flexDirection: 'row',
    marginBottom: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.04,
    shadowRadius: 3,
    elevation: 1,
  },
  rowMain: { flex: 1, marginRight: 12 },
  rowTitle: { fontSize: 14, fontWeight: '600', color: '#1E293B' },
  rowSub: { fontSize: 12, color: '#64748B', marginTop: 2 },
  rowRight: { alignItems: 'flex-end' },
  rowMeta: { fontSize: 11, color: '#94A3B8' },
  badge: {
    fontSize: 10,
    color: '#64748B',
    backgroundColor: '#F1F5F9',
    borderRadius: 4,
    paddingHorizontal: 5,
    paddingVertical: 2,
    marginTop: 4,
    overflow: 'hidden',
  },
  badgeActive: { color: '#2563EB', backgroundColor: '#DBEAFE' },
});
