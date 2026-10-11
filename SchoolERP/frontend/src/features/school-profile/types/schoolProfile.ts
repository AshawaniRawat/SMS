export type SchoolStatus = 1 | 2 | 3 | 4;
export type SchoolBoardType = 1 | 2 | 3 | 4 | 5 | 99;

export interface SchoolProfile {
  id: string; name: string; code: string; shortName: string | null;
  registrationNumber: string | null; affiliationNumber: string | null;
  boardCode: string | null; boardType: SchoolBoardType | null;
  email: string | null; phone: string | null; alternatePhone: string | null;
  website: string | null; addressLine1: string | null; addressLine2: string | null;
  city: string | null; state: string | null; postalCode: string | null;
  countryCode: string | null; logoUrl: string | null; timeZoneId: string | null;
  defaultCurrency: string | null; principalName: string | null;
  status: SchoolStatus; createdAt: string; updatedAt: string | null;
}

export type UpdateSchoolProfileRequest = Omit<SchoolProfile,
  'id' | 'code' | 'logoUrl' | 'status' | 'createdAt' | 'updatedAt'>;
