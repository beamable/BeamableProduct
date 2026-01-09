/**
 * ⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.
 * All manual edits will be lost when this file is regenerated.
 */

import type { Era } from './Era';
import type { IsoChronology } from './IsoChronology';

export type LocalDate = { 
  chronology: IsoChronology; 
  dayOfMonth: number; 
  dayOfWeek: "SATURDAY" | "MONDAY" | "THURSDAY" | "TUESDAY" | "FRIDAY" | "WEDNESDAY" | "SUNDAY"; 
  dayOfYear: number; 
  era: Era; 
  leapYear: boolean; 
  month: "DECEMBER" | "APRIL" | "JULY" | "SEPTEMBER" | "JUNE" | "FEBRUARY" | "OCTOBER" | "AUGUST" | "NOVEMBER" | "MARCH" | "MAY" | "JANUARY"; 
  monthValue: number; 
  year: number; 
};
