import { useRef, useState } from 'react'
import { motion } from 'framer-motion'
import { useEntityList } from '@/hooks/useEntityList'
import { usersApi } from '@/lib/usersApi'
import { useAuth } from '@/context/AuthContext'
import PageHeader, { Card } from '@/components/PageHeader'
import { roleLabel } from '@/lib/roles'
import { describeApiError } from '@/lib/apiError'

// Phase F1 - Frontend Hardening (Section 13/9): the <input accept="..."> attribute is only a
// hint to the OS file picker's filter - it doesn't stop a user from selecting a different file
// type via "All files", a drag-and-drop, or a paste. Catching an obviously-wrong type or an
// oversized file here means instant feedback instead of a round trip to the backend (and a
// generic error) for something that could never have succeeded.
const ALLOWED_PHOTO_TYPES = ['image/jpeg', 'image/png', 'image/webp']
const MAX_PHOTO_BYTES = 5 * 1024 * 1024

export default function IdCard() {
  const { data: profile, loading, error, reload } = useEntityList(() =>
    usersApi.myProfile().then((p) => [p]),
  )
  const { refreshUser } = useAuth()
  const me = profile[0]
  const fileInputRef = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState(false)
  const [uploadError, setUploadError] = useState<string | null>(null)

  async function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0]
    if (!file) return
    setUploadError(null)

    if (!ALLOWED_PHOTO_TYPES.includes(file.type)) {
      setUploadError('That file type is not supported — please choose a JPG, PNG or WEBP image.')
      if (fileInputRef.current) fileInputRef.current.value = ''
      return
    }
    if (file.size > MAX_PHOTO_BYTES) {
      setUploadError('That photo is too large — please choose one under 5MB.')
      if (fileInputRef.current) fileInputRef.current.value = ''
      return
    }

    setUploading(true)
    try {
      await usersApi.uploadPhoto(file)
      reload()
      // Keeps this page's own view fresh AND tells AuthContext to refetch,
      // so the sidebar avatar picks up the new photo right away too.
      refreshUser()
    } catch (err) {
      setUploadError(
        describeApiError(err, {
          400: 'Could not upload this photo — try a JPG, PNG or WEBP under 5MB.',
        }),
      )
    } finally {
      setUploading(false)
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  return (
    <motion.div initial={{ opacity: 0, y: 10 }} animate={{ opacity: 1, y: 0 }} className="flex flex-col gap-6">
      <PageHeader title="My ID Card" subtitle="Your hospital credential — upload a photo to complete it" />

      {loading && <Card>Loading…</Card>}
      {error && <Card className="text-critical">{error}</Card>}

      {me && (
        <div className="flex flex-col items-center gap-6 sm:items-start">
          {/* The card itself — fixed badge proportions, navy body with a gold
              frame and foil-style corner, meant to read as an actual
              institutional credential rather than a settings panel. */}
          <div className="relative w-full max-w-[380px] overflow-hidden rounded-[18px] border border-gold/30 bg-sidebar p-6 shadow-[0_12px_30px_rgba(10,25,48,0.35)]">
            <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_top_right,rgba(179,136,44,0.18),transparent_55%)]" />
            <div className="relative flex items-center justify-between">
              <div className="flex items-center gap-2.5">
                <img
                  src="/brand/WindowIcon.svg"
                  alt="NeuroStrokeCare"
                  className="h-9 w-9 rounded-[9px] object-cover ring-1 ring-gold/50"
                />
                <div className="flex flex-col leading-tight">
                  <span className="font-display text-[14px] font-semibold text-white">NeuroStrokeCare</span>
                  <span className="text-[10px] text-sidebar-muted">Mansoura University Hospital</span>
                </div>
              </div>
              <span className="rounded-full border border-gold/50 px-2.5 py-1 text-[9.5px] font-bold uppercase tracking-wider text-gold">
                Staff ID
              </span>
            </div>

            <div className="relative mt-6 flex items-center gap-4">
              <div className="flex h-[76px] w-[76px] flex-shrink-0 items-center justify-center overflow-hidden rounded-full border-2 border-gold bg-white/10">
                {me.profilePhotoUrl ? (
                  <img src={me.profilePhotoUrl} alt="" className="h-full w-full object-cover" />
                ) : (
                  <span className="font-display text-[22px] font-semibold text-gold">
                    {me.firstName[0]}
                    {me.lastName[0]}
                  </span>
                )}
              </div>
              <div className="flex flex-col gap-0.5">
                <span className="font-display text-[19px] font-semibold text-white">
                  {me.firstName} {me.lastName}
                </span>
                <span className="text-[12.5px] font-medium text-gold">{roleLabel(me.role)}</span>
                <span className="text-[11.5px] text-sidebar-muted">{me.email}</span>
              </div>
            </div>

            <div className="relative mt-6 flex items-end justify-between border-t border-white/10 pt-3.5">
              <div className="flex flex-col">
                <span className="text-[9.5px] uppercase tracking-wider text-sidebar-muted">Employee ID</span>
                <span className="font-display text-[14px] font-semibold text-white">
                  {me.employeeId ?? 'Not assigned yet'}
                </span>
              </div>
              <div className="flex flex-col text-right">
                <span className="text-[9.5px] uppercase tracking-wider text-sidebar-muted">Member since</span>
                <span className="text-[12px] text-white">
                  {new Date(me.createdAt).toLocaleDateString(undefined, { year: 'numeric', month: 'short' })}
                </span>
              </div>
            </div>
          </div>

          <Card className="flex w-full max-w-[380px] flex-col gap-3">
            {!me.employeeId && (
              <p className="text-[12.5px] text-text-secondary">
                Your employee ID hasn't been assigned yet — ask an Admin to set it from the Staff page.
              </p>
            )}
            <input
              ref={fileInputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp"
              onChange={handleFileChange}
              className="hidden"
            />
            <button
              type="button"
              onClick={() => fileInputRef.current?.click()}
              disabled={uploading}
              className="rounded-lg bg-accent px-4 py-2.5 text-[13.5px] font-semibold text-white transition-opacity hover:opacity-90 disabled:opacity-60"
            >
              {uploading ? 'Uploading…' : me.profilePhotoUrl ? 'Change photo' : 'Upload a photo'}
            </button>
            {uploadError && <p className="text-[12.5px] font-medium text-critical">{uploadError}</p>}
          </Card>
        </div>
      )}
    </motion.div>
  )
}
