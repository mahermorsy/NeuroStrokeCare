import { useRef, useState } from 'react'
import { motion } from 'framer-motion'
import { useEntityList } from '@/hooks/useEntityList'
import { usersApi } from '@/lib/usersApi'
import { useAuth } from '@/context/AuthContext'
import PageHeader, { Card, PrimaryButton } from '@/components/PageHeader'
import { Field, TextInput } from '@/components/FormField'
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

  // ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS (area 4): self-service editing, limited to
  // the same non-privileged fields the backend's UpdateProfileRequest accepts (FirstName/
  // LastName/Email/PhoneNumber). EmployeeId/Profession/JobTitle/AcademicDegree/Department/
  // Role are shown read-only above and can only be changed by an Admin from the Staff page -
  // this form has no way to send them, so it cannot be used for privilege escalation.
  const [editingProfile, setEditingProfile] = useState(false)
  const [profileForm, setProfileForm] = useState({ firstName: '', lastName: '', email: '', phoneNumber: '' })
  const [profileSubmitting, setProfileSubmitting] = useState(false)
  const [profileError, setProfileError] = useState<string | null>(null)
  const [profileSuccess, setProfileSuccess] = useState(false)

  function openEditProfile() {
    if (!me) return
    setProfileForm({
      firstName: me.firstName,
      lastName: me.lastName,
      email: me.email,
      phoneNumber: me.phoneNumber ?? '',
    })
    setProfileError(null)
    setProfileSuccess(false)
    setEditingProfile(true)
  }

  async function handleProfileSubmit(e: React.FormEvent) {
    e.preventDefault()
    if (profileSubmitting) return
    setProfileSubmitting(true)
    setProfileError(null)
    try {
      const result = await usersApi.updateProfile({
        firstName: profileForm.firstName.trim(),
        lastName: profileForm.lastName.trim(),
        email: profileForm.email.trim(),
        phoneNumber: profileForm.phoneNumber.trim() || undefined,
      })
      if (!result.success) {
        setProfileError(result.errors?.join(' ') || result.message || 'Could not save your profile — try again.')
        return
      }
      setEditingProfile(false)
      setProfileSuccess(true)
      reload()
      refreshUser()
    } catch (err) {
      setProfileError(
        describeApiError(err, { 400: 'Could not save your profile — check the fields and try again.' }),
      )
    } finally {
      setProfileSubmitting(false)
    }
  }

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

            {/* ADDITIONAL CLINICAL UX & STAFF PROFILE TASKS (area 3): Profession / Job
                title / Academic degree / Department are all optional - a field is only
                rendered when the Admin has actually set it, so an unfilled profile still
                reads as a clean card instead of a row of blank labels. */}
            {(me.profession || me.jobTitle || me.academicDegree || me.department) && (
              <div className="relative mt-5 flex flex-wrap gap-x-5 gap-y-2 border-t border-white/10 pt-3.5">
                {me.profession && (
                  <div className="flex flex-col">
                    <span className="text-[9.5px] uppercase tracking-wider text-sidebar-muted">Profession</span>
                    <span className="text-[12.5px] font-medium text-white">{me.profession}</span>
                  </div>
                )}
                {me.jobTitle && (
                  <div className="flex flex-col">
                    <span className="text-[9.5px] uppercase tracking-wider text-sidebar-muted">Job title</span>
                    <span className="text-[12.5px] font-medium text-white">{me.jobTitle}</span>
                  </div>
                )}
                {me.academicDegree && (
                  <div className="flex flex-col">
                    <span className="text-[9.5px] uppercase tracking-wider text-sidebar-muted">Degree</span>
                    <span className="text-[12.5px] font-medium text-white">{me.academicDegree}</span>
                  </div>
                )}
                {me.department && (
                  <div className="flex flex-col">
                    <span className="text-[9.5px] uppercase tracking-wider text-sidebar-muted">Department</span>
                    <span className="text-[12.5px] font-medium text-white">{me.department}</span>
                  </div>
                )}
              </div>
            )}

            <div className="relative mt-5 flex items-end justify-between border-t border-white/10 pt-3.5">
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

          <Card className="flex w-full max-w-[380px] flex-col gap-3">
            <div className="flex items-center justify-between">
              <h2 className="text-[13.5px] font-semibold text-text">My profile</h2>
              {!editingProfile && (
                <button
                  type="button"
                  onClick={openEditProfile}
                  className="rounded-lg border border-border-subtle px-2.5 py-1 text-[12.5px] font-medium text-accent hover:bg-accent/10"
                >
                  Edit
                </button>
              )}
            </div>

            {!editingProfile ? (
              <div className="flex flex-col gap-1 text-[13px] text-text-secondary">
                <span>
                  {me.firstName} {me.lastName}
                </span>
                <span>{me.email}</span>
                <span>{me.phoneNumber || 'No phone number on file'}</span>
                {profileSuccess && (
                  <p className="mt-1 rounded-lg bg-success-bg px-3 py-2 text-[12.5px] font-medium text-success">
                    Profile updated.
                  </p>
                )}
              </div>
            ) : (
              <form onSubmit={handleProfileSubmit} className="flex flex-col gap-3">
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                  <Field label="First name">
                    <TextInput
                      required
                      value={profileForm.firstName}
                      onChange={(e) => setProfileForm({ ...profileForm, firstName: e.target.value })}
                    />
                  </Field>
                  <Field label="Last name">
                    <TextInput
                      required
                      value={profileForm.lastName}
                      onChange={(e) => setProfileForm({ ...profileForm, lastName: e.target.value })}
                    />
                  </Field>
                </div>
                <Field label="Email">
                  <TextInput
                    type="email"
                    required
                    value={profileForm.email}
                    onChange={(e) => setProfileForm({ ...profileForm, email: e.target.value })}
                  />
                </Field>
                <Field label="Phone (optional)">
                  <TextInput
                    value={profileForm.phoneNumber}
                    onChange={(e) => setProfileForm({ ...profileForm, phoneNumber: e.target.value })}
                  />
                </Field>
                {profileError && (
                  <p className="rounded-lg bg-critical-bg px-3 py-2 text-[12.5px] font-medium text-critical">
                    {profileError}
                  </p>
                )}
                <div className="flex gap-2">
                  <PrimaryButton type="submit" disabled={profileSubmitting}>
                    {profileSubmitting ? 'Saving…' : 'Save changes'}
                  </PrimaryButton>
                  <button
                    type="button"
                    onClick={() => setEditingProfile(false)}
                    disabled={profileSubmitting}
                    className="rounded-lg border border-border-subtle px-3.5 py-2 text-[13px] font-medium text-text-secondary hover:bg-border-soft disabled:opacity-60"
                  >
                    Cancel
                  </button>
                </div>
              </form>
            )}
          </Card>
        </div>
      )}
    </motion.div>
  )
}
