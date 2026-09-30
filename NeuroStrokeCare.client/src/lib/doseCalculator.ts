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
