import { useState } from 'react';
import { View, Text, FlatList, StyleSheet, TouchableOpacity, Alert, TextInput } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useMutation } from '@tanstack/react-query';
import { api } from '../../src/api/client';
import { useAuthStore } from '../../src/store/auth';

type Lead = { id: string; name: string; phone: string; note: string };

const LEADS: Lead[] = [
  { id: '1', name: 'Mike Johnson',  phone: '+18155498540', note: 'Storm damage — needs roof inspection' },
  { id: '2', name: 'Sarah Williams', phone: '+18155498540', note: 'Full replacement quote requested' },
];

function QuickCallForm() {
  const { activeLocationId } = useAuthStore();
  const [phone, setPhone] = useState('');

  const { mutate, isPending } = useMutation({
    mutationFn: () => api.initiateOutboundCall(phone.trim(), activeLocationId),
    onSuccess: () => {
      Alert.alert('Call Scheduled', `The assistant will call ${phone.trim()}.`);
      setPhone('');
    },
    onError: (err: Error) => Alert.alert('Failed', err.message),
  });

  const canSubmit = phone.trim().length >= 7 && !isPending;

  return (
    <View style={styles.quickCard}>
      <Text style={styles.quickLabel}>Quick call</Text>
      <View style={styles.quickRow}>
        <TextInput
          style={styles.quickInput}
          placeholder="Phone number"
          placeholderTextColor="#94A3B8"
          keyboardType="phone-pad"
          value={phone}
          onChangeText={setPhone}
          returnKeyType="done"
        />
        <TouchableOpacity
          style={[styles.scheduleBtn, !canSubmit && styles.scheduleBtnDisabled]}
          onPress={() => mutate()}
          disabled={!canSubmit}
          activeOpacity={0.75}
        >
          <Ionicons name="call-outline" size={18} color="#FFFFFF" />
          <Text style={styles.scheduleBtnText}>{isPending ? 'Calling…' : 'Call'}</Text>
        </TouchableOpacity>
      </View>
    </View>
  );
}

function LeadCard({ lead }: { lead: Lead }) {
  const { activeLocationId } = useAuthStore();

  const { mutate, isPending } = useMutation({
    mutationFn: () => api.initiateOutboundCall(lead.phone, activeLocationId),
    onSuccess: () =>
      Alert.alert('Call Scheduled', `The assistant will call ${lead.name} at ${lead.phone}.`),
    onError: (err: Error) =>
      Alert.alert('Failed', err.message),
  });

  return (
    <View style={styles.card}>
      <View style={styles.cardInfo}>
        <Text style={styles.name}>{lead.name}</Text>
        <Text style={styles.phone}>{lead.phone}</Text>
        <Text style={styles.note}>{lead.note}</Text>
      </View>
      <TouchableOpacity
        style={[styles.scheduleBtn, isPending && styles.scheduleBtnDisabled]}
        onPress={() => mutate()}
        disabled={isPending}
        activeOpacity={0.75}
      >
        <Ionicons name="call-outline" size={18} color="#FFFFFF" />
        <Text style={styles.scheduleBtnText}>{isPending ? 'Calling…' : 'Schedule'}</Text>
      </TouchableOpacity>
    </View>
  );
}

export default function OutboundScreen() {
  return (
    <View style={styles.container}>
      <View style={styles.header}>
        <Text style={styles.heading}>Outbound</Text>
        <Text style={styles.sub}>Schedule the assistant to call a lead</Text>
      </View>
      <View style={styles.quickWrap}>
        <QuickCallForm />
      </View>
      <FlatList
        data={LEADS}
        keyExtractor={(o) => o.id}
        renderItem={({ item }) => <LeadCard lead={item} />}
        contentContainerStyle={styles.list}
        ListEmptyComponent={<Text style={styles.empty}>No leads.</Text>}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F8FAFC' },
  header: { padding: 20, paddingBottom: 12 },
  heading: { fontSize: 26, fontWeight: '700', color: '#1E293B' },
  sub: { fontSize: 13, color: '#64748B', marginTop: 2 },
  quickWrap: { paddingHorizontal: 20, paddingBottom: 8 },
  quickCard: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 16,
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06,
    shadowRadius: 4,
    elevation: 2,
  },
  quickLabel: { fontSize: 12, fontWeight: '600', color: '#64748B', marginBottom: 10, textTransform: 'uppercase', letterSpacing: 0.5 },
  quickRow: { flexDirection: 'row', alignItems: 'center', gap: 10 },
  quickInput: {
    flex: 1,
    height: 44,
    borderWidth: 1,
    borderColor: '#E2E8F0',
    borderRadius: 10,
    paddingHorizontal: 12,
    fontSize: 15,
    color: '#1E293B',
    backgroundColor: '#F8FAFC',
  },
  list: { paddingHorizontal: 20, paddingBottom: 20 },
  card: {
    backgroundColor: '#FFFFFF',
    borderRadius: 12,
    padding: 16,
    marginBottom: 12,
    flexDirection: 'row',
    alignItems: 'center',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.06,
    shadowRadius: 4,
    elevation: 2,
  },
  cardInfo: { flex: 1, marginRight: 12 },
  name: { fontSize: 15, fontWeight: '700', color: '#1E293B' },
  phone: { fontSize: 13, color: '#2563EB', marginTop: 2 },
  note: { fontSize: 12, color: '#64748B', marginTop: 4 },
  scheduleBtn: {
    backgroundColor: '#2563EB',
    borderRadius: 10,
    paddingVertical: 10,
    paddingHorizontal: 14,
    flexDirection: 'row',
    alignItems: 'center',
    gap: 6,
  },
  scheduleBtnDisabled: { backgroundColor: '#93C5FD' },
  scheduleBtnText: { color: '#FFFFFF', fontSize: 13, fontWeight: '600' },
  empty: { color: '#94A3B8', textAlign: 'center', marginTop: 60 },
});
