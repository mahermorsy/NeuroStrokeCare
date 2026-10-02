// Weight-based thrombolytic dosing for acute ischemic stroke. The clinician
// picks the drug actually stocked/ordered at the moment of calculation —
// the two protocols are clinically different and must not be conflated:
//
//  Alteplase (Actilyse): 0.9 mg/kg, capped at 90 mg total — 10% given as an
//  IV bolus over 1 minute, the remaining 90% as an infusion over 60 minutes.
//
//  Tenecteplase (Metalyse): 0.25 mg/kg, capped at 25 mg total — given as a
//  single IV bolus over 5-10 seconds, no infusion phase.
//
// This is a calculator, not a prescription: every result must still be
// checked by the treating clinician against the patient's actual chart
// before administration.

export type ThrombolyticDrug = 'Alteplase' | 'Tenecteplase'

export interface ThrombolyticDose {
  drug: ThrombolyticDrug
  totalDoseMg: number
  cappedByMax: boolean
  bolusMg: number
  /** null for Tenecteplase — single bolus, no infusion phase */
  infusionMg: number | null
  /** null for Tenecteplase */
  infusionRateMgPerHour: number | null
  administration: string
}

const PROTOCOLS: Record<ThrombolyticDrug, { mgPerKg: number; maxTotalMg: number; bolusFraction: number }> = {
  Alteplase: { mgPerKg: 0.9, maxTotalMg: 90, bolusFraction: 0.1 },
  Tenecteplase: { mgPerKg: 0.25, maxTotalMg: 25, bolusFraction: 1 },
}

export function calculateThrombolyticDose(weightKg: number, drug: ThrombolyticDrug): ThrombolyticDose {
  const { mgPerKg, maxTotalMg, bolusFraction } = PROTOCOLS[drug]
  const rawDose = weightKg * mgPerKg
  const totalDoseMg = Math.min(rawDose, maxTotalMg)
  const bolusMg = totalDoseMg * bolusFraction
  const infusionMg = totalDoseMg - bolusMg

  if (drug === 'Tenecteplase') {
    return {
      drug,
      totalDoseMg: round1(totalDoseMg),
      cappedByMax: rawDose > maxTotalMg,
      bolusMg: round1(totalDoseMg),
      infusionMg: null,
      infusionRateMgPerHour: null,
      administration: 'Single IV bolus over 5–10 seconds. No infusion phase.',
    }
  }

  return {
    drug,
    totalDoseMg: round1(totalDoseMg),
    cappedByMax: rawDose > maxTotalMg,
    bolusMg: round1(bolusMg),
    infusionMg: round1(infusionMg),
    infusionRateMgPerHour: round1(infusionMg), // 90% infused over 60 min == mg/hour
    administration: '10% as an IV bolus over 1 minute, then the remaining 90% as an infusion over 60 minutes.',
  }
}

function round1(n: number) {
  return Math.round(n * 10) / 10
}

// Weight-based status epilepticus dosing. Mirrors the thrombolytic calculator
// above — this is a bedside reference, not a prescription; every result must
// still be checked by the treating clinician before administration.
//
//  Lorazepam (first-line benzodiazepine): 0.1 mg/kg, capped at 4 mg — IV push
//  over 2 minutes. May repeat once after 5–10 minutes if seizures continue.
//
//  Phenytoin (second-line, after a benzodiazepine): 20 mg/kg loading dose,
//  capped at 1500 mg — IV infusion, max rate 50 mg/min (slow to 25 mg/min in
//  the elderly or in cardiac disease). Requires cardiac/BP monitoring during
//  the infusion.

export type SeizureDrug = 'Lorazepam' | 'Phenytoin'

export interface SeizureDose {
  drug: SeizureDrug
  totalDoseMg: number
  cappedByMax: boolean
  maxRateNote: string
  administration: string
  repeatNote: string | null
}

const SEIZURE_PROTOCOLS: Record<SeizureDrug, { mgPerKg: number; maxTotalMg: number }> = {
  Lorazepam: { mgPerKg: 0.1, maxTotalMg: 4 },
  Phenytoin: { mgPerKg: 20, maxTotalMg: 1500 },
}

export function calculateSeizureDose(weightKg: number, drug: SeizureDrug): SeizureDose {
  const { mgPerKg, maxTotalMg } = SEIZURE_PROTOCOLS[drug]
  const rawDose = weightKg * mgPerKg
  const totalDoseMg = Math.min(rawDose, maxTotalMg)
  const cappedByMax = rawDose > maxTotalMg

  if (drug === 'Lorazepam') {
    return {
      drug,
      totalDoseMg: round1(totalDoseMg),
      cappedByMax,
      maxRateNote: 'Max rate 2 mg/min',
      administration: 'IV push over 2 minutes.',
      repeatNote: 'May repeat once after 5–10 minutes if seizures continue.',
    }
  }

  return {
    drug,
    totalDoseMg: round1(totalDoseMg),
    cappedByMax,
    maxRateNote: 'Max infusion rate 50 mg/min (use 25 mg/min in elderly or cardiac disease)',
    administration: 'IV infusion, diluted in saline. Monitor ECG and blood pressure throughout the infusion.',
    repeatNote: null,
  }
}
