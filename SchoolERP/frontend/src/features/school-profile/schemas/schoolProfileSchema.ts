import { z } from 'zod';

const optional = (max: number) => z.string().trim().max(max);
export const schoolProfileSchema = z.object({
  name: z.string().trim().min(1, 'School name is required.').max(200),
  shortName: optional(100), registrationNumber: optional(100), affiliationNumber: optional(100),
  boardCode: optional(100), boardType: z.string(),
  email: z.union([z.literal(''), z.email('Enter a valid email address.')]).transform(value => value.trim()),
  phone: optional(30), alternatePhone: optional(30),
  website: z.union([z.literal(''), z.url('Enter an absolute URL.').refine(value => /^https?:\/\//i.test(value), 'Only HTTP and HTTPS URLs are allowed.')]),
  addressLine1: optional(200), addressLine2: optional(200), city: optional(100), state: optional(100),
  postalCode: optional(20),
  countryCode: z.union([z.literal(''), z.string().trim().regex(/^[A-Za-z]{2}$/, 'Use a two-letter country code.')]),
  timeZoneId: optional(100),
  defaultCurrency: z.union([z.literal(''), z.string().trim().regex(/^[A-Za-z]{3}$/, 'Use a three-letter currency code.')]),
  principalName: optional(200)
});
export type SchoolProfileForm = z.input<typeof schoolProfileSchema>;
