import { api } from '@/lib/api'

/**
 * Every controller in the API (Ward, Bed, Patient, Admission, the eight
 * assessment types, LabResults, DoorTiming) follows the exact same shape:
 *
 *   GET    /api/{Entity}                       -> T[]
 *   GET    /api/{Entity}/paged?pageNumber=&pageSize=&search=...
 *   GET    /api/{Entity}/{id}                  -> T
 *   POST   /api/{Entity}?actingUserId=         -> Guid (new id)
 *   PUT    /api/{Entity}?actingUserId=         -> 204
 *   PATCH  /api/{Entity}/{id}/status?actingUserId=&status= -> 204
 *   DELETE /api/{Entity}/{id}                  -> 204 (only Ward/Bed support this)
 *
 * This factory gives every page the same small client instead of hand-rolling
 * axios calls per entity.
 */
export function entityApi<T>(entityName: string) {
  const base = `/${entityName}`

  return {
    list: () => api.get<T[]>(base).then((r) => r.data),
    getById: (id: string) => api.get<T>(`${base}/${id}`).then((r) => r.data),
    create: (payload: unknown, actingUserId: string) =>
      api.post<string>(base, payload, { params: { actingUserId } }).then((r) => r.data),
    update: (payload: unknown, actingUserId: string) =>
      api.put<void>(base, payload, { params: { actingUserId } }),
    changeStatus: (id: string, status: number, actingUserId: string) =>
      api.patch<void>(`${base}/${id}/status`, null, { params: { actingUserId, status } }),
    remove: (id: string) => api.delete<void>(`${base}/${id}`),
  }
}
