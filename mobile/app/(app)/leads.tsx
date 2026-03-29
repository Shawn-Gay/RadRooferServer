import { View, Text, FlatList, StyleSheet, ActivityIndicator } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { api } from '../../src/api/client';
import { useAuthStore } from '../../src/store/auth';
import type { Appointment } from '../../src/types/api';

function AppointmentRow({ appt }: { appt: Appointment }) {
  const start = new Date(appt.startTime).toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });

  // Notes format: "Name: X\nPhone: Y\nAddress: Z\nReason: W"
  const lines = appt.notes?.split('\n') ?? [];
  const title = lines[0] ?? 'Unknown caller';
  const details = lines.slice(1).filter(Boolean);

  return (
    <View style={styles.row}>
      <View style={styles.rowHeader}>
        <Text style={styles.title}>{title}</Text>
        <Text style={[styles.badge, appt.status === 'Scheduled' && styles.badgeActive]}>
          {appt.status}
        </Text>
      </View>
      {details.map((line, i) => (
        <Text key={i} style={styles.detail}>{line}</Text>
      ))}
      <Text style={styles.date}>{start}</Text>
    </View>
  );
}

export default function AppointmentsScreen() {
  const { activeLocationId } = useAuthStore();

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['appointments', activeLocationId],
    queryFn: () => api.getAppointments(activeLocationId),
  });

  if (isLoading) {
    return (
      <View style={styles.centered}>
        <ActivityIndicator color="#2563EB" />
      </View>
    );
  }

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.heading}>Appointments</Text>
        <Text style={styles.count}>{data?.totalCount ?? 0} total</Text>
      </View>
      <FlatList
        data={data?.items}
        keyExtractor={(o) => o.id}
        renderItem={({ item }) => <AppointmentRow appt={item} />}
        contentContainerStyle={styles.list}
        onRefresh={refetch}
        refreshing={false}
        ListEmptyComponent={<Text style={styles.empty}>No appointments yet.</Text>}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F8FAFC' },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header: {
    flexDirection: 'row',
    justifyContent: 'space-between',
    alignItems: 'baseline',
    padding: 20,
    paddingBottom: 12,
  },
  heading: { fontSize: 26, fontWeight: '700', color: '#1E293B' },
  count: { fontSize: 13, color: '#64748B' },
  list: { paddingHorizontal: 20, paddingBottom: 20 },
  row: {
    backgroundColor: '#FFFFFF',
    borderRadius: 10,
    padding: 14,
    marginBottom: 8,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.04,
    shadowRadius: 3,
    elevation: 1,
  },
  rowHeader: { flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4 },
  title: { fontSize: 14, fontWeight: '600', color: '#1E293B', flex: 1, marginRight: 8 },
  badge: {
    fontSize: 11,
    color: '#64748B',
    backgroundColor: '#F1F5F9',
    borderRadius: 4,
    paddingHorizontal: 6,
    paddingVertical: 2,
    overflow: 'hidden',
  },
  badgeActive: { color: '#2563EB', backgroundColor: '#DBEAFE' },
  detail: { fontSize: 12, color: '#475569', marginTop: 2 },
  date: { fontSize: 11, color: '#94A3B8', marginTop: 6 },
  empty: { color: '#94A3B8', textAlign: 'center', marginTop: 40 },
});
