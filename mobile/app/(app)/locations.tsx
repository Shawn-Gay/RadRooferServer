import { View, Text, FlatList, TouchableOpacity, StyleSheet, ActivityIndicator } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { useQuery } from '@tanstack/react-query';
import { api } from '../../src/api/client';
import { useAuthStore } from '../../src/store/auth';
import type { Location } from '../../src/types/api';

function LocationRow({
  location,
  isSelected,
  onSelect,
}: {
  location: Location;
  isSelected: boolean;
  onSelect: () => void;
}) {
  return (
    <TouchableOpacity style={[styles.row, isSelected && styles.rowActive]} onPress={onSelect}>
      <View style={styles.rowMain}>
        <Text style={[styles.name, isSelected && styles.nameActive]}>{location.name}</Text>
        {!location.isActive && <Text style={styles.inactive}>Inactive</Text>}
      </View>
      {isSelected && <Ionicons name="checkmark-circle" size={22} color="#2563EB" />}
    </TouchableOpacity>
  );
}

export default function LocationsScreen() {
  const { activeLocationId, setActiveLocation } = useAuthStore();

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['locations'],
    queryFn: () => api.getLocations(),
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
        <Text style={styles.heading}>Locations</Text>
        <Text style={styles.sub}>Tap to filter dashboard by location</Text>
      </View>
      <FlatList
        data={data?.items}
        keyExtractor={(o) => o.id}
        renderItem={({ item }) => (
          <LocationRow
            location={item}
            isSelected={activeLocationId === item.id}
            onSelect={() => setActiveLocation(item.id)}
          />
        )}
        contentContainerStyle={styles.list}
        onRefresh={refetch}
        refreshing={false}
        ListHeaderComponent={
          activeLocationId ? (
            <TouchableOpacity style={styles.clearButton} onPress={() => setActiveLocation('')}>
              <Text style={styles.clearText}>Show all locations</Text>
            </TouchableOpacity>
          ) : null
        }
        ListEmptyComponent={<Text style={styles.empty}>No locations found.</Text>}
      />
    </View>
  );
}

const styles = StyleSheet.create({
  container: { flex: 1, backgroundColor: '#F8FAFC' },
  centered: { flex: 1, justifyContent: 'center', alignItems: 'center' },
  header: { padding: 20, paddingBottom: 12 },
  heading: { fontSize: 26, fontWeight: '700', color: '#1E293B' },
  sub: { fontSize: 13, color: '#64748B', marginTop: 2 },
  list: { paddingHorizontal: 20, paddingBottom: 20 },
  clearButton: { paddingVertical: 10, marginBottom: 8 },
  clearText: { fontSize: 13, color: '#2563EB' },
  row: {
    backgroundColor: '#FFFFFF',
    borderRadius: 10,
    padding: 16,
    marginBottom: 8,
    flexDirection: 'row',
    alignItems: 'center',
    borderWidth: 1.5,
    borderColor: 'transparent',
    shadowColor: '#000',
    shadowOffset: { width: 0, height: 1 },
    shadowOpacity: 0.04,
    shadowRadius: 3,
    elevation: 1,
  },
  rowActive: { borderColor: '#2563EB', backgroundColor: '#EFF6FF' },
  rowMain: { flex: 1 },
  name: { fontSize: 15, fontWeight: '600', color: '#1E293B' },
  nameActive: { color: '#2563EB' },
  inactive: { fontSize: 11, color: '#94A3B8', marginTop: 2 },
  empty: { color: '#94A3B8', textAlign: 'center', marginTop: 40 },
});
