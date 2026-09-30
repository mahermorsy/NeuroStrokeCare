import { entityApi } from '@/lib/entityApi'
import type { FollowUpNoteResponse } from '@/types/entities'

// Standard entity CRUD shape (GET/POST/status), same factory every other
// page uses — FollowUpNoteController follows the exact same REST pattern.
export const followUpNotesApi = entityApi<FollowUpNoteResponse>('FollowUpNote')
