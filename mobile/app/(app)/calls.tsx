import { View, Text, FlatList, StyleSheet, ActivityIndicator } from 'react-native';
import { useQuery } from '@tanstack/react-query';
import { api } from '../../src/api/client';
import { useAuthStore } from '../../src/store/auth';
import type { CallLog } from '../../src/types/api';

function CallRow({ call }: { call: CallLog }) {
  const started = new Date(call.startedAt).toLocaleString(undefined, {
    month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit',
  });

  const durationMs = call.endedAt
    ? new Date(call.endedAt).getTime() - new Date(call.startedAt).getTime()
    : null;
  const duration = durationMs != null
    ? `${Math.floor(durationMs / 60000)}m ${Math.floor((durationMs % 60000) / 1000)}s`
    : null;

  const isInbound = call.direction === 'Inbound';

  return (
    <View style={styles.card}>
      <View style={styles.cardHeader}>
        <View style={[styles.directionBadge, isInbound ? styles.inbound : styles.outbound]}>
          <Text style={[styles.directionText, isInbound ? styles.inboundText : styles.outboundText]}>
            {call.direction}
          </Text>
        </View>
        <Text style={styles.time}>{started}</Text>
      </View>
      {!!call.summary && (
        <Text style={styles.summary} numberOfLines={2}>{call.summary}</Text>
      )}
      <View style={styles.cardFooter}>
        <Text style={styles.meta}>{call.status}</Text>
        {!!duration && <Text style={styles.meta}>{duration}</Text>}
      </View>
    </View>
  );
}

export default function CallsScreen() {
  const { activeLocationId } = useAuthStore();

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['call-logs', activeLocationId],
    queryFn: () => api.getCallLogs(activeLocationId),
  });

  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.heading}>Call Logs</Text>
      </View>
      {isLoading ? (
        <ActivityIndicator style={{ marginTop: 40 }} color="#2563EB" />
      ) : (
        <FlatList
          data={data?.items}
          keyExtractor={(o) => o.id}
          renderItem={({ item }) => <CallRow call={item} />}
          contentContainerStyle={styles.list}
          onRefresh={refetch}
          refreshing={false}
          ListEmptyComponent={<Text style={styles.empty}>No calls yet.</Text>}
        />
      )}
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F8FAFC' },
  header: { padding: 20, paddingBottom: 12 },
  heading: { fontSize: 26, fontWeight: '700', color: '#1E293B' },
  list: { paddingHorizontal: 20, paddingBottom: 20 },
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 14,
    marginBottom: 10,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.05,
    shadowRadius: 4,
    elevation: 2,
  },
  cardHeader: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 },
  directionBadge: { paddingHorizontal: 8, paddingVertical: 3, borderRadius: 6 },
  inbound: { backgroundColor: '#DBEAFE' },
  outbound: { backgroundColor: '#F1F5F9' },
  directionText: { fontSize: 11, fontWeight: '600' },
  inboundText: { color: '#2563EB' },
  outboundText: { color: '#64748B' },
  time: { fontSize: 12, color: '#94A3B8' },
  summary: { fontSize: 13, color: '#475569', marginBottom: 8, lineHeight: 18 },
  cardFooter: { flexDirection: 'row', justifyContent: 'space-between' },
  meta: { fontSize: 11, color: '#94A3B8' },
  empty: { color: '#94A3B8', textAlign: 'center', marginTop: 60 },
});
